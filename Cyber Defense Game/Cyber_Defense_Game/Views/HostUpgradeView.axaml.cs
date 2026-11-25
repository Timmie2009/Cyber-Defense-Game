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

        var (id1, id2) = UpgradeStore.PickTwoRandom();

        Upgrade1Title.Text = UpgradeStore.Option1.Name;
        Upgrade1Desc.Text = UpgradeStore.Option1.Description;
        Upgrade2Title.Text = UpgradeStore.Option2.Name;
        Upgrade2Desc.Text = UpgradeStore.Option2.Description;

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
        int count1, count2, totalVoted;
        lock (_votes)
        {
            count1 = _votes.Values.Count(v => v == 0);
            count2 = _votes.Values.Count(v => v == 1);
            totalVoted = _votes.Count;
        }

        int totalPlayers = GameConnection.CurrentPlayers.Count;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            VoteCountText.Text = $"Votes: {totalVoted} / {totalPlayers}";
            Upgrade1Votes.Text = $"{count1} votes";
            Upgrade2Votes.Text = $"{count2} votes";
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

        int count1, count2;
        lock (_votes)
        {
            count1 = _votes.Values.Count(v => v == 0);
            count2 = _votes.Values.Count(v => v == 1);
        }

        UpgradeOption winner = count2 > count1
            ? UpgradeStore.Option2
            : count1 > count2
                ? UpgradeStore.Option1
                : (Random.Shared.Next(2) == 0 ? UpgradeStore.Option1 : UpgradeStore.Option2);

        UpgradeStore.ApplyWinningUpgrade(winner);

        TimerText.Text = reason;
        WinnerText.Text = $"Selected Upgrade: {winner.Name}!";
        ResultPanel.IsVisible = true;
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowGame();
    }
}
