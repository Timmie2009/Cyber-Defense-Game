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
    private bool _allAnsweredEarly;
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

        if (everyoneAnswered)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_timeUp) return;
                _allAnsweredEarly = true;
                _timerCts?.Cancel();
                FinishRound("Everyone answered!");
            });
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.AnswerReceived += OnAnswerReceived;

        _questionIndex = 0;
        var all = QuestionStore.GetAll();
        if (all.Count > 0)
        {
            string firstName = all[0].Title;
            QuestionStore.SetActiveQuestion(firstName);
            _ = GameConnection.LoadQuestionAsync(firstName);
        }

        StartRound();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timerCts?.Cancel();
        GameConnection.AnswerReceived -= OnAnswerReceived;
        base.OnDetachedFromVisualTree(e);
    }

    private void StartRound()
    {
        _currentQuestion = QuestionStore.GetCurrentQuestion();

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

        _timeUp = false;
        _allAnsweredEarly = false;
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

    private async void FinishRound(string reason)
    {
        if (_timeUp) return;
        _timeUp = true;

        try
        {
            await GameConnection.EndRoundAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EndRound failed: {ex}");
        }

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
    }


    private void RevealResults()
    {
        string[] options =
        {
            $"A: {_currentQuestion.OptionA}",
            $"B: {_currentQuestion.OptionB}",
            $"C: {_currentQuestion.OptionC}",
            $"D: {_currentQuestion.OptionD}"
        };
        List<string> results = new();

        lock (_playerAnswers)
        {
            foreach (var playerName in GameConnection.CurrentPlayers)
            {
                var entry = _playerAnswers.Values.FirstOrDefault(a => a.Name == playerName);

                if (string.IsNullOrEmpty(entry.Name))
                {
                    results.Add($"{playerName}: No answer (❌ Wrong)");
                }
                else
                {
                    string chosen = entry.AnswerIndex >= 0 && entry.AnswerIndex < options.Length
                        ? options[entry.AnswerIndex]
                        : "Unknown";
                    bool isRight = entry.AnswerIndex == _currentQuestion.CorrectIndex;
                    string status = isRight ? "✅ Right" : "❌ Wrong";

                    results.Add($"{entry.Name}: {chosen} ({status})");
                }
            }
        }

        AnswersList.ItemsSource = results;
        ResultsPanel.IsVisible = true;
    }
}
