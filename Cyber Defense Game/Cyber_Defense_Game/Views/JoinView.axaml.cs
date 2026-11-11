using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Cyber_Defense_Game.Views;

public partial class JoinView : UserControl
{
    private readonly MainView? _main;

    public JoinView() => InitializeComponent();
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
        catch (System.Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
            System.Console.WriteLine(ex.ToString());
        }
    }

    //private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    //{
    //    if (sender is TextBox textBox)
    //    {
    //        textBox.SelectAll();
    //    }
    //}
    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}