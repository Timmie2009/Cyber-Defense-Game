using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Cyber_Defense_Game.Views;

public partial class MenuView : UserControl
{
    public event EventHandler? HostClicked;
    public event EventHandler? JoinClicked;

    public MenuView() => InitializeComponent();

    private void Host_Click(object? sender, RoutedEventArgs e) => HostClicked?.Invoke(this, EventArgs.Empty);
    private void Join_Click(object? sender, RoutedEventArgs e) => JoinClicked?.Invoke(this, EventArgs.Empty);
}