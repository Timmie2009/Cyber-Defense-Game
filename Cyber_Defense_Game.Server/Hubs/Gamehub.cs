using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cyber_Defense_Game.Server.Hubs;

public class QuizQuestion
{
    public string Title { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public int CorrectIndex { get; set; }
    public string Difficulty { get; set; } = "Normal";
}

public class LobbyRoom
{
    public string Code { get; init; } = string.Empty;
    public string HostConnectionId { get; set; } = string.Empty;
    public ConcurrentDictionary<string, string> Players { get; } = new();
}

public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, LobbyRoom> Lobbies = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionToLobby = new();
    private static readonly object FileLock = new();
    private static readonly string QuestionsFolder = Path.Combine(AppContext.BaseDirectory, "data", "questions");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static GameHub()
    {
        EnsureDefaultQuestion();
    }

    private static string LobbyGroup(string code) => $"lobby_{code}";
    private static string HostGroup(string code) => $"lobby_{code}_host";

    private static string GenerateUniqueCode()
    {
        for (int i = 0; i < 100; i++)
        {
            string code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            if (!Lobbies.ContainsKey(code))
                return code;
        }
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string GetSafeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(title.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Untitled" : clean;
    }

    private static void EnsureDefaultQuestion()
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(QuestionsFolder);
            if (!Directory.EnumerateFiles(QuestionsFolder, "*.json").Any())
            {
                SaveQuestionFile(new QuizQuestion
                {
                    Title = "Sky Color",
                    QuestionText = "What color is the sky?",
                    OptionA = "Red",
                    OptionB = "Green",
                    OptionC = "Blue",
                    OptionD = "White",
                    CorrectIndex = 2,
                    Difficulty = "Easy"
                });
            }
        }
    }

    private static void SaveQuestionFile(QuizQuestion question)
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(QuestionsFolder);
            var path = Path.Combine(QuestionsFolder, $"{GetSafeFileName(question.Title)}.json");
            var json = JsonSerializer.Serialize(question, JsonOptions);
            File.WriteAllText(path, json);
        }
    }

    public Task<List<QuizQuestion>> GetAllQuestions()
    {
        lock (FileLock)
        {
            Directory.CreateDirectory(QuestionsFolder);
            var list = new List<QuizQuestion>();
            var orderedFiles = Directory.EnumerateFiles(QuestionsFolder, "*.json")
                .OrderBy(File.GetCreationTimeUtc);

            foreach (var file in orderedFiles)
            {
                try
                {
                    var q = JsonSerializer.Deserialize<QuizQuestion>(File.ReadAllText(file));
                    if (q != null) list.Add(q);
                }
                catch { }
            }
            return Task.FromResult(list);
        }
    }

    public async Task SaveQuestion(QuizQuestion question)
    {
        SaveQuestionFile(question);
        await Clients.All.SendAsync("QuestionSaved", question);
    }

    public async Task DeleteQuestion(string questionName)
    {
        lock (FileLock)
        {
            var path = Path.Combine(QuestionsFolder, $"{GetSafeFileName(questionName)}.json");
            if (File.Exists(path))
                File.Delete(path);
        }
        await Clients.All.SendAsync("QuestionDeleted", questionName);
    }

    public async Task<string> RegisterAsHost()
    {
        await LeaveCurrentLobbyAsync();

        string code = GenerateUniqueCode();
        var room = new LobbyRoom
        {
            Code = code,
            HostConnectionId = Context.ConnectionId
        };

        Lobbies[code] = room;
        ConnectionToLobby[Context.ConnectionId] = code;

        await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup(code));
        await Groups.AddToGroupAsync(Context.ConnectionId, HostGroup(code));
        await Clients.Caller.SendAsync("PlayersUpdated", new List<string>());

        return code;
    }

    public async Task<bool> JoinLobby(string code, string name)
    {
        string cleanCode = (code ?? string.Empty).Trim();
        if (cleanCode.Length != 6 || !Lobbies.TryGetValue(cleanCode, out var room))
            return false;

        await LeaveCurrentLobbyAsync();

        string cleanName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
        room.Players[Context.ConnectionId] = cleanName;
        ConnectionToLobby[Context.ConnectionId] = cleanCode;

        await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup(cleanCode));
        await BroadcastPlayersAsync(room);
        return true;
    }

    public async Task LoadQuestion(string questionName)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("QuestionLoaded", questionName);
    }

    public async Task EndRound()
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("RoundEnded");
    }

    public async Task StartUpgradeVote(int option1Id, int option2Id)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("UpgradeVoteStarted", option1Id, option2Id);
    }

    public async Task SubmitUpgradeVote(int choiceIndex)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code) &&
            Lobbies.TryGetValue(code, out var room))
        {
            var name = room.Players.GetValueOrDefault(Context.ConnectionId, "Unknown");
            await Clients.Group(HostGroup(code)).SendAsync("UpgradeVoteReceived", Context.ConnectionId, name, choiceIndex);
        }
    }

    public async Task EliminateOption(int wrongIndex)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("OptionEliminated", wrongIndex);
    }

    public async Task ChangeImage(int imageNumber)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("ImageChanged", imageNumber);
    }

    public async Task SubmitAnswer(int answer)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code) &&
            Lobbies.TryGetValue(code, out var room))
        {
            var name = room.Players.GetValueOrDefault(Context.ConnectionId, "Unknown");
            await Clients.Group(HostGroup(code)).SendAsync("AnswerReceived", Context.ConnectionId, name, answer);
        }
    }

    public async Task StartGame()
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("GameStarted");
    }

    public async Task NextQuestion()
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("QuestionChanged");
    }

    public async Task TriggerGameOver(int finalScore)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("GameOver", finalScore);
    }

    public async Task TriggerVictory(int finalScore, int livesLeft)
    {
        if (ConnectionToLobby.TryGetValue(Context.ConnectionId, out var code))
            await Clients.Group(LobbyGroup(code)).SendAsync("Victory", finalScore, livesLeft);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentLobbyAsync();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveCurrentLobbyAsync()
    {
        if (!ConnectionToLobby.TryRemove(Context.ConnectionId, out var oldCode))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyGroup(oldCode));
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, HostGroup(oldCode));

        if (Lobbies.TryGetValue(oldCode, out var room))
        {
            if (room.HostConnectionId == Context.ConnectionId)
            {
                Lobbies.TryRemove(oldCode, out _);
            }
            else if (room.Players.TryRemove(Context.ConnectionId, out _))
            {
                await BroadcastPlayersAsync(room);
            }
        }
    }

    private Task BroadcastPlayersAsync(LobbyRoom room) =>
        Clients.Group(LobbyGroup(room.Code)).SendAsync("PlayersUpdated", room.Players.Values.ToList());
}
