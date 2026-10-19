using osu.Framework.Graphics;
using osu.Framework.Screens;
using TaktChase.Game.Gameplay;
using TaktChase.Game.Gameplay.Beatmaps;

namespace TaktChase.Game.Screens;

/// <summary>
/// Hosts the demonstration play session. There is no menu in this stage: the game starts straight into gameplay,
/// and restarting simply replaces the session.
/// </summary>
public partial class GameplayScreen : Screen
{
    private readonly double audioOffset;

    public GameplayScreen(double audioOffset = 0)
    {
        this.audioOffset = audioOffset;
    }

    /// <summary>The current session.</summary>
    public GameplayPlayer Player { get; private set; } = null!;

    protected override void LoadComplete()
    {
        base.LoadComplete();
        startSession();
    }

    private void startSession()
    {
        // Release the previous session's audio right away; its disposal happens later, asynchronously.
        Player?.Stop();

        InternalChild = Player = new GameplayPlayer(DemoBeatmap.Create(), audioOffset)
        {
            RestartRequested = () => Schedule(startSession),
        };
    }
}
