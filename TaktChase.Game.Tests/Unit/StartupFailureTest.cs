using System;
using System.Collections.Generic;
using NUnit.Framework;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Tests.Unit;

[TestFixture]
public class StartupFailureTest
{
    private static LaunchOptions parse(params string[] args) => LaunchOptions.Parse(args, _ => null);

    [Test]
    public void TestSummarizeIncludesRootCauseWhenDifferent()
    {
        var exception = new InvalidOperationException("externa", new DllNotFoundException("libvulkan.so.1"));

        string summary = StartupFailure.Summarize(exception);

        Assert.That(summary, Does.Contain("InvalidOperationException: externa"));
        Assert.That(summary, Does.Contain("DllNotFoundException: libvulkan.so.1"));
    }

    [Test]
    public void TestSummarizeUnwrapsSingleInnerAggregateAndKeepsOneLine()
    {
        var exception = new AggregateException(new InvalidOperationException("linha 1\nlinha 2"));

        string summary = StartupFailure.Summarize(exception);

        Assert.That(summary, Does.StartWith("InvalidOperationException: linha 1"));
        Assert.That(summary, Does.Not.Contain("\n"));
    }

    [Test]
    public void TestSummarizeIsBounded()
    {
        string summary = StartupFailure.Summarize(new InvalidOperationException(new string('x', 5000)));

        Assert.That(summary.Length, Is.LessThanOrEqualTo(401));
    }

    [Test]
    public void TestFallbackIsOfferedOnlyForExplicitVulkanOnFirstRun()
    {
        Assert.That(StartupFailure.ShouldOfferOpenGLFallback(parse("--renderer=vulkan")), Is.True);
        Assert.That(StartupFailure.ShouldOfferOpenGLFallback(parse("--renderer=vulkan", "--no-fallback")), Is.False);
        Assert.That(StartupFailure.ShouldOfferOpenGLFallback(parse("--renderer=opengl")), Is.False);
        Assert.That(StartupFailure.ShouldOfferOpenGLFallback(parse()), Is.False);
    }

    [Test]
    public void TestFallbackRunNeverOffersAnotherRestart()
    {
        // Proteção contra ciclo de reinicialização: mesmo que o pedido fosse Vulkan, uma execução marcada como fallback não reinicia.
        var options = LaunchOptions.Parse(new[] { "--renderer=vulkan" }, name => name == LaunchOptions.FALLBACK_FROM_ENV ? "Vulkan" : null);

        Assert.That(StartupFailure.ShouldOfferOpenGLFallback(options), Is.False);
    }

    [Test]
    public void TestDiagnosisShowsRequestedRendererAndReason()
    {
        string diagnosis = StartupFailure.BuildDiagnosis(parse("--renderer=vulkan"), "motivo-x", null, Array.Empty<string>());

        Assert.That(diagnosis, Does.Contain("Renderer solicitado: Vulkan"));
        Assert.That(diagnosis, Does.Contain("motivo-x"));
        Assert.That(diagnosis, Does.Contain("--renderer=opengl"));
    }

    [Test]
    public void TestDiagnosisKeepsOriginalFailureWhenFallbackAlsoFails()
    {
        string? environment(string name) => name switch
        {
            LaunchOptions.FALLBACK_FROM_ENV => "Vulkan",
            LaunchOptions.FALLBACK_REASON_ENV => "causa-original-vulkan",
            _ => null,
        };

        var options = LaunchOptions.Parse(new[] { "--renderer=opengl" }, environment);
        string diagnosis = StartupFailure.BuildDiagnosis(options, "falha-do-opengl", null, new List<string>());

        Assert.That(diagnosis, Does.Contain("falha-do-opengl"));
        Assert.That(diagnosis, Does.Contain("causa-original-vulkan"));
    }

    [Test]
    public void TestDiagnosisListsFrameworkErrors()
    {
        string diagnosis = StartupFailure.BuildDiagnosis(parse(), "r", null, new[] { "No usable renderer was found!" });

        Assert.That(diagnosis, Does.Contain("No usable renderer was found!"));
    }
}
