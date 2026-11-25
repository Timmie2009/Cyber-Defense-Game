using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Cyber_Defense_Game.Views;

public partial class PlayerUpgradeView : UserControl
{
    private readonly MainView? _main;
    private bool _hasVoted;

    public PlayerUpgradeView()
    {
        InitializeComponent();
    }

    public PlayerUpgradeView(MainView main) : this() => _main = main;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _hasVoted = false;

        if (UpgradeStore.Option1 != null)
        {
            Option1Button.IsVisible = true;
            Option1Button.Content = $"{UpgradeStore.Option1.Name} ({UpgradeStore.Option1.Cost} pts)";
        }
        else
        {
            Option1Button.IsVisible = false;
        }

        if (UpgradeStore.Option2 != null)
        {
            Option2Button.IsVisible = true;
            Option2Button.Content = $"{UpgradeStore.Option2.Name} ({UpgradeStore.Option2.Cost} pts)";
        }
        else
        {
            Option2Button.IsVisible = false;
        }

        StatusText.Text = "Vote for an upgrade or skip to save score!";
    }

    private void Option1_Click(object? sender, RoutedEventArgs e)
    {
        if (UpgradeStore.Option1 != null)
            SendVote(0, UpgradeStore.Option1.Name);
    }

    private void Option2_Click(object? sender, RoutedEventArgs e)
    {
        if (UpgradeStore.Option2 != null)
            SendVote(1, UpgradeStore.Option2.Name);
    }

    private void Skip_Click(object? sender, RoutedEventArgs e) =>
        SendVote(2, "Skip (Save Score)");

    private async void SendVote(int choiceIndex, string label)
    {
        if (_hasVoted) return;
        _hasVoted = true;

        try
        {
            await GameConnection.SubmitUpgradeVoteAsync(choiceIndex);
            StatusText.Text = $"You voted for {label}! Waiting for host...";
        }
        catch (Exception ex)
        {
            _hasVoted = false;
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
