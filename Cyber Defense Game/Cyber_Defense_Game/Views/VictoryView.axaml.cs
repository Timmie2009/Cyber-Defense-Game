using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
    }

    private void BackToMenu_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }
}