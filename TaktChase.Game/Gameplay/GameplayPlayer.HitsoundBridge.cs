using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Input.Events;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Input;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;
using TaktChase.Game.UI;

namespace TaktChase.Game.Gameplay;

/// <summary>
/// One play session: owns the audio track, the gameplay clock and the logic. This is the small integration point that keeps
/// the existing TaktChase architecture intact while adding osu!-style sound and slider/spinner compatibility hooks.
/// </summary>
public partial class GameplayPlayer : Container
{
    private readonly HitsoundManager hitsounds = new();
    private ISampleStore? sampleStore;

    private void playJudgementSound(JudgementEvent judgement)
    {
        if (judgement.IsSuccessfulHit)
            hitsounds.PlayHitSound(judgement.Result);
    }

    [BackgroundDependencyLoader]
    private void load(ITrackStore tracks, GameHost host, ISampleStore sampleStore)
    {
        this.sampleStore = sampleStore;
        hitsounds.Initialize(sampleStore);
    }

    protected override bool OnKeyDown(KeyDownEvent e)
    {
        if (e.Repeat || e.ControlPressed || e.AltPressed)
            return base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.Z:
            case Key.X:
                return true;

            case Key.Escape:
            case Key.Space:
                return true;

            case Key.R:
                return true;
        }

        return base.OnKeyDown(e);
    }

    private void onJudged(JudgementEvent judgement)
    {
        playJudgementSound(judgement);
    }
}
