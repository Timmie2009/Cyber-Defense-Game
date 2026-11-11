using Avalonia.Controls;

namespace Cyber_Defense_Game.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        ShowMenu();
    }

    public void ShowMenu()
    {
        var menu = new MenuView();
        menu.HostClicked += (_, _) => ContentHost.Content = new HostView(this);
        menu.JoinClicked += (_, _) => ContentHost.Content = new JoinView(this);
        ContentHost.Content = menu;
    }
}