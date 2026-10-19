using NUnit.Framework;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Drawables;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class HitCircleAnimationTest
{
    [TestCase(-100, 0)]
    [TestCase(0, 0)]
    [TestCase(100, 0.25)]
    [TestCase(200, 0.5)]
    [TestCase(400, 1)]
    [TestCase(900, 1)]
    public void TestBodyFadesInOverTheFadeInTime(double timeAfterAppear, double expected)
        => Assert.That(HitCircleAnimation.BodyAlpha(5000 + timeAfterAppear, 5000, 400), Is.EqualTo(expected).Within(1e-6));

    [TestCase(-10, 0)]
    [TestCase(0, 0)]
    [TestCase(400, 0.5)]
    [TestCase(800, 1)]
    [TestCase(850, 1)]
    public void TestApproachCircleFadesInTwiceAsSlowAsTheBody(double timeAfterAppear, double expected)
        => Assert.That(HitCircleAnimation.ApproachAlpha(timeAfterAppear, 0, 400, 1200), Is.EqualTo(expected).Within(1e-6));

    [Test]
    public void TestApproachFadeInIsCappedByTheApproachTime()
    {
        // 2 * 400 = 800 ms would be longer than the 450 ms approach time, so it is fully visible by 450 ms.
        Assert.That(HitCircleAnimation.ApproachAlpha(450, 0, 400, 450), Is.EqualTo(1).Within(1e-6));
        Assert.That(HitCircleAnimation.ApproachAlpha(225, 0, 400, 450), Is.EqualTo(0.5).Within(1e-6));
    }

    [Test]
    public void TestApproachScaleShrinksLinearlyToTheCircleAtHitTime()
    {
        const double appear = 1000;
        const double approachTime = 1200;

        Assert.That(HitCircleAnimation.ApproachScale(appear - 500, appear, approachTime, 4), Is.EqualTo(4).Within(1e-6));
        Assert.That(HitCircleAnimation.ApproachScale(appear, appear, approachTime, 4), Is.EqualTo(4).Within(1e-6));
        Assert.That(HitCircleAnimation.ApproachScale(appear + 600, appear, approachTime, 4), Is.EqualTo(2.5).Within(1e-6));
        Assert.That(HitCircleAnimation.ApproachScale(appear + approachTime, appear, approachTime, 4), Is.EqualTo(1).Within(1e-6));
        Assert.That(HitCircleAnimation.ApproachScale(appear + approachTime + 100, appear, approachTime, 4), Is.EqualTo(1).Within(1e-6));
    }

    [Test]
    public void TestApproachReachesTheCircleExactlyAtTheHitTimeForAnyApproachRate()
    {
        var circle = new HitCircle(5000, TestBeatmaps.Origin);

        foreach (double ar in new[] { 0.0, 3, 5, 7, 9.5, 10 })
        {
            var difficulty = new BeatmapDifficulty(5, ar, 5);
            double appear = circle.GetAppearTime(difficulty);

            Assert.That(HitCircleAnimation.ApproachScale(5000, appear, difficulty.ApproachTime, 4), Is.EqualTo(1).Within(1e-6), $"AR {ar}");
            Assert.That(HitCircleAnimation.ApproachScale(appear, appear, difficulty.ApproachTime, 4), Is.EqualTo(4).Within(1e-6), $"AR {ar}");
        }
    }

    [Test]
    public void TestAnimationIsAFunctionOfGameplayTimeOnly()
    {
        // The same time always gives the same appearance, regardless of what was evaluated before (no frame-rate state).
        double first = HitCircleAnimation.ApproachScale(1300, 1000, 1200, 4);
        HitCircleAnimation.ApproachScale(1900, 1000, 1200, 4);
        double second = HitCircleAnimation.ApproachScale(1300, 1000, 1200, 4);

        Assert.That(second, Is.EqualTo(first));
    }

    [TestCase(0, 0)]
    [TestCase(150, 280)]
    [TestCase(-150, -280)]
    [TestCase(75, 140)]
    [TestCase(-75, -140)]
    [TestCase(500, 280)]
    [TestCase(-500, -280)]
    public void TestHitErrorTickOffset(double error, float expected)
        => Assert.That(HitErrorBar.TickOffset(error, 150), Is.EqualTo(expected).Within(1e-3));

    [Test]
    public void TestHitErrorTickOffsetFollowsTheWindowOfTheDifficulty()
    {
        double meh = JudgementWindows.FromOverallDifficulty(10).MehWindow; // 100 ms

        Assert.That(HitErrorBar.TickOffset(100, meh), Is.EqualTo(280).Within(1e-3));
        Assert.That(HitErrorBar.TickOffset(50, meh), Is.EqualTo(140).Within(1e-3));
    }
}
