using System;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Gameplay.Judgements;

/// <summary>
/// Timing windows, in milliseconds around an object's hit time. All windows are symmetric (apart from the early-miss window)
/// and inclusive: an absolute error exactly equal to a window still earns that window's result.
/// Ordering: <c>Great &lt;= Ok &lt;= Meh &lt;= Miss</c>.
/// </summary>
public sealed class JudgementWindows
{
    /// <summary>Largest absolute error that still earns <see cref="HitResult.Great"/> (300).</summary>
    public double GreatWindow { get; }

    /// <summary>Largest absolute error that still earns <see cref="HitResult.Ok"/> (100).</summary>
    public double OkWindow { get; }

    /// <summary>
    /// Largest absolute error that still earns <see cref="HitResult.Meh"/> (50). An object that has not been hit
    /// by <c>hitTime + MehWindow</c> is missed automatically.
    /// </summary>
    public double MehWindow { get; }

    /// <summary>
    /// How early before the hit time an input starts to count. An input earlier than <c>hitTime - MehWindow</c> but not
    /// earlier than <c>hitTime - MissWindow</c> is judged as a <see cref="HitResult.Miss"/> (a premature press).
    /// Inputs earlier than this are ignored.
    /// </summary>
    public double MissWindow { get; }

    public JudgementWindows(double greatWindow, double okWindow, double mehWindow, double missWindow)
    {
        if (!(greatWindow > 0))
            throw new ArgumentOutOfRangeException(nameof(greatWindow), "The 300 window must be greater than zero.");

        if (!(okWindow >= greatWindow))
            throw new ArgumentOutOfRangeException(nameof(okWindow), "The 100 window must not be smaller than the 300 window.");

        if (!(mehWindow >= okWindow))
            throw new ArgumentOutOfRangeException(nameof(mehWindow), "The 50 window must not be smaller than the 100 window.");

        if (!(missWindow >= mehWindow))
            throw new ArgumentOutOfRangeException(nameof(missWindow), "The miss window must not be smaller than the 50 window.");

        GreatWindow = greatWindow;
        OkWindow = okWindow;
        MehWindow = mehWindow;
        MissWindow = missWindow;
    }

    /// <summary>Creates the windows for an Overall Difficulty using the osu!standard formulas (see <see cref="OsuDifficultyMath"/>).</summary>
    public static JudgementWindows FromOverallDifficulty(double overallDifficulty) => new JudgementWindows(
        OsuDifficultyMath.GreatWindow(overallDifficulty),
        OsuDifficultyMath.OkWindow(overallDifficulty),
        OsuDifficultyMath.MehWindow(overallDifficulty),
        OsuDifficultyMath.MISS_WINDOW);

    /// <summary>Maps a timing error to a result.</summary>
    /// <param name="timingError">Input time minus hit time in milliseconds (negative = early, positive = late).</param>
    /// <returns>
    /// The hit result, or <c>null</c> if the input falls outside every window that accepts input
    /// (earlier than <see cref="MissWindow"/> before the hit time, later than <see cref="MehWindow"/> after it, or not a number).
    /// An early input between <see cref="MehWindow"/> and <see cref="MissWindow"/> yields <see cref="HitResult.Miss"/>.
    /// </returns>
    public HitResult? Evaluate(double timingError)
    {
        double absolute = Math.Abs(timingError);

        if (absolute <= GreatWindow)
            return HitResult.Great;

        if (absolute <= OkWindow)
            return HitResult.Ok;

        if (absolute <= MehWindow)
            return HitResult.Meh;

        // Late inputs past the 50 window can never be accepted: the object has already been missed.
        if (timingError < 0 && absolute <= MissWindow)
            return HitResult.Miss;

        return null;
    }
}
