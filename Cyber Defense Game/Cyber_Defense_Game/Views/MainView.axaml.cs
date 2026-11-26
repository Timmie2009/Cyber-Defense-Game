using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace Cyber_Defense_Game.Views;

public partial class MainView : UserControl
{
    static MainView()
    {
        Button.ClickEvent.AddClassHandler<Button>((_, _) =>
            BrowserMusic.PlayEffectSafe("ui_click"));
        InputElement.PointerEnteredEvent.AddClassHandler<Button>((_, e) =>
        {
            if (e.Pointer.Type == PointerType.Mouse)
                BrowserMusic.PlayEffectSafe("ui_hover");
        });
    }

    public MainView()
    {
        InitializeComponent();
        ShowMenu();

        Loaded += MainView_Loaded;
    }

    private void OnGameStarted()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            bool startingFromHostLobby = ContentHost.Content is HostView;
            ShowCountdown();
            if (startingFromHostLobby)
                BrowserMusic.PlayEffectSafe("game_start");
        });
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

    private void OnGameOverReceived(int _)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(ShowGameOver);
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

        GameConnection.GameOverReceived -= OnGameOverReceived;
        GameConnection.GameOverReceived += OnGameOverReceived;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameConnection.GameStarted -= OnGameStarted;
        UpgradeStore.UpgradePhaseStarted -= OnUpgradePhaseStarted;
        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        GameConnection.GameOverReceived -= OnGameOverReceived;
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

    public void ShowGameOver()
    {
        if (ContentHost.Content is GameOverView)
            return;

        ContentHost.Content = new GameOverView(this);
        BrowserMusic.PlayEffectSafe("game_over_breach");
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= MainView_Loaded;

        try
        {
            await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initial SignalR connect warning: {ex}");
        }
    }

    public void ShowMenu()
    {
        BrowserMusic.StopSafe();
        UpgradeStore.ResetGame();

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
        {
            if (ContentHost.Content is HostUpgradeView)
                BrowserMusic.PlayEffectSafe("whoosh");
            ContentHost.Content = new HostQuestionView(this);
        }
        else
            ContentHost.Content = new PlayerAnswerView(this);
    }
}

public static partial class BrowserMusic
{
    public static void PlayEffectSafe(string effect)
    {
        if (OperatingSystem.IsBrowser() && GameConnection.IsHost)
            PlayEffect(effect);
    }

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.playLobbyEffect")]
    private static partial void PlayEffect(string effect);

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
