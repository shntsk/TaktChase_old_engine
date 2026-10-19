using NUnit.Framework;
using TaktChase.Game.Gameplay;

namespace TaktChase.Game.Tests.Unit.Gameplay;

/// <summary>What a touch-down does in each state of a session. Real touchscreen delivery is not covered here (it needs a device).</summary>
[TestFixture]
public class TouchInputMapperTest
{
    private static GameplaySession create(params double[] times) => new GameplaySession(new GameplayEngine(TestBeatmaps.AtOrigin(times)));

    [Test]
    public void TestRunningSessionTreatsATouchAsAPress()
    {
        var session = create(1000);
        session.Frame(900);

        Assert.That(TouchInputMapper.Map(session), Is.EqualTo(TouchAction.Press));
    }

    [Test]
    public void TestPausedSessionTreatsATouchAsAResume()
    {
        var session = create(1000);
        session.Frame(900);
        session.Pause();

        Assert.That(TouchInputMapper.Map(session), Is.EqualTo(TouchAction.Resume));
    }

    [Test]
    public void TestTouchAfterResumeIsAPressAgain()
    {
        var session = create(1000);
        session.Frame(900);
        session.Pause();
        session.Resume();

        Assert.That(TouchInputMapper.Map(session), Is.EqualTo(TouchAction.Press));
    }

    [Test]
    public void TestResultsScreenTreatsATouchAsARestart()
    {
        var session = create(1000);
        session.Press(1000, TestBeatmaps.Origin);
        session.Frame(1000);
        session.Frame(1000 + GameplaySession.RESULTS_DELAY);

        Assert.That(session.ShouldShowResults(1000 + GameplaySession.RESULTS_DELAY), Is.True);
        session.MarkResultsShown();

        Assert.That(TouchInputMapper.Map(session), Is.EqualTo(TouchAction.Restart));
    }

    [Test]
    public void TestTouchBetweenFinishAndResultsIsStillAPressThatIsIgnoredByTheSession()
    {
        var session = create(1000);
        session.Press(1000, TestBeatmaps.Origin);
        session.Frame(1000);

        // The results are not shown yet, so this is not a restart; the session ignores presses after the last object.
        Assert.That(TouchInputMapper.Map(session), Is.EqualTo(TouchAction.Press));
        Assert.That(session.Press(1001, TestBeatmaps.Origin), Is.Null);
    }
}
