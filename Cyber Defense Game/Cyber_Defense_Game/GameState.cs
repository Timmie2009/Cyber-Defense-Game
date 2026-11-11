using System;

namespace Cyber_Defense_Game;

public static class GameState
{
    private static int _selectedImage = 1;

    public static event Action? ImageChanged;

    public static int SelectedImage
    {
        get => _selectedImage;
        set
        {
            _selectedImage = value;
            ImageChanged?.Invoke();
        }
    }

    public static string GetImagePath(int index) => index switch
    {
        1 => "avares://Cyber_Defense_Game/Assets/Brorn2.png",
        2 => "avares://Cyber_Defense_Game/Assets/Brorn3.png",
        3 => "avares://Cyber_Defense_Game/Assets/FrikandelBjorn.png",
        _ => "avares://Cyber_Defense_Game/Assets/website_in_aanbouw.png"
    };
}