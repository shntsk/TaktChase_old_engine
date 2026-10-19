using System;
using System.Collections.Generic;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Gameplay;

/// <summary>
/// Accumulates score, combo, accuracy and hit errors from judgement events, and from nothing else.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Score: each judgement adds <c>BasePoints * (COMBO_STEPS + min(combo, COMBO_STEPS)) / COMBO_STEPS</c> using the combo
/// held BEFORE the hit (integer arithmetic), so a full combo doubles the base points.</item>
/// <item>Combo: +1 for every successful hit, reset to zero on a miss. <see cref="MaxCombo"/> keeps the best run.</item>
/// <item>Accuracy: sum of base points divided by <c>300 * judged objects</c> (100% while nothing has been judged).</item>
/// <item>Hit errors: the timing error of every successful hit, in judgement order.</item>
/// </list>
/// </remarks>
public sealed class ScoreProcessor
{
    /// <summary>Combo value at which the multiplier stops growing (it reaches its maximum of 2x).</summary>
    public const int COMBO_STEPS = 40;

    private readonly List<double> hitErrors = new List<double>();
    private readonly int[] counts = new int[Enum.GetValues<HitResult>().Length];
    private long basePointsTotal;

    public long Score { get; private set; }

    public int Combo { get; private set; }

    public int MaxCombo { get; private set; }

    /// <summary>Number of judged objects.</summary>
    public int JudgedCount { get; private set; }

    /// <summary>Accuracy in the range 0..1.</summary>
    public double Accuracy => JudgedCount == 0 ? 1 : (double)basePointsTotal / ((long)HitResultExtensions.MAX_BASE_POINTS * JudgedCount);

    /// <summary>Timing errors (ms, negative = early) of every successful hit, in judgement order.</summary>
    public IReadOnlyList<double> HitErrors => hitErrors;

    /// <summary>Mean timing error in ms (negative = the player is early on average), or 0 with no successful hits.</summary>
    public double MeanHitError
    {
        get
        {
            if (hitErrors.Count == 0)
                return 0;

            double sum = 0;
            foreach (double e in hitErrors)
                sum += e;

            return sum / hitErrors.Count;
        }
    }

    /// <summary>Population standard deviation of the hit errors in ms, or 0 with fewer than two hits.</summary>
    public double HitErrorDeviation
    {
        get
        {
            if (hitErrors.Count < 2)
                return 0;

            double mean = MeanHitError;
            double sumSquares = 0;

            foreach (double e in hitErrors)
                sumSquares += (e - mean) * (e - mean);

            return Math.Sqrt(sumSquares / hitErrors.Count);
        }
    }

    public int CountOf(HitResult result) => counts[(int)result];

    public void Apply(JudgementEvent judgement)
    {
        HitResult result = judgement.Result;

        int multiplierNumerator = COMBO_STEPS + Math.Min(Combo, COMBO_STEPS);
        Score += (long)result.BasePoints() * multiplierNumerator / COMBO_STEPS;
        basePointsTotal += result.BasePoints();

        counts[(int)result]++;
        JudgedCount++;

        if (result.IsHit())
        {
            Combo++;
            MaxCombo = Math.Max(MaxCombo, Combo);

            if (judgement.TimingError is double error)
                hitErrors.Add(error);
        }
        else
            Combo = 0;
    }
}
