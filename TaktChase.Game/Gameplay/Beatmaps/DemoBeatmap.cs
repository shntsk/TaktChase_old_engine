using System;
using System.Collections.Generic;
using osuTK;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// The built-in deterministic demonstration beatmap. It is generated from a fixed beat grid
/// (no randomness, no clock), and matches the music in <c>tools/generate_demo_track.py</c>:
/// 120 BPM, a kick on every beat starting at track time zero.
/// </summary>
public static class DemoBeatmap
{
    public const string AUDIO_FILE = "demo-track.wav";

    public const double BPM = 120;

    /// <summary>Length of one beat in milliseconds.</summary>
    public const double BEAT_LENGTH = 60000 / BPM;

    public const double LEAD_IN = 1000;

    /// <summary>Beat on which the first object is placed (the first four beats are a count-in).</summary>
    public const int FIRST_BEAT = 4;

    public static Beatmap Create()
    {
        var objects = new List<HitObject>();

        void add(double beat, Vector2 position) =>
            objects.Add(new HitCircle(beat * BEAT_LENGTH, position, objects.Count % 4 + 1));

        // Section A (beats 4-15): quarter notes sweeping left to right.
        for (int i = 0; i < 12; i++)
        {
            float x = 100 + 312 * ((i % 4) / 3f);
            float y = i % 2 == 0 ? 150 : 234;
            add(FIRST_BEAT + i, snap(x, y));
        }

        // Section B (beats 16-31): eighth-note pairs travelling along an arc.
        for (int i = 0; i < 32; i++)
        {
            double angle = i * 0.4;
            add(16 + i * 0.5, snap(Playfield.CENTRE.X + 150 * (float)Math.Sin(angle), Playfield.CENTRE.Y - 100 * (float)Math.Cos(angle)));
        }

        // Section C (beats 32-47): syncopated bars (hits on 0, 1, 1.5, 2.5 and 3) jumping between anchor points.
        Vector2[] anchors =
        {
            snap(90, 90), snap(420, 110), snap(400, 290), snap(110, 300), snap(256, 192), snap(256, 70),
        };

        double[] syncopation = { 0, 1, 1.5, 2.5, 3 };
        int anchor = 0;

        for (int bar = 0; bar < 4; bar++)
        {
            foreach (double offset in syncopation)
                add(32 + bar * 4 + offset, anchors[anchor++ % anchors.Length]);
        }

        // Section D (beats 48-63): a stream of eighth notes around a circle (8 per bar).
        for (int i = 0; i < 32; i++)
        {
            double angle = i * Math.PI / 4;
            add(48 + i * 0.5, snap(Playfield.CENTRE.X + 130 * (float)Math.Cos(angle), Playfield.CENTRE.Y + 130 * (float)Math.Sin(angle)));
        }

        // Section E (beats 64-79): quarter notes on a wide figure.
        for (int i = 0; i < 16; i++)
        {
            double angle = i * Math.PI / 4;
            add(64 + i, snap(Playfield.CENTRE.X + 190 * (float)Math.Cos(angle), Playfield.CENTRE.Y + 120 * (float)Math.Sin(angle * 2)));
        }

        // Finale on beat 80: the final accent in the music.
        add(80, Playfield.CENTRE);

        return new Beatmap("Demonstration", "TaktChase", AUDIO_FILE, LEAD_IN, BeatmapDifficulty.Default, objects);
    }

    /// <summary>Rounds to whole playfield units so positions are identical on every platform and runtime.</summary>
    private static Vector2 snap(float x, float y) => new Vector2((float)Math.Round(x), (float)Math.Round(y));
}
