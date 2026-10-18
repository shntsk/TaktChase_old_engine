using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TaktChase.Game.Graphics;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Tests.Unit;

[TestFixture]
public class GraphicsDiagnosticsTest
{
    private static LaunchOptions parse(params string[] args) => LaunchOptions.Parse(args, _ => null);

    private static GraphicsSnapshot snapshot(string? rendererType, string? rendererInfo = "info-from-host", bool hasWindow = true, bool viaEnvironment = false, bool isWindows = false)
        => new GraphicsSnapshot
        {
            RendererInfo = rendererInfo,
            RendererTypeName = rendererType,
            HasWindow = hasWindow,
            SelectedViaEnvironment = viaEnvironment,
            ResolvedRenderer = viaEnvironment ? null : "resolved-by-framework",
            IsWindows = isWindows,
        };

    private static GraphicsReport build(GraphicsSnapshot snapshot, LaunchOptions options, VulkanProbeResult? probe = null, IReadOnlyList<string>? errors = null)
        => GraphicsDiagnostics.Build(snapshot, options, probe, Array.Empty<string>(), Array.Empty<string>(), errors ?? Array.Empty<string>());

    private static string entry(GraphicsReport report, string key) => report.Entries.Single(e => e.Key == key).Value;

    [Test]
    public void TestGraphicsSurfaceAndVSyncAreReportedAsUnavailable()
    {
        var report = build(snapshot("VeldridRenderer", viaEnvironment: true), parse("--renderer=vulkan"));

        Assert.That(entry(report, "Graphics surface"), Does.StartWith("Unavailable"));
        Assert.That(entry(report, "Renderer VSync"), Does.StartWith("Unavailable"));
    }

    [Test]
    public void TestRequestedBackendIsNeverReportedAsTheDetectedSurface()
    {
        // The request says Vulkan, but the public API cannot confirm a Vulkan surface: the report must not claim one.
        var report = build(snapshot("VeldridRenderer", viaEnvironment: true), parse("--renderer=vulkan"));

        Assert.That(entry(report, "Graphics surface"), Does.Not.Contain("Vulkan"));
        Assert.That(entry(report, "Request check"), Does.StartWith("Partially verified"));
        Assert.That(report.Alerts, Is.Empty);
    }

    [Test]
    public void TestActiveRendererComesFromTheHostSnapshot()
    {
        var report = build(snapshot("GLRenderer", rendererInfo: "host-reported-text"), parse("--renderer=opengl"));

        Assert.That(entry(report, "Requested renderer"), Is.EqualTo("OpenGL"));
        Assert.That(entry(report, "Active renderer (host.RendererInfo)"), Is.EqualTo("host-reported-text"));
        Assert.That(entry(report, "Renderer type"), Is.EqualTo("GLRenderer"));
        Assert.That(report.BackendSummary, Is.EqualTo("GLRenderer"));
    }

    [Test]
    public void TestOpenGLRequestWithGLRendererIsConsistent()
    {
        var report = build(snapshot("GLRenderer", viaEnvironment: true), parse("--renderer=opengl"));

        Assert.That(GraphicsDiagnostics.VerifyRequest(RendererRequest.OpenGL, "GLRenderer"), Is.EqualTo(RequestVerification.Consistent));
        Assert.That(entry(report, "Request check"), Does.StartWith("Consistent"));
        Assert.That(report.Alerts, Is.Empty);
        Assert.That(report.NeedsAttention, Is.False);
    }

    [Test]
    public void TestVulkanRequestWithGLRendererIsAMismatch()
    {
        var report = build(snapshot("GLRenderer", viaEnvironment: true), parse("--renderer=vulkan"));

        Assert.That(report.Alerts, Has.Count.EqualTo(1));
        Assert.That(report.Alerts[0], Does.Contain("Vulkan was requested").And.Contain("GLRenderer"));
        Assert.That(report.NeedsAttention, Is.True);
    }

    [Test]
    public void TestOpenGLRequestWithVeldridRendererIsAMismatch()
    {
        var report = build(snapshot("VeldridRenderer", viaEnvironment: true), parse("--renderer=opengl"));

        Assert.That(report.Alerts, Has.Count.EqualTo(1));
        Assert.That(report.Alerts[0], Does.Contain("OpenGL was requested").And.Contain("VeldridRenderer"));
    }

