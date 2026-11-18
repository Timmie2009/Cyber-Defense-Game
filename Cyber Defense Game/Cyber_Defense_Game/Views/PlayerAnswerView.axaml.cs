using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
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

    private void OnQuestionLoaded(string questionName)
    {
        var q = QuestionStore.GetByName(questionName);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _hasAnswered = false;
            SelectedImage.IsVisible = false;
            SelectedImage.Source = null;
            AnswerGrid.IsVisible = true;
            StatusText.Text = $"[{q.Difficulty}] {q.Title} - Pick an answer!";
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        QuestionStore.QuestionLoaded += OnQuestionLoaded;

        var current = QuestionStore.GetCurrentQuestion();
        StatusText.Text = $"[{current.Difficulty}] {current.Title} - Pick an answer!";
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        base.OnDetachedFromVisualTree(e);
    }

    private void Red_Click(object? sender, RoutedEventArgs e) =>
        SendAnswer(0, "Red", "avares://Cyber_Defense_Game/Assets/Rood.png");

    private void Green_Click(object? sender, RoutedEventArgs e) =>
        SendAnswer(1, "Green", "avares://Cyber_Defense_Game/Assets/Groen.png");

    private void Blue_Click(object? sender, RoutedEventArgs e) =>
        SendAnswer(2, "Blue", "avares://Cyber_Defense_Game/Assets/Blauw.png");

    private void White_Click(object? sender, RoutedEventArgs e) =>
        SendAnswer(3, "White", "avares://Cyber_Defense_Game/Assets/Wit.png");

    private async void SendAnswer(int index, string label, string assetPath)
    {
        if (_hasAnswered) return;
        _hasAnswered = true;

        try
        {
            await GameConnection.SubmitAnswerAsync(index);

            using var stream = AssetLoader.Open(new Uri(assetPath));
            SelectedImage.Source = new Bitmap(stream);
            AnswerGrid.IsVisible = false;
            SelectedImage.IsVisible = true;

            StatusText.Text = $"You picked {label}! Waiting for other players...";
        }
        catch (Exception ex)
        {
            _hasAnswered = false;
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
