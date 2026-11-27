using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cyber_Defense_Game;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class QuizQuestion
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("questionText")]
    public string QuestionText { get; set; } = string.Empty;

    [JsonPropertyName("optionA")]
    public string OptionA { get; set; } = string.Empty;

    [JsonPropertyName("optionB")]
    public string OptionB { get; set; } = string.Empty;

    [JsonPropertyName("optionC")]
    public string OptionC { get; set; } = string.Empty;

    [JsonPropertyName("optionD")]
    public string OptionD { get; set; } = string.Empty;

    [JsonPropertyName("correctIndex")]
    public int CorrectIndex { get; set; }

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; set; } = "Normal";
}

public static class QuestionStore
{
    private static readonly string QuestionsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberDefenseGame",
        "Questions");

    private static readonly List<QuizQuestion> _questions = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static string CurrentQuestionName { get; private set; } = "Sky Color";
    public static event Action? QuestionsChanged;
    public static event Action<string>? QuestionLoaded;

    static QuestionStore()
    {
        LoadLocalFiles();
        if (_questions.Count == 0)
        {
            AddOrUpdateLocal(new QuizQuestion
            {
                Title = "Sky Color",
                QuestionText = "What color is the sky?",
                OptionA = "Red",
                OptionB = "Green",
                OptionC = "Blue",
                OptionD = "White",
                CorrectIndex = 2,
                Difficulty = "Easy"
            });
        }
    }

    public static List<QuizQuestion> GetAll() => _questions.ToList();

    public static QuizQuestion GetByName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            var found = _questions.FirstOrDefault(q =>
                string.Equals(q.Title, name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (found != null) return found;
        }

        return _questions.First();
    }

    public static QuizQuestion GetCurrentQuestion() => GetByName(CurrentQuestionName);

    public static void SetActiveQuestion(string questionName)
    {
        CurrentQuestionName = questionName;
        QuestionLoaded?.Invoke(questionName);
    }

    public static void AddOrUpdateLocal(QuizQuestion question)
    {
        if (string.IsNullOrWhiteSpace(question.Title)) return;

        string trimmedTitle = question.Title.Trim();
        question.Title = trimmedTitle;

        int existingIndex = _questions.FindIndex(q =>
            string.Equals(q.Title, trimmedTitle, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _questions[existingIndex] = question;
        }
        else
        {
            _questions.Add(question);
        }

        SaveQuestionFile(question);
        QuestionsChanged?.Invoke();
    }

    public static void RemoveLocal(string questionName)
    {
        if (string.IsNullOrWhiteSpace(questionName)) return;

        _questions.RemoveAll(q =>
            string.Equals(q.Title, questionName.Trim(), StringComparison.OrdinalIgnoreCase));
        DeleteQuestionFile(questionName);
        QuestionsChanged?.Invoke();
    }

    private static string GetSafeFileName(string title)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(title.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(clean) ? "Untitled" : clean;
    }

    private static void SaveQuestionFile(QuizQuestion question)
    {
        if (OperatingSystem.IsBrowser()) return;

        try
        {
            Directory.CreateDirectory(QuestionsFolder);
            var path = Path.Combine(QuestionsFolder, $"{GetSafeFileName(question.Title)}.json");
            var json = JsonSerializer.Serialize(question, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch { }
    }

    private static void DeleteQuestionFile(string title)
    {
        if (OperatingSystem.IsBrowser()) return;

        try
        {
            var path = Path.Combine(QuestionsFolder, $"{GetSafeFileName(title)}.json");
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch { }
    }

    private static void LoadLocalFiles()
    {
        if (OperatingSystem.IsBrowser()) return;

        try
        {
            if (!Directory.Exists(QuestionsFolder)) return;

            var orderedFiles = Directory.EnumerateFiles(QuestionsFolder, "*.json")
                .OrderBy(File.GetCreationTimeUtc);

            foreach (var file in orderedFiles)
            {
                var q = JsonSerializer.Deserialize<QuizQuestion>(File.ReadAllText(file));
                if (q != null && !string.IsNullOrWhiteSpace(q.Title))
                    _questions.Add(q);
            }
        }
        catch { }
    }
}
