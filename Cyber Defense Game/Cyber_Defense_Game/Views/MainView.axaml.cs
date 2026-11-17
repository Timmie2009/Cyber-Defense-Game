using System;
using Avalonia;
using Avalonia.Controls;

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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        GameConnection.GameStarted -= OnGameStarted;
        GameConnection.GameStarted += OnGameStarted;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameConnection.GameStarted -= OnGameStarted;
        base.OnDetachedFromVisualTree(e);
    }

    public void ShowCountdown()
    {
        ContentHost.Content = new CountdownView(this);
    }

    private async void MainView_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= MainView_Loaded;

        try
        {
            await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net/");
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
        var menu = new MenuView();

        menu.HostClicked += (_, _) =>
            ContentHost.Content = new HostView(this);

        menu.JoinClicked += (_, _) =>
            ContentHost.Content = new JoinView(this);

        ContentHost.Content = menu;
    }
}
