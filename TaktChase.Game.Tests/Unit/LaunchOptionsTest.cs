using System;
using NUnit.Framework;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Tests.Unit;

[TestFixture]
public class LaunchOptionsTest
{
    private static string? noEnvironment(string name) => null;

    [Test]
    public void TestDefaultIsAuto()
    {
        var options = LaunchOptions.Parse(Array.Empty<string>(), noEnvironment);

        Assert.That(options.Renderer, Is.EqualTo(RendererRequest.Auto));
        Assert.That(options.ShowFps, Is.False);
        Assert.That(options.NoFallback, Is.False);
        Assert.That(options.IsFallbackRun, Is.False);
        Assert.That(options.Warnings, Is.Empty);
    }

    [TestCase("--renderer=auto", RendererRequest.Auto)]
    [TestCase("--renderer=opengl", RendererRequest.OpenGL)]
    [TestCase("--renderer=gl", RendererRequest.OpenGL)]
    [TestCase("--renderer=vulkan", RendererRequest.Vulkan)]
    [TestCase("--renderer=VK", RendererRequest.Vulkan)]
    public void TestRendererEqualsForm(string argument, RendererRequest expected)
    {
        var options = LaunchOptions.Parse(new[] { argument }, noEnvironment);

        Assert.That(options.Renderer, Is.EqualTo(expected));
        Assert.That(options.Warnings, Is.Empty);
    }

    [Test]
    public void TestRendererSeparateValueForm()
    {
        var options = LaunchOptions.Parse(new[] { "--renderer", "vulkan" }, noEnvironment);

        Assert.That(options.Renderer, Is.EqualTo(RendererRequest.Vulkan));
        Assert.That(options.Warnings, Is.Empty);
    }

    [Test]
    public void TestInvalidRendererWarnsAndFallsBackToAuto()
    {
        var options = LaunchOptions.Parse(new[] { "--renderer=directx" }, noEnvironment);

        Assert.That(options.Renderer, Is.EqualTo(RendererRequest.Auto));
        Assert.That(options.Warnings, Has.Count.EqualTo(1));
    }

    [Test]
    public void TestRendererWithoutValueWarns()
    {
        var options = LaunchOptions.Parse(new[] { "--renderer" }, noEnvironment);

        Assert.That(options.Renderer, Is.EqualTo(RendererRequest.Auto));
        Assert.That(options.Warnings, Has.Count.EqualTo(1));
    }

    [Test]
    public void TestUnknownArgumentIsReportedNeverSilentlyIgnored()
    {
        var options = LaunchOptions.Parse(new[] { "--banana" }, noEnvironment);

        Assert.That(options.Warnings, Has.Count.EqualTo(1));
        Assert.That(options.Warnings[0], Does.Contain("--banana"));
    }

    [Test]
    public void TestFlags()
    {
        var options = LaunchOptions.Parse(new[] { "--show-fps", "--no-fallback" }, noEnvironment);

        Assert.That(options.ShowFps, Is.True);
        Assert.That(options.NoFallback, Is.True);
        Assert.That(options.Warnings, Is.Empty);
    }

    [Test]
    public void TestFallbackRunIsReadFromEnvironment()
    {
        string? environment(string name) => name switch
        {
            LaunchOptions.FALLBACK_FROM_ENV => "Vulkan",
            LaunchOptions.FALLBACK_REASON_ENV => "any reason",
            _ => null,
        };

        var options = LaunchOptions.Parse(new[] { "--renderer=opengl" }, environment);

        Assert.That(options.IsFallbackRun, Is.True);
        Assert.That(options.FallbackFrom, Is.EqualTo("Vulkan"));
        Assert.That(options.FallbackReason, Is.EqualTo("any reason"));
    }

    [Test]
    public void TestBlankFallbackEnvironmentIsNotAFallbackRun()
    {
        string? environment(string name) => name == LaunchOptions.FALLBACK_FROM_ENV ? "   " : null;

        var options = LaunchOptions.Parse(Array.Empty<string>(), environment);

        Assert.That(options.IsFallbackRun, Is.False);
    }
}
