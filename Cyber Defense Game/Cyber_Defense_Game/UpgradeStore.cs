using System;
using System.Collections.Generic;
using System.Linq;

namespace Cyber_Defense_Game;

public record UpgradeOption(int Id, string Name, string Description);

public static class UpgradeStore
{
    public static readonly List<UpgradeOption> AllUpgrades = new()
    {
        new(0, "Patch Upgrade", "Herstelt verloren verdediging/levens van het team.\n\nTactische keuze: Kiezen als het team fouten heeft gemaakt en dreigt te verliezen."),
        new(1, "Extra Time", "Geeft extra denktijd bij de komende vragen.\n\nTactische keuze: Kiezen als de vragen moeilijker worden en er meer overleg nodig is."),
        new(2, "Threat Scanner", "Streept 1 fout antwoord weg bij de volgende vraag.\n\nTactische keuze: Kiezen om de kans op een goed antwoord te vergroten (van 25% naar 33%)."),
        new(3, "Overclock", "Geeft 2x punten bij elk goed antwoord in de volgende ronde.\n\nTactische keuze: Kiezen als het team goed bezig is en snel extra punten wil sparen.")
    };

    public static UpgradeOption Option1 { get; private set; } = AllUpgrades[0];
    public static UpgradeOption Option2 { get; private set; } = AllUpgrades[1];

    public static int QuestionsPlayed { get; set; } = 0;
    public static int QuestionIndex { get; set; } = 0;
    public static int TeamScore { get; set; } = 0;

    public static event Action? UpgradePhaseStarted;

    public static (int FirstId, int SecondId) PickTwoRandom()
    {
        var picked = AllUpgrades.OrderBy(_ => Random.Shared.Next()).Take(2).ToList();
        SetActiveUpgrades(picked[0].Id, picked[1].Id);
        return (picked[0].Id, picked[1].Id);
    }

    public static void SetActiveUpgrades(int firstId, int secondId)
    {
        Option1 = AllUpgrades.FirstOrDefault(u => u.Id == firstId) ?? AllUpgrades[0];
        Option2 = AllUpgrades.FirstOrDefault(u => u.Id == secondId) ?? AllUpgrades[1];
        UpgradePhaseStarted?.Invoke();
    }
}
