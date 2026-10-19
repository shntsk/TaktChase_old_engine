using System.Diagnostics;
using osu.Framework.Timing;

namespace TaktChase.Game.Gameplay.Timing;

/// <summary>
/// The authoritative gameplay time source. Its time is the audio track's playback position, shifted by the global audio offset.
/// </summary>
/// <remarks>
/// Layers (innermost first), all from <c>osu.Framework.Timing</c> as pinned by the project:
/// <list type="number">
/// <item>The source clock (an <c>osu.Framework.Audio.Track.Track</c> in the game): the ground truth for time.</item>
/// <item><see cref="DecouplingFramedClock"/>: lets time run before the track starts (negative lead-in time) and after
/// the track has ended, and hands over to the track at time zero. The track is never played from a fake position.</item>
/// <item><see cref="InterpolatingFramedClock"/>: audio positions only update in coarse steps, so between steps time is
/// interpolated with a stopwatch.</item>
/// </list>
/// The audio offset is applied by this class (see <see cref="AudioOffsetConverter"/>), not by a framework offset clock, so the
/// sign convention is defined in exactly one place and covered by unit tests.
/// The time is only updated by <see cref="ProcessFrame"/>, once per update frame, which keeps all logic within one frame consistent.
/// Input handlers use <see cref="EstimateCurrentTime"/> to obtain a time closer to the moment the input arrived.
/// </remarks>
public sealed class GameplayClock
{
    private readonly DecouplingFramedClock decoupled;
    private readonly InterpolatingFramedClock interpolated;
    private readonly InputTimeEstimator estimator = InputTimeEstimator.CreateDefault();

    /// <param name="source">The clock holding the real playback position, normally an audio track.</param>
    /// <param name="audioOffset">See <see cref="AudioOffset"/>.</param>
    public GameplayClock(IClock source, double audioOffset = 0)
    {
        decoupled = new DecouplingFramedClock(source);
        interpolated = new InterpolatingFramedClock(decoupled);
        AudioOffset = audioOffset;
    }

    /// <summary>
    /// Global audio offset in milliseconds: the delay between a position in the track and the moment the player hears it
    /// (audio output latency). Gameplay time is <c>track time - AudioOffset</c>. A positive value means the sound
    /// reaches the player late; use a negative value if it arrives early.
    /// </summary>
    public double AudioOffset { get; set; }

    /// <summary>Gameplay time in milliseconds as of the last <see cref="ProcessFrame"/> (negative during the lead-in).</summary>
    public double CurrentTime => AudioOffsetConverter.ToGameplayTime(interpolated.CurrentTime, AudioOffset);

    public double ElapsedFrameTime => interpolated.ElapsedFrameTime;

    public double Rate => interpolated.Rate;

    public bool IsRunning => interpolated.IsRunning;

    /// <summary>Whether <see cref="ProcessFrame"/> has run at least once, so <see cref="EstimateCurrentTime"/> is meaningful.</summary>
    public bool HasProcessedFrame => estimator.HasFrame;

    /// <summary>Advances the clock to the current frame. Call exactly once per update frame.</summary>
    public void ProcessFrame()
    {
        interpolated.ProcessFrame();
        estimator.RecordFrame(CurrentTime, Stopwatch.GetTimestamp(), interpolated.Rate, interpolated.IsRunning);
    }

    /// <summary>
    /// Gameplay time right now, extrapolated from the last frame with a high-resolution timestamp.
    /// Same time basis as <see cref="CurrentTime"/>, never earlier than it.
    /// </summary>
    public double EstimateCurrentTime() => estimator.Estimate(Stopwatch.GetTimestamp());

    /// <summary>
    /// Starts time at <c>-<paramref name="leadIn"/></c> and lets it run. The source (audio track) is started
    /// automatically when the underlying time reaches zero.
    /// </summary>
    public void Start(double leadIn)
    {
        decoupled.Seek(-leadIn);
        decoupled.Start();
    }

    /// <summary>Pauses time and the source (the audio track).</summary>
    public void Pause()
    {
        decoupled.Stop();
        estimator.Freeze();
    }

    /// <summary>Resumes time and the source after <see cref="Pause"/>.</summary>
    public void Resume()
    {
        decoupled.Start();

        // Restart extrapolation from now, so the time spent paused is never added to an input estimate.
        estimator.RecordFrame(CurrentTime, Stopwatch.GetTimestamp(), interpolated.Rate, true);
    }
}
