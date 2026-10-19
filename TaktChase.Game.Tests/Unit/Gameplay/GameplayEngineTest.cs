using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osuTK;
using TaktChase.Game.Gameplay;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.Gameplay.Timing;

namespace TaktChase.Game.Tests.Unit.Gameplay;

/// <summary>
/// Rules tests for <see cref="GameplayEngine"/>. All beatmaps use OD 5 unless stated: 300 within 50 ms, 100 within 100 ms,
/// 50 within 150 ms (all inclusive), a press up to 400 ms early is a Miss, and an object is missed once 150 ms have passed.
/// Circles have radius 32 (CS 5).
/// </summary>
[TestFixture]
public class GameplayEngineTest
{
    private static readonly Vector2 origin = TestBeatmaps.Origin;

    [Test]
    public void TestHitAtExactTime()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        var judgement = engine.RegisterInput(1000, origin);

        Assert.That(judgement, Is.Not.Null);
        Assert.That(judgement!.Value.Result, Is.EqualTo(HitResult.Great));
        Assert.That(judgement.Value.TimingError, Is.EqualTo(0));
        Assert.That(judgement.Value.Index, Is.EqualTo(0));
        Assert.That(judgement.Value.Time, Is.EqualTo(1000));
        Assert.That(engine.Score.Score, Is.EqualTo(300));
        Assert.That(engine.Score.Combo, Is.EqualTo(1));
        Assert.That(engine.Score.Accuracy, Is.EqualTo(1));
        Assert.That(engine.IsFinished, Is.True);
    }

    [TestCase(-150, HitResult.Meh)]
    [TestCase(-100.5, HitResult.Meh)]
    [TestCase(-100, HitResult.Ok)]
    [TestCase(-50.5, HitResult.Ok)]
    [TestCase(-50, HitResult.Great)]
    [TestCase(0, HitResult.Great)]
    [TestCase(50, HitResult.Great)]
    [TestCase(50.5, HitResult.Ok)]
    [TestCase(100, HitResult.Ok)]
    [TestCase(100.5, HitResult.Meh)]
    [TestCase(150, HitResult.Meh)]
    [TestCase(-150.5, HitResult.Miss)]
    [TestCase(-300, HitResult.Miss)]
    [TestCase(-400, HitResult.Miss)]
    public void TestJudgementBoundaries(double offset, HitResult expected)
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        var judgement = engine.RegisterInput(1000 + offset, origin);

        Assert.That(judgement, Is.Not.Null);
        Assert.That(judgement!.Value.Result, Is.EqualTo(expected));
        Assert.That(judgement.Value.TimingError, Is.EqualTo(offset).Within(1e-9));
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));
    }

    [Test]
    public void TestInputBeforeTheMissWindowIsIgnored()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        Assert.That(engine.RegisterInput(1000 - 400.5, origin), Is.Null);
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(0));
        Assert.That(engine.IsJudged(0), Is.False);

        // The object is still hittable afterwards.
        Assert.That(engine.RegisterInput(1000, origin)!.Value.Result, Is.EqualTo(HitResult.Great));
    }

    [Test]
    public void TestEarlyPressInsideMissWindowMissesAndConsumesTheObject()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        var early = engine.RegisterInput(800, origin);

        Assert.That(early!.Value.Result, Is.EqualTo(HitResult.Miss));
        Assert.That(early.Value.TimingError, Is.EqualTo(-200));
        Assert.That(engine.Score.Combo, Is.EqualTo(0));
        Assert.That(engine.Score.HitErrors, Is.Empty, "A premature press is not a successful hit.");
        Assert.That(engine.IsFinished, Is.True);

        Assert.That(engine.RegisterInput(1000, origin), Is.Null, "The object cannot be judged a second time.");
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));
    }

    [Test]
    public void TestObjectIsMissedWhenItsWindowElapses()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        var judgements = new List<JudgementEvent>();
        engine.Judged += judgements.Add;

        engine.Update(1150);
        Assert.That(judgements, Is.Empty, "The 50 window is inclusive.");

        engine.Update(1150.001);
        Assert.That(judgements, Has.Count.EqualTo(1));
        Assert.That(judgements[0].Result, Is.EqualTo(HitResult.Miss));
        Assert.That(judgements[0].TimingError, Is.Null);
        Assert.That(judgements[0].Time, Is.EqualTo(1150), "Stamped with the moment the window elapsed.");
        Assert.That(engine.Score.Combo, Is.EqualTo(0));
        Assert.That(engine.Score.Accuracy, Is.EqualTo(0));
    }

    [Test]
    public void TestMissTimeDoesNotDependOnTheUpdateTime()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        JudgementEvent? seen = null;
        engine.Judged += j => seen = j;

        engine.Update(60000);

        Assert.That(seen!.Value.Time, Is.EqualTo(1150));
    }

    [Test]
    public void TestLateInputAfterTheWindowFindsTheObjectAlreadyMissed()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        Assert.That(engine.RegisterInput(1150.5, origin), Is.Null);
        Assert.That(engine.Score.CountOf(HitResult.Miss), Is.EqualTo(1));
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));
    }

    [Test]
    public void TestResultsDoNotDependOnHowOftenUpdateIsCalled()
    {
        // (time, press) script; presses are processed before the Update of the same tick, like framework input events.
        var presses = new (double time, int index)[] { (1010, 0), (1620, 2), (1980, 3) };

        List<(int, HitResult, double?, double)> run(double step)
        {
            var map = TestBeatmaps.Spread(1000, 1300, 1600, 1900);
            var engine = new GameplayEngine(map);
            var result = new List<(int, HitResult, double?, double)>();
            engine.Judged += j => result.Add((j.Index, j.Result, j.TimingError, j.Time));

            int next = 0;

            for (double t = 0; t <= 4000; t += step)
            {
                while (next < presses.Length && presses[next].time <= t)
                {
                    engine.RegisterInput(presses[next].time, TestBeatmaps.PositionOf(presses[next].index));
                    next++;
                }

                engine.Update(t);
            }

            return result;
        }

        var reference = run(1);

        Assert.That(reference, Has.Count.EqualTo(4));
        Assert.That(reference.Select(r => r.Item2), Is.EqualTo(new[] { HitResult.Great, HitResult.Miss, HitResult.Great, HitResult.Ok }));

        foreach (double step in new[] { 7, 16.6, 33.3, 100, 250 })
            Assert.That(run(step), Is.EqualTo(reference), $"update step {step}");
    }

    [Test]
    public void TestOneInputJudgesAtMostOneObject()
    {
        var engine = new GameplayEngine(TestBeatmaps.Create(new HitCircle(1000, origin), new HitCircle(1010, origin)));

        var first = engine.RegisterInput(1005, origin);
        Assert.That(first!.Value.Index, Is.EqualTo(0));
        Assert.That(engine.IsJudged(0), Is.True);
        Assert.That(engine.IsJudged(1), Is.False, "One press must not judge both stacked circles.");
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));

        var second = engine.RegisterInput(1005, origin);
        Assert.That(second!.Value.Index, Is.EqualTo(1));

        Assert.That(engine.RegisterInput(1005, origin), Is.Null);
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(2));
    }

    [Test]
    public void TestEachObjectIsJudgedExactlyOnce()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        int raised = 0;
        engine.Judged += _ => raised++;

        Assert.That(engine.RegisterInput(1000, origin), Is.Not.Null);
        Assert.That(engine.RegisterInput(1000, origin), Is.Null, "A duplicate input at the same time hits nothing.");
        Assert.That(engine.RegisterInput(1001, origin), Is.Null);

        engine.Update(5000);

        Assert.That(raised, Is.EqualTo(1));
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));
        Assert.That(engine.Score.CountOf(HitResult.Miss), Is.EqualTo(0));
    }

    [Test]
    public void TestAutoMissedObjectCannotBeHitLater()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        engine.Update(1200);

        Assert.That(engine.RegisterInput(1200, origin), Is.Null);
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(1));
        Assert.That(engine.Score.CountOf(HitResult.Miss), Is.EqualTo(1));
    }

    [Test]
    public void TestCloseObjectsAreJudgedInTheOrderTheyAreHit()
    {
        // B is hit first although A is earlier: A's hit time has already passed, so A does not block B.
        var a = new HitCircle(1000, new Vector2(100, 100));
        var b = new HitCircle(1040, new Vector2(300, 100));
        var engine = new GameplayEngine(TestBeatmaps.Create(a, b));

        var hitB = engine.RegisterInput(1020, b.Position);
        Assert.That(hitB!.Value.Index, Is.EqualTo(1));
        Assert.That(hitB.Value.Result, Is.EqualTo(HitResult.Great));
        Assert.That(hitB.Value.TimingError, Is.EqualTo(-20));

        var hitA = engine.RegisterInput(1030, a.Position);
        Assert.That(hitA!.Value.Index, Is.EqualTo(0));
        Assert.That(hitA.Value.TimingError, Is.EqualTo(30));
        Assert.That(engine.IsFinished, Is.True);
    }

    [Test]
    public void TestOverlappingObjectsGoToTheEarliestFirst()
    {
        // Two circles whose areas overlap; the pointer is inside both. The earlier object takes the press.
        var a = new HitCircle(1000, new Vector2(100, 100));
        var b = new HitCircle(1100, new Vector2(120, 100));
        var engine = new GameplayEngine(TestBeatmaps.Create(a, b));

        var judgement = engine.RegisterInput(1000, new Vector2(110, 100));

        Assert.That(judgement!.Value.Index, Is.EqualTo(0));
        Assert.That(engine.IsJudged(1), Is.False);
    }

    [Test]
    public void TestLaterObjectIsLockedUntilTheEarlierOneHasHadItsTime()
    {
        var a = new HitCircle(1000, new Vector2(100, 100));
        var b = new HitCircle(1100, new Vector2(400, 100));
        var engine = new GameplayEngine(TestBeatmaps.Create(a, b));

        Assert.That(engine.RegisterInput(900, b.Position), Is.Null, "B is inside its window but A has not reached its hit time.");
        Assert.That(engine.RegisterInput(999.9, b.Position), Is.Null);
        Assert.That(engine.IsJudged(1), Is.False);

        var unlocked = engine.RegisterInput(1000, b.Position);
        Assert.That(unlocked!.Value.Index, Is.EqualTo(1));
        Assert.That(unlocked.Value.TimingError, Is.EqualTo(-100));
        Assert.That(unlocked.Value.Result, Is.EqualTo(HitResult.Ok));
    }

    [Test]
    public void TestPressOnTheEarlierObjectIsNotBlockedByTheLaterOne()
    {
        var a = new HitCircle(1000, new Vector2(100, 100));
        var b = new HitCircle(1100, new Vector2(400, 100));
        var engine = new GameplayEngine(TestBeatmaps.Create(a, b));

        var judgement = engine.RegisterInput(900, a.Position);

        Assert.That(judgement!.Value.Index, Is.EqualTo(0));
        Assert.That(judgement.Value.Result, Is.EqualTo(HitResult.Ok));
    }

    [TestCase(32, 0, true)]
    [TestCase(-32, 0, true)]
    [TestCase(0, 32, true)]
    [TestCase(0, -32, true)]
    [TestCase(32.01, 0, false)]
    [TestCase(22, 22, true)]
    [TestCase(23, 23, false)]
    public void TestHitAreaFollowsCircleSize(float dx, float dy, bool expectedHit)
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000)); // CS 5: radius 32

        var judgement = engine.RegisterInput(1000, origin + new Vector2(dx, dy));

        Assert.That(judgement.HasValue, Is.EqualTo(expectedHit));
        Assert.That(engine.Score.JudgedCount, Is.EqualTo(expectedHit ? 1 : 0), "A press outside every circle costs nothing.");
    }

    [Test]
    public void TestSmallerCirclesHaveSmallerHitAreas()
    {
        var map = TestBeatmaps.Create(new BeatmapDifficulty(5, 5, 8), new HitCircle(1000, origin)); // CS 8: radius 18.56
        var engine = new GameplayEngine(map);

        Assert.That(engine.RegisterInput(1000, origin + new Vector2(25, 0)), Is.Null);
        Assert.That(engine.RegisterInput(1000, origin + new Vector2(20, 0)), Is.Null);
        Assert.That(engine.RegisterInput(1000, origin + new Vector2(18, 0)), Is.Not.Null);
    }

    [Test]
    public void TestOverallDifficultyControlsTheWindows()
    {
        var easy = new GameplayEngine(TestBeatmaps.Create(new BeatmapDifficulty(0, 5, 5), new HitCircle(1000, origin)));
        var hard = new GameplayEngine(TestBeatmaps.Create(new BeatmapDifficulty(10, 5, 5), new HitCircle(1000, origin)));

        Assert.That(easy.RegisterInput(1070, origin)!.Value.Result, Is.EqualTo(HitResult.Great)); // OD0: 300 within 80 ms
        Assert.That(hard.RegisterInput(1070, origin)!.Value.Result, Is.EqualTo(HitResult.Meh)); // OD10: 100 within 60 ms, 50 within 100 ms
    }

    [Test]
    public void TestApproachRateAndCircleSizeDoNotChangeTheWindows()
    {
        var slow = new GameplayEngine(TestBeatmaps.Create(new BeatmapDifficulty(5, 0, 5), new HitCircle(1000, origin)));
        var fast = new GameplayEngine(TestBeatmaps.Create(new BeatmapDifficulty(5, 10, 5), new HitCircle(1000, origin)));

        Assert.That(slow.Windows.GreatWindow, Is.EqualTo(fast.Windows.GreatWindow));
        Assert.That(slow.RegisterInput(1075, origin)!.Value.Result, Is.EqualTo(fast.RegisterInput(1075, origin)!.Value.Result));
    }

    [Test]
    public void TestExplicitWindowsOverrideTheBeatmapDifficulty()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000), new JudgementWindows(10, 20, 30, 40));

        Assert.That(engine.RegisterInput(1025, origin)!.Value.Result, Is.EqualTo(HitResult.Meh));
    }

    [Test]
    public void TestTimeNeverMovesBackwards()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));

        engine.Update(2000);
        Assert.That(engine.CurrentTime, Is.EqualTo(2000));

        engine.Update(1500);
        Assert.That(engine.CurrentTime, Is.EqualTo(2000));

        Assert.That(engine.RegisterInput(1000, origin), Is.Null, "An out-of-order input is treated as happening now, after the miss.");
        Assert.That(engine.CurrentTime, Is.EqualTo(2000));
    }

    [Test]
    public void TestComboAcrossHitsAndMisses()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000, 1500, 2000, 2500));

        engine.RegisterInput(1000, origin);
        engine.RegisterInput(1500, origin);
        Assert.That(engine.Score.Combo, Is.EqualTo(2));

        engine.Update(2151); // the third object's window elapsed
        Assert.That(engine.Score.Combo, Is.EqualTo(0));

        engine.RegisterInput(2500, origin);
        Assert.That(engine.Score.Combo, Is.EqualTo(1));
        Assert.That(engine.Score.MaxCombo, Is.EqualTo(2));
        Assert.That(engine.Score.CountOf(HitResult.Miss), Is.EqualTo(1));
    }

    [Test]
    public void TestScoreAccuracyAndHitErrorsComeFromTheJudgements()
    {
        var engine = new GameplayEngine(TestBeatmaps.Spread(1000, 2000, 3000, 4000));

        engine.RegisterInput(1000, TestBeatmaps.PositionOf(0)); // 300, error 0
        engine.RegisterInput(2060, TestBeatmaps.PositionOf(1)); // 100, error +60
        engine.RegisterInput(3120, TestBeatmaps.PositionOf(2)); // 50, error +120
        engine.Update(4200); // the last object is missed

        var score = engine.Score;
        Assert.That(score.CountOf(HitResult.Great), Is.EqualTo(1));
        Assert.That(score.CountOf(HitResult.Ok), Is.EqualTo(1));
        Assert.That(score.CountOf(HitResult.Meh), Is.EqualTo(1));
        Assert.That(score.CountOf(HitResult.Miss), Is.EqualTo(1));
        Assert.That(score.Accuracy, Is.EqualTo(450.0 / 1200.0).Within(1e-12));
        Assert.That(score.Score, Is.EqualTo(300 + 102 + 52)); // 300*40/40, 100*41/40, 50*42/40 (integer division)
        Assert.That(score.HitErrors, Is.EqualTo(new[] { 0.0, 60.0, 120.0 }));
        Assert.That(score.MeanHitError, Is.EqualTo(60).Within(1e-9));
        Assert.That(score.HitErrorDeviation, Is.EqualTo(Math.Sqrt(2400)).Within(1e-9));
    }

    [Test]
    public void TestJudgedEventIsRaisedAfterTheScoreWasUpdated()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000, 1500));
        var judgedCounts = new List<int>();
        engine.Judged += j => judgedCounts.Add(engine.Score.JudgedCount);

        engine.RegisterInput(1000, origin);
        engine.Update(5000);

        Assert.That(judgedCounts, Is.EqualTo(new[] { 1, 2 }));
    }

    [Test]
    public void TestFinishedIsRaisedOnceWhenTheLastObjectIsHit()
    {
        var engine = new GameplayEngine(TestBeatmaps.Spread(1000, 1300));
        int finished = 0;
        engine.Finished += () => finished++;

        engine.RegisterInput(1000, TestBeatmaps.PositionOf(0));
        Assert.That(finished, Is.EqualTo(0));
        Assert.That(engine.IsFinished, Is.False);

        engine.RegisterInput(1300, TestBeatmaps.PositionOf(1));
        Assert.That(finished, Is.EqualTo(1));
        Assert.That(engine.IsFinished, Is.True);

        engine.Update(9000);
        engine.RegisterInput(9000, origin);
        Assert.That(finished, Is.EqualTo(1));
    }

    [Test]
    public void TestFinishedIsRaisedOnceWhenTheLastObjectIsMissed()
    {
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000, 1500));
        int finished = 0;
        engine.Finished += () => finished++;

        engine.Update(1151);
        Assert.That(finished, Is.EqualTo(0));

        engine.Update(1651);
        engine.Update(2000);
        Assert.That(finished, Is.EqualTo(1));
        Assert.That(engine.Score.Accuracy, Is.EqualTo(0));
    }

    [Test]
    public void TestEmptyBeatmapFinishesOnTheFirstUpdate()
    {
        var engine = new GameplayEngine(TestBeatmaps.Create());
        int finished = 0;
        engine.Finished += () => finished++;

        Assert.That(engine.TotalObjects, Is.EqualTo(0));
        Assert.That(engine.IsFinished, Is.True);
        Assert.That(finished, Is.EqualTo(0));

        engine.Update(0);
        engine.Update(1000);

        Assert.That(finished, Is.EqualTo(1));
    }

    [Test]
    public void TestAudioOffsetShiftsTheTimeBasisOfInputs()
    {
        // Sound reaches the player 60 ms late: the player taps when the track is at 1060 after hearing the beat at 1000.
        const double latency = 60;
        var late = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        var judgement = late.RegisterInput(AudioOffsetConverter.ToGameplayTime(1060, latency), origin);

        Assert.That(judgement!.Value.Result, Is.EqualTo(HitResult.Great));
        Assert.That(judgement.Value.TimingError, Is.EqualTo(0));

        // The same tap without compensation is 60 ms late.
        var uncompensated = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        Assert.That(uncompensated.RegisterInput(1060, origin)!.Value.TimingError, Is.EqualTo(60));
    }

    [Test]
    public void TestAudioOffsetSignInHitErrors()
    {
        // With a positive offset a tap at the beat's track time is EARLY relative to what the player hears (error < 0).
        var positive = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        Assert.That(positive.RegisterInput(AudioOffsetConverter.ToGameplayTime(1000, 60), origin)!.Value.TimingError, Is.EqualTo(-60));

        // With a negative offset (sound arrives early) the same tap is late.
        var negative = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        Assert.That(negative.RegisterInput(AudioOffsetConverter.ToGameplayTime(1000, -40), origin)!.Value.TimingError, Is.EqualTo(40));
    }

    [Test]
    public void TestMissTimesUseTheSameBasisAsObjectTimes()
    {
        // Track time 1300 with a 100 ms offset is gameplay time 1200: the object at 1000 was missed at 1150 (gameplay time).
        var engine = new GameplayEngine(TestBeatmaps.AtOrigin(1000));
        JudgementEvent? miss = null;
        engine.Judged += j => miss = j;

        engine.Update(AudioOffsetConverter.ToGameplayTime(1300, 100));

        Assert.That(miss!.Value.Time, Is.EqualTo(1150));
    }
}
