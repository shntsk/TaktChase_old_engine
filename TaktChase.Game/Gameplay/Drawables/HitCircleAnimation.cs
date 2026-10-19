using System;

namespace TaktChase.Game.Gameplay.Drawables;

/// <summary>
/// Pure functions that give the appearance of a hit circle for a gameplay time. Kept apart from the drawable so the
/// animation curves are unit-testable and always a function of gameplay time (never of the frame rate).
/// </summary>
/// <remarks>
/// Shape modelled on osu!lazer's default circle: the body fades in over the fade-in time, the approach circle fades in over
/// twice that (capped by the approach time) and shrinks linearly from <c>startScale</c> to 1, reaching 1 at the hit time.
/// The 4x start scale is the author's recollection of lazer's value and is not verified.
/// </remarks>
public static class HitCircleAnimation
{
    /// <summary>Opacity (0..1) of the circle body.</summary>
    public static float BodyAlpha(double time, double appearTime, double fadeInTime)
        => (float)Math.Clamp((time - appearTime) / fadeInTime, 0, 1);

    /// <summary>Opacity (0..1) of the approach circle.</summary>
    public static float ApproachAlpha(double time, double appearTime, double fadeInTime, double approachTime)
        => (float)Math.Clamp((time - appearTime) / Math.Min(fadeInTime * 2, approachTime), 0, 1);

    /// <summary>Scale of the approach circle relative to the circle: <paramref name="startScale"/> at appear time, 1 at the hit time.</summary>
    public static float ApproachScale(double time, double appearTime, double approachTime, float startScale)
    {
        double progress = Math.Clamp((time - appearTime) / approachTime, 0, 1);
        return 1 + (startScale - 1) * (float)(1 - progress);
    }
}
