using System;
using System.Linq;
using NUnit.Framework;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class BeatmapTest
{
    [Test]
    public void TestHitObjectsAreSortedByStartTime()
    {
        var map = TestBeatmaps.Create(
            new HitCircle(3000, TestBeatmaps.Origin),
            new HitCircle(1000, TestBeatmaps.Origin),
            new HitCircle(2000, TestBeatmaps.Origin));

        Assert.That(map.HitObjects.Select(h => h.StartTime), Is.EqualTo(new[] { 1000.0, 2000.0, 3000.0 }));
        Assert.That(map.FirstObjectTime, Is.EqualTo(1000));
        Assert.That(map.LastObjectTime, Is.EqualTo(3000));
    }

    [Test]
    public void TestObjectsWithEqualTimesKeepTheirAuthoredOrder()
    {
        var map = TestBeatmaps.Create(
            new HitCircle(1000, TestBeatmaps.Origin, 1),
            new HitCircle(1000, TestBeatmaps.Origin, 2),
            new HitCircle(1000, TestBeatmaps.Origin, 3));

        Assert.That(map.HitObjects.Cast<HitCircle>().Select(h => h.Number), Is.EqualTo(new[] { 1, 2, 3 }));
    }

    [Test]
    public void TestEmptyBeatmapHasZeroBounds()
    {
        var map = TestBeatmaps.Create();

        Assert.That(map.HitObjects, Is.Empty);
        Assert.That(map.FirstObjectTime, Is.EqualTo(0));
        Assert.That(map.LastObjectTime, Is.EqualTo(0));
    }

    [Test]
    public void TestNegativeLeadInIsRejected()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Beatmap("t", "a", "f", -1, BeatmapDifficulty.Default, Array.Empty<HitObject>()));

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NegativeInfinity)]
    public void TestNonFiniteStartTimeIsRejected(double time)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new HitCircle(time, TestBeatmaps.Origin));
}
