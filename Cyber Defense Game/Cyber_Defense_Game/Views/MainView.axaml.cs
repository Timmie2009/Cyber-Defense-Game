using System;
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

    private async void MainView_Loaded(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Loaded -= MainView_Loaded;

        try
        {
            await GameConnection.ConnectAsync("http://145.93.116.9/");
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