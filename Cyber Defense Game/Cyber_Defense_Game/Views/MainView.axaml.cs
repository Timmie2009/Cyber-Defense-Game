
﻿using Avalonia.Controls;

﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Threading.Tasks;


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
        if (!GameConnection.IsHost)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (ContentHost.Content is PlayerUpgradeView)
                {
                    ShowGame();
                }
            });
        }
    }

    private void OnGameOverReceived(int _)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(ShowGameOver);
    }

    private void OnVictoryReceived(int finalScore, int livesLeft)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(ShowVictory);
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

        GameConnection.VictoryReceived -= OnVictoryReceived;
        GameConnection.VictoryReceived += OnVictoryReceived;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameConnection.GameStarted -= OnGameStarted;
        UpgradeStore.UpgradePhaseStarted -= OnUpgradePhaseStarted;
        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        GameConnection.GameOverReceived -= OnGameOverReceived;
        GameConnection.VictoryReceived -= OnVictoryReceived;
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

    public void ShowVictory()
    {
        ContentHost.Content = new VictoryView(this);
    }

    private async void MainView_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= MainView_Loaded;

        // If opened via scanning the Host QR code (?code=123456), jump straight to JoinView
        string? urlCode = BrowserInput.GetUrlLobbyCode();
        if (!string.IsNullOrWhiteSpace(urlCode) && urlCode.Length == 6)
        {
            GameConnection.IsHost = false;
            GameConnection.PendingJoinCode = urlCode;
            ContentHost.Content = new JoinView(this);
        }

        try
        {
            await GameConnection.ConnectAsync(GameConnection.DefaultServerUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initial SignalR connect warning: {ex}");
        }

    }

    public void ShowMenu()
    {
        var menu = new MenuView();
        menu.HostClicked += (_, _) => ContentHost.Content = new HostView(this);
        menu.JoinClicked += (_, _) => ContentHost.Content = new JoinView(this);
        ContentHost.Content = menu;
    }

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
            return Prompt(message, defaultValue);
        return null;
    }

    public static string? GetUrlLobbyCode()
    {
        if (OperatingSystem.IsBrowser())
            return GetQueryCode();
        return null;
    }

    public static Task<string?> ScanQrCodeAsync()
    {
        if (OperatingSystem.IsBrowser())
            return ScanQrJs();
        return Task.FromResult<string?>(null);
    }

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.prompt")]
    private static partial string? Prompt(string message, string defaultValue);

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.getLobbyCodeFromUrl")]
    private static partial string? GetQueryCode();

    [SupportedOSPlatform("browser")]
    [JSImport("globalThis.scanLobbyQrCode")]
    private static partial Task<string?> ScanQrJs();
}

