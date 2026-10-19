namespace TaktChase.Game.Gameplay;

/// <summary>What a touch-down should do in the current state of a play session.</summary>
public enum TouchAction
{
    /// <summary>Register a press at the touched position (the normal gameplay hit).</summary>
    Press,

    /// <summary>Resume a paused session. The touch is not a hit.</summary>
    Resume,

    /// <summary>Start a new session from the results screen.</summary>
    Restart,
}

/// <summary>
/// Maps a touch-down to a gameplay action. Touchscreens have no keyboard, so the actions that desktop players reach with
/// Escape/Space (resume) and R (restart) are reached by tapping. A tap that resumes or restarts is never also a hit.
/// Framework-free, so it is covered by unit tests.
/// </summary>
public static class TouchInputMapper
{
    public static TouchAction Map(GameplaySession session)
    {
        if (session.ResultsShown)
            return TouchAction.Restart;

        if (session.IsPaused)
            return TouchAction.Resume;

        return TouchAction.Press;
    }
}
