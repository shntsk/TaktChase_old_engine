using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// Estado de inicialização compartilhado entre o launcher e o jogo.
/// </summary>
public static class StartupState
{
    private static volatile bool completed;

    /// <summary>
    /// Verdadeiro depois que a janela, o renderer e a cena inicial foram criados com sucesso.
    /// Exceções posteriores a esse ponto NÃO são tratadas como falha de inicialização de renderer.
    /// </summary>
    public static bool Completed => completed;

    public static LaunchOptions Options { get; private set; } = LaunchOptions.Default;

    public static VulkanProbeResult? Probe { get; private set; }

    public static IReadOnlyList<string> Notes { get; private set; } = Array.Empty<string>();

    public static void Configure(LaunchOptions options, VulkanProbeResult? probe, IReadOnlyList<string> notes)
    {
        Options = options;
        Probe = probe;
        Notes = notes;
    }

    public static void MarkCompleted() => completed = true;
}
