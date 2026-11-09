namespace Cyber_Defense_Game;

public static class GameState
{
    // Which image the host screen shows (1, 2 or 3)
    public static int SelectedImage { get; set; } = 1;

    public static string GetImagePath(int index) => index switch
    {
        1 => "avares://Cyber_Defense_Game/Assets/Brorn2.png",
        2 => "avares://Cyber_Defense_Game/Assets/Brorn3.png",
        3 => "avares://Cyber_Defense_Game/Assets/FrikandelBjorn.png",
        _ => "avares://Cyber_Defense_Game/Assets/website_in_aanbouw.png"
    };
}