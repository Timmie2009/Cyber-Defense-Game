using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Cyber_Defense_Game.Views;

public partial class VictoryView : UserControl
{
    private readonly MainView? _main;

    public VictoryView()
    {
        InitializeComponent();
    }

    public VictoryView(MainView main) : this() => _main = main;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BrowserMusic.StopSafe();

        FinalScoreText.Text = $"Final Team Score: {UpgradeStore.TeamScore}";
        LivesText.Text = $"Lives Remaining: {UpgradeStore.TeamLives} / 3";

        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Cyber_Defense_Game/Assets/Victory.png"));
            VictoryImage.Source = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Victory.png load warning: {ex.Message}");
        }
    }

    private void BackToMenu_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }
}