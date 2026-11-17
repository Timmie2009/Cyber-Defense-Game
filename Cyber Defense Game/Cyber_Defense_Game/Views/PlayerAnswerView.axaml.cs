using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Cyber_Defense_Game.Views;

public partial class PlayerAnswerView : UserControl
{
    private readonly MainView? _main;
    private bool _hasAnswered;

    public PlayerAnswerView()
    {
        InitializeComponent();
    }

    public PlayerAnswerView(MainView main) : this() => _main = main;

    private void Red_Click(object? sender, RoutedEventArgs e) => SendAnswer(0, "Red");
    private void Green_Click(object? sender, RoutedEventArgs e) => SendAnswer(1, "Green");
    private void Blue_Click(object? sender, RoutedEventArgs e) => SendAnswer(2, "Blue");
    private void White_Click(object? sender, RoutedEventArgs e) => SendAnswer(3, "White");

    private async void SendAnswer(int index, string label)
    {
        if (_hasAnswered) return;
        _hasAnswered = true;

        try
        {
            await GameConnection.SubmitAnswerAsync(index);
            StatusText.Text = $"You picked {label}! Waiting for other players...";
        }
        catch (Exception ex)
        {
            _hasAnswered = false;
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
