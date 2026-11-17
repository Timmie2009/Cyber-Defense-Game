using System;
using Avalonia.Controls;
using System.Runtime.InteropServices.JavaScript;

namespace Cyber_Defense_Game.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        ShowMenu();

        Loaded += MainView_Loaded;
    }

    private async void MainView_Loaded(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
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
{
    BrowserMusic.Play();
    ContentHost.Content = new HostView(this);
};

        menu.JoinClicked += (_, _) =>
            ContentHost.Content = new JoinView(this);

        ContentHost.Content = menu;
    }
}


public static partial class BrowserMusic
{
    [JSImport("playHostMusic", "main")]
    public static partial void Play();

    [JSImport("stopHostMusic", "main")]
    public static partial void Stop();
}
