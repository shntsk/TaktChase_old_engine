# Circle gameplay rules (stage 2)

Status: **written without a compiler, network access or the osu!lazer sources at hand.** Formulas below are from the author's
knowledge of osu!standard and osu!lazer and were NOT compared against the lazer repository. Nothing here has been built or tested
until CI says so. Do not read this document as a claim of exact osu!lazer parity.

## What was implemented

| Area | Rule in TaktChase | Source of the rule |
|---|---|---|
| Results | `Great` (300), `Ok` (100), `Meh` (50), `Miss` | lazer member names; point values 300/100/50/0 |
| Windows (half-width, inclusive, ms) | 300: `80 - 6·OD`; 100: `140 - 8·OD`; 50: `200 - 10·OD` | osu!standard `DifficultyRange` anchors (OD0/5/10 = 80/50/20, 140/100/60, 200/150/100) |
| Early press | A press on an object between the 50 window and 400 ms early is a Miss on that object; earlier presses are ignored | recollection of lazer's `ResultFor` with a 400 ms miss window; **unverified** |
| Auto miss | An object not hit by `start + 50 window` is missed, stamped at that moment | lazer `CanBeHit` behaviour; **unverified in detail** |
| Approach time | `1800 / 1200 / 450` ms at AR 0 / 5 / 10, linear in between | osu!standard `DifficultyRange` |
| Fade-in | `400 · min(1, approachTime / 450)` ms; approach circle fades in over `min(2·fadeIn, approachTime)` | recollection of lazer's `TimeFadeIn`; **unverified** |
| Approach circle | Linear scale from 4x to 1x, reaching 1x at the hit time | the 4x start is a recollection; **unverified** |
| Circle size | radius `32 · (1 - 0.7 · (CS - 5) / 5)` playfield units (54.4 / 32 / 9.6 at CS 0 / 5 / 10) | osu!standard; lazer applies an extra small "fudge" factor that is NOT applied here |
| Hit detection | Pointer inside the radius (inclusive) | osu!standard |
| One input, one object | An input judges at most the earliest eligible object under the pointer; a judged object is never judged again | design requirement |
| Note lock | A later object cannot be hit while an earlier unjudged object's hit time is still in the future; once it has passed, it no longer blocks | recollection of lazer's start-time-ordered hit policy; **unverified in detail** |
| Accuracy | `(300·n300 + 100·n100 + 50·n50) / (300 · judged)` | osu!standard accuracy for circles |
| Score | TaktChase's own: base points x `(40 + min(combo, 40)) / 40` (integer division). **Not** lazer's ScoreV1 or standardised score | project design |

Not implemented (out of scope): sliders, spinners, mods, rate changes, stacking, lazer's integer rounding of windows, any ruleset
other than circles.

## Time basis

All gameplay logic uses one basis, **gameplay time = track time − audio offset** (`AudioOffsetConverter`):

- Object start times, approach/fade scheduling, input times, miss times and timing errors are all gameplay time.
- The offset is applied in `GameplayClock` itself (not by a framework offset clock), so there is a single definition and it is unit tested.
- Convention: a **positive** offset means the sound reaches the player late. A tap at track position `t + offset` is judged as a tap at `t`.
- `--audio-offset` feeds this directly. Whether the sign matches what players expect from other games is a convention choice; the
  real-hardware check is in `docs/validation-plan.md`.
- Timing error = input time − hit time (negative = early). Hit error bar, mean error and deviation use exactly these values.

## Input timestamps

The framework's input events were NOT verified (no access to the pinned package source) to carry a hardware timestamp. Instead:

- Input events reach `GameplayPlayer` at the start of an update frame, before the player refreshes the gameplay clock, so reading the
  clock there would return the previous frame's time.
- `InputTimeEstimator` adds the real time since the last clock frame (from `Stopwatch` timestamps, scaled by the clock rate, capped at
  250 ms, never extrapolated while paused) to that frame's time.
- This removes the up-to-one-frame bias but is still the time the event reached the game, not the instant the key went down. If a later
  framework version exposes an event timestamp, `GameplayPlayer.press` is the only place to change.

## Code review findings fixed in this stage

- The audio offset sign was defined through `FramedOffsetClock.Offset`, whose exact semantics could not be verified. The offset is now applied explicitly.
- Input was judged with the previous frame's clock time (see above).
- `GameplayPlayer` restart relied on asynchronous disposal to stop the old track, so two tracks could overlap. `Stop()` now releases the track immediately; the
  failed-track fallback also releases the failed track and unsubscribes the audio-thread event handlers. The track is disposed through `IDisposable` when it is
  released (**unverified** that this is the correct teardown for the pinned audio API).
- Pause/resume, completion and "which inputs reach the engine" moved from the player into the framework-free `GameplaySession` so they are unit tested.
- Fixed-size visuals: circle border thickness and numbers follow the circle size; the approach circle fades in; hit and miss animations are shorter.
- `GameplayClock` no longer implements `IFrameBasedClock` (nothing needed it), which removes dependence on interface members that could not be verified.

## Framework API assumptions (compile-time risk)

These calls come from earlier code or from the author's knowledge of osu-framework and could not be checked against the pinned
`ppy.osu.Framework 2026.1008.0`. They are the first places to look if CI reports a compile error in `TaktChase.Game`:

- `DecouplingFramedClock(IClock)`, `.Seek`, `.Start`, `.Stop`; `InterpolatingFramedClock(IClock)`, `.ProcessFrame`, `.CurrentTime`, `.Rate`, `.IsRunning`, `.ElapsedFrameTime`.
- `Track.StopAsync()`, `Track.Failed`, `Track.Completed`, `Track.IsLoaded`, `ITrackStore.Get`, `ITrackStore.GetVirtual`, and `ITrackStore` being injectable.
- `MouseDownEvent.ScreenSpaceMousePosition`, `TransformSequence<T>.Expire()` / `.FadeOut()` chaining.

## Tests

Unit tests live in `TaktChase.Game.Tests/Unit/Gameplay` and use no framework host, window, GPU or audio: `OsuDifficultyMathTest`,
`BeatmapDifficultyTest`, `JudgementWindowsTest`, `HitResultTest`, `ScoreProcessorTest`, `GameplayEngineTest`, `GameplaySessionTest`,
`InputTimeEstimatorTest`, `AudioOffsetConverterTest`, `HitCircleAnimationTest`, `BeatmapTest`, `DemoBeatmapTest`.
Not unit-tested (needs a framework host or real hardware): `GameplayClock` on a real track, `GameplayPlayer`, drawables, audio.
