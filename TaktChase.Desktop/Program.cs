using System;
using TaktChase.Game.Startup;

namespace TaktChase.Desktop;

public static class Program
{
    /// <summary>
    /// Ponto de entrada fino, como no template oficial. Toda a lógica de seleção de
    /// renderer, diagnóstico de falha e fallback fica em <see cref="TaktChaseLauncher"/>
    /// (projeto Game), onde pode ser testada sem GPU.
    /// </summary>
    [STAThread]
    public static int Main(string[] args) => TaktChaseLauncher.Run(args);
}
