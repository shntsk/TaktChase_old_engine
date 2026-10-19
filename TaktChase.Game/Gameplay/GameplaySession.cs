using osuTK;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Gameplay;

/// <summary>
/// The framework-free control flow of one play session: which inputs and frames reach the <see cref="GameplayEngine"/>,
/// pausing, and when the results should appear. <see cref="GameplayPlayer"/> only translates framework events into calls here.
/// </summary>
/// <remarks>
/// While paused, no frame advances the engine (so no object can be missed) and no press is accepted. Time is supplied by the
/// caller; the real clock stops while paused, so the paused wall-clock duration never shows up as gameplay time.
/// </remarks>
public sealed class GameplaySession
{
    /// <summary>How long after the last judgement the results appear, in milliseconds of gameplay time.</summary>
    public const double RESULTS_DELAY = 1200;

    public GameplayEngine Engine { get; }

    public bool IsPaused { get; private set; }

    /// <summary>Gameplay time of the first frame processed after the last object was judged, or <c>null</c> while playing.</summary>
    public double? FinishedAt { get; private set; }

    public bool ResultsShown { get; private set; }

    public GameplaySession(GameplayEngine engine)
    {
        Engine = engine;
    }

    /// <summary>Processes one update frame at gameplay time <paramref name="time"/>. Has no effect on the engine while paused.</summary>
    public void Frame(double time)
    {
        if (!IsPaused)
            Engine.Update(time);

        if (Engine.IsFinished && FinishedAt == null)
            FinishedAt = time;
    }

    /// <summary>Registers a press. Ignored (returns <c>null</c>) while paused or after the last object has been judged.</summary>
    public JudgementEvent? Press(double time, Vector2 position)
    {
        if (IsPaused || Engine.IsFinished)
            return null;

        return Engine.RegisterInput(time, position);
    }

    /// <summary>Toggles pause. Returns <c>false</c> (and does nothing) once the map is finished.</summary>
    public bool TogglePause()
    {
        if (Engine.IsFinished)
            return false;

        IsPaused = !IsPaused;
        return true;
    }

    /// <summary>Whether the results should be shown now (once, after <see cref="RESULTS_DELAY"/> past the finish).</summary>
    public bool ShouldShowResults(double time) => !ResultsShown && FinishedAt is double finished && time >= finished + RESULTS_DELAY;

    public void MarkResultsShown() => ResultsShown = true;
}
