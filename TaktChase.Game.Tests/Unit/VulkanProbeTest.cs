using System;
using System.Collections.Generic;
using NUnit.Framework;
using TaktChase.Game.Startup;

namespace TaktChase.Game.Tests.Unit;

[TestFixture]
public class VulkanProbeTest
{
    private sealed class FakeVulkanApi : IVulkanApi
    {
        public bool Loads { get; init; } = true;
        public bool HasInstanceVersion { get; init; } = true;
        public int CreateResult { get; init; }
        public int EnumerateResult { get; init; }
        public uint DeviceCount { get; init; } = 1;

        public bool InstanceDestroyed { get; private set; }

        public IReadOnlyList<string> CandidateLibraryNames { get; } = new[] { "fake-vulkan" };

        public bool TryLoad(out string libraryName)
        {
            libraryName = Loads ? "fake-vulkan" : string.Empty;
            return Loads;
        }

        public bool TryGetInstanceVersion(out uint version)
        {
            version = (1u << 22) | (3u << 12) | 250u;
            return HasInstanceVersion;
        }

        public int CreateInstance(out IntPtr instance)
        {
            instance = CreateResult == 0 ? new IntPtr(1) : IntPtr.Zero;
            return CreateResult;
        }

        public int EnumeratePhysicalDeviceCount(IntPtr instance, out uint count)
        {
            count = DeviceCount;
            return EnumerateResult;
        }

        public void DestroyInstance(IntPtr instance) => InstanceDestroyed = true;

        public void Dispose()
        {
        }
    }

    [Test]
    public void TestMissingLoader()
    {
        var result = VulkanProbe.Run(new FakeVulkanApi { Loads = false });

        Assert.That(result.LoaderFound, Is.False);
        Assert.That(result.LooksUsable, Is.False);
        Assert.That(result.TriedLibraries, Does.Contain("fake-vulkan"));
    }

    [Test]
    public void TestUsableEnvironment()
    {
        var api = new FakeVulkanApi();

        var result = VulkanProbe.Run(api);

        Assert.That(result.LoaderFound, Is.True);
        Assert.That(result.InstanceVersion, Is.EqualTo("1.3.250"));
        Assert.That(result.PhysicalDeviceCount, Is.EqualTo(1u));
        Assert.That(result.LooksUsable, Is.True);
        Assert.That(api.InstanceDestroyed, Is.True);
    }

    [Test]
    public void TestIncompatibleDriverIsExplained()
    {
        var result = VulkanProbe.Run(new FakeVulkanApi { CreateResult = -9 });

        Assert.That(result.LooksUsable, Is.False);
        Assert.That(string.Join("\n", result.ToLines()), Does.Contain("VK_ERROR_INCOMPATIBLE_DRIVER"));
    }

    [Test]
    public void TestZeroPhysicalDevicesIsNotUsable()
    {
        var api = new FakeVulkanApi { DeviceCount = 0 };

        var result = VulkanProbe.Run(api);

        Assert.That(result.LooksUsable, Is.False);
        Assert.That(api.InstanceDestroyed, Is.True);
    }

    [Test]
    public void TestFormatVersion()
    {
        Assert.That(VulkanProbe.FormatVersion((1u << 22) | (3u << 12) | 250u), Is.EqualTo("1.3.250"));
    }
}
