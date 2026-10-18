using osu.Framework;
using osu.Framework.Platform;

namespace TaktChase.Game.Tests;

public static class Program
{
    public static void Main()
    {
        using (GameHost host = Host.GetSuitableDesktopHost("TaktChaseVisualTests"))
        using (var game = new TaktChaseTestBrowser())
            host.Run(game);
    }
}
