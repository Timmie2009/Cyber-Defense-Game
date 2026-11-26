using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia.Input;
using System.Threading.Tasks;

namespace Cyber_Defense_Game.Views;

public partial class HostQuestionView : UserControl
{
    private readonly MainView? _main;
    private readonly Dictionary<string, (string Name, int AnswerIndex)> _playerAnswers = new();
    private CancellationTokenSource? _timerCts;
    private bool _timeUp;
    private TopLevel? _attachedTopLevel;
    private bool _roundOverclockActive;
    private QuizQuestion _currentQuestion = QuestionStore.GetCurrentQuestion();

    public HostQuestionView()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            _attachedTopLevel = TopLevel.GetTopLevel(this);
            _attachedTopLevel?.AddHandler(KeyDownEvent, OnHostKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        };

        Unloaded += (_, _) =>
        {
            _attachedTopLevel?.RemoveHandler(KeyDownEvent, OnHostKeyDown);
            _attachedTopLevel = null;
        };
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

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {// Inside OnAttachedToVisualTree:
     // In OnAttachedToVisualTree:
        if (TopLevel.GetTopLevel(this) is { } attachTopLevel)
        {
            attachTopLevel.KeyDown -= OnHostKeyDown;
            attachTopLevel.KeyDown += OnHostKeyDown;
        }

        // In OnDetachedFromVisualTree:
        if (TopLevel.GetTopLevel(this) is { } detachTopLevel)
        {
            detachTopLevel.KeyDown -= OnHostKeyDown;
        }
        base.OnAttachedToVisualTree(e);

        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.AnswerReceived += OnAnswerReceived;

        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        GameConnection.PlayersUpdated += OnPlayersUpdated;

        UpgradeStore.EnsureGameQueue();

        if (UpgradeStore.GameQueue.Count > 0)
        {
            if (UpgradeStore.QuestionIndex >= UpgradeStore.GameQueue.Count)
                UpgradeStore.QuestionIndex = 0;

            string currentName = UpgradeStore.GameQueue[UpgradeStore.QuestionIndex].Title;
            QuestionStore.SetActiveQuestion(currentName);
            await SafeLoadQuestionAsync(currentName);
        }

        StartRound();
    }
    private async void OnHostKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        e.Handled = true;

        if (!_timeUp)
        {
            _timeUp = true;
            _timerCts?.Cancel();

            int roundScore = _roundOverclockActive ? 200 : 100;
            UpgradeStore.TeamScore += roundScore;
            UpdateStatusBadge(false);

            try
            {
                await GameConnection.EndRoundAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EndRound debug warning: {ex.Message}");
            }
        }

        NextQuestion_Click(this, new RoutedEventArgs());
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

    private void UpdateStatusBadge(bool roundThreatScannerActive)
    {
        ScoreText.Text = $"Score: {UpgradeStore.TeamScore}";
        string hearts = new string('❤', Math.Max(0, UpgradeStore.TeamLives));
        LivesText.Text = $"Lives: {hearts} ({UpgradeStore.TeamLives}/3)";

        List<string> buffs = new();
        if (UpgradeStore.ExtraTimeRoundsLeft > 0)
            buffs.Add($"⏱ Extra Time ({UpgradeStore.ExtraTimeRoundsLeft} left)");
        if (_roundOverclockActive)
            buffs.Add($"⚡ Overclock 2x ({UpgradeStore.OverclockRoundsLeft} left)");
        if (roundThreatScannerActive)
            buffs.Add($"🔍 Threat Scanner ({UpgradeStore.ThreatScannerRoundsLeft} left)");

        if (buffs.Count > 0)
        {
            ActiveBuffText.Text = string.Join(" | ", buffs);
            ActiveBuffText.IsVisible = true;
        }
        else
        {
            ActiveBuffText.IsVisible = false;
        }
    }

