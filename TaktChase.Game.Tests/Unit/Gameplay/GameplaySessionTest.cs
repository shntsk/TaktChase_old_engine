using NUnit.Framework;
using TaktChase.Game.Gameplay;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Tests.Unit.Gameplay;

/// <summary>Pause/resume and completion flow. The real clock stops while paused, so a paused period supplies no new gameplay time.</summary>
[TestFixture]
public class GameplaySessionTest
{
    private static readonly osuTK.Vector2 origin = TestBeatmaps.Origin;

    private static GameplaySession create(params double[] times) => new GameplaySession(new GameplayEngine(TestBeatmaps.AtOrigin(times)));

    [Test]
    public void TestFramesAdvanceTheEngineWhilePlaying()
    {
        var session = create(1000);

        session.Frame(500);
        Assert.That(session.Engine.CurrentTime, Is.EqualTo(500));

        session.Frame(1200);
        Assert.That(session.Engine.Score.CountOf(HitResult.Miss), Is.EqualTo(1));
    }

    [Test]
    public void TestPausedSessionDoesNotAdvanceOrMissObjects()
    {
        var session = create(1000);
        session.Frame(900);

        Assert.That(session.TogglePause(), Is.True);
        Assert.That(session.IsPaused, Is.True);

        // Even if a frame arrives with a much later time, nothing is judged while paused.
        session.Frame(5000);

        Assert.That(session.Engine.CurrentTime, Is.EqualTo(900));
        Assert.That(session.Engine.Score.JudgedCount, Is.EqualTo(0));
    }

    [Test]
    public void TestPressesAreIgnoredWhilePaused()
    {
        var session = create(1000);
        session.Frame(990);
        session.TogglePause();

        Assert.That(session.Press(1000, origin), Is.Null);
        Assert.That(session.Engine.IsJudged(0), Is.False);
        Assert.That(session.Engine.Score.JudgedCount, Is.EqualTo(0));
    }

    [Test]
    public void TestPausedDurationIsNotGameplayTime()
    {
        var session = create(1000);
        session.Frame(900);
        session.TogglePause();

        // Many frames pass in real time while the clock is stopped at 900.
        for (int i = 0; i < 600; i++)
            session.Frame(900);

        session.TogglePause();
        Assert.That(session.IsPaused, Is.False);

        session.Frame(900);
        Assert.That(session.Engine.IsJudged(0), Is.False, "The object must not be missed because of the pause.");

        var judgement = session.Press(1000, origin);
        Assert.That(judgement!.Value.Result, Is.EqualTo(HitResult.Great));
        Assert.That(judgement.Value.TimingError, Is.EqualTo(0));
    }

    [Test]
    public void TestResumeWithASlightlyEarlierClockDoesNotMoveTimeBackwards()
    {
        var session = create(1000);
        session.Frame(900);
        session.TogglePause();
        session.TogglePause();

        // The audio position after a resume can be a little behind the last frame before the pause.
        session.Frame(895);

        Assert.That(session.Engine.CurrentTime, Is.EqualTo(900));
        Assert.That(session.Engine.Score.JudgedCount, Is.EqualTo(0));
    }

    [Test]
    public void TestObjectsStillMissOnceTimeAdvancesAfterResume()
    {
        var session = create(1000);
        session.Frame(900);
        session.TogglePause();
        session.TogglePause();

        session.Frame(1151);

        Assert.That(session.Engine.Score.CountOf(HitResult.Miss), Is.EqualTo(1));
    }

    [Test]
    public void TestPauseTogglesBackAndForth()
    {
        var session = create(1000);

        Assert.That(session.TogglePause(), Is.True);
        Assert.That(session.IsPaused, Is.True);
        Assert.That(session.TogglePause(), Is.True);
        Assert.That(session.IsPaused, Is.False);
    }

    [Test]
    public void TestCannotPauseOnceFinished()
    {
        var session = create(1000);
        session.Press(1000, origin);

        Assert.That(session.Engine.IsFinished, Is.True);
        Assert.That(session.TogglePause(), Is.False);
        Assert.That(session.IsPaused, Is.False);
    }

    [Test]
    public void TestPressesAfterTheLastObjectAreIgnored()
    {
        var session = create(1000);
        session.Press(1000, origin);

        Assert.That(session.Press(1001, origin), Is.Null);
        Assert.That(session.Engine.Score.JudgedCount, Is.EqualTo(1));
    }

    [Test]
    public void TestFinishTimeIsRecordedByTheFirstFrameAfterTheLastJudgement()
    {
        var session = create(1000);
        Assert.That(session.FinishedAt, Is.Null);

        session.Frame(1000);
        Assert.That(session.FinishedAt, Is.Null);

        session.Press(1000, origin);
        session.Frame(1016);

        Assert.That(session.FinishedAt, Is.EqualTo(1016));

        session.Frame(1032);
        Assert.That(session.FinishedAt, Is.EqualTo(1016), "The finish time is recorded once.");
    }

    [Test]
    public void TestResultsAppearAfterTheDelayExactlyOnce()
    {
        var session = create(1000);
        session.Press(1000, origin);
        session.Frame(1016);

        Assert.That(session.ShouldShowResults(1016), Is.False);
        Assert.That(session.ShouldShowResults(1016 + GameplaySession.RESULTS_DELAY - 1), Is.False);
        Assert.That(session.ShouldShowResults(1016 + GameplaySession.RESULTS_DELAY), Is.True);

        session.MarkResultsShown();

        Assert.That(session.ResultsShown, Is.True);
        Assert.That(session.ShouldShowResults(10000), Is.False);
    }

    [Test]
    public void TestResultsAreNotShownBeforeTheMapIsFinished()
    {
        var session = create(1000, 2000);
        session.Frame(1100);

        Assert.That(session.ShouldShowResults(100000), Is.False);
    }

    [Test]
    public void TestMapFinishedByMissesShowsResults()
    {
        var session = create(1000);
        session.Frame(1151);
        Assert.That(session.FinishedAt, Is.EqualTo(1151));
        Assert.That(session.ShouldShowResults(1151 + GameplaySession.RESULTS_DELAY), Is.True);
    }
}
