using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cyber_Defense_Game.Views;

public partial class HostQuestionView : UserControl
{
    private readonly MainView? _main;
    private readonly Dictionary<string, (string Name, int AnswerIndex)> _playerAnswers = new();
    private CancellationTokenSource? _timerCts;
    private bool _timeUp;
    private int _questionIndex = 0;
    private QuizQuestion _currentQuestion = QuestionStore.GetCurrentQuestion();

    public HostQuestionView()
    {
        InitializeComponent();
    }

    public HostQuestionView(MainView main) : this() => _main = main;

    private void OnAnswerReceived(string connectionId, string name, int answerIndex)
    {
        if (_timeUp) return;

        bool everyoneAnswered = false;

        lock (_playerAnswers)
        {
            _playerAnswers[connectionId] = (name, answerIndex);

            int totalPlayers = GameConnection.CurrentPlayers.Count;
            if (totalPlayers > 0 && _playerAnswers.Count >= totalPlayers)
            {
                everyoneAnswered = true;
            }
        }

        UpdateAnsweredCount();

        if (everyoneAnswered)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_timeUp) return;
                _timerCts?.Cancel();
                FinishRound("Everyone answered!");
            });
        }
    }

    private void OnPlayersUpdated(List<string> _) => UpdateAnsweredCount();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.AnswerReceived += OnAnswerReceived;

        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        GameConnection.PlayersUpdated += OnPlayersUpdated;

        _questionIndex = 0;
        var all = QuestionStore.GetAll();
        if (all.Count > 0)
        {
            string firstName = all[0].Title;
            QuestionStore.SetActiveQuestion(firstName);
            _ = SafeLoadQuestionAsync(firstName);
        }

        StartRound();
    }

    private async Task SafeLoadQuestionAsync(string questionName)
    {
        try
        {
            await GameConnection.LoadQuestionAsync(questionName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadQuestion warning: {ex.Message}");
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timerCts?.Cancel();
        BrowserMusic.StopSafe();
        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        base.OnDetachedFromVisualTree(e);
    }

    private void StartRound()
    {
        _currentQuestion = QuestionStore.GetCurrentQuestion();

        try
        {
            BrowserMusic.PlayDifficultySafe(_currentQuestion.Difficulty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Music warning: {ex.Message}");
        }

        string fullQuestionText = $"[{_currentQuestion.Difficulty}] {_currentQuestion.QuestionText}";
        QuestionTitleText.Text = fullQuestionText;
        QuestionTitleText.FontSize = fullQuestionText.Length switch
        {
            > 120 => 20,
            > 80 => 24,
            > 50 => 30,
            _ => 36
        };

        OptionAText.Text = $"A: {_currentQuestion.OptionA}";
        OptionBText.Text = $"B: {_currentQuestion.OptionB}";
        OptionCText.Text = $"C: {_currentQuestion.OptionC}";
        OptionDText.Text = $"D: {_currentQuestion.OptionD}";

        lock (_playerAnswers)
        {
            _playerAnswers.Clear();
        }

        UpdateAnsweredCount();

        _timeUp = false;
        ResultsPanel.IsVisible = false;
        AnswersList.ItemsSource = null;

        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();
        _ = RunCountdownAsync(_timerCts.Token);
    }

    private async void NextQuestion_Click(object? sender, RoutedEventArgs e)
    {
        var all = QuestionStore.GetAll();
        if (all.Count == 0) return;

        _questionIndex = (_questionIndex + 1) % all.Count;
        string nextName = all[_questionIndex].Title;

        try
        {
            await GameConnection.LoadQuestionAsync(nextName);
            QuestionStore.SetActiveQuestion(nextName);
            StartRound();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadQuestion failed: {ex}");
        }
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        try
        {
            for (int seconds = 30; seconds > 0; seconds--)
            {
                if (token.IsCancellationRequested) return;

                TimerText.Text = $"Time left: {seconds}";
                await Task.Delay(1000, token);
            }

            if (token.IsCancellationRequested) return;

            FinishRound("Time's up!");
        }
        catch (TaskCanceledException)
        {
            // Expected when everyone answers early, moving to next round, or leaving the screen
        }
    }

    private void UpdateAnsweredCount()
    {
        int answered;
        lock (_playerAnswers)
        {
            answered = _playerAnswers.Count;
        }

        int total = GameConnection.CurrentPlayers.Count;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            AnsweredCountText.Text = $"Answered: {answered} / {total}");
    }

    private async void FinishRound(string reason)
    {
        if (_timeUp) return;
        _timeUp = true;

        string[] options =
        {
            $"A ({_currentQuestion.OptionA})",
            $"B ({_currentQuestion.OptionB})",
            $"C ({_currentQuestion.OptionC})",
            $"D ({_currentQuestion.OptionD})"
        };
        string correctLabel = _currentQuestion.CorrectIndex >= 0 && _currentQuestion.CorrectIndex < options.Length
            ? options[_currentQuestion.CorrectIndex]
            : "Unknown";

        TimerText.Text = $"{reason} Correct answer: {correctLabel}";
        RevealResults();

        try
        {
            await GameConnection.EndRoundAsync();
        }
        catch (Exception ex)
        {
            TimerText.Text = $"EndRound error: {ex.Message}";
        }
    }

    private void RevealResults()
    {
        string[] colorLabels =
        {
            $"Red (A: {_currentQuestion.OptionA})",
            $"Green (B: {_currentQuestion.OptionB})",
            $"Blue (C: {_currentQuestion.OptionC})",
            $"White (D: {_currentQuestion.OptionD})"
        };

        int[] counts = new int[4];
        int totalVotes = 0;

        lock (_playerAnswers)
        {
            foreach (var entry in _playerAnswers.Values)
            {
                if (entry.AnswerIndex >= 0 && entry.AnswerIndex < 4)
                {
                    counts[entry.AnswerIndex]++;
                    totalVotes++;
                }
            }
        }

        List<string> results = new();
        for (int i = 0; i < 4; i++)
        {
            double percentage = totalVotes > 0
                ? Math.Round((double)counts[i] / totalVotes * 100, 1)
                : 0;

            string correctTag = i == _currentQuestion.CorrectIndex ? " ✅ (Correct)" : "";
            results.Add($"{colorLabels[i]}: {percentage}% ({counts[i]} votes){correctTag}");
        }

        AnswersList.ItemsSource = results;
        ResultsPanel.IsVisible = true;
    }
}
