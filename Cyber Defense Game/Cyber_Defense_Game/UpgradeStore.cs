using System;
using System.Collections.Generic;
using System.Linq;

namespace Cyber_Defense_Game;

public record UpgradeOption(int Id, string Name, int Cost, string Description);

public static class UpgradeStore
{
    // Set each upgrade's Cost manually right here!
    public static readonly List<UpgradeOption> AllUpgrades = new()
    {
        new(0, "Patch Upgrade", 150, "Herstelt verloren verdediging/levens van het team.\n\nTactische keuze: Kiezen als het team fouten heeft gemaakt en dreigt te verliezen."),
        new(1, "Extra Time", 100, "Geeft extra denktijd bij de komende vragen.\n\nTactische keuze: Kiezen als de vragen moeilijker worden en er meer overleg nodig is."),
        new(2, "Threat Scanner", 120, "Streept 1 fout antwoord weg bij de volgende vraag.\n\nTactische keuze: Kiezen om de kans op een goed antwoord te vergroten (van 25% naar 33%)."),
        new(3, "Overclock", 200, "Geeft 2x punten bij elk goed antwoord in de volgende ronde.\n\nTactische keuze: Kiezen als het team goed bezig is en snel extra punten wil sparen.")
    };

    public static UpgradeOption? Option1 { get; private set; }
    public static UpgradeOption? Option2 { get; private set; }

    public static List<QuizQuestion> GameQueue { get; private set; } = new();

    public static int QuestionsPlayed { get; set; } = 0;
    public static int QuestionIndex { get; set; } = 0;
    public static int TeamScore { get; set; } = 0;
    public static int TeamLives { get; set; } = 3;

    public static int ExtraTimeRoundsLeft { get; set; } = 0;
    public static bool ThreatScannerActive { get; set; } = false;
    public static bool OverclockActive { get; set; } = false;
    public static int EliminatedOptionIndex { get; set; } = -1;

    public static event Action? UpgradePhaseStarted;

    public static void ResetGame()
    {
        QuestionsPlayed = 0;
        QuestionIndex = 0;
        TeamScore = 0;
        TeamLives = 3;
        ExtraTimeRoundsLeft = 0;
        ThreatScannerActive = false;
        OverclockActive = false;
        EliminatedOptionIndex = -1;
        GameQueue.Clear();
    }

    public static void BuildGameQueue()
    {
        var all = QuestionStore.GetAll();
        var queue = new List<QuizQuestion>();

        string[] tiers = { "Easy", "Normal", "Hard", "IMPOSSIBLE" };

        foreach (var tier in tiers)
        {
            var picked = all
                .Where(q => string.Equals(q.Difficulty, tier, StringComparison.OrdinalIgnoreCase) ||
                            (tier == "IMPOSSIBLE" && string.Equals(q.Difficulty, "IMPOSIBLME", StringComparison.OrdinalIgnoreCase)))
                .OrderBy(_ => Random.Shared.Next())
                .Take(5)
                .ToList();

            queue.AddRange(picked);
        }

        // Fallback if no questions matched the difficulty names
        if (queue.Count == 0 && all.Count > 0)
        {
            queue.AddRange(all.OrderBy(_ => Random.Shared.Next()).Take(20));
        }

        GameQueue = queue;
        QuestionIndex = 0;
    }

    public static void EnsureGameQueue()
    {
        if (GameQueue.Count == 0)
        {
            BuildGameQueue();
        }
    }

    public static (int FirstId, int SecondId) PickTwoAffordable()
    {
        var affordable = AllUpgrades
            .Where(u => u.Cost <= TeamScore)
            .OrderBy(_ => Random.Shared.Next())
            .Take(2)
            .ToList();

        int id1 = affordable.Count > 0 ? affordable[0].Id : -1;
        int id2 = affordable.Count > 1 ? affordable[1].Id : -1;

        SetActiveUpgrades(id1, id2);
        return (id1, id2);
    }

    public static void SetActiveUpgrades(int firstId, int secondId)
    {
        Option1 = firstId >= 0 ? AllUpgrades.FirstOrDefault(u => u.Id == firstId) : null;
        Option2 = secondId >= 0 ? AllUpgrades.FirstOrDefault(u => u.Id == secondId) : null;
        UpgradePhaseStarted?.Invoke();
    }

    public static void ApplyWinningUpgrade(UpgradeOption winner)
    {
        if (TeamScore < winner.Cost) return;

        TeamScore -= winner.Cost;

        switch (winner.Id)
        {
            case 0: // Patch Upgrade
                TeamLives = Math.Min(3, TeamLives + 1);
                break;
            case 1: // Extra Time (next 5 questions)
                ExtraTimeRoundsLeft = 5;
                break;
            case 2: // Threat Scanner (next question)
                ThreatScannerActive = true;
                break;
            case 3: // Overclock (next question)
                OverclockActive = true;
                break;
        }
    }
}