    private async void StartRound()
    {
        _currentQuestion = QuestionStore.GetCurrentQuestion();
        BrowserMusic.PlayEffectSafe("question_appear");

        _roundOverclockActive = UpgradeStore.OverclockRoundsLeft > 0;
        if (_roundOverclockActive)
            UpgradeStore.OverclockRoundsLeft--;

        bool useThreatScanner = UpgradeStore.ThreatScannerRoundsLeft > 0;
        if (useThreatScanner)
            UpgradeStore.ThreatScannerRoundsLeft--;

        int roundSeconds = 30;
        if (UpgradeStore.ExtraTimeRoundsLeft > 0)
        {
            roundSeconds = 45;
            UpgradeStore.ExtraTimeRoundsLeft--;
        }

        UpdateStatusBadge(useThreatScanner);
        if (_roundOverclockActive || useThreatScanner || roundSeconds > 30)
            BrowserMusic.PlayEffectSafe("powerup_activate");

        try
        {
            BrowserMusic.PlayDifficultySafe(_currentQuestion.Difficulty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Music warning: {ex.Message}");
        }

        int questionNumber = UpgradeStore.QuestionIndex + 1;
        int totalInQueue = Math.Max(1, UpgradeStore.GameQueue.Count);
        string fullQuestionText = $"Q{questionNumber}/{totalInQueue} [{_currentQuestion.Difficulty}] {_currentQuestion.QuestionText}";
        QuestionTitleText.Text = fullQuestionText;
        QuestionTitleText.FontSize = fullQuestionText.Length switch
        {
            > 120 => 20,
            > 80 => 24,
            > 50 => 30,
            _ => 36
        };

        Border[] borders = { OptionABorder, OptionBBorder, OptionCBorder, OptionDBorder };
        foreach (var b in borders) b.Opacity = 1.0;

        OptionAText.Text = $"A: {_currentQuestion.OptionA}";
        OptionBText.Text = $"B: {_currentQuestion.OptionB}";
        OptionCText.Text = $"C: {_currentQuestion.OptionC}";
        OptionDText.Text = $"D: {_currentQuestion.OptionD}";

        if (useThreatScanner)
        {
            var wrongIndices = Enumerable.Range(0, 4)
                .Where(i => i != _currentQuestion.CorrectIndex)
                .ToList();

            int eliminated = wrongIndices[Random.Shared.Next(wrongIndices.Count)];
            borders[eliminated].Opacity = 0.25;

            TextBlock[] texts = { OptionAText, OptionBText, OptionCText, OptionDText };
            texts[eliminated].Text += " (ELIMINATED)";

            try
            {
                await GameConnection.EliminateOptionAsync(eliminated);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EliminateOption warning: {ex.Message}");
            }
        }

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
        _ = RunCountdownAsync(roundSeconds, _timerCts.Token);
    }

    private async void NextQuestion_Click(object? sender, RoutedEventArgs e)
    {
        UpgradeStore.EnsureGameQueue();
        if (UpgradeStore.GameQueue.Count == 0) return;

        UpgradeStore.QuestionsPlayed++;
        UpgradeStore.QuestionIndex++;

        if (UpgradeStore.TeamLives <= 0)
        {
            try
            {
                await GameConnection.TriggerGameOverAsync(UpgradeStore.TeamScore);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TriggerGameOver warning: {ex.Message}");
            }

            _main?.ShowGameOver();
            return;
        }

        // MUST come BEFORE (UpgradeStore.QuestionsPlayed % 5 == 0)!
        if (UpgradeStore.QuestionIndex >= UpgradeStore.GameQueue.Count)
        {
            _timerCts?.Cancel();
            BrowserMusic.StopSafe();
            BrowserMusic.PlayEffectSafe("victory_fanfare");

            QuestionTitleText.Text = $"Quiz Complete! Final Team Score: {UpgradeStore.TeamScore}";
            TimerText.Text = $"Lives Remaining: {UpgradeStore.TeamLives}/3";
            if (sender is Button btn)
            {
                btn.Content = "Back to Menu";
            }

            try
            {
                await GameConnection.TriggerVictoryAsync(UpgradeStore.TeamScore, UpgradeStore.TeamLives);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TriggerVictory warning: {ex.Message}");
            }

            _main?.ShowVictory();
            return;
        }

        if (UpgradeStore.QuestionsPlayed % 5 == 0)
        {
            _main?.ShowUpgradeVote();
            return;
        }

        string nextName = UpgradeStore.GameQueue[UpgradeStore.QuestionIndex].Title;

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

    private async Task RunCountdownAsync(int startSeconds, CancellationToken token)
    {
        try
        {
            for (int seconds = startSeconds; seconds > 0; seconds--)
            {
                if (token.IsCancellationRequested) return;

                TimerText.Text = $"Time left: {seconds}";
                if (seconds <= 5)
                    BrowserMusic.PlayEffectSafe(seconds == 5 ? "timer_urgent" : "timer_tick");
                await Task.Delay(1000, token);
            }

            if (token.IsCancellationRequested) return;

            BrowserMusic.PlayEffectSafe("time_up");
            FinishRound("Time's up!");
        }
        catch (TaskCanceledException) { }
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
        int totalPlayers = GameConnection.CurrentPlayers.Count > 0
        ? GameConnection.CurrentPlayers.Count
        : totalVotes;
        int correctVotes = _currentQuestion.CorrectIndex >= 0 && _currentQuestion.CorrectIndex < 4
        ? counts[_currentQuestion.CorrectIndex]
        : 0;
        int roundScore = totalPlayers > 0
        ? (int)Math.Round((double)correctVotes / totalPlayers * 100)
        : 0;
        if (totalPlayers > 0 && roundScore < 50)
        {
            UpgradeStore.TeamLives = Math.Max(0, UpgradeStore.TeamLives - 1);
        }
        if (totalPlayers > 0)
            BrowserMusic.PlayEffectSafe(roundScore >= 50 ? "answer_correct" : "answer_wrong");
        if (_roundOverclockActive)
        {
            roundScore *= 2;
        }
        UpgradeStore.TeamScore += roundScore;
        UpdateStatusBadge(false);
        ScoreText.Text = $"Score: {UpgradeStore.TeamScore} (+{roundScore})";
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
        if (UpgradeStore.QuestionIndex + 1 >= UpgradeStore.GameQueue.Count)
            BrowserMusic.PlayEffectSafe("Scoreboard_reveal");
    }
}
