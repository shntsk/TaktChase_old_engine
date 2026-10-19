using System;
using osuTK;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// A slider-like hit object. The current gameplay loop still treats sliders as timing events that occur at their start time,
/// but the structure is kept as a first-class beatmap object so future osu!-parity work can add proper ticks/repeats.
/// </summary>
public sealed class Slider : HitObject
{
    public double Duration { get; }

    public int RepeatCount { get; }

    public Vector2 EndPosition { get; }

    public Slider(double startTime, Vector2 startPosition, Vector2 endPosition, double duration, int repeatCount = 1)
        : base(startTime, startPosition)
    {
        if (duration < 0)
            throw new ArgumentOutOfRangeException(nameof(duration), "Slider duration must not be negative.");

        if (repeatCount < 1)
            throw new ArgumentOutOfRangeException(nameof(repeatCount), "A slider must repeat at least once.");

        Duration = duration;
        RepeatCount = repeatCount;
        EndPosition = endPosition;
    }

    public override bool Contains(Vector2 position, BeatmapDifficulty difficulty)
    {
        var startDelta = position - Position;
        var endDelta = position - EndPosition;
        double radius = difficulty.CircleRadius;
        return startDelta.LengthSquared <= radius * radius || endDelta.LengthSquared <= radius * radius;
    }
}

/// <summary>
/// A spinner-like hit object. In the current implementation it is treated as a single timed event created at its start time,
/// but it is represented as a separate object type so osu!-style spinning and completion logic can be added cleanly.
/// </summary>
public sealed class Spinner : HitObject
{
    public double Duration { get; }

    public double Radius { get; }

    public Spinner(double startTime, Vector2 position, double duration, double radius = 90)
        : base(startTime, position)
    {
        if (duration < 0)
            throw new ArgumentOutOfRangeException(nameof(duration), "Spinner duration must not be negative.");

        if (radius <= 0)
            throw new ArgumentOutOfRangeException(nameof(radius), "Spinner radius must be positive.");

        Duration = duration;
        Radius = radius;
    }

    public override bool Contains(Vector2 position, BeatmapDifficulty difficulty)
    {
        var delta = position - Position;
        return delta.LengthSquared <= (Radius * Radius);
    }
}
