using Avalonia;
using Avalonia.Controls;
using System.Collections.ObjectModel;

namespace Cyber_Defense_Game.Views;

public partial class HostQuestionView : UserControl
{
    private readonly MainView? _main;
    private readonly ObservableCollection<string> _receivedAnswers = new();

    public HostQuestionView()
    {
        InitializeComponent();
        AnswersList.ItemsSource = _receivedAnswers;
    }

    public HostQuestionView(MainView main) : this() => _main = main;

    private void OnAnswerReceived(string connectionId, string name, int answerIndex)
    {
        string[] options = { "Red", "Green", "Blue", "White" };
        string chosen = answerIndex >= 0 && answerIndex < options.Length ? options[answerIndex] : "Unknown";

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            _receivedAnswers.Add($"{name} answered: {chosen}"));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        GameConnection.AnswerReceived -= OnAnswerReceived;
        GameConnection.AnswerReceived += OnAnswerReceived;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        GameConnection.AnswerReceived -= OnAnswerReceived;
        base.OnDetachedFromVisualTree(e);
    }
}
