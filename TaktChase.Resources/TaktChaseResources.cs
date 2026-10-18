using System.Reflection;

namespace TaktChase.Resources;

/// <summary>
/// Ponto de acesso aos recursos embutidos (texturas) do TaktChase.
/// Segue o mesmo padrão do template oficial osu-framework-game.
/// </summary>
public static class TaktChaseResources
{
    public static Assembly ResourceAssembly => typeof(TaktChaseResources).Assembly;
}
