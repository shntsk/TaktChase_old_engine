using System;
using System.Collections.Generic;
using System.Linq;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// The smallest useful beatmap: metadata, an audio file name, difficulty settings and a time-ordered list of hit objects.
/// </summary>
public sealed class Beatmap
{
    public string Title { get; }

    public string Artist { get; }

    /// <summary>Name of the audio track resource (looked up in the game's track store).</summary>
    public string AudioFile { get; }

    /// <summary>
    /// Silence before track time zero, in milliseconds. Gameplay time starts at <c>-LeadInTime</c>
    /// so the player has time to get ready before the music starts.
    /// </summary>
    public double LeadInTime { get; }

    public BeatmapDifficulty Difficulty { get; }

    /// <summary>Hit objects sorted by start time (objects with equal start times keep their given order).</summary>
    public IReadOnlyList<HitObject> HitObjects { get; }

    public Beatmap(string title, string artist, string audioFile, double leadInTime, BeatmapDifficulty difficulty, IEnumerable<HitObject> hitObjects)
    {
        if (leadInTime < 0)
            throw new ArgumentOutOfRangeException(nameof(leadInTime), "The lead-in time must not be negative.");

        Title = title;
        Artist = artist;
        AudioFile = audioFile;
        LeadInTime = leadInTime;
        Difficulty = difficulty;

        // LINQ's OrderBy is a stable sort, which keeps same-time objects in their authored order.
        HitObjects = hitObjects.OrderBy(h => h.StartTime).ToArray();
    }

    public double FirstObjectTime => HitObjects.Count == 0 ? 0 : HitObjects[0].StartTime;

    public double LastObjectTime => HitObjects.Count == 0 ? 0 : HitObjects[HitObjects.Count - 1].StartTime;
}
