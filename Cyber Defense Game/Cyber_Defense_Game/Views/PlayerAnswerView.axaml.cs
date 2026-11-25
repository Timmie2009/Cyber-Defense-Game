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
    private int _selectedAnswerIndex = -1;

    public PlayerAnswerView()
    {
        InitializeComponent();
    }

    public PlayerAnswerView(MainView main) : this() => _main = main;

    private void ResetButtons()
    {
        Button[] buttons = { RedButton, GreenButton, BlueButton, WhiteButton };
        foreach (var btn in buttons)
        {
            btn.IsEnabled = true;
            btn.Opacity = 1.0;
        }
    }

    private void OnQuestionLoaded(string questionName)
    {
        var q = QuestionStore.GetByName(questionName);

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _hasAnswered = false;
            _selectedAnswerIndex = -1;
            SelectedImage.IsVisible = false;
            SelectedImage.Source = null;
            ResetButtons();
            AnswerGrid.IsVisible = true;
            StatusText.Text = $"[{q.Difficulty}] {q.Title} - Pick an answer!";
        });
    }

    private void OnOptionEliminated(int wrongIndex)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Button[] buttons = { RedButton, GreenButton, BlueButton, WhiteButton };
            if (wrongIndex >= 0 && wrongIndex < buttons.Length)
            {
                buttons[wrongIndex].IsEnabled = false;
                buttons[wrongIndex].Opacity = 0.2;
                StatusText.Text = "Threat Scanner removed 1 wrong answer!";
            }
        });
    }

    private void OnRoundEnded()
    {
        var current = QuestionStore.GetCurrentQuestion();
        bool isCorrect = _hasAnswered && _selectedAnswerIndex == current.CorrectIndex;

        string resultAsset = isCorrect
            ? "avares://Cyber_Defense_Game/Assets/Goed%20quizstickers.png"
            : "avares://Cyber_Defense_Game/Assets/Fout%20quizstickers.png";

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            try
            {
                using var stream = AssetLoader.Open(new Uri(resultAsset));
                SelectedImage.Source = new Bitmap(stream);
                AnswerGrid.IsVisible = false;
                SelectedImage.IsVisible = true;

                StatusText.Text = isCorrect
                    ? "Correct! 🎉"
                    : _hasAnswered ? "Wrong! ❌" : "Time's up! ❌";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Image load failed: {ex.Message}";
            }
        });
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        QuestionStore.QuestionLoaded += OnQuestionLoaded;

        GameConnection.OptionEliminated -= OnOptionEliminated;
        GameConnection.OptionEliminated += OnOptionEliminated;

        GameConnection.RoundEnded -= OnRoundEnded;
        GameConnection.RoundEnded += OnRoundEnded;

        ResetButtons();
        var current = QuestionStore.GetCurrentQuestion();
        StatusText.Text = $"[{current.Difficulty}] {current.Title} - Pick an answer!";
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        QuestionStore.QuestionLoaded -= OnQuestionLoaded;
        GameConnection.OptionEliminated -= OnOptionEliminated;
        GameConnection.RoundEnded -= OnRoundEnded;
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
        _selectedAnswerIndex = index;

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
            _selectedAnswerIndex = -1;
            StatusText.Text = $"Error: {ex.Message}";
        }
    }
}
