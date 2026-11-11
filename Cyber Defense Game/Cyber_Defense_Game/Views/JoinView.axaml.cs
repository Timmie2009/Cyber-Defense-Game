using Avalonia.Controls;
using Avalonia.Input;
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

    private void Select(int index)
    {
        GameState.SelectedImage = index;
        StatusText.Text = $"Host image set to {index}";
    }

    private void OnNameTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            GameState.PlayerName = textBox.Text;
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}