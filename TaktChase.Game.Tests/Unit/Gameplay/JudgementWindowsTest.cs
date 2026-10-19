using System;
using NUnit.Framework;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class JudgementWindowsTest
{
    // OD 5: 300 within 50 ms, 100 within 100 ms, 50 within 150 ms, early press counts as a Miss up to 400 ms.
    private readonly JudgementWindows windows = JudgementWindows.FromOverallDifficulty(5);

    [Test]
    public void TestFromOverallDifficulty()
    {
        Assert.That(windows.GreatWindow, Is.EqualTo(50));
        Assert.That(windows.OkWindow, Is.EqualTo(100));
        Assert.That(windows.MehWindow, Is.EqualTo(150));
        Assert.That(windows.MissWindow, Is.EqualTo(400));
    }

    [TestCase(0, HitResult.Great)]
    [TestCase(49.999, HitResult.Great)]
    [TestCase(50, HitResult.Great)]
    [TestCase(-50, HitResult.Great)]
    [TestCase(50.001, HitResult.Ok)]
    [TestCase(-50.001, HitResult.Ok)]
    [TestCase(100, HitResult.Ok)]
    [TestCase(-100, HitResult.Ok)]
    [TestCase(100.001, HitResult.Meh)]
    [TestCase(-100.001, HitResult.Meh)]
    [TestCase(150, HitResult.Meh)]
    [TestCase(-150, HitResult.Meh)]
    [TestCase(-150.001, HitResult.Miss)]
    [TestCase(-250, HitResult.Miss)]
    [TestCase(-400, HitResult.Miss)]
    public void TestEvaluateBoundaries(double error, HitResult expected)
        => Assert.That(windows.Evaluate(error), Is.EqualTo(expected));

    [TestCase(150.001)]
    [TestCase(400)]
    [TestCase(10000)]
    [TestCase(-400.001)]
    [TestCase(-10000)]
    [TestCase(double.NaN)]
    public void TestEvaluateOutsideEveryWindowIsNull(double error)
        => Assert.That(windows.Evaluate(error), Is.Null);

    [Test]
    public void TestSymmetryOfSuccessfulWindows()
    {
        for (double error = 0; error <= 150; error += 0.25)
            Assert.That(windows.Evaluate(error), Is.EqualTo(windows.Evaluate(-error)), $"error {error}");
    }

    [Test]
    public void TestWindowsAtOtherOverallDifficulties()
    {
        var easy = JudgementWindows.FromOverallDifficulty(0);
        var hard = JudgementWindows.FromOverallDifficulty(10);

        Assert.That(easy.Evaluate(80), Is.EqualTo(HitResult.Great));
        Assert.That(easy.Evaluate(80.5), Is.EqualTo(HitResult.Ok));
        Assert.That(hard.Evaluate(20), Is.EqualTo(HitResult.Great));
        Assert.That(hard.Evaluate(20.5), Is.EqualTo(HitResult.Ok));
        Assert.That(hard.Evaluate(100.5), Is.Null);
    }

    [Test]
    public void TestInvalidWindowsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementWindows(0, 100, 150, 400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementWindows(50, 40, 150, 400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementWindows(50, 100, 90, 400));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementWindows(50, 100, 150, 140));
        Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementWindows(double.NaN, 100, 150, 400));
    }

    [Test]
    public void TestEqualWindowsAreAllowed()
        => Assert.DoesNotThrow(() => new JudgementWindows(50, 50, 50, 50));
}
