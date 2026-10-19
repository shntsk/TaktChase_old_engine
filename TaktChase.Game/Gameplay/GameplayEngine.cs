using System;
using System.Collections.Generic;
using osuTK;
using TaktChase.Game.Gameplay.Beatmaps;
using TaktChase.Game.Gameplay.Judgements;

namespace TaktChase.Game.Gameplay;

/// <summary>
/// Deterministic gameplay rules: judges inputs and elapsed hit windows against a beatmap.
/// It has no dependency on the framework, drawing, input devices or real time; it is driven purely by the
/// gameplay times and positions it is given, so the same input stream always produces the same judgements
/// regardless of how often <see cref="Update"/> is called.
/// </summary>
/// <remarks>
/// Rules:
/// <list type="bullet">
/// <item>An object is judged exactly once: either by an input or by its window elapsing (a miss).</item>
/// <item>One input judges at most one object: the earliest unjudged object whose window is open and which contains the pointer.</item>
/// <item>Note lock: an unjudged object whose hit time has not been reached blocks every later object, so a later object cannot
/// be hit before an earlier one has had its time, but once an earlier object's hit time has passed it no longer blocks.</item>
/// <item>Time (<see cref="Update"/>, <see cref="RegisterInput"/>) never moves backwards; earlier times are treated as the current time.</item>
/// <item>Every time is gameplay time: track time minus the audio offset (see <c>AudioOffsetConverter</c>). Object start times,
/// input times, miss times and timing errors all share that single basis.</item>
/// <item>A press that lands on an object earlier than the 50 window but inside the miss window judges that object as a Miss
/// (a premature press); a press earlier than the miss window is ignored.</item>
/// </list>
/// </remarks>
public sealed class GameplayEngine
{
    private readonly IReadOnlyList<HitObject> objects;
    private readonly bool[] judged;
    private int firstUnjudged;

    public Beatmap Beatmap { get; }

    public JudgementWindows Windows { get; }

    public ScoreProcessor Score { get; } = new ScoreProcessor();

    /// <summary>The latest gameplay time processed, in milliseconds.</summary>
    public double CurrentTime { get; private set; } = double.NegativeInfinity;

    /// <summary>Raised once for every judged object, after <see cref="Score"/> has been updated.</summary>
    public event Action<JudgementEvent>? Judged;

    /// <summary>Raised once, when the last object has been judged.</summary>
    public event Action? Finished;

    /// <param name="beatmap">The beatmap to judge.</param>
    /// <param name="windows">
    /// Judgement windows. When omitted they are derived from the beatmap's Overall Difficulty
    /// (see <see cref="BeatmapDifficulty.CreateJudgementWindows"/>); pass explicit windows only to override that.
    /// </param>
    public GameplayEngine(Beatmap beatmap, JudgementWindows? windows = null)
    {
        Beatmap = beatmap;
        Windows = windows ?? beatmap.Difficulty.CreateJudgementWindows();
        objects = beatmap.HitObjects;
        judged = new bool[objects.Count];

        // An empty beatmap has nothing to play: it is finished from the start (reported on the first Update).
    }

    public int TotalObjects => objects.Count;

    public bool IsFinished => Score.JudgedCount == objects.Count;

    private bool finishedRaised;

    /// <summary>Whether the object at <paramref name="index"/> in the beatmap's hit object list has been judged.</summary>
    public bool IsJudged(int index) => judged[index];

    /// <summary>
    /// Advances gameplay time, missing every object whose hit window has fully elapsed.
    /// </summary>
    public void Update(double time)
    {
        advanceTo(time);
        raiseFinishedIfNeeded();
    }

    /// <summary>
    /// Registers one input press (a click or key press). Elapsed windows up to <paramref name="time"/> are resolved
    /// first, so the result never depends on when <see cref="Update"/> was last called.
    /// </summary>
    /// <param name="time">Gameplay time of the press in milliseconds.</param>
    /// <param name="position">Pointer position in playfield units at the time of the press.</param>
    /// <returns>The judgement the press caused, or <c>null</c> if it hit nothing (it is then ignored without penalty).</returns>
    public JudgementEvent? RegisterInput(double time, Vector2 position)
    {
        advanceTo(time);

        var difficulty = Beatmap.Difficulty;
        JudgementEvent? result = null;

        for (int i = firstUnjudged; i < objects.Count; i++)
        {
            if (judged[i])
                continue;

            HitObject obj = objects[i];

            // Objects are sorted by start time, so no later object can have an open window either.
            if (obj.StartTime - Windows.MissWindow > CurrentTime)
                break;

            if (obj.Contains(position, difficulty))
            {
                double error = CurrentTime - obj.StartTime;
                HitResult hitResult = Windows.Evaluate(error) ?? HitResult.Miss;
                result = judge(i, hitResult, error, CurrentTime);
                break;
            }

            // Note lock: this object's hit time is still in the future, so nothing after it may be hit yet.
            if (obj.StartTime > CurrentTime)
                break;
        }

        raiseFinishedIfNeeded();
        return result;
    }

    private void advanceTo(double time)
    {
        if (time > CurrentTime)
            CurrentTime = time;

        for (int i = firstUnjudged; i < objects.Count; i++)
        {
            if (judged[i])
                continue;

            // Sorted by start time and the windows are constant: once one object is still alive, all later ones are too.
            if (objects[i].StartTime + Windows.MehWindow >= CurrentTime)
                break;

            // Stamped with the moment the window elapsed (not the update time), so it does not depend on the update rate.
            judge(i, HitResult.Miss, null, objects[i].StartTime + Windows.MehWindow);
        }
    }

    private JudgementEvent judge(int index, HitResult result, double? timingError, double time)
    {
        judged[index] = true;

        while (firstUnjudged < objects.Count && judged[firstUnjudged])
            firstUnjudged++;

        var judgement = new JudgementEvent(objects[index], index, result, timingError, time);
        Score.Apply(judgement);
        Judged?.Invoke(judgement);
        return judgement;
    }

    private void raiseFinishedIfNeeded()
    {
        if (finishedRaised || !IsFinished)
            return;

        finishedRaised = true;
        Finished?.Invoke();
    }
}
