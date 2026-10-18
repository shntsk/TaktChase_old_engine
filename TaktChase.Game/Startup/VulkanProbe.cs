using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaktChase.Game.Startup;

/// <summary>
/// Minimal access to the Vulkan loader, abstracted so it can be tested without a driver.
/// </summary>
public interface IVulkanApi : IDisposable
{
    /// <summary>Library names that were tried, so they can be listed in the diagnostics.</summary>
    IReadOnlyList<string> CandidateLibraryNames { get; }

    bool TryLoad(out string libraryName);

    /// <summary>False if the loader does not export <c>vkEnumerateInstanceVersion</c> (Vulkan 1.0 loader).</summary>
    bool TryGetInstanceVersion(out uint version);

    /// <summary>Calls <c>vkCreateInstance</c> with no layers or extensions. Returns the VkResult.</summary>
    int CreateInstance(out IntPtr instance);

    int EnumeratePhysicalDeviceCount(IntPtr instance, out uint count);

    void DestroyInstance(IntPtr instance);
}

/// <summary>
/// Result of the pre-check. It is DIAGNOSTIC ONLY: it does not replace the framework's real initialization,
/// which is what validates the surface, queues, and required features.
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

    /// <summary>Loader present, instance created, and at least one physical device enumerated.</summary>
    public bool LooksUsable => LoaderFound && CreateInstanceResult == 0 && EnumerateResult is null or 0 && PhysicalDeviceCount > 0;

    public IReadOnlyList<string> ToLines()
    {
        var lines = new List<string>();

        if (!LoaderFound)
        {
            lines.Add($"Vulkan loader NOT found (tried: {string.Join(", ", TriedLibraries)}).");

            if (Error != null)
                lines.Add($"Detail: {Error}");

            return lines;
        }

        lines.Add($"Vulkan loader: {LoaderName}" + (InstanceVersion != null ? $" (instance version {InstanceVersion})" : " (vkEnumerateInstanceVersion unavailable)"));

        if (CreateInstanceResult is int create)
        {
            lines.Add(create == 0
                ? "vkCreateInstance: VK_SUCCESS"
                : $"vkCreateInstance failed: {VulkanProbe.DescribeResult(create)}"
                  + (create == -9 ? " — the loader found no compatible Vulkan driver." : string.Empty));
        }

        if (PhysicalDeviceCount is uint count)
            lines.Add($"Vulkan physical devices enumerated: {count}" + (count == 0 ? " — no usable driver/GPU." : string.Empty));
        else if (EnumerateResult is int enumerate)
            lines.Add($"vkEnumeratePhysicalDevices failed: {VulkanProbe.DescribeResult(enumerate)}");

        if (Error != null)
            lines.Add($"Detail: {Error}");

        lines.Add(LooksUsable
            ? "Pre-check completed. This does NOT guarantee that the framework can create a surface and a device; the real initialization is the proof."
            : "The pre-check indicates a problem in the Vulkan environment.");

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

    /// <summary>Runs the pre-check against the system's real loader.</summary>
    public static VulkanProbeResult RunNative()
    {
        using var api = new NativeVulkanApi();
        return Run(api);
    }

    /// <summary>Decodes a packed Vulkan version number (major.minor.patch).</summary>
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
/// Real implementation, through the system loader (libvulkan.so.1 / vulkan-1.dll).
/// It only queries and creates/destroys an instance; it is not a renderer.
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
