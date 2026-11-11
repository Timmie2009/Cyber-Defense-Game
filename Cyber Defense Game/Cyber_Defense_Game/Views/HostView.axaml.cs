using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Cyber_Defense_Game.Views;

public partial class HostView : UserControl
{
    private readonly MainView? _main;

    public HostView()
    {
        InitializeComponent();
        LoadImage();
    }

    public HostView(MainView main) : this() => _main = main;

    private void LoadImage()
    {
        var uri = new Uri(GameState.GetImagePath(GameState.SelectedImage));
        if (AssetLoader.Exists(uri))
        {
            using var stream = AssetLoader.Open(uri);
            DisplayImage.Source = new Bitmap(stream);
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}