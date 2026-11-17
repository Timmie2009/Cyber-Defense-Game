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
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            PlayerList.ItemsSource = players.ToList());
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        GameState.ImageChanged -= OnImageChanged;
        GameState.ImageChanged += OnImageChanged;

        GameConnection.PlayersUpdated -= OnPlayersUpdated;
        GameConnection.PlayersUpdated += OnPlayersUpdated;
        OnPlayersUpdated(GameConnection.CurrentPlayers);

        LoadImage();

        _ = ConnectToServerAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
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
        BrowserMusic.Stop();
        _main?.ShowMenu();
    }
}

