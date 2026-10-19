using NUnit.Framework;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class OsuDifficultyMathTest
{
    private const double tolerance = 1e-9;

    [TestCase(0, 10, 20, 30, 10)]
    [TestCase(5, 10, 20, 30, 20)]
    [TestCase(10, 10, 20, 30, 30)]
    [TestCase(2.5, 10, 20, 30, 15)]
    [TestCase(7.5, 10, 20, 30, 25)]
    public void TestDifficultyRangeInterpolatesLinearly(double difficulty, double min, double mid, double max, double expected)
        => Assert.That(OsuDifficultyMath.DifficultyRange(difficulty, min, mid, max), Is.EqualTo(expected).Within(tolerance));

    [TestCase(0, 1800)]
    [TestCase(5, 1200)]
    [TestCase(7, 900)]
    [TestCase(10, 450)]
    [TestCase(2.5, 1500)]
    public void TestApproachTime(double approachRate, double expected)
        => Assert.That(OsuDifficultyMath.ApproachTime(approachRate), Is.EqualTo(expected).Within(tolerance));

    [Test]
    public void TestApproachTimeDecreasesWithApproachRate()
    {
        double previous = double.PositiveInfinity;

        for (double ar = 0; ar <= 10; ar += 0.5)
        {
            double time = OsuDifficultyMath.ApproachTime(ar);
            Assert.That(time, Is.LessThan(previous), $"AR {ar}");
            previous = time;
        }
    }

    [TestCase(450, 400)]
    [TestCase(1200, 400)]
    [TestCase(225, 200)]
    [TestCase(300, 266.6666666666667)]
    public void TestFadeInTimeIsShortenedBelowMinimumApproachTime(double approachTime, double expected)
        => Assert.That(OsuDifficultyMath.FadeInTime(approachTime), Is.EqualTo(expected).Within(1e-6));

    [TestCase(0, 54.4)]
    [TestCase(5, 32)]
    [TestCase(10, 9.6)]
    [TestCase(4, 36.48)]
    public void TestCircleRadius(double circleSize, double expected)
        => Assert.That(OsuDifficultyMath.CircleRadius(circleSize), Is.EqualTo(expected).Within(1e-9));

    [Test]
    public void TestCircleRadiusDecreasesWithCircleSize()
    {
        double previous = double.PositiveInfinity;

        for (double cs = 0; cs <= 10; cs += 0.5)
        {
            double radius = OsuDifficultyMath.CircleRadius(cs);
            Assert.That(radius, Is.LessThan(previous), $"CS {cs}");
            previous = radius;
        }
    }

    [TestCase(0, 80, 140, 200)]
    [TestCase(5, 50, 100, 150)]
    [TestCase(10, 20, 60, 100)]
    [TestCase(7.5, 35, 80, 125)]
    [TestCase(2.5, 65, 120, 175)]
    public void TestHitWindowFormulas(double od, double great, double ok, double meh)
    {
        Assert.That(OsuDifficultyMath.GreatWindow(od), Is.EqualTo(great).Within(tolerance));
        Assert.That(OsuDifficultyMath.OkWindow(od), Is.EqualTo(ok).Within(tolerance));
        Assert.That(OsuDifficultyMath.MehWindow(od), Is.EqualTo(meh).Within(tolerance));
    }

    [Test]
    public void TestHitWindowsAreOrderedAtEveryDifficulty()
    {
        for (double od = 0; od <= 10; od += 0.5)
        {
            Assert.That(OsuDifficultyMath.GreatWindow(od), Is.LessThanOrEqualTo(OsuDifficultyMath.OkWindow(od)), $"OD {od}");
            Assert.That(OsuDifficultyMath.OkWindow(od), Is.LessThanOrEqualTo(OsuDifficultyMath.MehWindow(od)), $"OD {od}");
            Assert.That(OsuDifficultyMath.MehWindow(od), Is.LessThanOrEqualTo(OsuDifficultyMath.MISS_WINDOW), $"OD {od}");
        }
    }
}
