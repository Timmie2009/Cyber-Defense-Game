
using System;


using System.Runtime.Versioning;

using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Browser;
using Cyber_Defense_Game;

internal sealed partial class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            var info = new SkiaSharp.SKImageInfo(1, 1);
            Console.WriteLine("Skia OK: " + info);
        }
        catch (Exception ex)
        {
            Console.WriteLine("TOP: " + ex);
            for (var e = ex.InnerException; e != null; e = e.InnerException)
                Console.WriteLine("INNER: " + e);
            if (ex is AggregateException ag)
                foreach (var i in ag.InnerExceptions)
                    Console.WriteLine("AGG: " + i);
        }

        await JSHost.ImportAsync("main", "../music.js");


        await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}
