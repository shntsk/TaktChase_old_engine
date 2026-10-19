using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Gameplay.Judgements;

/// <summary>
/// One judgement of one hit object. Every gameplay statistic is derived from these events.
/// </summary>
/// <param name="HitObject">The judged object.</param>
/// <param name="Index">Index of the object in the beatmap's hit object list.</param>
/// <param name="Result">The result.</param>
/// <param name="TimingError">
/// Input time minus hit time in milliseconds (negative = early, positive = late), or <c>null</c> when the
/// object was missed because its window elapsed without any input.
/// </param>
/// <param name="Time">Gameplay time at which the judgement happened (the input time, or the time the window elapsed).</param>
public readonly record struct JudgementEvent(HitObject HitObject, int Index, HitResult Result, double? TimingError, double Time)
{
    /// <summary>Whether this judgement is a successful hit that should appear in the hit error bar.</summary>
    public bool IsSuccessfulHit => Result.IsHit() && TimingError.HasValue;
}
