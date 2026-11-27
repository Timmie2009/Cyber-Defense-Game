using System.Collections.Generic;

namespace Cyber_Defense_Game;

public class QuizQuestion
{
    public string Title { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public int CorrectIndex { get; set; } // 0 = A, 1 = B, 2 = C, 3 = D
}

public static class QuestionStore
{
    public static List<QuizQuestion> Questions { get; } = new()
    {
        new QuizQuestion
        {
            Title = "Sky Color",
            QuestionText = "What color is the sky?",
            OptionA = "Red",
            OptionB = "Green",
            OptionC = "Blue",
            OptionD = "White",
            CorrectIndex = 2
        }
    };

    public static int CurrentIndex { get; set; } = 0;

    public static QuizQuestion GetCurrentQuestion()
    {
        if (Questions.Count == 0)
        {
            return new QuizQuestion
            {
                Title = "Default",
                QuestionText = "What color is the sky?",
                OptionA = "Red",
                OptionB = "Green",
                OptionC = "Blue",
                OptionD = "White",
                CorrectIndex = 2
            };
        }

        if (CurrentIndex >= Questions.Count)
            CurrentIndex = 0;

        return Questions[CurrentIndex];
    }

    public static void Next()
    {
        if (Questions.Count == 0) return;
        CurrentIndex = (CurrentIndex + 1) % Questions.Count;
    }
}
