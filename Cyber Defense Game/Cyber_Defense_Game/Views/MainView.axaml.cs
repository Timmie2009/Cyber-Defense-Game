using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Cyber_Defense_Game.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        ShowMenu();

        Loaded += MainView_Loaded;
    }

    private void OnGameStarted()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(ShowCountdown);
    }

    private void OnUpgradePhaseStarted()
    {
        if (!GameConnection.IsHost)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ShowUpgradeVote);
        }
    }

    private void OnQuestionLoaded(string _)
    {
        if (!GameConnection.IsHost && ContentHost.Content is PlayerUpgradeView)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(ShowGame);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameConnection.GameStarted -= OnGameStarted;
        GameConnection.GameStarted += OnGameStarted;

        UpgradeStore.UpgradePhaseStarted -= OnUpgradePhaseStarted;
        UpgradeStore.UpgradePhaseStarted += OnUpgradePhaseStarted;

        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        QuestionStore.QuestionLoaded += OnQuestionLoaded;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameConnection.GameStarted -= OnGameStarted;
        UpgradeStore.UpgradePhaseStarted -= OnUpgradePhaseStarted;
        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        base.OnDetachedFromVisualTree(e);
    }

    public void ShowCountdown()
    {
        ContentHost.Content = new CountdownView(this);
    }

    public void ShowUpgradeVote()
    {
        if (GameConnection.IsHost)
            ContentHost.Content = new HostUpgradeView(this);
        else
            ContentHost.Content = new PlayerUpgradeView(this);
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= MainView_Loaded;

        try
        {
            await GameConnection.ConnectAsync("https://ts.net");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());

            await Avalonia.Threading.Dispatcher.UIThread
                .InvokeAsync(() =>
                {
                    ContentHost.Content = new TextBlock
                    {
                        Text = $"SignalR fout:\n{ex.Message}",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    };
                });
        }
    }

    public void ShowMenu()
    {
        BrowserMusic.StopSafe();
        UpgradeStore.QuestionsPlayed = 0;
        UpgradeStore.QuestionIndex = 0;
        UpgradeStore.TeamScore = 0;

        var menu = new MenuView();

        menu.HostClicked += (_, _) =>
        {
            GameConnection.IsHost = true;
            BrowserMusic.PlaySafe();
            ContentHost.Content = new HostView(this);
        };

        menu.JoinClicked += (_, _) =>
        {
            GameConnection.IsHost = false;
            ContentHost.Content = new JoinView(this);
        };

        menu.CreateQuestionClicked += (_, _) =>
            ContentHost.Content = new CreateQuestionView(this);

        ContentHost.Content = menu;
    }

    public void ShowGame()
    {
        if (GameConnection.IsHost)
            ContentHost.Content = new HostQuestionView(this);
        else
            ContentHost.Content = new PlayerAnswerView(this);
    }
}

public static partial class BrowserMusic
{
    public static void PlaySafe()
    {
        if (OperatingSystem.IsBrowser())
        {
            Play();
        }
    }

    public static void PlayDifficultySafe(string difficulty)
    {
        if (OperatingSystem.IsBrowser())
        {
            PlayDifficulty(difficulty);
        }
    }

    public static void StopSafe()
    {
        if (OperatingSystem.IsBrowser())
        {
            Stop();
        }
    }

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.playHostMusic")]
    private static partial void Play();

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.playDifficultyMusic")]
    private static partial void PlayDifficulty(string difficulty);

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.stopHostMusic")]
    private static partial void Stop();
}

public static partial class BrowserInput
{
    public static string? AskName(string message, string defaultValue = "")
    {
        if (OperatingSystem.IsBrowser())
        {
            return Prompt(message, defaultValue);
        }
        return null;
    }

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.prompt")]
    private static partial string? Prompt(string message, string defaultValue);
}
