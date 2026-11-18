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

    private static readonly Dictionary<string, QuizQuestion> _questionsByName =
        new(StringComparer.OrdinalIgnoreCase);

    public static string CurrentQuestionName { get; private set; } = "Sky Color";
    public static event Action? QuestionsChanged;
    public static event Action<string>? QuestionLoaded;

    static QuestionStore()
    {
        LoadLocalFiles();
        if (_questionsByName.Count == 0)
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
    public static void RemoveLocal(string questionName)
    {
        if (string.IsNullOrWhiteSpace(questionName)) return;

        _questionsByName.Remove(questionName.Trim());
        DeleteQuestionFile(questionName);
        QuestionsChanged?.Invoke();
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

    public static List<QuizQuestion> GetAll() => _questionsByName.Values.ToList();

    public static QuizQuestion GetByName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name) && _questionsByName.TryGetValue(name, out var q))
            return q;

        return _questionsByName.Values.First();
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

        _questionsByName[question.Title.Trim()] = question;
        SaveQuestionFile(question);
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
            var json = JsonSerializer.Serialize(question, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }

    private static void LoadLocalFiles()
    {
        if (OperatingSystem.IsBrowser()) return;

        try
        {
            if (!Directory.Exists(QuestionsFolder)) return;
            foreach (var file in Directory.EnumerateFiles(QuestionsFolder, "*.json"))
            {
                var q = JsonSerializer.Deserialize<QuizQuestion>(File.ReadAllText(file));
                if (q != null && !string.IsNullOrWhiteSpace(q.Title))
                    _questionsByName[q.Title.Trim()] = q;
            }
        }
        catch { }
    }
}
