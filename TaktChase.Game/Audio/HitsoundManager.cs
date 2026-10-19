using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Audio;

/// <summary>
/// Lightweight preserver of osu!-style hit sound behaviour. It resolves only the playable sample names that are present in the
/// repo and skips missing/empty ones, so the gameplay timing and judgement logic remain unchanged.
/// </summary>
public sealed class HitsoundManager
{
    private static readonly IReadOnlyDictionary<HitResult, string> sample_names = new Dictionary<HitResult, string>
    {
        [HitResult.Great] = "Skins/lite/normal-hitnormal",
        [HitResult.Ok] = "Skins/lite/normal-hitclap",
        [HitResult.Meh] = "Skins/lite/soft-hitnormal",
    };

    private ISampleStore? samples;

    public void Initialize(ISampleStore sampleStore)
    {
        samples = sampleStore;
    }

    public void PlayHitSound(HitResult result)
    {
        if (samples == null || result == HitResult.Miss)
            return;

        if (!sample_names.TryGetValue(result, out var resourceName))
            return;

        var sample = samples.Get(resourceName);
        sample?.Play();
    }
}
