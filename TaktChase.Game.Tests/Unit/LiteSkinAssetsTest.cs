using System.Collections.Generic;
using NUnit.Framework;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TaktChase.Game.Gameplay.Skin;
using TaktChase.Resources;

namespace TaktChase.Game.Tests.Unit;

/// <summary>Checks that the packaged skin PNGs are embedded, decodable, not blank, and have the expected size and transparency.</summary>
[TestFixture]
public class LiteSkinAssetsTest
{
    private static IEnumerable<TestCaseData> assets()
    {
        yield return new TestCaseData(LiteSkinLayout.HIT_CIRCLE_TEXTURE, 600, 600);
        yield return new TestCaseData(LiteSkinLayout.APPROACH_CIRCLE_TEXTURE, 528, 528);

        for (int digit = 0; digit <= 9; digit++)
            yield return new TestCaseData(LiteSkinLayout.DigitTexture(digit), (int)LiteSkinLayout.DigitImageWidth(digit) * 2, 192);
    }

    [TestCaseSource(nameof(assets))]
    public void TestAssetIsEmbeddedDecodableAndNotBlank(string name, int width, int height)
    {
        // "Skin/lite/x" is embedded as "<assembly>.Textures.Skin.lite.x.png".
        string resource = $"TaktChase.Resources.Textures.{name.Replace('/', '.')}.png";
        using var stream = TaktChaseResources.ResourceAssembly.GetManifestResourceStream(resource);
        Assert.That(stream, Is.Not.Null, $"Embedded resource {resource} is missing.");

        using var image = Image.Load<Rgba32>(stream!);
        Assert.That(image.Width, Is.EqualTo(width));
        Assert.That(image.Height, Is.EqualTo(height));

        int visible = 0, transparent = 0, opaque = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                byte alpha = image[x, y].A;
                if (alpha > 0) visible++;
                if (alpha == 0) transparent++;
                if (alpha == 255) opaque++;
            }
        }

        Assert.That(visible, Is.GreaterThan(0), "The image is completely transparent.");
        Assert.That(transparent, Is.GreaterThan(0), "The image has no transparency (corners must be transparent).");
        Assert.That(opaque, Is.GreaterThan(0), "The image has no fully opaque pixels.");
    }

    [Test]
    public void TestApproachCircleIsAThinRingWithTransparentCentre()
    {
        using var stream = TaktChaseResources.ResourceAssembly.GetManifestResourceStream("TaktChase.Resources.Textures.Skin.lite.approachcircle.png");
        using var image = Image.Load<Rgba32>(stream!);
        Assert.That(image[image.Width / 2, image.Height / 2].A, Is.EqualTo(0));
        Assert.That(image[10, image.Height / 2].A, Is.GreaterThan(0), "The ring must reach the left edge region.");
    }
}
