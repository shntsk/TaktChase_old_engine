# osu! gameplay parity reference

This project intentionally follows the behaviour and feel of osu!standard as a gameplay reference, but the implementation is written as an original re-implementation in TaktChase rather than copied from ppy/osu.

The following behaviour is matched by design:
- 300 / 100 / 50 / Miss windows derived from the osu!standard overall-difficulty formulas already used in `JudgementWindows.FromOverallDifficulty`.
- Score and combo logic follows the same idea as osu!standard: each judgement adds base points and rewards combo continuation while resetting on misses.
- Input processing keeps the guidance that only the earliest valid, unjudged object is judged and that early hits before the 50 window but inside the miss window count as misses.
- Slider and spinner object types are represented as first-class beatmap objects so future osu!-style gameplay behaviour can be added without rewriting the engine.

This document is deliberately a reference summary and a clear attribution note; it does not include upstream source code.

Attribution
- Reference project: ppy/osu
- Reference behaviour: osu!standard gameplay timing, judgement windows, score/accuracy rules and object flow.
- License: ppy/osu is MIT licensed; this project reuses behaviour and design ideas but does not copy upstream source.
