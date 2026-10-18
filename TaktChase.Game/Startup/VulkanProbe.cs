using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaktChase.Game.Startup;

/// <summary>
/// Acesso mínimo ao loader Vulkan, abstraído para permitir testes sem driver.
/// </summary>
public interface IVulkanApi : IDisposable
{
    /// <summary>Nomes de biblioteca tentados, para constar no diagnóstico.</summary>
    IReadOnlyList<string> CandidateLibraryNames { get; }

    bool TryLoad(out string libraryName);

    /// <summary>Falso se o loader não exporta <c>vkEnumerateInstanceVersion</c> (loader Vulkan 1.0).</summary>
    bool TryGetInstanceVersion(out uint version);

    /// <summary>Chama <c>vkCreateInstance</c> sem camadas nem extensões. Retorna o VkResult.</summary>
    int CreateInstance(out IntPtr instance);

    int EnumeratePhysicalDeviceCount(IntPtr instance, out uint count);

    void DestroyInstance(IntPtr instance);
}

/// <summary>
/// Resultado da pré-verificação. É DIAGNÓSTICO: não substitui a inicialização real do framework,
/// que é quem valida superfície, filas e recursos necessários.
/// </summary>
public sealed class VulkanProbeResult
{
    public bool LoaderFound { get; init; }
    public string? LoaderName { get; init; }
    public IReadOnlyList<string> TriedLibraries { get; init; } = Array.Empty<string>();
    public string? InstanceVersion { get; init; }
    public int? CreateInstanceResult { get; init; }
    public int? EnumerateResult { get; init; }
    public uint? PhysicalDeviceCount { get; init; }
    public string? Error { get; init; }

    /// <summary>Loader presente, instância criada e ao menos um dispositivo físico enumerado.</summary>
    public bool LooksUsable => LoaderFound && CreateInstanceResult == 0 && EnumerateResult is null or 0 && PhysicalDeviceCount > 0;

    public IReadOnlyList<string> ToLines()
    {
        var lines = new List<string>();

        if (!LoaderFound)
        {
            lines.Add($"Loader Vulkan NÃO encontrado (tentado: {string.Join(", ", TriedLibraries)}).");

            if (Error != null)
                lines.Add($"Detalhe: {Error}");

            return lines;
        }

        lines.Add($"Loader Vulkan: {LoaderName}" + (InstanceVersion != null ? $" (versão de instância {InstanceVersion})" : " (vkEnumerateInstanceVersion indisponível)"));

        if (CreateInstanceResult is int create)
        {
            lines.Add(create == 0
                ? "vkCreateInstance: VK_SUCCESS"
                : $"vkCreateInstance falhou: {VulkanProbe.DescribeResult(create)}"
                  + (create == -9 ? " — nenhum driver Vulkan compatível foi encontrado pelo loader." : string.Empty));
        }

        if (PhysicalDeviceCount is uint count)
            lines.Add($"Dispositivos físicos Vulkan enumerados: {count}" + (count == 0 ? " — nenhum driver/GPU utilizável." : string.Empty));
        else if (EnumerateResult is int enumerate)
            lines.Add($"vkEnumeratePhysicalDevices falhou: {VulkanProbe.DescribeResult(enumerate)}");

        if (Error != null)
            lines.Add($"Detalhe: {Error}");

        lines.Add(LooksUsable
            ? "Pré-verificação concluída. Isto NÃO garante que o framework consiga criar superfície e dispositivo; a inicialização real é a prova."
            : "Pré-verificação indica problema no ambiente Vulkan.");

        return lines;
    }
}

public static class VulkanProbe
{
    public static VulkanProbeResult Run(IVulkanApi api)
    {
        try
        {
            if (!api.TryLoad(out string libraryName))
                return new VulkanProbeResult { LoaderFound = false, TriedLibraries = api.CandidateLibraryNames };

            string? version = api.TryGetInstanceVersion(out uint packed) ? FormatVersion(packed) : null;

            int create = api.CreateInstance(out IntPtr instance);

            if (create != 0)
            {
                return new VulkanProbeResult
                {
                    LoaderFound = true,
                    LoaderName = libraryName,
                    TriedLibraries = api.CandidateLibraryNames,
                    InstanceVersion = version,
                    CreateInstanceResult = create,
                };
            }

            try
            {
                int enumerate = api.EnumeratePhysicalDeviceCount(instance, out uint count);

                return new VulkanProbeResult
                {
                    LoaderFound = true,
                    LoaderName = libraryName,
                    TriedLibraries = api.CandidateLibraryNames,
                    InstanceVersion = version,
                    CreateInstanceResult = create,
                    EnumerateResult = enumerate,
                    PhysicalDeviceCount = enumerate == 0 ? count : null,
                };
            }
            finally
            {
                api.DestroyInstance(instance);
            }
        }
        catch (Exception e)
        {
            return new VulkanProbeResult
            {
                LoaderFound = false,
                TriedLibraries = api.CandidateLibraryNames,
                Error = $"{e.GetType().Name}: {e.Message}",
            };
        }
    }

