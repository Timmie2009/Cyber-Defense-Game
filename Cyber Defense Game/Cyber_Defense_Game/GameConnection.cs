using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cyber_Defense_Game;

public static class GameConnection
{
    public const string ClientVersion = "v1.3.0";

    public static bool IsHost { get; set; }
    public static event Action? QuestionChanged;

    private static HubConnection? _connection;
    private static Task? _connectTask;

    public static event Action? RoundEnded;
    public static event Action<string, string, int>? UpgradeVoteReceived;
    public static event Action<int>? OptionEliminated;
    public static event Action<int>? GameOverReceived;
    public static event Action<int, int>? VictoryReceived;

    public static List<string> CurrentPlayers { get; private set; } = new();
    public static event Action? GameStarted;

    public static event Action<List<string>>? PlayersUpdated;
    public static event Action<string, string, int>? AnswerReceived;

    public static Task ConnectAsync(string serverUrl)
    {
        if (_connectTask != null)
            return _connectTask;

        _connectTask = ConnectInternalAsync(serverUrl);
        return _connectTask;
    }

    private static async Task ConnectInternalAsync(string serverUrl)
    {
        try
        {
            _connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl.TrimEnd('/')}/gamehub")
                .WithAutomaticReconnect()
                .Build();

            _connection.On<int>("ImageChanged", n => GameState.SelectedImage = n);
            _connection.On<List<string>>("PlayersUpdated", p =>
            {
                Console.WriteLine($"PlayersUpdated received: {p?.Count ?? 0} players");
                CurrentPlayers = p ?? new List<string>();
                PlayersUpdated?.Invoke(CurrentPlayers);
            });
            _connection.On<string, string, int>("AnswerReceived",
                (id, name, answer) => AnswerReceived?.Invoke(id, name, answer));

            _connection.On("GameStarted", () => GameStarted?.Invoke());

            _connection.On("QuestionChanged", () => QuestionChanged?.Invoke());

            _connection.On<QuizQuestion>("QuestionSaved", q =>
            {
                if (q != null) QuestionStore.AddOrUpdateLocal(q);
            });

            _connection.On<string>("QuestionLoaded", questionName =>
            {
                UpgradeStore.EliminatedOptionIndex = -1;
                QuestionStore.SetActiveQuestion(questionName);
            });

            _connection.On<string>("QuestionDeleted", questionName =>
            {
                QuestionStore.RemoveLocal(questionName);
            });

            _connection.On("RoundEnded", () => RoundEnded?.Invoke());

            _connection.On<int, int>("UpgradeVoteStarted", (id1, id2) =>
            {
                UpgradeStore.SetActiveUpgrades(id1, id2);
            });

            _connection.On<string, string, int>("UpgradeVoteReceived",
                (id, name, choice) => UpgradeVoteReceived?.Invoke(id, name, choice));

            _connection.On<int>("OptionEliminated", wrongIndex =>
            {
                UpgradeStore.EliminatedOptionIndex = wrongIndex;
                OptionEliminated?.Invoke(wrongIndex);
            });

            _connection.On<int>("GameOver", finalScore =>
            {
                UpgradeStore.TeamScore = finalScore;
                GameOverReceived?.Invoke(finalScore);
            });

            _connection.On<int, int>("Victory", (finalScore, livesLeft) =>
            {
                UpgradeStore.TeamScore = finalScore;
                UpgradeStore.TeamLives = livesLeft;
                VictoryReceived?.Invoke(finalScore, livesLeft);
            });

            await _connection.StartAsync();

            try
            {
                var serverQuestions = await _connection.InvokeAsync<List<QuizQuestion>>("GetAllQuestions");
                if (serverQuestions != null)
                {
                    foreach (var q in serverQuestions)
                        QuestionStore.AddOrUpdateLocal(q);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllQuestions warning: {ex.Message}");
            }
        }
        catch
        {
            _connectTask = null;
            _connection = null;
            throw;
        }
    }

    public static Task JoinLobbyAsync(string name) =>
        _connection?.InvokeAsync("JoinLobby", name) ?? Task.CompletedTask;

    public static Task RegisterAsHostAsync() =>
        _connection?.InvokeAsync("RegisterAsHost") ?? Task.CompletedTask;

    public static Task SubmitAnswerAsync(int answer) =>
        _connection?.InvokeAsync("SubmitAnswer", answer) ?? Task.CompletedTask;

    public static Task ChangeImageAsync(int imageNumber) =>
        _connection?.InvokeAsync("ChangeImage", imageNumber) ?? Task.CompletedTask;

    public static Task StartGameAsync() =>
        _connection?.InvokeAsync("StartGame") ?? Task.CompletedTask;

    public static Task NextQuestionAsync() =>
        _connection?.InvokeAsync("NextQuestion") ?? Task.CompletedTask;

    public static Task SaveQuestionAsync(QuizQuestion question) =>
        _connection?.InvokeAsync("SaveQuestion", question) ?? Task.CompletedTask;

    public static Task LoadQuestionAsync(string questionName) =>
        _connection?.InvokeAsync("LoadQuestion", questionName) ?? Task.CompletedTask;

    public static Task DeleteQuestionAsync(string questionName) =>
        _connection?.InvokeAsync("DeleteQuestion", questionName) ?? Task.CompletedTask;

    public static Task EndRoundAsync() =>
        _connection?.InvokeAsync("EndRound") ?? Task.CompletedTask;

    public static Task StartUpgradeVoteAsync(int option1Id, int option2Id) =>
        _connection?.InvokeAsync("StartUpgradeVote", option1Id, option2Id) ?? Task.CompletedTask;

    public static Task SubmitUpgradeVoteAsync(int choiceIndex) =>
        _connection?.InvokeAsync("SubmitUpgradeVote", choiceIndex) ?? Task.CompletedTask;

    public static Task EliminateOptionAsync(int wrongIndex) =>
        _connection?.InvokeAsync("EliminateOption", wrongIndex) ?? Task.CompletedTask;

    public static Task TriggerGameOverAsync(int finalScore) =>
        _connection?.InvokeAsync("TriggerGameOver", finalScore) ?? Task.CompletedTask;

    public static Task TriggerVictoryAsync(int finalScore, int livesLeft) =>
        _connection?.InvokeAsync("TriggerVictory", finalScore, livesLeft) ?? Task.CompletedTask;
}
