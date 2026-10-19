using System.Collections.Generic;
using System.Linq;
using osuTK;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Tests.Unit.Gameplay;

/// <summary>Small deterministic beatmaps shared by the gameplay unit tests.</summary>
internal static class TestBeatmaps
{
    /// <summary>OD 5 (300 +/-50, 100 +/-100, 50 +/-150, early miss 400), AR 5, CS 5 (radius exactly 32).</summary>
    public static BeatmapDifficulty Difficulty { get; } = new BeatmapDifficulty(overallDifficulty: 5, approachRate: 5, circleSize: 5);

    public static Vector2 Origin { get; } = new Vector2(100, 100);

    public static Beatmap Create(params HitObject[] objects) => new Beatmap("Test", "Test", "none.wav", 0, Difficulty, objects);

    public static Beatmap Create(BeatmapDifficulty difficulty, params HitObject[] objects) => new Beatmap("Test", "Test", "none.wav", 0, difficulty, objects);

    /// <summary>Circles at the given times, all at <see cref="Origin"/>.</summary>
    public static Beatmap AtOrigin(params double[] times) => Create(times.Select(t => (HitObject)new HitCircle(t, Origin)).ToArray());

    /// <summary>Circles at the given times, each on its own position (spaced far apart so only the intended circle is under the pointer).</summary>
    public static Beatmap Spread(params double[] times)
    {
        var list = new List<HitObject>();

        for (int i = 0; i < times.Length; i++)
            list.Add(new HitCircle(times[i], PositionOf(i)));

        return Create(list.ToArray());
    }

    public static Vector2 PositionOf(int index) => new Vector2(60 + 100 * index, 100);
}
