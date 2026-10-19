using System;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// The three osu!standard difficulty settings that matter for circle-only gameplay, and the values derived from them.
/// Nothing else in the game hard-codes timing windows, approach times or circle sizes.
/// </summary>
public sealed class BeatmapDifficulty
{
    /// <summary>Settings used by the demonstration beatmap. They are TaktChase's own choice, not taken from any osu! beatmap.</summary>
    public static BeatmapDifficulty Default { get; } = new BeatmapDifficulty(overallDifficulty: 5, approachRate: 7, circleSize: 4);

    /// <summary>Overall Difficulty (0..10). Controls the judgement windows.</summary>
    public double OverallDifficulty { get; }

    /// <summary>Approach Rate (0..10). Controls how long before its hit time an object appears.</summary>
    public double ApproachRate { get; }

    /// <summary>Circle Size (0..10). Controls the circle radius.</summary>
    public double CircleSize { get; }

    /// <summary>How long before the hit time an object appears and starts approaching, in milliseconds.</summary>
    public double ApproachTime => OsuDifficultyMath.ApproachTime(ApproachRate);

    /// <summary>Duration of the circle's fade-in after it appears, in milliseconds.</summary>
    public double FadeInTime => OsuDifficultyMath.FadeInTime(ApproachTime);

    /// <summary>Hit circle radius in playfield units. Also the hit-detection radius.</summary>
    public double CircleRadius => OsuDifficultyMath.CircleRadius(CircleSize);

    public BeatmapDifficulty(double overallDifficulty, double approachRate, double circleSize)
    {
        OverallDifficulty = validate(overallDifficulty, nameof(overallDifficulty));
        ApproachRate = validate(approachRate, nameof(approachRate));
        CircleSize = validate(circleSize, nameof(circleSize));
    }

    /// <summary>Creates the judgement windows for this difficulty's Overall Difficulty.</summary>
    public JudgementWindows CreateJudgementWindows() => JudgementWindows.FromOverallDifficulty(OverallDifficulty);

    private static double validate(double value, string name)
    {
        if (double.IsNaN(value) || value < 0 || value > 10)
            throw new ArgumentOutOfRangeException(name, value, "The value must be between 0 and 10.");

        return value;
    }
}