    /// <summary>Executa a pré-verificação com o loader real do sistema.</summary>
    public static VulkanProbeResult RunNative()
    {
        using var api = new NativeVulkanApi();
        return Run(api);
    }

    /// <summary>Decodifica um número de versão Vulkan empacotado (major.minor.patch).</summary>
    public static string FormatVersion(uint packed)
    {
        uint major = packed >> 22;
        uint minor = (packed >> 12) & 0x3FF;
        uint patch = packed & 0xFFF;
        return $"{major}.{minor}.{patch}";
    }

    public static string DescribeResult(int result)
    {
        string? name = result switch
        {
            0 => "VK_SUCCESS",
            -1 => "VK_ERROR_OUT_OF_HOST_MEMORY",
            -2 => "VK_ERROR_OUT_OF_DEVICE_MEMORY",
            -3 => "VK_ERROR_INITIALIZATION_FAILED",
            -4 => "VK_ERROR_DEVICE_LOST",
            -6 => "VK_ERROR_LAYER_NOT_PRESENT",
            -7 => "VK_ERROR_EXTENSION_NOT_PRESENT",
            -8 => "VK_ERROR_FEATURE_NOT_PRESENT",
            -9 => "VK_ERROR_INCOMPATIBLE_DRIVER",
            _ => null,
        };

        return name != null ? $"{name} ({result})" : $"VkResult {result}";
    }
}

/// <summary>
/// Implementação real, via o loader do sistema (libvulkan.so.1 / vulkan-1.dll).
/// Apenas consulta e cria/destrói uma instância; não é um renderer.
/// </summary>
internal sealed class NativeVulkanApi : IVulkanApi
{
    [StructLayout(LayoutKind.Sequential)]
    private struct VkInstanceCreateInfo
    {
        public int sType;
        public IntPtr pNext;
        public uint flags;
        public IntPtr pApplicationInfo;
        public uint enabledLayerCount;
        public IntPtr ppEnabledLayerNames;
        public uint enabledExtensionCount;
        public IntPtr ppEnabledExtensionNames;
    }

    private const int vk_structure_type_instance_create_info = 1;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int EnumerateInstanceVersionDelegate(out uint version);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int CreateInstanceDelegate(ref VkInstanceCreateInfo createInfo, IntPtr allocator, out IntPtr instance);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int EnumeratePhysicalDevicesDelegate(IntPtr instance, ref uint count, IntPtr devices);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void DestroyInstanceDelegate(IntPtr instance, IntPtr allocator);

    private IntPtr library;

    public IReadOnlyList<string> CandidateLibraryNames { get; } = getCandidates();

    private static string[] getCandidates()
    {
        if (OperatingSystem.IsWindows())
            return new[] { "vulkan-1.dll" };

        if (OperatingSystem.IsLinux())
            return new[] { "libvulkan.so.1", "libvulkan.so" };

        return Array.Empty<string>();
    }

    public bool TryLoad(out string libraryName)
    {
        foreach (string candidate in CandidateLibraryNames)
        {
            if (NativeLibrary.TryLoad(candidate, out library))
            {
                libraryName = candidate;
                return true;
            }
        }

        libraryName = string.Empty;
        return false;
    }

    public bool TryGetInstanceVersion(out uint version)
    {
        version = 0;

        if (!NativeLibrary.TryGetExport(library, "vkEnumerateInstanceVersion", out IntPtr export))
            return false;

        var function = Marshal.GetDelegateForFunctionPointer<EnumerateInstanceVersionDelegate>(export);
        return function(out version) == 0;
    }

    public int CreateInstance(out IntPtr instance)
    {
        var function = Marshal.GetDelegateForFunctionPointer<CreateInstanceDelegate>(getExport("vkCreateInstance"));

        var info = new VkInstanceCreateInfo { sType = vk_structure_type_instance_create_info };
        return function(ref info, IntPtr.Zero, out instance);
    }

    public int EnumeratePhysicalDeviceCount(IntPtr instance, out uint count)
    {
        var function = Marshal.GetDelegateForFunctionPointer<EnumeratePhysicalDevicesDelegate>(getExport("vkEnumeratePhysicalDevices"));

        count = 0;
        return function(instance, ref count, IntPtr.Zero);
    }

    public void DestroyInstance(IntPtr instance)
    {
        var function = Marshal.GetDelegateForFunctionPointer<DestroyInstanceDelegate>(getExport("vkDestroyInstance"));
        function(instance, IntPtr.Zero);
    }

    private IntPtr getExport(string name) => NativeLibrary.GetExport(library, name);

    public void Dispose()
    {
        if (library != IntPtr.Zero)
        {
            NativeLibrary.Free(library);
            library = IntPtr.Zero;
        }
    }
}
