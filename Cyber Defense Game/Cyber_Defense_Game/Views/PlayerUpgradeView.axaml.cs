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
        Option1Button.Content = UpgradeStore.Option1.Name;
        Option2Button.Content = UpgradeStore.Option2.Name;
        StatusText.Text = "Vote for an upgrade!";
    }

    private void Option1_Click(object? sender, RoutedEventArgs e) => SendVote(0, UpgradeStore.Option1.Name);
    private void Option2_Click(object? sender, RoutedEventArgs e) => SendVote(1, UpgradeStore.Option2.Name);

    private async void SendVote(int choiceIndex, string upgradeName)
    {
        if (_hasVoted) return;
        _hasVoted = true;

        try
        {
            await GameConnection.SubmitUpgradeVoteAsync(choiceIndex);
            StatusText.Text = $"You voted for {upgradeName}! Waiting for host...";
        }
        catch (Exception ex)
        {
            _hasVoted = false;
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
