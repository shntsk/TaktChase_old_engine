using osuTK.Graphics;

namespace TaktChase.Game.UI;

/// <summary>
/// Paleta escura e minimalista do TaktChase. Cores de acento inspiradas no sistema de cores
/// da Apple (interface Cupertino), com o par ciano/rosa como identidade de ritmo.
/// </summary>
public static class TaktPalette
{
    public static readonly Color4 BACKGROUND = new Color4(10, 10, 14, 255);
    public static readonly Color4 SURFACE = new Color4(24, 24, 31, 255);
    public static readonly Color4 HAIRLINE = new Color4(255, 255, 255, 28);

    public static readonly Color4 TEXT_PRIMARY = new Color4(245, 245, 247, 255);
    public static readonly Color4 TEXT_SECONDARY = new Color4(142, 142, 147, 255);

    /// <summary>Anel que "persegue" o alvo.</summary>
    public static readonly Color4 CHASE = new Color4(100, 210, 255, 255);

    /// <summary>Alvo do toque.</summary>
    public static readonly Color4 TARGET = new Color4(255, 55, 95, 255);

    public static readonly Color4 OK = new Color4(48, 209, 88, 255);
    public static readonly Color4 WARNING = new Color4(255, 159, 10, 255);
}
