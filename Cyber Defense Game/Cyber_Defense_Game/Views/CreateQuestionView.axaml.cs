using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cyber_Defense_Game.Views;

public partial class CreateQuestionView : UserControl
{
    private readonly MainView? _main;
    private List<QuizQuestion> _displayedQuestions = new();
    private string? _editingOriginalTitle;

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
        _displayedQuestions = QuestionStore.GetAll();
        SavedQuestionsList.ItemsSource = _displayedQuestions
            .Select((q, i) => $"{i + 1}. [{q.Difficulty}] [{q.Title}] {q.QuestionText}")
            .ToList();
    }

    private void SavedQuestionsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        int index = SavedQuestionsList.SelectedIndex;
        if (index < 0 || index >= _displayedQuestions.Count) return;

        var selected = _displayedQuestions[index];
        _editingOriginalTitle = selected.Title;

        HeaderText.Text = $"Editing: {selected.Title}";
        SaveButton.Content = "Update Question";

        TitleInput.Text = selected.Title;
        QuestionInput.Text = selected.QuestionText;
        OptionAInput.Text = selected.OptionA;
        OptionBInput.Text = selected.OptionB;
        OptionCInput.Text = selected.OptionC;
        OptionDInput.Text = selected.OptionD;

        DifficultyBox.SelectedIndex = selected.Difficulty switch
        {
            "Easy" => 0,
            "Hard" => 2,
            _ => 1
        };

        CorrectAnswerBox.SelectedIndex = selected.CorrectIndex >= 0 && selected.CorrectIndex <= 3
            ? selected.CorrectIndex
            : 0;

        StatusText.Text = "";
    }

    private void Clear_Click(object? sender, RoutedEventArgs e) => ResetForm();

    private void ResetForm()
    {
        _editingOriginalTitle = null;
        SavedQuestionsList.SelectedIndex = -1;

        HeaderText.Text = "Create a Question";
        SaveButton.Content = "Save Question";

        TitleInput.Text = "";
        QuestionInput.Text = "";
        OptionAInput.Text = "";
        OptionBInput.Text = "";
        OptionCInput.Text = "";
        OptionDInput.Text = "";
        DifficultyBox.SelectedIndex = 1;
        CorrectAnswerBox.SelectedIndex = 0;
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
        string newTitle = TitleInput.Text.Trim();

        var updatedQuestion = new QuizQuestion
        {
            Title = newTitle,
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
            // If the title was renamed while editing, delete the old <OldTitle>.json file
            if (!string.IsNullOrEmpty(_editingOriginalTitle) &&
                !string.Equals(_editingOriginalTitle, newTitle, StringComparison.OrdinalIgnoreCase))
            {
                QuestionStore.RemoveLocal(_editingOriginalTitle);
                await GameConnection.DeleteQuestionAsync(_editingOriginalTitle);
            }

            QuestionStore.AddOrUpdateLocal(updatedQuestion);
            await GameConnection.SaveQuestionAsync(updatedQuestion);

            bool wasEditing = !string.IsNullOrEmpty(_editingOriginalTitle);
            ResetForm();

            StatusText.Foreground = Brushes.Green;
            StatusText.Text = wasEditing
                ? $"Updated '{updatedQuestion.Title}.json'!"
                : $"Saved '{updatedQuestion.Title}.json'!";
            RefreshList();
        }
        catch (Exception ex)
        {
            StatusText.Foreground = Brushes.Red;
            StatusText.Text = $"Save failed: {ex.Message}";
        }
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        int index = SavedQuestionsList.SelectedIndex;
        if (index < 0 || index >= _displayedQuestions.Count)
        {
            StatusText.Foreground = Brushes.Red;
            StatusText.Text = "Select a question from the list first!";
            return;
        }

        string titleToDelete = _displayedQuestions[index].Title;

        try
        {
            QuestionStore.RemoveLocal(titleToDelete);
            await GameConnection.DeleteQuestionAsync(titleToDelete);

            ResetForm();
            StatusText.Foreground = Brushes.Green;
            StatusText.Text = $"Deleted '{titleToDelete}.json'!";
            RefreshList();
        }
        catch (Exception ex)
        {
            StatusText.Foreground = Brushes.Red;
            StatusText.Text = $"Delete failed: {ex.Message}";
        }
    }

    private void Back_Click(object? sender, RoutedEventArgs e) => _main?.ShowMenu();
}
