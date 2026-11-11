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

        GameState.ImageChanged += OnImageChanged;
    }

    private void OnImageChanged()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(LoadImage);
    }

    protected override void OnDetachedFromVisualTree(
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        GameState.ImageChanged -= OnImageChanged;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnAttachedToVisualTree(
        Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        GameState.ImageChanged -= OnImageChanged;
        GameState.ImageChanged += OnImageChanged;
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