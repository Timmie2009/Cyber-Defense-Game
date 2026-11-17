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
    private bool _connectStarted;

    public HostView()
    {
        InitializeComponent();
    }

    public HostView(MainView main) : this() => _main = main;

    private async Task ConnectToServerAsync()
    {
        try
        {
            await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net/");
            await GameConnection.RegisterAsHostAsync();
            Console.WriteLine("SignalR connected");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SignalR failed: {ex}");
        }
    }

    private void OnImageChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(LoadImage);
    }

    private void OnPlayersUpdated(List<PlayerInfo> players)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            PlayerList.ItemsSource = players.Select(p => p.Name).ToList());
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

        if (!_connectStarted)
        {
            _connectStarted = true;
            _ = ConnectToServerAsync();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameState.ImageChanged -= OnImageChanged;
        GameConnection.PlayersUpdated -= OnPlayersUpdated;
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

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}
