using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Cyber_Defense_Game.Views;

public partial class HostView : UserControl
{
    private readonly MainView? _main;
    private bool _lobbyOpen;
    private int _playerCount;

    private readonly ListBox _playerList;
    private readonly TextBlock _lobbyCodeText;
    private readonly Avalonia.Controls.Image _qrCodeImage;
    private readonly Avalonia.Controls.Image _displayImage;

    public HostView()
    {
        var playBtn = new Button { Content = "Play" };
        playBtn.Click += Play_Click;

        var backBtn = new Button { Content = "Back" };
        backBtn.Click += Back_Click;

        var topPanel = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 20),
            Children =
            {
                new TextBlock
                {
                    Text = "Host Lobby",
                    FontSize = 26,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 10,
                    Children = { playBtn, backBtn }
                }
            }
        };
        DockPanel.SetDock(topPanel, Dock.Top);

        _playerList = new ListBox();
        var leftPanel = new StackPanel
        {
            Width = 200,
            Margin = new Thickness(0, 0, 20, 0),
            Spacing = 5,
            Children =
            {
                new TextBlock { Text = "Players", FontWeight = FontWeight.Bold },
                _playerList
            }
        };
        DockPanel.SetDock(leftPanel, Dock.Left);

        _lobbyCodeText = new TextBlock
        {
            Text = "------",
            FontSize = 36,
            FontWeight = FontWeight.Bold,
            LetterSpacing = 4,
            Foreground = Brushes.White,
            MinHeight = 46,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        _qrCodeImage = new Avalonia.Controls.Image
        {
            Width = 180,
            Height = 180,
            Stretch = Stretch.Uniform
        };

        var rightCard = new Border
        {
            Background = SolidColorBrush.Parse("#1A1F2E"),
            BorderBrush = SolidColorBrush.Parse("#00E5FF"),
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(16),
            Margin = new Thickness(20, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Children =
                {
                    new TextBlock
                    {
                        Text = "JOIN CODE",
                        FontSize = 14,
                        FontWeight = FontWeight.Bold,
                        Foreground = SolidColorBrush.Parse("#00E5FF"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    _lobbyCodeText,
                    new Border
                    {
                        Background = Brushes.White,
                        Padding = new Thickness(8),
                        CornerRadius = new CornerRadius(8),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Child = _qrCodeImage
                    },
                    new TextBlock
                    {
                        Text = "Scan QR or enter 6-digit code",
                        FontSize = 12,
                        Foreground = SolidColorBrush.Parse("#B0BEC5"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = GameConnection.DefaultServerUrl,
                        FontSize = 10,
                        Foreground = SolidColorBrush.Parse("#607D8B"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };
        DockPanel.SetDock(rightCard, Dock.Right);

        _displayImage = new Avalonia.Controls.Image { Stretch = Stretch.Uniform };

        Content = new DockPanel
        {
            Margin = new Thickness(20),
            Children = { topPanel, leftPanel, rightCard, _displayImage }
        };
    }

    public HostView(MainView main) : this() => _main = main;

    private async Task ConnectToServerAsync()
    {
        try
        {
            await GameConnection.ConnectAsync(GameConnection.DefaultServerUrl);
            string code = await GameConnection.RegisterAsHostAsync();

            if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
                code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
                GameConnection.CurrentLobbyCode = code;
            }

            string joinUrl = $"{GameConnection.DefaultServerUrl}/?code={code}";
            var qrBitmap = QrCodeGenerator.CreateBitmap(joinUrl, scale: 8);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                _lobbyCodeText.Text = code;
                _qrCodeImage.Source = qrBitmap;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host SignalR failed: {ex}");
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                _playerList.ItemsSource = new List<string> { $"Error: {ex.Message}" });
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
            _playerList.ItemsSource = snapshot;
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _lobbyOpen = true;
        var players = GameConnection.CurrentPlayers.ToList();
        _playerCount = players.Count;
        _playerList.ItemsSource = players;

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
                _displayImage.Source = new Bitmap(stream);
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
