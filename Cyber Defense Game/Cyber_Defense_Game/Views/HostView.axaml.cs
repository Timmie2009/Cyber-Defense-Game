using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cyber_Defense_Game.Views;

public partial class HostView : UserControl
{
    private readonly MainView? _main;
    private bool _lobbyOpen;
    private int _playerCount;

    public HostView()
    {
        InitializeComponent();
    }

    public HostView(MainView main) : this() => _main = main;

    private async Task ConnectToServerAsync()
    {
        try
        {
            await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net");
            await GameConnection.RegisterAsHostAsync();
            Console.WriteLine("Host registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host SignalR failed: {ex}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                PlayerList.ItemsSource = new List<string> { $"Error: {ex.Message}" });
        }
    }

    private void OnImageChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(LoadImage);
    }

    private void OnPlayersUpdated(List<string> players)
    {
        var snapshot = players.ToList();
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!_lobbyOpen)
                return;

            if (snapshot.Count > _playerCount)
                BrowserMusic.PlayEffectSafe("player_join");
            else if (snapshot.Count < _playerCount)
                BrowserMusic.PlayEffectSafe("player_leave");

            _playerCount = snapshot.Count;
            PlayerList.ItemsSource = snapshot;
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _lobbyOpen = true;
        var players = GameConnection.CurrentPlayers.ToList();
        _playerCount = players.Count;
        PlayerList.ItemsSource = players;

        GameState.ImageChanged -= OnImageChanged;
        GameState.ImageChanged += OnImageChanged;

        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        GameConnection.PlayersUpdated += OnPlayersUpdated;

        LoadImage();

        _ = ConnectToServerAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _lobbyOpen = false;
        BrowserMusic.StopSafe();
        GameState.ImageChanged -= OnImageChanged;
        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        GameConnection.IsHost = true;
        base.OnDetachedFromVisualTree(e);
    }

    private void LoadImage()
    {
        try
        {
            var uri = new Uri(GameState.GetImagePath(GameState.SelectedImage));
            if (AssetLoader.Exists(uri))
            {
                using var stream = AssetLoader.Open(uri);
                DisplayImage.Source = new Bitmap(stream);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadImage failed: {ex}");
        }
    }


    private async void Play_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            await GameConnection.StartGameAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StartGame failed: {ex}");
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }
}

