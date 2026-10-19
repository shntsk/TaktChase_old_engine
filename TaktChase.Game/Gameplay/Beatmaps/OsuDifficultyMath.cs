using System;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// The osu!standard difficulty formulas used by TaktChase, as pure functions of a 0..10 difficulty value.
/// </summary>
/// <remarks>
/// These are the commonly documented osu!standard formulas (no mods, no rate changes), written from the author's knowledge of
/// osu!lazer's <c>IBeatmapDifficultyInfo.DifficultyRange</c>, <c>OsuHitWindows</c> and <c>OsuHitObject</c>. The numbers have NOT
/// been compared against the osu!lazer source in this repository's CI (no network access while writing them): see
/// <c>docs/gameplay-rules.md</c> for what is verified and what is assumed.
/// </remarks>
public static class OsuDifficultyMath
{
    /// <summary>Shortest approach (pre-empt) time, in ms. Used to scale the fade-in of very fast approach rates.</summary>
    public const double PREEMPT_MIN = 450;

    /// <summary>Hit circle radius at scale 1, in playfield units (osu! "pixels" on the 512x384 playfield).</summary>
    public const double OBJECT_RADIUS = 64;

    /// <summary>
    /// Maps a difficulty value to a value interpolated linearly between <paramref name="min"/> (difficulty 0),
    /// <paramref name="mid"/> (difficulty 5) and <paramref name="max"/> (difficulty 10).
    /// </summary>
    public static double DifficultyRange(double difficulty, double min, double mid, double max)
    {
        if (difficulty > 5)
            return mid + (max - mid) * (difficulty - 5) / 5;

        if (difficulty < 5)
            return mid - (mid - min) * (5 - difficulty) / 5;

        return mid;
    }

    /// <summary>Time in ms between an object appearing and its hit time, for an Approach Rate (1800 at AR0, 1200 at AR5, 450 at AR10).</summary>
    public static double ApproachTime(double approachRate) => DifficultyRange(approachRate, 1800, 1200, 450);

    /// <summary>Fade-in duration in ms for a given approach time: 400 ms, shortened when the approach time is below 450 ms.</summary>
    public static double FadeInTime(double approachTime) => 400 * Math.Min(1, approachTime / PREEMPT_MIN);

    /// <summary>Hit circle radius in playfield units for a Circle Size (54.4 at CS0, 32 at CS5, 9.6 at CS10).</summary>
    public static double CircleRadius(double circleSize) => OBJECT_RADIUS * (1 - 0.7 * (circleSize - 5) / 5) / 2;

    /// <summary>Half-width in ms of the 300 window: 80 at OD0, 50 at OD5, 20 at OD10.</summary>
    public static double GreatWindow(double overallDifficulty) => DifficultyRange(overallDifficulty, 80, 50, 20);

    /// <summary>Half-width in ms of the 100 window: 140 at OD0, 100 at OD5, 60 at OD10.</summary>
    public static double OkWindow(double overallDifficulty) => DifficultyRange(overallDifficulty, 140, 100, 60);

    /// <summary>Half-width in ms of the 50 window: 200 at OD0, 150 at OD5, 100 at OD10.</summary>
    public static double MehWindow(double overallDifficulty) => DifficultyRange(overallDifficulty, 200, 150, 100);

    /// <summary>How early in ms a press still counts (as a Miss on the object): 400 for every OD.</summary>
    public const double MISS_WINDOW = 400;
}