    [TestCase(RendererRequest.OpenGL)]
    [TestCase(RendererRequest.Vulkan)]
    public void TestMissingRendererTypeIsReportedAsUnavailableNotAssumed(RendererRequest request)
    {
        Assert.That(GraphicsDiagnostics.VerifyRequest(request, null), Is.EqualTo(RequestVerification.Unavailable));
        Assert.That(GraphicsDiagnostics.VerifyRequest(request, "  "), Is.EqualTo(RequestVerification.Unavailable));

        var report = build(snapshot(null, rendererInfo: null, hasWindow: false), parse(request == RendererRequest.Vulkan ? "--renderer=vulkan" : "--renderer=opengl"));

        Assert.That(entry(report, "Renderer type"), Is.EqualTo(GraphicsReport.NOT_AVAILABLE));
        Assert.That(entry(report, "Active renderer (host.RendererInfo)"), Is.EqualTo(GraphicsReport.NOT_AVAILABLE));
        Assert.That(entry(report, "Window"), Does.StartWith("Absent"));
        Assert.That(report.Alerts, Has.Count.EqualTo(1));
    }

    [Test]
    public void TestAutoRequestIsNotCheckedAgainstTheRenderer()
    {
        Assert.That(GraphicsDiagnostics.VerifyRequest(RendererRequest.Auto, "VeldridRenderer"), Is.EqualTo(RequestVerification.NotApplicable));
        Assert.That(GraphicsDiagnostics.VerifyRequest(RendererRequest.Auto, null), Is.EqualTo(RequestVerification.NotApplicable));
    }

    [Test]
    public void TestResolvedRendererIsNotApplicableWhenSelectedThroughEnvironment()
    {
        var viaEnvironment = build(snapshot("GLRenderer", viaEnvironment: true), parse("--renderer=opengl"));
        var automatic = build(snapshot("GLRenderer"), parse());

        Assert.That(entry(viaEnvironment, "ResolvedRenderer (automatic mode)"), Does.StartWith("Not applicable"));
        Assert.That(entry(automatic, "ResolvedRenderer (automatic mode)"), Is.EqualTo("resolved-by-framework"));
    }

    [Test]
    public void TestWindowsAutomaticVeldridNoteDoesNotClaimDirect3D11WasDetected()
    {
        var report = build(snapshot("VeldridRenderer", isWindows: true), parse());

        Assert.That(entry(report, "Automatic selection note"), Does.Contain("cannot be confirmed"));
        Assert.That(report.Entries.Any(e => e.Key == "Automatic selection note"), Is.True);

        var linux = build(snapshot("VeldridRenderer", isWindows: false), parse());

        Assert.That(linux.Entries.Any(e => e.Key == "Automatic selection note"), Is.False);
    }

    [Test]
    public void TestFallbackRunReportsFallbackReasonAndActiveRenderer()
    {
        string? environment(string name) => name switch
        {
            LaunchOptions.FALLBACK_FROM_ENV => "Vulkan",
            LaunchOptions.FALLBACK_REASON_ENV => "original-vulkan-cause",
            _ => null,
        };

        var options = LaunchOptions.Parse(new[] { "--renderer=opengl" }, environment);
        var report = build(snapshot("GLRenderer", viaEnvironment: true), options);

        Assert.That(entry(report, "Fallback"), Does.StartWith("Yes"));
        Assert.That(entry(report, "Fallback reason"), Is.EqualTo("original-vulkan-cause"));
        Assert.That(report.Alerts.Single(), Does.Contain("original-vulkan-cause").And.Contain("GLRenderer"));
        Assert.That(report.NeedsAttention, Is.True);
    }

    [Test]
    public void TestStartupErrorsAndLaunchWarningsRequireAttention()
    {
        var withErrors = build(snapshot("GLRenderer"), parse(), errors: new[] { "something failed" });
        var withWarning = build(snapshot("GLRenderer"), parse("--banana"));

        Assert.That(withErrors.NeedsAttention, Is.True);
        Assert.That(withErrors.StartupErrors, Does.Contain("something failed"));
        Assert.That(withWarning.Alerts.Single(), Does.Contain("--banana"));
    }

    [Test]
    public void TestReportTextListsRequestedRendererAndUnavailableValues()
    {
        var report = build(snapshot("VeldridRenderer", viaEnvironment: true), parse("--renderer=vulkan"));

        string text = report.ToText();

        Assert.That(text, Does.Contain("Requested renderer: Vulkan"));
        Assert.That(text, Does.Contain("Graphics surface: Unavailable"));
        Assert.That(text, Does.Contain("Renderer VSync: Unavailable"));
    }
}
