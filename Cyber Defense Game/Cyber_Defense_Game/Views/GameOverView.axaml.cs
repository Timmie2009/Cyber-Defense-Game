using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

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
    }

    private void BackToMenu_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }
}
