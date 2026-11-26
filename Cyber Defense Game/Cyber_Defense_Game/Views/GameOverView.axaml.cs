using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace Cyber_Defense_Game.Views;

public partial class GameOverView : UserControl
{
    private readonly MainView? _main;

    public GameOverView()
    {
        InitializeComponent();
    }

    public GameOverView(MainView main) : this() => _main = main;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BrowserMusic.StopSafe();

        FinalScoreText.Text = $"Final Team Score: {UpgradeStore.TeamScore}";
        ProgressText.Text = $"Questions Survived: {UpgradeStore.QuestionsPlayed}";

        try
        {
            using var stream = AssetLoader.Open(new Uri("avares://Cyber_Defense_Game/Assets/Lose.png"));
            LoseImage.Source = new Bitmap(stream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lose.png load warning: {ex.Message}");
        }
    }

    private void BackToMenu_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }
}