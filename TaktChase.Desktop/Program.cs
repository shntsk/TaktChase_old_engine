using System;
using TaktChase.Game.Startup;

namespace TaktChase.Desktop;

public static class Program
{
    /// <summary>
    /// Thin entry point, as in the official template. All renderer selection,
    /// failure diagnostics, and fallback logic lives in <see cref="TaktChaseLauncher"/>
    /// (Game project), where it can be tested without a GPU.
    /// </summary>
    [STAThread]
    public static int Main(string[] args) => TaktChaseLauncher.Run(args);
}
