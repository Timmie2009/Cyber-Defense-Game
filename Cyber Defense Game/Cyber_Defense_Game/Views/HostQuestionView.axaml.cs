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

    // 0 = Red, 1 = Green, 2 = Blue, 3 = White
    private const int CorrectAnswerIndex = 2;

    public HostQuestionView()
    {
        InitializeComponent();
    }

    public HostQuestionView(MainView main) : this() => _main = main;

    private void OnAnswerReceived(string connectionId, string name, int answerIndex)
    {
        if (_timeUp) return;

        lock (_playerAnswers)
        {
            _playerAnswers[connectionId] = (name, answerIndex);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.AnswerReceived += OnAnswerReceived;

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
        lock (_playerAnswers)
        {
            _playerAnswers.Clear();
        }

        _timeUp = false;
        ResultsPanel.IsVisible = false;
        AnswersList.ItemsSource = null;

        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();
        _ = RunCountdownAsync(_timerCts.Token);
    }

    private async void NextQuestion_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await GameConnection.NextQuestionAsync();
            StartRound();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NextQuestion failed: {ex}");
        }
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        try
        {
            for (int seconds = 10; seconds > 0; seconds--)
            {
                if (token.IsCancellationRequested) return;

                TimerText.Text = $"Time left: {seconds}";
                await Task.Delay(1000, token);
            }

            if (token.IsCancellationRequested) return;

            _timeUp = true;
            TimerText.Text = "Time's up! Correct answer: Blue";
            RevealResults();
        }
        catch (TaskCanceledException)
        {
            // Expected when starting the next round or leaving the view
        }
    }

    private void RevealResults()
    {
        string[] options = { "Red", "Green", "Blue", "White" };
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
                    bool isRight = entry.AnswerIndex == CorrectAnswerIndex;
                    string status = isRight ? "✅ Right" : "❌ Wrong";

                    results.Add($"{playerName}: {chosen} ({status})");
                }
            }
        }

        AnswersList.ItemsSource = results;
        ResultsPanel.IsVisible = true;
    }
}
