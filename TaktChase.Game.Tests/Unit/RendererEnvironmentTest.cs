using System;
using NUnit.Framework;
using osu.Framework.Platform;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Tests.Unit;

[TestFixture]
public class RendererEnvironmentTest
{
    [Test]
    public void TestAutoDefinesNothing()
    {
        Assert.That(RendererEnvironment.GetVariables(RendererRequest.Auto), Is.Empty);
    }

    [Test]
    public void TestOpenGLRequestsGLRendererWithOpenGLSurface()
    {
        var variables = RendererEnvironment.GetVariables(RendererRequest.OpenGL);

        Assert.That(variables[RendererEnvironment.RENDERER_VARIABLE], Is.EqualTo("gl"));
        Assert.That(variables[RendererEnvironment.SURFACE_VARIABLE], Is.EqualTo("opengl"));
    }

    [Test]
    public void TestVulkanRequestsVeldridRendererWithVulkanSurface()
    {
        var variables = RendererEnvironment.GetVariables(RendererRequest.Vulkan);

        Assert.That(variables[RendererEnvironment.RENDERER_VARIABLE], Is.EqualTo("veldrid"));
        Assert.That(variables[RendererEnvironment.SURFACE_VARIABLE], Is.EqualTo("vulkan"));
    }

    [TestCase(RendererRequest.OpenGL)]
    [TestCase(RendererRequest.Vulkan)]
    public void TestSurfaceValueIsParsedByTheFrameworkEnum(RendererRequest request)
    {
        // The framework reads OSU_GRAPHICS_SURFACE with Enum.TryParse<GraphicsSurfaceType>(ignoreCase: true) and silently
        // ignores invalid values; this test guarantees that the value we set is actually accepted.
        string value = RendererEnvironment.GetVariables(request)[RendererEnvironment.SURFACE_VARIABLE];

        Assert.That(Enum.TryParse<GraphicsSurfaceType>(value, true, out _), Is.True);
    }

    [Test]
    public void TestExternalOverrideDetection()
    {
        Assert.That(RendererEnvironment.HasExternalOverride(_ => null), Is.False);
        Assert.That(RendererEnvironment.HasExternalOverride(_ => string.Empty), Is.False);
        Assert.That(RendererEnvironment.HasExternalOverride(name => name == RendererEnvironment.RENDERER_VARIABLE ? "veldrid" : null), Is.True);
        Assert.That(RendererEnvironment.HasExternalOverride(name => name == RendererEnvironment.SURFACE_VARIABLE ? "vulkan" : null), Is.True);
    }
}
