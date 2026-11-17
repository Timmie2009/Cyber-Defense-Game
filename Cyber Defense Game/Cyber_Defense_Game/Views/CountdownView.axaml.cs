using Avalonia;
using Avalonia.Controls;
using System.Threading.Tasks;

namespace Cyber_Defense_Game.Views;

public partial class CountdownView : UserControl
{
    private readonly MainView? _main;

    public CountdownView()
    {
        InitializeComponent();
    }

    public CountdownView(MainView main) : this() => _main = main;

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        CountdownText.Text = "Game starting in 3";
        await Task.Delay(1000);

        CountdownText.Text = "2";
        await Task.Delay(1000);

        CountdownText.Text = "1";
        await Task.Delay(1000);

        CountdownText.Text = "GO";
        await Task.Delay(1000);

        // TODO: Navigate to your gameplay screen here
    }
}
