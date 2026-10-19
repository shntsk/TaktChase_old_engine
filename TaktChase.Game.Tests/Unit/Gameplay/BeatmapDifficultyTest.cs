using System;
using NUnit.Framework;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class BeatmapDifficultyTest
{
    [Test]
    public void TestDerivedValues()
    {
        var difficulty = new BeatmapDifficulty(overallDifficulty: 8, approachRate: 9, circleSize: 3);

        Assert.That(difficulty.OverallDifficulty, Is.EqualTo(8));
        Assert.That(difficulty.ApproachRate, Is.EqualTo(9));
        Assert.That(difficulty.CircleSize, Is.EqualTo(3));
        Assert.That(difficulty.ApproachTime, Is.EqualTo(600).Within(1e-9));
        Assert.That(difficulty.CircleRadius, Is.EqualTo(32 * (1 - 0.7 * -2 / 5)).Within(1e-9));
        Assert.That(difficulty.FadeInTime, Is.EqualTo(400).Within(1e-9));
    }

    [Test]
    public void TestWindowsDeriveFromOverallDifficultyOnly()
    {
        var windows = new BeatmapDifficulty(overallDifficulty: 8, approachRate: 1, circleSize: 9).CreateJudgementWindows();

        Assert.That(windows.GreatWindow, Is.EqualTo(32).Within(1e-9));
        Assert.That(windows.OkWindow, Is.EqualTo(76).Within(1e-9));
        Assert.That(windows.MehWindow, Is.EqualTo(120).Within(1e-9));
        Assert.That(windows.MissWindow, Is.EqualTo(400));
    }

    [TestCase(-0.1)]
    [TestCase(10.1)]
    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    public void TestOutOfRangeValuesAreRejected(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new BeatmapDifficulty(value, 5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BeatmapDifficulty(5, value, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new BeatmapDifficulty(5, 5, value));
    }

    [TestCase(0)]
    [TestCase(10)]
    public void TestRangeEdgesAreAccepted(double value)
        => Assert.DoesNotThrow(() => new BeatmapDifficulty(value, value, value));

    [Test]
    public void TestApproachTimeControlsAppearTime()
    {
        var slow = new BeatmapDifficulty(5, 0, 5);
        var fast = new BeatmapDifficulty(5, 10, 5);
        var circle = new HitCircle(5000, TestBeatmaps.Origin);

        Assert.That(circle.GetAppearTime(slow), Is.EqualTo(3200).Within(1e-9));
        Assert.That(circle.GetAppearTime(fast), Is.EqualTo(4550).Within(1e-9));
    }

    [Test]
    public void TestCircleSizeControlsHitArea()
    {
        var big = new BeatmapDifficulty(5, 5, 0);
        var small = new BeatmapDifficulty(5, 5, 10);
        var circle = new HitCircle(1000, TestBeatmaps.Origin);
        var probe = TestBeatmaps.Origin + new osuTK.Vector2(20, 0);

        Assert.That(circle.Contains(probe, big), Is.True);
        Assert.That(circle.Contains(probe, small), Is.False);
    }
}
