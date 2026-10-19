using NUnit.Framework;
using TaktChase.Game.Gameplay;

namespace TaktChase.Game.Tests.Unit.Gameplay;

/// <summary>
/// <see cref="GameplaySession.Pause"/> and <see cref="GameplaySession.Resume"/> are idempotent, unlike <c>TogglePause</c>: lifecycle
/// events such as the app losing focus can fire more than once and must never un-pause a session.
/// </summary>
[TestFixture]
public class GameplaySessionPauseTest
{
    private static GameplaySession create(params double[] times) => new GameplaySession(new GameplayEngine(TestBeatmaps.AtOrigin(times)));

    [Test]
    public void TestPauseChangesStateOnlyTheFirstTime()
    {
        var session = create(1000);

        Assert.That(session.Pause(), Is.True);
        Assert.That(session.IsPaused, Is.True);

        Assert.That(session.Pause(), Is.False);
        Assert.That(session.IsPaused, Is.True, "A repeated pause must not resume the session.");
    }

    [Test]
    public void TestResumeChangesStateOnlyWhenPaused()
    {
        var session = create(1000);

        Assert.That(session.Resume(), Is.False);
        Assert.That(session.IsPaused, Is.False);

        session.Pause();

        Assert.That(session.Resume(), Is.True);
        Assert.That(session.IsPaused, Is.False);
        Assert.That(session.Resume(), Is.False);
    }

    [Test]
    public void TestCannotPauseOnceFinished()
    {
        var session = create(1000);
        session.Press(1000, TestBeatmaps.Origin);

        Assert.That(session.Pause(), Is.False);
        Assert.That(session.IsPaused, Is.False);
    }

    [Test]
    public void TestPauseDoesNotLetObjectsBeMissedWhileInTheBackground()
    {
        var session = create(1000);
        session.Frame(900);
        session.Pause();

        // The app is in the background: frames, if any arrive, carry a much later time.
        session.Frame(8000);
        Assert.That(session.Engine.Score.JudgedCount, Is.EqualTo(0));

        session.Resume();
        session.Frame(900);

        var judgement = session.Press(1000, TestBeatmaps.Origin);
        Assert.That(judgement!.Value.TimingError, Is.EqualTo(0));
    }
}
