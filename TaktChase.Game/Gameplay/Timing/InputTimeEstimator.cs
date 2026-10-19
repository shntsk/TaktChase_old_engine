using System;
using System.Diagnostics;

namespace TaktChase.Game.Gameplay.Timing;

/// <summary>
/// Estimates the gameplay time of an input event from the last processed clock frame and a high-resolution timestamp.
/// </summary>
/// <remarks>
/// <para>
/// The gameplay clock only advances once per update frame, and framework input events are dispatched at the start of an
/// update frame, BEFORE the player's own update has refreshed the clock. Reading the clock's current time in an input
/// handler therefore returns the time of the previous frame, which is up to one frame (about 16 ms at 60 Hz) too early.
/// </para>
/// <para>
/// This class adds the real time elapsed since the last clock frame (measured with <see cref="Stopwatch"/> timestamps,
/// scaled by the clock rate) to the last frame's gameplay time. Elapsed time is capped so a long stall can never push
/// an input far into the future, and the estimate never goes below the last frame's time.
/// </para>
/// <para>
/// Limit: the pinned osu-framework version's input events were NOT verified to carry a hardware timestamp. The estimate
/// is the time at which the event reached the game's update thread, not the instant the key physically went down.
/// </para>
/// </remarks>
public sealed class InputTimeEstimator
{
    /// <summary>Default cap, in milliseconds, on how far past the last frame an estimate may extrapolate.</summary>
    public const double DEFAULT_MAX_EXTRAPOLATION = 250;

    private readonly double ticksPerMillisecond;
    private readonly double maxExtrapolation;

    private double frameTime;
    private long frameTimestamp;
    private double rate = 1;
    private bool running;

    /// <param name="ticksPerSecond">Resolution of the timestamps passed in (<see cref="Stopwatch.Frequency"/> for real use).</param>
    /// <param name="maxExtrapolation">See <see cref="DEFAULT_MAX_EXTRAPOLATION"/>.</param>
    public InputTimeEstimator(long ticksPerSecond, double maxExtrapolation = DEFAULT_MAX_EXTRAPOLATION)
    {
        if (ticksPerSecond <= 0)
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), "The timestamp resolution must be positive.");

        if (!(maxExtrapolation >= 0))
            throw new ArgumentOutOfRangeException(nameof(maxExtrapolation), "The extrapolation cap must not be negative.");

        ticksPerMillisecond = ticksPerSecond / 1000.0;
        this.maxExtrapolation = maxExtrapolation;
    }

    /// <summary>Creates an estimator that uses <see cref="Stopwatch"/> timestamps.</summary>
    public static InputTimeEstimator CreateDefault() => new InputTimeEstimator(Stopwatch.Frequency);

    /// <summary>Whether a frame has been recorded. Estimates are meaningless before that.</summary>
    public bool HasFrame { get; private set; }

    /// <summary>Records the clock state at the end of a frame.</summary>
    /// <param name="gameplayTime">Gameplay time of the frame, in milliseconds.</param>
    /// <param name="timestamp">Timestamp taken when the frame was processed.</param>
    /// <param name="clockRate">Playback rate of the clock (1 = real time).</param>
    /// <param name="isRunning">Whether the clock is advancing. A paused clock is never extrapolated.</param>
    public void RecordFrame(double gameplayTime, long timestamp, double clockRate, bool isRunning)
    {
        frameTime = gameplayTime;
        frameTimestamp = timestamp;
        rate = clockRate;
        running = isRunning;
        HasFrame = true;
    }

    /// <summary>Stops extrapolation (the clock was paused). The next <see cref="RecordFrame"/> restarts it.</summary>
    public void Freeze() => running = false;

    /// <summary>Estimates the gameplay time at <paramref name="timestamp"/>. Never earlier than the last recorded frame.</summary>
    public double Estimate(long timestamp)
    {
        if (!running || !(rate > 0))
            return frameTime;

        double elapsed = (timestamp - frameTimestamp) / ticksPerMillisecond;

        if (!(elapsed > 0))
            return frameTime;

        return frameTime + Math.Min(elapsed, maxExtrapolation) * rate;
    }
}
