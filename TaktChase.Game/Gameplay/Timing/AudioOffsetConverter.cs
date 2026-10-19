namespace TaktChase.Game.Gameplay.Timing;

/// <summary>
/// The single definition of the audio offset sign convention.
/// </summary>
/// <remarks>
/// The audio offset is the delay in milliseconds between a position in the track and the moment the player hears it
/// (output latency). A POSITIVE offset means the sound reaches the player LATE. A player reacting to what they hear at
/// track position <c>t</c> physically presses when the track is already at <c>t + offset</c>, so gameplay time is
/// <c>trackTime - offset</c>. A negative offset means the sound arrives early.
/// </remarks>
public static class AudioOffsetConverter
{
    /// <summary>Converts a position of the audio track to gameplay time.</summary>
    public static double ToGameplayTime(double trackTime, double audioOffset) => trackTime - audioOffset;

    /// <summary>Converts gameplay time back to the position of the audio track.</summary>
    public static double ToTrackTime(double gameplayTime, double audioOffset) => gameplayTime + audioOffset;
}
