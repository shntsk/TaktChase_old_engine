using System;
using osuTK;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// A timed, positioned gameplay object. This is pure data plus geometry/timing rules:
/// it knows nothing about drawing or input devices.
/// </summary>
public abstract class HitObject
{
    /// <summary>The moment, in milliseconds of track time, at which the object should be hit.</summary>
    public double StartTime { get; }

    /// <summary>Centre of the object in playfield units (see <see cref="Playfield"/>).</summary>
    public Vector2 Position { get; }

    protected HitObject(double startTime, Vector2 position)
    {
        if (double.IsNaN(startTime) || double.IsInfinity(startTime))
            throw new ArgumentOutOfRangeException(nameof(startTime), "The start time must be a finite number.");

        StartTime = startTime;
        Position = position;
    }

    /// <summary>The time at which the object becomes visible (the start of its approach).</summary>
    public virtual double GetAppearTime(BeatmapDifficulty difficulty) => StartTime - difficulty.ApproachTime;

    /// <summary>Whether a pointer at <paramref name="position"/> (playfield units) is on the object.</summary>
    public abstract bool Contains(Vector2 position, BeatmapDifficulty difficulty);
}

/// <summary>A circle that must be hit once, with an approach circle shrinking onto it.</summary>
public sealed class HitCircle : HitObject
{
    /// <summary>The number shown on the circle, to help the player read patterns.</summary>
    public int Number { get; }

    public HitCircle(double startTime, Vector2 position, int number = 1)
        : base(startTime, position)
    {
        Number = number;
    }

    public override bool Contains(Vector2 position, BeatmapDifficulty difficulty)
        => (position - Position).LengthSquared <= difficulty.CircleRadius * difficulty.CircleRadius;
}
