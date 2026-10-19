using System;
using NUnit.Framework;
using TaktChase.Game.Gameplay.Timing;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class InputTimeEstimatorTest
{
    // One tick = one millisecond, so timestamps read as milliseconds in the tests.
    private static InputTimeEstimator create(double cap = InputTimeEstimator.DEFAULT_MAX_EXTRAPOLATION) => new InputTimeEstimator(1000, cap);

    [Test]
    public void TestHasNoFrameInitially()
    {
        Assert.That(create().HasFrame, Is.False);

        var estimator = create();
        estimator.RecordFrame(0, 0, 1, true);
        Assert.That(estimator.HasFrame, Is.True);
    }

    [Test]
    public void TestEstimateExtrapolatesFromTheLastFrame()
    {
        var estimator = create();
        estimator.RecordFrame(1000, 5000, 1, true);

        Assert.That(estimator.Estimate(5000), Is.EqualTo(1000));
        Assert.That(estimator.Estimate(5016), Is.EqualTo(1016).Within(1e-9));
        Assert.That(estimator.Estimate(5100), Is.EqualTo(1100).Within(1e-9));
    }

    [Test]
    public void TestEstimateIsCloserToTheTruthThanTheLastFrame()
    {
        // The frame was processed at gameplay time 1000; the key event reaches the game 15 ms later.
        var estimator = create();
        estimator.RecordFrame(1000, 0, 1, true);

        double truth = 1015;
        double estimate = estimator.Estimate(15);

        Assert.That(Math.Abs(estimate - truth), Is.LessThan(Math.Abs(1000 - truth)));
    }

    [TestCase(0.5, 1008)]
    [TestCase(1, 1016)]
    [TestCase(2, 1032)]
    public void TestEstimateScalesWithTheClockRate(double rate, double expected)
    {
        var estimator = create();
        estimator.RecordFrame(1000, 0, rate, true);

        Assert.That(estimator.Estimate(16), Is.EqualTo(expected).Within(1e-9));
    }

    [Test]
    public void TestPausedClockIsNeverExtrapolated()
    {
        var estimator = create();
        estimator.RecordFrame(1000, 0, 1, false);

        Assert.That(estimator.Estimate(500), Is.EqualTo(1000));
    }

    [Test]
    public void TestFreezeStopsExtrapolationUntilTheNextFrame()
    {
        var estimator = create();
        estimator.RecordFrame(1000, 0, 1, true);
        estimator.Freeze();

        Assert.That(estimator.Estimate(50), Is.EqualTo(1000));

        estimator.RecordFrame(1000, 60, 1, true);
        Assert.That(estimator.Estimate(70), Is.EqualTo(1010).Within(1e-9));
    }

    [Test]
    public void TestZeroOrNegativeRateIsNotExtrapolated()
    {
        var estimator = create();

        estimator.RecordFrame(1000, 0, 0, true);
        Assert.That(estimator.Estimate(50), Is.EqualTo(1000));

        estimator.RecordFrame(1000, 0, -1, true);
        Assert.That(estimator.Estimate(50), Is.EqualTo(1000));
    }

    [Test]
    public void TestEstimateNeverGoesBeforeTheLastFrame()
    {
        var estimator = create();
        estimator.RecordFrame(1000, 100, 1, true);

        Assert.That(estimator.Estimate(50), Is.EqualTo(1000));
        Assert.That(estimator.Estimate(100), Is.EqualTo(1000));
    }

    [Test]
    public void TestExtrapolationIsCapped()
    {
        var estimator = create(cap: 250);
        estimator.RecordFrame(1000, 0, 1, true);

        Assert.That(estimator.Estimate(1000), Is.EqualTo(1250).Within(1e-9));
        Assert.That(estimator.Estimate(60000), Is.EqualTo(1250).Within(1e-9));
    }

    [Test]
    public void TestWorksDuringTheNegativeLeadIn()
    {
        var estimator = create();
        estimator.RecordFrame(-500, 0, 1, true);

        Assert.That(estimator.Estimate(16), Is.EqualTo(-484).Within(1e-9));
    }

    [Test]
    public void TestHighResolutionTimestamps()
    {
        // 10 MHz, like a typical Stopwatch.Frequency: 160,000 ticks are 16 ms.
        var estimator = new InputTimeEstimator(10_000_000);
        estimator.RecordFrame(2000, 1_000_000, 1, true);

        Assert.That(estimator.Estimate(1_160_000), Is.EqualTo(2016).Within(1e-9));
    }

    [Test]
    public void TestRecordingAgainReplacesThePreviousFrame()
    {
        var estimator = create();
        estimator.RecordFrame(1000, 0, 1, true);
        estimator.RecordFrame(1016, 16, 1, true);

        Assert.That(estimator.Estimate(24), Is.EqualTo(1024).Within(1e-9));
    }

    [Test]
    public void TestInvalidArgumentsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputTimeEstimator(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputTimeEstimator(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputTimeEstimator(1000, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputTimeEstimator(1000, double.NaN));
    }

    [Test]
    public void TestDefaultEstimatorUsesTheStopwatchFrequency()
    {
        var estimator = InputTimeEstimator.CreateDefault();
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        estimator.RecordFrame(100, now, 1, true);

        double estimate = estimator.Estimate(now + System.Diagnostics.Stopwatch.Frequency / 100); // 10 ms later

        Assert.That(estimate, Is.EqualTo(110).Within(0.01));
    }
}
