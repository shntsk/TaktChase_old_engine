using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TaktChase.Game.Gameplay;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

[TestFixture]
public class DemoBeatmapTest
{
    private const int expected_object_count = 12 + 32 + 20 + 32 + 16 + 1;

    [Test]
    public void TestBeatmapShape()
    {
        var map = DemoBeatmap.Create();

        Assert.That(map.HitObjects, Has.Count.EqualTo(expected_object_count));
        Assert.That(map.AudioFile, Is.EqualTo(DemoBeatmap.AUDIO_FILE));
        Assert.That(map.FirstObjectTime, Is.EqualTo(DemoBeatmap.FIRST_BEAT * DemoBeatmap.BEAT_LENGTH));
        Assert.That(map.LastObjectTime, Is.EqualTo(80 * DemoBeatmap.BEAT_LENGTH));
        Assert.That(map.LeadInTime, Is.EqualTo(DemoBeatmap.LEAD_IN));
        Assert.That(map.Difficulty, Is.SameAs(BeatmapDifficulty.Default));
    }

    [Test]
    public void TestBeatmapIsDeterministic()
    {
        var a = DemoBeatmap.Create();
        var b = DemoBeatmap.Create();

        Assert.That(a.HitObjects.Select(h => (h.StartTime, h.Position.X, h.Position.Y)),
            Is.EqualTo(b.HitObjects.Select(h => (h.StartTime, h.Position.X, h.Position.Y))));
    }

    [Test]
    public void TestObjectsAreStrictlyOrderedInTimeWithAtLeastAnEighthNoteBetweenThem()
    {
        var objects = DemoBeatmap.Create().HitObjects;

        for (int i = 1; i < objects.Count; i++)
            Assert.That(objects[i].StartTime - objects[i - 1].StartTime, Is.GreaterThanOrEqualTo(DemoBeatmap.BEAT_LENGTH / 2), $"object {i}");
    }

    [Test]
    public void TestEveryCircleIsFullyInsideThePlayfield()
    {
        var map = DemoBeatmap.Create();
        double radius = map.Difficulty.CircleRadius;

        foreach (var obj in map.HitObjects)
        {
            Assert.That(obj.Position.X, Is.GreaterThanOrEqualTo(radius).And.LessThanOrEqualTo(Playfield.WIDTH - radius), $"x at {obj.StartTime}");
            Assert.That(obj.Position.Y, Is.GreaterThanOrEqualTo(radius).And.LessThanOrEqualTo(Playfield.HEIGHT - radius), $"y at {obj.StartTime}");
        }
    }

    [Test]
    public void TestCircleNumbersAreReadable()
    {
        foreach (var circle in DemoBeatmap.Create().HitObjects.Cast<HitCircle>())
            Assert.That(circle.Number, Is.InRange(1, 4));
    }

    [Test]
    public void TestPerfectPlayerGetsEveryResultAtFullCombo()
    {
        var map = DemoBeatmap.Create();
        var engine = new GameplayEngine(map);
        int finished = 0;
        engine.Finished += () => finished++;

        foreach (var obj in map.HitObjects)
        {
            var judgement = engine.RegisterInput(obj.StartTime, obj.Position);

            Assert.That(judgement, Is.Not.Null, $"object at {obj.StartTime}");
            Assert.That(judgement!.Value.Result, Is.EqualTo(HitResult.Great), $"object at {obj.StartTime}");
        }

        Assert.That(finished, Is.EqualTo(1));
        Assert.That(engine.IsFinished, Is.True);
        Assert.That(engine.Score.Accuracy, Is.EqualTo(1));
        Assert.That(engine.Score.Combo, Is.EqualTo(expected_object_count));
        Assert.That(engine.Score.MaxCombo, Is.EqualTo(expected_object_count));
        Assert.That(engine.Score.CountOf(HitResult.Great), Is.EqualTo(expected_object_count));
        Assert.That(engine.Score.Score, Is.EqualTo(61640)); // sum of 300 * (40 + min(combo, 40)) / 40 over 113 hits
        Assert.That(engine.Score.HitErrors.All(e => e == 0), Is.True);
    }

    [Test]
    public void TestConsistentlyLatePlayerKeepsTheirOffsetInTheHitErrors()
    {
        var map = DemoBeatmap.Create();
        var engine = new GameplayEngine(map);

        foreach (var obj in map.HitObjects)
            engine.RegisterInput(obj.StartTime + 40, obj.Position);

        Assert.That(engine.Score.CountOf(HitResult.Great), Is.EqualTo(expected_object_count));
        Assert.That(engine.Score.MeanHitError, Is.EqualTo(40).Within(1e-9));
        Assert.That(engine.Score.HitErrorDeviation, Is.EqualTo(0).Within(1e-9));
    }

    [Test]
    public void TestSloppyPlayerGetsOnlyFiftyResults()
    {
        var map = DemoBeatmap.Create();
        var engine = new GameplayEngine(map);

        foreach (var obj in map.HitObjects)
            engine.RegisterInput(obj.StartTime - 120, obj.Position);

        Assert.That(engine.Score.CountOf(HitResult.Meh), Is.EqualTo(expected_object_count));
        Assert.That(engine.Score.Accuracy, Is.EqualTo(50.0 / 300.0).Within(1e-12));
    }

    [Test]
    public void TestIdlePlayerMissesEverything()
    {
        var map = DemoBeatmap.Create();
        var engine = new GameplayEngine(map);
        var events = new List<JudgementEvent>();
        engine.Judged += events.Add;

        engine.Update(map.LastObjectTime + 1000);

        Assert.That(events, Has.Count.EqualTo(expected_object_count));
        Assert.That(events.All(e => e.Result == HitResult.Miss && e.TimingError == null), Is.True);
        Assert.That(engine.Score.Accuracy, Is.EqualTo(0));
        Assert.That(engine.Score.Combo, Is.EqualTo(0));
        Assert.That(engine.Score.Score, Is.EqualTo(0));
        Assert.That(engine.IsFinished, Is.True);
        Assert.That(events.Select(e => e.Index), Is.EqualTo(Enumerable.Range(0, expected_object_count)));
    }

    [Test]
    public void TestWindowsComeFromTheDemoOverallDifficulty()
    {
        var engine = new GameplayEngine(DemoBeatmap.Create());

        Assert.That(engine.Windows.GreatWindow, Is.EqualTo(50).Within(1e-9));
        Assert.That(engine.Windows.OkWindow, Is.EqualTo(100).Within(1e-9));
        Assert.That(engine.Windows.MehWindow, Is.EqualTo(150).Within(1e-9));
    }
}
