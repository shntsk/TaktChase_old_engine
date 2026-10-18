using System.Reflection;

namespace TaktChase.Resources;

/// <summary>
/// Entry point for TaktChase's embedded resources (textures).
/// Follows the same pattern as the official osu-framework-game template.
/// </summary>
public static class TaktChaseResources
{
    public static Assembly ResourceAssembly => typeof(TaktChaseResources).Assembly;
}
