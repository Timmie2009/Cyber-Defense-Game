using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cyber_Defense_Game.Views;

public partial class HostUpgradeView : UserControl
{
    private readonly MainView? _main;
    private readonly Dictionary<string, int> _votes = new();
    private CancellationTokenSource? _timerCts;
    private bool _votingEnded;

    public HostUpgradeView()
    {
        InitializeComponent();
    }

    public HostUpgradeView(MainView main) : this() => _main = main;

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameConnection.UpgradeVoteReceived -= OnVoteReceived;
        GameConnection.UpgradeVoteReceived += OnVoteReceived;

        var (id1, id2) = UpgradeStore.PickTwoAffordable();

        ScoreStatusText.Text = $"Available Score: {UpgradeStore.TeamScore} pts";

        if (UpgradeStore.Option1 != null)
        {
            Card1Border.IsVisible = true;
            Upgrade1Title.Text = UpgradeStore.Option1.Name;
            Upgrade1Cost.Text = $"Cost: {UpgradeStore.Option1.Cost} pts";
            Upgrade1Desc.Text = UpgradeStore.Option1.Description;
        }
        else
        {
            Card1Border.IsVisible = false;
        }

        if (UpgradeStore.Option2 != null)
        {
            Card2Border.IsVisible = true;
            Upgrade2Title.Text = UpgradeStore.Option2.Name;
            Upgrade2Cost.Text = $"Cost: {UpgradeStore.Option2.Cost} pts";
            Upgrade2Desc.Text = UpgradeStore.Option2.Description;
        }
        else
        {
            Card2Border.IsVisible = false;
        }

        _votes.Clear();
        _votingEnded = false;
        ResultPanel.IsVisible = false;
        UpdateVoteDisplay();

        try
        {
            await GameConnection.StartUpgradeVoteAsync(id1, id2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartUpgradeVote failed: {ex.Message}");
        }

        _timerCts?.Cancel();
        _timerCts = new CancellationTokenSource();
        _ = RunCountdownAsync(_timerCts.Token);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timerCts?.Cancel();
        GameConnection.UpgradeVoteReceived -= OnVoteReceived;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnVoteReceived(string connectionId, string name, int choiceIndex)
    {
        if (_votingEnded) return;

        bool everyoneVoted = false;

        lock (_votes)
        {
            _votes[connectionId] = choiceIndex;
            int totalPlayers = GameConnection.CurrentPlayers.Count;
            if (totalPlayers > 0 && _votes.Count >= totalPlayers)
            {
                everyoneVoted = true;
            }
        }

        UpdateVoteDisplay();

        if (everyoneVoted)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_votingEnded) return;
                _timerCts?.Cancel();
                FinishVoting("Everyone voted!");
            });
        }
    }

    private void UpdateVoteDisplay()
    {
        int count1, count2, skipCount, totalVoted;
        lock (_votes)
        {
            count1 = _votes.Values.Count(v => v == 0);
            count2 = _votes.Values.Count(v => v == 1);
            skipCount = _votes.Values.Count(v => v == 2);
            totalVoted = _votes.Count;
        }

        int totalPlayers = GameConnection.CurrentPlayers.Count;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            VoteCountText.Text = $"Votes: {totalVoted} / {totalPlayers}";
            Upgrade1Votes.Text = $"{count1} votes";
            Upgrade2Votes.Text = $"{count2} votes";
            SkipVotesText.Text = $"{skipCount} votes";
        });
    }

    private async Task RunCountdownAsync(CancellationToken token)
    {
        try
        {
            for (int seconds = 15; seconds > 0; seconds--)
            {
                if (token.IsCancellationRequested) return;
                TimerText.Text = $"Time left: {seconds}";
                await Task.Delay(1000, token);
            }

            if (token.IsCancellationRequested) return;
            FinishVoting("Time's up!");
        }
        catch (TaskCanceledException) { }
    }

    private void FinishVoting(string reason)
    {
        if (_votingEnded) return;
        _votingEnded = true;

        int count1, count2, skipCount;
        lock (_votes)
        {
            count1 = _votes.Values.Count(v => v == 0);
            count2 = _votes.Values.Count(v => v == 1);
            skipCount = _votes.Values.Count(v => v == 2);
        }

        TimerText.Text = reason;

        // Determine highest votes among available options (0 = Option1, 1 = Option2, 2 = Skip)
        int winningChoice = 2; // Default to Skip if no votes or Skip wins
        int bestVotes = skipCount;

        if (UpgradeStore.Option1 != null && count1 > bestVotes)
        {
            bestVotes = count1;
            winningChoice = 0;
        }

        if (UpgradeStore.Option2 != null && count2 > bestVotes)
        {
            bestVotes = count2;
            winningChoice = 1;
        }

        if (winningChoice == 0 && UpgradeStore.Option1 != null)
        {
            UpgradeStore.ApplyWinningUpgrade(UpgradeStore.Option1);
            WinnerText.Text = $"Purchased {UpgradeStore.Option1.Name} (-{UpgradeStore.Option1.Cost} pts)!";
        }
        else if (winningChoice == 1 && UpgradeStore.Option2 != null)
        {
            UpgradeStore.ApplyWinningUpgrade(UpgradeStore.Option2);
            WinnerText.Text = $"Purchased {UpgradeStore.Option2.Name} (-{UpgradeStore.Option2.Cost} pts)!";
        }
        else
        {
            WinnerText.Text = "Skipped upgrade — Score saved!";
        }

        ScoreStatusText.Text = $"Remaining Score: {UpgradeStore.TeamScore} pts";
        ResultPanel.IsVisible = true;
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowGame();
    }
}
