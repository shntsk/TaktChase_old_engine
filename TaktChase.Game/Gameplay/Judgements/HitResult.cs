namespace TaktChase.Game.Gameplay.Judgements;

/// <summary>
/// The outcome of judging a single hit object. Declared from worst to best, so the numeric order of the values is the
/// quality order. The names follow osu!lazer's <c>HitResult</c> members for the osu!standard circle results:
/// <c>Great</c> = 300, <c>Ok</c> = 100, <c>Meh</c> = 50.
/// </summary>
public enum HitResult
{
    Miss = 0,

    /// <summary>The "50" result.</summary>
    Meh = 1,

    /// <summary>The "100" result.</summary>
    Ok = 2,

    /// <summary>The "300" result.</summary>
    Great = 3,
}

public static class HitResultExtensions
{
    /// <summary>Highest base points a single object can award; the accuracy denominator per object.</summary>
    public const int MAX_BASE_POINTS = 300;

    /// <summary>Whether the result counts as a successful hit (keeps and extends the combo).</summary>
    public static bool IsHit(this HitResult result) => result != HitResult.Miss;

    /// <summary>Base points awarded for the result (300 / 100 / 50 / 0), before the combo multiplier.</summary>
    public static int BasePoints(this HitResult result) => result switch
    {
        HitResult.Great => 300,
        HitResult.Ok => 100,
        HitResult.Meh => 50,
        _ => 0,
    };

    /// <summary>Short label shown to the player: "300", "100", "50" or "Miss".</summary>
    public static string DisplayName(this HitResult result) => result switch
    {
        HitResult.Great => "300",
        HitResult.Ok => "100",
        HitResult.Meh => "50",
        _ => "Miss",
    };
}
