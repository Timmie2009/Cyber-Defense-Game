using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;

namespace Cyber_Defense_Game.Android;

[Activity(
    Label = "Cyber_Defense_Game.Android",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode, ScreenOrientation = ScreenOrientation.Landscape)]
public class MainActivity : AvaloniaMainActivity
{
}
