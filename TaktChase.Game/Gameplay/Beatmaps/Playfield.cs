using osuTK;

namespace TaktChase.Game.Gameplay.Beatmaps;

/// <summary>
/// The logical coordinate space hit objects are placed in. The view scales it to the window;
/// gameplay logic only ever sees these units.
/// </summary>
public static class Playfield
{
    public const float WIDTH = 512;
    public const float HEIGHT = 384;

    public static readonly Vector2 SIZE = new Vector2(WIDTH, HEIGHT);

    public static readonly Vector2 CENTRE = new Vector2(WIDTH / 2, HEIGHT / 2);
}
