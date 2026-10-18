using System;
using System.Collections.Generic;

namespace TaktChase.Game.Startup;

/// <summary>
/// Translates a <see cref="RendererRequest"/> into the OFFICIAL osu-framework environment variables
/// (wiki "Environment variables"): <c>OSU_GRAPHICS_RENDERER</c> and <c>OSU_GRAPHICS_SURFACE</c>.
/// </summary>
/// <remarks>
/// The framework reads these variables in the static constructor of <c>FrameworkEnvironment</c>,
/// so they must be set BEFORE any call into the framework (before the host is created).
/// When either is set, <c>GameHost.ChooseAndSetupRenderer</c> uses exactly the requested
/// renderer/surface pair and fails permanently, with no internal fallback.
/// If only the surface is set, the assumed renderer is "veldrid"; if only the renderer is set,
/// the assumed surface is OpenGL. That is why TaktChase always sets BOTH variables.
/// An invalid surface value is silently ignored by the framework (Enum.TryParse), and an unknown renderer
/// value falls back to GLRenderer.
/// </remarks>
public static class RendererEnvironment
{
    public const string RENDERER_VARIABLE = "OSU_GRAPHICS_RENDERER";
    public const string SURFACE_VARIABLE = "OSU_GRAPHICS_SURFACE";

    /// <summary>
    /// Variables to set for the request. Nothing is set for <see cref="RendererRequest.Auto"/>,
    /// so the framework runs its own selection and its own fallback.
    /// </summary>
    public static IReadOnlyDictionary<string, string> GetVariables(RendererRequest request)
    {
        switch (request)
        {
            case RendererRequest.OpenGL:
                // GLRenderer (the framework's "legacy" OpenGL renderer); it only supports the opengl surface.
                // "gl" is the value with an explicit branch in GameHost.SetupRendererAndWindow(string, GraphicsSurfaceType);
                // "opengl" is also documented on the wiki, but it would only work by falling into the switch's default branch.
                return new Dictionary<string, string>
                {
                    [RENDERER_VARIABLE] = "gl",
                    [SURFACE_VARIABLE] = "opengl",
                };

            case RendererRequest.Vulkan:
                // VeldridRenderer + Vulkan surface.
                return new Dictionary<string, string>
                {
                    [RENDERER_VARIABLE] = "veldrid",
                    [SURFACE_VARIABLE] = "vulkan",
                };

            default:
                return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// True if the environment already carries a renderer/surface choice made outside TaktChase.
    /// In that case the framework obeys it (without fallback), even with <c>--renderer=auto</c>.
    /// </summary>
    public static bool HasExternalOverride(Func<string, string?> getEnvironmentVariable)
        => !string.IsNullOrEmpty(getEnvironmentVariable(RENDERER_VARIABLE))
           || !string.IsNullOrEmpty(getEnvironmentVariable(SURFACE_VARIABLE));
}
