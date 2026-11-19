using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Cyber_Defense_Game.Views;

public partial class JoinView : UserControl
{
    private readonly MainView? _main;
    private string _enteredName = string.Empty;

    public JoinView()
    {
        InitializeComponent();
        NameInput.PointerPressed += (_, _) => NameInput.Focus();
        NameInput.TextChanged += (_, _) => _enteredName = NameInput.Text ?? string.Empty;
    }

    public JoinView(MainView main) : this() => _main = main;

    private void Image1_Click(object? sender, RoutedEventArgs e) => Select(1);
    private void Image2_Click(object? sender, RoutedEventArgs e) => Select(2);
    private void Image3_Click(object? sender, RoutedEventArgs e) => Select(3);

    private async void Select(int index)
    {
        try
        {
            await GameConnection.ChangeImageAsync(index);

            StatusText.Text = index == 2
                ? "🚨 ALERT: Björn heeft de frikandel gestolen! 🌭"
                : $"Image {index} sent to server!";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
            Console.WriteLine(ex.ToString());
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e)
    {
        _main?.ShowMenu();
    }

    private async void Submit_Click(object? sender, RoutedEventArgs e)
    {
        // Shift focus so mobile virtual keyboards finalize IME composition
        SubmitNameButton.Focus();

        string playerName = !string.IsNullOrWhiteSpace(NameInput.Text)
            ? NameInput.Text.Trim()
            : !string.IsNullOrWhiteSpace(_enteredName)
                ? _enteredName.Trim()
                : "Player";

        try
        {
            GameConnection.IsHost = false;
            await GameConnection.ConnectAsync("https://cyberquiz.tail9822f1.ts.net");
            await GameConnection.JoinLobbyAsync(playerName);
            StatusText.Text = $"Joined the lobby as {playerName}!";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
        }
    }
}
