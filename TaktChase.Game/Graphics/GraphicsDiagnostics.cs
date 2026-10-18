using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Platform;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Graphics;

/// <summary>
/// Coleta o relatório gráfico a partir do host do framework já inicializado.
/// O backend informado é SEMPRE o que o host reporta (RendererInfo, tipo do renderer e superfície da janela),
/// nunca apenas o argumento recebido.
/// </summary>
public static class GraphicsDiagnostics
{
    public static GraphicsReport Capture(GameHost host, LaunchOptions options, VulkanProbeResult? probe, IReadOnlyList<string> notes)
    {
        var entries = new List<KeyValuePair<string, string>>();
        var alerts = new List<string>();

        string rendererInfo = host.RendererInfo ?? GraphicsReport.NOT_AVAILABLE;
        string rendererType = host.Renderer?.GetType().Name ?? GraphicsReport.NOT_AVAILABLE;
        string surface = host.Window != null ? host.Window.GraphicsSurface.Type.ToString() : "sem janela (headless)";

        bool selectedViaEnvironment =
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RendererEnvironment.RENDERER_VARIABLE))
            || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(RendererEnvironment.SURFACE_VARIABLE));

        add(entries, "TaktChase", typeof(GraphicsReport).Assembly.GetName().Version?.ToString() ?? GraphicsReport.NOT_AVAILABLE);
        add(entries, "Renderer solicitado", options.Renderer.ToString());
        add(entries, "Seleção", selectedViaEnvironment
            ? "variáveis de ambiente OSU_GRAPHICS_* (sem fallback interno do framework)"
            : "automática do framework (com a ordem de fallback do framework)");
        add(entries, "Backend efetivo (host.RendererInfo)", rendererInfo);
        add(entries, "Tipo do renderer", rendererType);
        add(entries, "Superfície gráfica da janela", surface);
        add(entries, "ResolvedRenderer (modo automático)", selectedViaEnvironment
            ? "n/d (seleção por variável de ambiente)"
            : host.ResolvedRenderer.ToString());
        add(entries, "VSync do renderer", host.Renderer != null ? host.Renderer.VerticalSync.ToString() : GraphicsReport.NOT_AVAILABLE);

        add(entries, "Fallback", options.IsFallbackRun
            ? $"sim — {options.FallbackFrom} falhou na inicialização"
            : "não");

        if (options.FallbackReason != null)
            add(entries, "Motivo do fallback", options.FallbackReason);

        if (probe != null)
        {
            foreach (string line in probe.ToLines())
                add(entries, "Pré-verificação Vulkan", line);
        }

        add(entries, "Sistema", RuntimeInformation.OSDescription);
        add(entries, "Arquitetura", RuntimeInformation.ProcessArchitecture.ToString());
        add(entries, ".NET", RuntimeInformation.FrameworkDescription);
        add(entries, "Processadores lógicos", Environment.ProcessorCount.ToString());

        // Alertas: sempre comparar o pedido com o que o host realmente inicializou.
        if (options.Renderer == RendererRequest.Vulkan && !surface.Equals("Vulkan", StringComparison.OrdinalIgnoreCase))
            alerts.Add($"Vulkan foi solicitado, mas a superfície ativa é '{surface}'.");

        if (options.Renderer == RendererRequest.OpenGL && !surface.Equals("OpenGL", StringComparison.OrdinalIgnoreCase))
            alerts.Add($"OpenGL foi solicitado, mas a superfície ativa é '{surface}'.");

        // As classes de renderer são internas ao framework (não há API pública para o backend Veldrid);
        // o nome do tipo vem de host.Renderer.GetType() e é conferido contra o que o pedido deveria produzir
        // (GameHost.SetupRendererAndWindow: "gl" -> GLRenderer, "veldrid" -> VeldridRenderer).
        if (options.Renderer == RendererRequest.Vulkan && !rendererType.Equals("VeldridRenderer", StringComparison.Ordinal))
            alerts.Add($"Vulkan foi solicitado (VeldridRenderer + superfície Vulkan), mas o tipo de renderer ativo é '{rendererType}'.");

        if (options.Renderer == RendererRequest.OpenGL && !rendererType.Equals("GLRenderer", StringComparison.Ordinal))
            alerts.Add($"OpenGL foi solicitado (GLRenderer), mas o tipo de renderer ativo é '{rendererType}'.");

        if (options.IsFallbackRun)
            alerts.Add($"Fallback: {options.FallbackFrom} falhou ao inicializar; esta execução usa '{surface}'."
                       + (options.FallbackReason != null ? $" Motivo: {options.FallbackReason}" : string.Empty));

        if (options.Renderer == RendererRequest.Auto && !selectedViaEnvironment && surface.Equals("Direct3D11", StringComparison.OrdinalIgnoreCase))
            alerts.Add("No Windows, a ordem automática do framework prefere Direct3D 11 (via Veldrid). Use --renderer=opengl ou --renderer=vulkan para comparar.");

        if (probe is { LooksUsable: false })
            alerts.Add("A pré-verificação Vulkan indicou problema no ambiente (veja as linhas acima).");

        foreach (string warning in options.Warnings)
            alerts.Add(warning);

        foreach (string note in notes)
            alerts.Add(note);

        IReadOnlyList<string> errors = RuntimeLogCapture.GetErrorLines();

        string summary = $"{surface} · {rendererType}";
        bool needsAttention = options.IsFallbackRun || alerts.Count > 0 || errors.Count > 0;

        return new GraphicsReport(entries, alerts, RuntimeLogCapture.GetGraphicsLines(), errors, summary, needsAttention);
    }

    private static void add(List<KeyValuePair<string, string>> entries, string key, string value)
        => entries.Add(new KeyValuePair<string, string>(key, value));
}
