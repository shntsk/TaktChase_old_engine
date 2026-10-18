using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using osu.Framework;
using osu.Framework.Platform;

namespace TaktChase.Game.Startup;

/// <summary>
/// Orquestra a seleção de backend ANTES de criar o host gráfico, executa o jogo e, se o Vulkan
/// explicitamente pedido falhar durante a inicialização, reinicia o processo com OpenGL.
/// </summary>
/// <remarks>
/// Por que o fallback é por reinício de processo: com as variáveis de ambiente oficiais o framework
/// falha de forma definitiva (sem fallback interno) e as variáveis são lidas uma única vez por
/// processo (construtor estático de FrameworkEnvironment). O modo "auto" usa o fallback do próprio
/// framework e não passa por aqui. Veja README, seção "Fallback e limitações".
/// </remarks>
public static class TaktChaseLauncher
{
    public const string GAME_NAME = "TaktChase";

    public static int Run(string[] args)
    {
        var options = LaunchOptions.Parse(args, Environment.GetEnvironmentVariable);
        var notes = new List<string>();

        // 1) Seleção de backend: variáveis oficiais do framework, definidas antes de qualquer
        //    chamada ao framework (elas são lidas no construtor estático de FrameworkEnvironment).
        if (options.Renderer == RendererRequest.Auto && RendererEnvironment.HasExternalOverride(Environment.GetEnvironmentVariable))
        {
            notes.Add($"{RendererEnvironment.RENDERER_VARIABLE}/{RendererEnvironment.SURFACE_VARIABLE} já estavam definidas no ambiente; "
                      + "o framework as usa sem fallback, mesmo com --renderer=auto.");
        }

        foreach (var variable in RendererEnvironment.GetVariables(options.Renderer))
            Environment.SetEnvironmentVariable(variable.Key, variable.Value);

        RuntimeLogCapture.Attach();

        // 2) Pré-verificação Vulkan (somente diagnóstico) quando o Vulkan foi pedido explicitamente.
        VulkanProbeResult? probe = options.Renderer == RendererRequest.Vulkan ? VulkanProbe.RunNative() : null;

        StartupState.Configure(options, probe, notes);

        // 3) Host e jogo, sempre pelos mecanismos do framework.
        //    O host é descartado (using) ANTES de qualquer tratamento de falha, para liberar a instância
        //    única do jogo antes de um eventual reinício do processo.
        try
        {
            var hostOptions = new HostOptions { FriendlyGameName = GAME_NAME };
            bool noUsableRenderer;

            using (GameHost host = Host.GetSuitableDesktopHost(GAME_NAME, hostOptions))
            using (osu.Framework.Game game = new TaktChaseGame(options))
            {
                host.Run(game);

                // GameHost.Run NÃO lança exceção quando a seleção automática esgota todos os renderers:
                // o framework registra "No usable renderer was found!" (LogLevel.Error), o host fica sem
                // Window e sem Renderer e Run simplesmente retorna. Sem esta verificação o processo sairia
                // com código 0 e sem nenhum diagnóstico.
                noUsableRenderer = !StartupState.Completed && host.Renderer == null;
            }

            if (noUsableRenderer)
            {
                return handleStartupFailure(options, probe,
                    new InvalidOperationException("O framework encerrou a inicialização sem um renderer utilizável (nenhuma janela/renderer foi criada)."));
            }

            return 0;
        }
        catch (Exception e) when (!StartupState.Completed)
        {
            return handleStartupFailure(options, probe, e);
        }
    }

    private static int handleStartupFailure(LaunchOptions options, VulkanProbeResult? probe, Exception exception)
    {
        string reason = StartupFailure.Summarize(exception);
        string diagnosis = StartupFailure.BuildDiagnosis(options, reason, probe, RuntimeLogCapture.GetErrorLines());
        string? logPath = writeFailureLog(options, diagnosis, exception);

        Console.Error.WriteLine(diagnosis);

        if (StartupFailure.ShouldOfferOpenGLFallback(options))
        {
            if (relaunchWithOpenGL(options, reason))
                return 0;

            diagnosis += Environment.NewLine + "Não foi possível reiniciar automaticamente com OpenGL.";
            Console.Error.WriteLine("Não foi possível reiniciar automaticamente com OpenGL.");
        }

        showErrorDialog(logPath == null ? diagnosis : diagnosis + Environment.NewLine + "Log: " + logPath);
        return 2;
    }

    private static bool relaunchWithOpenGL(LaunchOptions options, string reason)
    {
        try
        {
            string? executable = Environment.ProcessPath;

            if (executable == null)
                return false;

            var startInfo = new ProcessStartInfo(executable) { UseShellExecute = false };

            // Executado via "dotnet TaktChase.Desktop.dll": o primeiro argumento é a própria DLL.
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            {
                string[] commandLine = Environment.GetCommandLineArgs();

                if (commandLine.Length > 0)
                    startInfo.ArgumentList.Add(commandLine[0]);
            }

            startInfo.ArgumentList.Add("--renderer=opengl");

            if (options.ShowFps)
                startInfo.ArgumentList.Add("--show-fps");

            startInfo.Environment[LaunchOptions.FALLBACK_FROM_ENV] = "Vulkan";
            startInfo.Environment[LaunchOptions.FALLBACK_REASON_ENV] = reason;

            Process.Start(startInfo)?.Dispose();
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Falha ao reiniciar com OpenGL: {e.GetType().Name}: {e.Message}");
            return false;
        }
    }

    private static string? writeFailureLog(LaunchOptions options, string diagnosis, Exception exception)
    {
        try
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), GAME_NAME, "logs");
            Directory.CreateDirectory(directory);

            // Uma execução de fallback que também falha NÃO pode sobrescrever o log da falha original.
            string path = Path.Combine(directory, options.IsFallbackRun ? "startup-failure-fallback.log" : "startup-failure.log");
            File.WriteAllText(path, $"{DateTime.Now:O}{Environment.NewLine}{diagnosis}{Environment.NewLine}{exception}{Environment.NewLine}");
            return path;
        }
        catch
        {
            return null;
        }
    }

    // Aplicativo WinExe não tem console no Windows: sem isto o erro seria invisível.
    private static void showErrorDialog(string message)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            MessageBoxW(IntPtr.Zero, message, GAME_NAME, 0x10 /* MB_ICONERROR */);
        }
        catch
        {
            // Sem diálogo disponível; o diagnóstico já foi escrito no stderr e no arquivo de log.
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
