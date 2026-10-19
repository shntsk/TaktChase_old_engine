using NUnit.Framework;
using osuTK;
using TaktChase.Game.Gameplay;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class ScoreProcessorTest
{
    private int nextIndex;

    private JudgementEvent judgement(HitResult result, double? error = null)
        => new JudgementEvent(new HitCircle(nextIndex * 100, Vector2.Zero), nextIndex++, result, error, 0);

    private static ScoreProcessor apply(params (HitResult result, double? error)[] events)
    {
        var processor = new ScoreProcessor();
        var test = new ScoreProcessorTest();

        foreach (var (result, error) in events)
            processor.Apply(test.judgement(result, error));

        return processor;
    }

    [Test]
    public void TestInitialState()
    {
        var score = new ScoreProcessor();

        Assert.That(score.Score, Is.EqualTo(0));
        Assert.That(score.Combo, Is.EqualTo(0));
        Assert.That(score.MaxCombo, Is.EqualTo(0));
        Assert.That(score.JudgedCount, Is.EqualTo(0));
        Assert.That(score.Accuracy, Is.EqualTo(1));
        Assert.That(score.HitErrors, Is.Empty);
        Assert.That(score.MeanHitError, Is.EqualTo(0));
        Assert.That(score.HitErrorDeviation, Is.EqualTo(0));
    }

    [Test]
    public void TestAccuracyUses300100And50Weights()
    {
        // (300 + 100 + 50 + 0) / (4 * 300)
        var score = apply((HitResult.Great, 0), (HitResult.Ok, 60), (HitResult.Meh, 120), (HitResult.Miss, null));

        Assert.That(score.Accuracy, Is.EqualTo(450.0 / 1200.0).Within(1e-12));
    }

    [Test]
    public void TestAccuracyOfAllGreatIsOneAndOfAllMissIsZero()
    {
        Assert.That(apply((HitResult.Great, 1), (HitResult.Great, -1)).Accuracy, Is.EqualTo(1));
        Assert.That(apply((HitResult.Miss, null), (HitResult.Miss, null)).Accuracy, Is.EqualTo(0));
    }

    [Test]
    public void TestAccuracyOfSingleResults()
    {
        Assert.That(apply((HitResult.Ok, 0)).Accuracy, Is.EqualTo(1.0 / 3.0).Within(1e-12));
        Assert.That(apply((HitResult.Meh, 0)).Accuracy, Is.EqualTo(1.0 / 6.0).Within(1e-12));
    }

    [Test]
    public void TestScoreGrowsWithComboMultiplier()
    {
        var processor = new ScoreProcessor();
        var test = new ScoreProcessorTest();

        processor.Apply(test.judgement(HitResult.Great, 0));
        Assert.That(processor.Score, Is.EqualTo(300)); // combo 0: 300 * 40 / 40

        processor.Apply(test.judgement(HitResult.Great, 0));
        Assert.That(processor.Score, Is.EqualTo(300 + 307)); // combo 1: 300 * 41 / 40, integer division

        processor.Apply(test.judgement(HitResult.Great, 0));
        Assert.That(processor.Score, Is.EqualTo(300 + 307 + 315)); // combo 2: 300 * 42 / 40
    }

    [Test]
    public void TestMultiplierStopsGrowingAtFullCombo()
    {
        var processor = new ScoreProcessor();
        var test = new ScoreProcessorTest();

        for (int i = 0; i < ScoreProcessor.COMBO_STEPS; i++)
            processor.Apply(test.judgement(HitResult.Great, 0));

        long before = processor.Score;
        processor.Apply(test.judgement(HitResult.Great, 0));
        Assert.That(processor.Score - before, Is.EqualTo(600));

        before = processor.Score;
        processor.Apply(test.judgement(HitResult.Great, 0));
        Assert.That(processor.Score - before, Is.EqualTo(600));
    }

    [Test]
    public void TestLowerResultsAwardTheirOwnBasePoints()
    {
        Assert.That(apply((HitResult.Ok, 0)).Score, Is.EqualTo(100));
        Assert.That(apply((HitResult.Meh, 0)).Score, Is.EqualTo(50));
        Assert.That(apply((HitResult.Miss, null)).Score, Is.EqualTo(0));
    }

    [Test]
    public void TestComboIncrementsOnHitsAndResetsOnMiss()
    {
        var processor = new ScoreProcessor();
        var test = new ScoreProcessorTest();

        processor.Apply(test.judgement(HitResult.Great, 0));
        processor.Apply(test.judgement(HitResult.Meh, 120));
        Assert.That(processor.Combo, Is.EqualTo(2));

        long scoreBeforeMiss = processor.Score;
        processor.Apply(test.judgement(HitResult.Miss));
        Assert.That(processor.Combo, Is.EqualTo(0));
        Assert.That(processor.Score, Is.EqualTo(scoreBeforeMiss));
        Assert.That(processor.MaxCombo, Is.EqualTo(2));

        processor.Apply(test.judgement(HitResult.Ok, 60));
        Assert.That(processor.Combo, Is.EqualTo(1));
        Assert.That(processor.MaxCombo, Is.EqualTo(2));
        Assert.That(processor.Score - scoreBeforeMiss, Is.EqualTo(100), "The multiplier starts over after a miss.");
    }

    [Test]
    public void TestMaxComboKeepsTheBestRun()
    {
        var score = apply(
            (HitResult.Great, 0), (HitResult.Great, 0), (HitResult.Great, 0),
            (HitResult.Miss, null),
            (HitResult.Great, 0), (HitResult.Great, 0));

        Assert.That(score.MaxCombo, Is.EqualTo(3));
        Assert.That(score.Combo, Is.EqualTo(2));
    }

    [Test]
    public void TestCountsPerResult()
    {
        var score = apply(
            (HitResult.Great, 0), (HitResult.Great, 0), (HitResult.Ok, 60),
            (HitResult.Meh, 120), (HitResult.Miss, null), (HitResult.Miss, null));

        Assert.That(score.CountOf(HitResult.Great), Is.EqualTo(2));
        Assert.That(score.CountOf(HitResult.Ok), Is.EqualTo(1));
        Assert.That(score.CountOf(HitResult.Meh), Is.EqualTo(1));
        Assert.That(score.CountOf(HitResult.Miss), Is.EqualTo(2));
        Assert.That(score.JudgedCount, Is.EqualTo(6));
    }

    [Test]
    public void TestHitErrorsRecordOnlySuccessfulHits()
    {
        var score = apply((HitResult.Great, -10), (HitResult.Miss, null), (HitResult.Ok, 60));

        Assert.That(score.HitErrors, Is.EqualTo(new[] { -10.0, 60.0 }));
    }

    [Test]
    public void TestEarlyMissWithTimingErrorIsNotAHitError()
    {
        var score = apply((HitResult.Miss, -200));

        Assert.That(score.HitErrors, Is.Empty);
        Assert.That(score.CountOf(HitResult.Miss), Is.EqualTo(1));
        Assert.That(score.Combo, Is.EqualTo(0));
    }

    [Test]
    public void TestMeanAndDeviationOfHitErrors()
    {
        var symmetric = apply((HitResult.Great, -10), (HitResult.Great, 10));
        Assert.That(symmetric.MeanHitError, Is.EqualTo(0).Within(1e-12));
        Assert.That(symmetric.HitErrorDeviation, Is.EqualTo(10).Within(1e-12));

        var late = apply((HitResult.Great, 5), (HitResult.Great, 15));
        Assert.That(late.MeanHitError, Is.EqualTo(10).Within(1e-12));
        Assert.That(late.HitErrorDeviation, Is.EqualTo(5).Within(1e-12));
    }

    [Test]
    public void TestDeviationNeedsTwoHits()
    {
        var score = apply((HitResult.Great, 30));

        Assert.That(score.MeanHitError, Is.EqualTo(30));
        Assert.That(score.HitErrorDeviation, Is.EqualTo(0));
    }
}
