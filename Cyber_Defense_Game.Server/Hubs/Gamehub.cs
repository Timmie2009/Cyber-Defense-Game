using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
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

public class GameHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> Players = new();
    private static readonly object FileLock = new();
    private static readonly string QuestionsFolder = Path.Combine(AppContext.BaseDirectory, "data", "questions");

    static GameHub()
    {
        EnsureDefaultQuestion();
    }

    private static string GetSafeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(title.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Untitled" : clean;
    }
    public async Task DeleteQuestion(string questionName)
    {
        lock (FileLock)
        {
            var path = Path.Combine(QuestionsFolder, $"{GetSafeFileName(questionName)}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        await Clients.All.SendAsync("QuestionDeleted", questionName);
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
            var json = JsonSerializer.Serialize(question, new JsonSerializerOptions { WriteIndented = true });
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

    public async Task LoadQuestion(string questionName)
    {
        await Clients.All.SendAsync("QuestionLoaded", questionName);
    }

    public async Task JoinLobby(string name)
    {
        Players[Context.ConnectionId] = name;
        await BroadcastPlayers();
    }

    public async Task ChangeImage(int imageNumber) =>
        await Clients.All.SendAsync("ImageChanged", imageNumber);

    public async Task SubmitAnswer(int answer)
    {
        var name = Players.GetValueOrDefault(Context.ConnectionId, "Unknown");
        await Clients.Group("hosts").SendAsync("AnswerReceived", Context.ConnectionId, name, answer);
    }

    public async Task RegisterAsHost()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "hosts");
        await Clients.Caller.SendAsync("PlayersUpdated", GetPlayerList());
    }

    public async Task StartGame() =>
        await Clients.All.SendAsync("GameStarted");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Players.TryRemove(Context.ConnectionId, out _);
        await BroadcastPlayers();
        await base.OnDisconnectedAsync(exception);
    }

    private static List<string> GetPlayerList() => Players.Values.ToList();
    private Task BroadcastPlayers() => Clients.All.SendAsync("PlayersUpdated", GetPlayerList());

    public async Task EndRound()
    {
        await Clients.All.SendAsync("RoundEnded");
    }
}
