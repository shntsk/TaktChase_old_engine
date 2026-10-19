using NUnit.Framework;
using TaktChase.Game.Gameplay.Timing;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class AudioOffsetConverterTest
{
    [TestCase(1000, 0, 1000)]
    [TestCase(1000, 60, 940)]
    [TestCase(1000, -40, 1040)]
    [TestCase(-500, 25, -525)]
    public void TestGameplayTimeIsTrackTimeMinusOffset(double trackTime, double offset, double expected)
        => Assert.That(AudioOffsetConverter.ToGameplayTime(trackTime, offset), Is.EqualTo(expected));

    [TestCase(1000, 60)]
    [TestCase(-250, -35.5)]
    [TestCase(0, 0)]
    public void TestConversionRoundTrips(double trackTime, double offset)
    {
        double gameplay = AudioOffsetConverter.ToGameplayTime(trackTime, offset);
        Assert.That(AudioOffsetConverter.ToTrackTime(gameplay, offset), Is.EqualTo(trackTime).Within(1e-9));
    }

    [Test]
    public void TestPositiveOffsetMeansGameplayRunsBehindTheTrack()
    {
        // The audio is heard 60 ms late: when the track is at 1060 the player has just heard position 1000.
        Assert.That(AudioOffsetConverter.ToGameplayTime(1060, 60), Is.EqualTo(1000));
    }
}
