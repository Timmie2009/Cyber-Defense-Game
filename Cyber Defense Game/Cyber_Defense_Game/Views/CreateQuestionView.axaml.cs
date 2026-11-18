using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Linq;

namespace Cyber_Defense_Game.Views;

public partial class CreateQuestionView : UserControl
{
    private readonly MainView? _main;

    public CreateQuestionView()
    {
        InitializeComponent();
        RefreshList();
    }

    public CreateQuestionView(MainView main) : this() => _main = main;

    private void OnQuestionsChanged() =>
        Avalonia.Threading.Dispatcher.UIThread.Post(RefreshList);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        QuestionStore.QuestionsChanged -= OnQuestionsChanged;
        QuestionStore.QuestionsChanged += OnQuestionsChanged;
        RefreshList();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        QuestionStore.QuestionsChanged -= OnQuestionsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void RefreshList()
    {
        SavedQuestionsList.ItemsSource = QuestionStore.GetAll()
            .Select((q, i) => $"{i + 1}. [{q.Difficulty}] [{q.Title}] {q.QuestionText}")
            .ToList();
    }

    private async void Save_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleInput.Text) ||
            string.IsNullOrWhiteSpace(QuestionInput.Text) ||
            string.IsNullOrWhiteSpace(OptionAInput.Text) ||
            string.IsNullOrWhiteSpace(OptionBInput.Text) ||
            string.IsNullOrWhiteSpace(OptionCInput.Text) ||
            string.IsNullOrWhiteSpace(OptionDInput.Text))
        {
            StatusText.Foreground = Brushes.Red;
            StatusText.Text = "Please fill in all fields!";
            return;
        }

        string difficulty = (DifficultyBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Normal";

        var newQuestion = new QuizQuestion
        {
            Title = TitleInput.Text.Trim(),
            QuestionText = QuestionInput.Text.Trim(),
            OptionA = OptionAInput.Text.Trim(),
            OptionB = OptionBInput.Text.Trim(),
            OptionC = OptionCInput.Text.Trim(),
            OptionD = OptionDInput.Text.Trim(),
            CorrectIndex = CorrectAnswerBox.SelectedIndex,
            Difficulty = difficulty
        };

        try
        {
            // Save locally immediately and send to server to create <Title>.json
            QuestionStore.AddOrUpdateLocal(newQuestion);
            await GameConnection.SaveQuestionAsync(newQuestion);

            TitleInput.Text = "";
            QuestionInput.Text = "";
            OptionAInput.Text = "";
            OptionBInput.Text = "";
            OptionCInput.Text = "";
            OptionDInput.Text = "";
            DifficultyBox.SelectedIndex = 1;
            CorrectAnswerBox.SelectedIndex = 0;

            StatusText.Foreground = Brushes.Green;
            StatusText.Text = $"Saved '{newQuestion.Title}.json'!";
            RefreshList();
        }
        catch (Exception ex)
        {
            StatusText.Foreground = Brushes.Red;
            StatusText.Text = $"Save failed: {ex.Message}";
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}
