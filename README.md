# TaktChase — Stage 0 (foundation)

A rhythm game built on temporal precision, aiming, and object reading, written in C# on top of
[osu-framework](https://github.com/ppy/osu-framework). Stage 0 delivered only the **foundation**:
window, home screen, OpenGL/Vulkan backend selection, graphics diagnostics, and CI. A circle-only gameplay prototype was
added afterwards (see "Gameplay prototype" below). There is no beatmap parser, no sliders or spinners, no skins and no multiplayer.

> **Validation status:** nothing in this package has been compiled or tested by the author, neither when it was created
> nor during the static reviews. Compilation and headless tests are validated by the CI workflows; OpenGL/Vulkan on a real GPU
> require the manual tests in [`docs/validation-plan.md`](docs/validation-plan.md). **No claim of universal
> compatibility is made.** The static review that produced this version is summarized in
> [`docs/static-review.md`](docs/static-review.md). Known dependency advisories are documented in
> [`docs/dependency-audit.md`](docs/dependency-audit.md).

## Versions and how they were verified

Verified on 2026-10-18 against official sources (NuGet pages, the `ppy/osu-framework` repository at tag
`2026.1008.0`, and the .NET documentation). Nothing was chosen by guesswork:

| Item | Version | Source |
|---|---|---|
| `ppy.osu.Framework` | **2026.1008.0** (the latest on NuGet; targets `net10.0`) | nuget.org/packages/ppy.osu.Framework |
| `ppy.osu.Framework.Templates` | 2026.1008.0 (same version as the framework) | nuget.org/packages/ppy.osu.Framework.Templates |
| .NET | `net10.0`; SDK `10.0.100` with `rollForward: latestFeature` | `global.json` identical to the framework repository's at that tag; the framework README requires the .NET 10 SDK |
| `Microsoft.NET.Test.Sdk` | **18.8.1** | `TemplateGame.Game.Tests.csproj` from the template; version listed on NuGet |
| `NUnit3TestAdapter` | **6.2.0** | same; latest stable on NuGet |
| `NUnit` | **no direct reference** (≥ 4.6.1, transitive) | the template does not reference NUnit; the dependency comes from `ppy.osu.Framework` (NUnit ≥ 4.6.1 in its NuGet dependency list) |
| `SixLabors.ImageSharp` | **3.1.11, transitive** (the framework declares `>= 3.1.11`) | has published security advisories; see [`docs/dependency-audit.md`](docs/dependency-audit.md) |

Structure (same organization as the template: Game / Desktop / Game.Tests / Resources):

```
TaktChase.sln
TaktChase.Game/       game logic, home screen, diagnostics, backend selection
TaktChase.Desktop/    thin entry point (WinExe)
TaktChase.Android/    Android application (not part of the solution, see docs/android.md)
TaktChase.Game.Tests/ visual test browser, test scenes, and unit tests (NUnit)
TaktChase.Resources/  embedded textures (TaktChase's own logo mark)
.github/workflows/    ci-windows.yml, ci-arch.yml, ci-android.yml (+ release.yml)
docs/                 validation-plan.md, static-review.md, dependency-audit.md, android.md
```

> The template was reproduced by hand (`dotnet new` was not run) and then compared statically with the files published in
> `osu.Framework.Templates/templates/template-empty` at tag `2026.1008.0`.
> Deliberate differences from the template: no iOS/Android projects, no `game.ico` (the template's icon belongs to ppy),
> no `.slnf`, `.vscode`, `.idea`, or `*.DotSettings`; the executable is named `TaktChase.Desktop`
> (`TemplateGame` in the template); the Desktop `Main` receives `args`, with `[STAThread]` as in the framework's own
> `SampleGame.Desktop`; `GenerateDocumentationFile` is not enabled.
> If you prefer, generate a reference project with `dotnet new install ppy.osu.Framework.Templates` and
> `dotnet new osu-framework-game -n Reference` and compare.

## How to build and run (on your machine)

```
dotnet restore
dotnet build -c Release
dotnet run --project TaktChase.Desktop -c Release -- --renderer=opengl --show-fps
```

## OpenGL and Vulkan selection

Selection happens in `TaktChaseLauncher.Run`, **before** the graphics host is created, using only the official
mechanisms documented on the framework's "Environment variables" wiki page.

| Argument | What TaktChase does | What the framework runs |
|---|---|---|
| `--renderer=auto` (default) | Sets nothing | `GameHost.ChooseAndSetupRenderer` with the framework's own fallback order |
| `--renderer=opengl` | `OSU_GRAPHICS_RENDERER=gl`, `OSU_GRAPHICS_SURFACE=opengl` | `GLRenderer` (OpenGL), no internal fallback |
| `--renderer=vulkan` | `OSU_GRAPHICS_RENDERER=veldrid`, `OSU_GRAPHICS_SURFACE=vulkan` | `VeldridRenderer` with a Vulkan surface, no internal fallback |

Other arguments: `--show-fps` (shows FPS/frame time at startup) and `--no-fallback` (disables the restart with
OpenGL when Vulkan fails).

**Important note about `auto`:** the order read from `GameHost.GetPreferredRenderersForCurrentPlatform` is, on
Windows, Direct3D 11 → experimental D3D11 → OpenGL → experimental Vulkan; on Linux, OpenGL → experimental OpenGL →
experimental Vulkan. Therefore **on Windows `auto` tends to choose Direct3D 11** (through Veldrid), not OpenGL; on Linux
it tends to choose OpenGL. Use `--renderer=opengl` or `--renderer=vulkan` to force a choice. A `Renderer` setting already
saved by the framework (`framework.ini`) can also change the `auto` order.

Details confirmed in the code at tag `2026.1008.0` (`FrameworkEnvironment` and `GameHost`):

- `OSU_GRAPHICS_RENDERER` accepts `gl`/`opengl` and `veldrid` (and `deferred`, not used here). The explicit branch of the
  `switch` is `"gl"`; `"opengl"` only works by falling into the default branch, which is why TaktChase uses `gl`. An unknown
  value falls back to `GLRenderer` without a warning.
- `OSU_GRAPHICS_SURFACE` is read with `Enum.TryParse<GraphicsSurfaceType>` (case-insensitive); an invalid value becomes
  "not set" without a warning.
- If only the surface is set, the assumed renderer is `veldrid`; if only the renderer is set, the assumed surface is
  OpenGL. That is why TaktChase always sets **both**.
- `veldrid` + the `vulkan` surface leads to `GraphicsDevice.CreateVulkan` (`VeldridDevice`); **nothing** selects Vulkan
  automatically in `VeldridRenderer`: the surface decides the backend.
- In automatic mode the framework does **not** try `VeldridRenderer` + Vulkan: the order only includes `Deferred_Vulkan`
  (an experimental renderer). TaktChase's `--renderer=vulkan` therefore exercises a different path from `auto`.

The variables are read by the framework only once per process (static constructor of `FrameworkEnvironment`), so they
are set before any call into the framework. If `OSU_GRAPHICS_RENDERER`/`OSU_GRAPHICS_SURFACE` are already set in the
environment, the framework obeys them even with `--renderer=auto`, and TaktChase logs a warning.

## Fallback and limitations

- **`auto`:** the fallback is the framework's own (it tries each renderer in the order above until one initializes). If **all**
  of them fail, `GameHost.Run` **does not throw**: it logs "No usable renderer was found!" and returns. The
  `TaktChaseLauncher` detects this case (`host.Renderer == null` without the home screen having loaded), shows the
  diagnosis, and exits with code 2.
- **`--renderer=vulkan`:** with the official variables the framework fails permanently. TaktChase
  catches the exception **only if the failure happens before the home screen loads**, writes the
  diagnosis (stderr, `%LOCALAPPDATA%/TaktChase/logs/startup-failure.log` or the equivalent under
  `~/.local/share`, and a dialog box on Windows) and **restarts the process** with `--renderer=opengl`,
  showing the fallback reason on screen (F3 panel and an amber indicator on the backend chip). The fallback only triggers after a real
  failure, never on assumptions about drivers.
- A failing `--renderer=opengl` has **no** fallback: the error is shown. Use `--renderer=auto`.
- **No restart loop:** the child process is always started with `--renderer=opengl` and with
  `TAKTCHASE_FALLBACK_FROM` set; under those conditions `ShouldOfferOpenGLFallback` is false (covered by a test).
  If the child also fails, its log goes to `startup-failure-fallback.log` (it does not overwrite the original failure's log)
  and the diagnosis includes the original cause.
- **Arguments passed on restart:** only `--renderer=opengl` and `--show-fps`. TaktChase's other arguments have no
  effect after the failure (`--no-fallback` was already taken into account; unknown arguments only produce a warning).
- The fallback is a process restart because the framework's variables cannot be changed within the same process; the framework
  offers no official equivalent mechanism for "request Vulkan and fall back to GL" in this mode.
- Failures that bring the process down inside the driver (native crashes) cannot be caught by C# code.
- The Vulkan pre-check loads the system loader, creates an instance, and releases it **in the same process** in which the framework
  later initializes Vulkan. Without hardware there is no way to confirm that every driver tolerates this; it is in
  the validation plan.
- A failure **after** the home screen has loaded is not treated as a renderer failure.

## Building vs. running Vulkan

- **To build:** only the .NET SDK and the NuGet packages. `ppy.osu.Framework` already contains the code for both
  rendering paths (OpenGL and Veldrid/Vulkan); there is no build option that turns a backend on or off, and none is silently
  disabled. CI statically verifies the build output (see [CI rendering verification](#ci-rendering-verification)).

- **To run Vulkan:** the system needs a **Vulkan loader** (`libvulkan.so.1` /
  `vulkan-1.dll`) **and** a GPU **Vulkan driver**. A successful build does not imply that either exists.
  TaktChase assumes nothing: with `--renderer=vulkan` it runs a pre-check (loads the loader, creates
  an instance, enumerates devices) for diagnostics only; the final proof is the framework's own initialization.

### CI rendering verification

The Veldrid assemblies are **not** named `Veldrid*.dll`: they carry the NuGet package IDs (`ppy.Veldrid.dll`,
`ppy.Veldrid.SPIRV.dll`, and so on), so an earlier filename glob could never match. The "Verify that both rendering paths are
in the build" step in both workflows now checks, without running any renderer:

1. `osu.Framework.dll` exists in `TaktChase.Desktop/bin/Release/net10.0`, and its metadata contains the type names `GLRenderer`
   and `VeldridRenderer` and the namespaces `osu.Framework.Graphics.OpenGL` / `osu.Framework.Graphics.Veldrid`. Both renderers
   are compiled into this one assembly (they are `internal` classes).
2. The generated `TaktChase.Desktop.deps.json` (the resolved runtime dependency graph) lists `ppy.Veldrid` and
   `ppy.Veldrid.SPIRV` as dependencies of `ppy.osu.Framework`, and also contains `ppy.Veldrid.OpenGLBindings`,
   `ppy.Veldrid.MetalBindings`, and `ppy.Vk`.
3. Every managed runtime asset that `deps.json` lists for those packages exists in the output folder, and the native
   `libveldrid-spirv` library listed for the CI platform (`linux-x64` / `win-x64`) exists under `runtimes/`.

File names come from `deps.json`, not from assumptions. This is evidence about packaging only. It does **not** show that either
renderer initializes, that a Vulkan loader or driver exists, or that shaders compile; those still require running on a real GPU
(see `docs/validation-plan.md`). The check is a name-based inspection of metadata strings, not a full metadata parse.

### Packages and drivers (verified on the Arch package pages and in Microsoft's documentation)

**Arch Linux x86_64**

- `dotnet-sdk-10.0` (the versioned line in the `extra` repository; the `dotnet-sdk` meta-package will move to a new major version
  in the future and would break `global.json`). On the package pages consulted on 2026-10-18, `10.0.10.sdk110-1` (`dotnet-sdk`) and
  `10.0.8.sdk108-1` (`dotnet-sdk-10.0`, an older page) appeared: the exact version changes over time and **was not confirmed for the moment
  of the build**. The `sdk1xx` range ≥ 100 satisfies `global.json` (`10.0.100`, `latestFeature`).
- OpenGL: `mesa` (provides `opengl-driver`).
- Vulkan on Intel UHD (Ice Lake, Mesa driver): `vulkan-intel`, which depends on `vulkan-icd-loader`.
- Optional for diagnostics: `vulkan-tools` (Vulkan tools). Confirm the binary with `pacman -Ql vulkan-tools | grep vulkaninfo`.
- The framework README asks for the system ffmpeg on Linux only for video decoding (not used in this stage).

**Windows x64**

- According to `release-notes/10.0/supported-os.md` in the `dotnet/core` repository (updated 2026-09-28), .NET 10
  is supported on: **Windows 11** 26H1, 25H2, 24H2, and 23H2 (Enterprise/Education); **Windows 10** only in the
  Enterprise/IoT/LTSC editions 1607, 1809, and 21H2; **Windows Server** 2025, 23H2, 2022, 2019, 2016, 2012 R2, and 2012
  (the last two with ESU). Windows 7 and 8.1 are not listed. TaktChase's target architecture: **x64**.
- That document lists the versions **supported by Microsoft for .NET**; it is not a guarantee that
  osu-framework, SDL3, or the graphics driver work on each of them.
- Arch Linux is **not** in the official list of distributions for .NET 10; .NET on Arch comes from the distribution's
  own packages.
- OpenGL and Vulkan depend on the vendor's **graphics driver** (installed by the user). **Not verified:** where `vulkan-1.dll` is distributed and which Intel driver version exposes Vulkan for the UHD G1; confirm in the Intel/Khronos documentation before claiming anything. TaktChase does not install or distribute drivers.

## Runtime diagnostics

The **F3** key (or the `diagnostics.txt` file in the game's data folder) shows real data only. The **active** renderer
always comes from the already initialized host, never from the argument: `GameHost.RendererInfo` and the type name of
`GameHost.Renderer`. Alerts appear when the request (`--renderer`) disagrees with the renderer type the host reports.

What the report contains:

- Requested renderer vs. active renderer (`GameHost.RendererInfo`), renderer type, and a **request check**
  (consistent / partially verified / mismatch / unavailable).
- Window presence (`GameHost.Window`; absent on headless hosts).
- `ResolvedRenderer` (automatic mode only: `GameHost` only fills it on that path).
- Fallback (from which backend, and the fallback reason) and mismatch alerts.
- The Vulkan pre-check result (if requested): loader, instance version, `vkCreateInstance`, device count.
- System, architecture, .NET version, logical processors.
- Framework log lines with graphics information (OpenGL version/renderer/vendor, and so on). The framework only logs these;
  there is no public property, so they are shown as log lines, without interpretation.
- Memory (working set, GC heap, collection counts), refreshed every 2 s while the panel is open.

**Reported as unavailable (not exposed by the public API of `ppy.osu.Framework` 2026.1008.0):**

- **Graphics surface.** `IWindow.GraphicsSurface` is not accessible from other assemblies (the CI build failed with
  CS0122 when it was read). The report does not read it through reflection and does not infer it from `--renderer`.
- **Renderer VSync state.** `IRenderer.VerticalSync` is not accessible either (CS0122), so the report says "Unavailable".
- **Direct3D 11 vs. Vulkan under `VeldridRenderer`.** `VeldridRenderer` can drive several backends. With `--renderer=vulkan` and
  `VeldridRenderer` active, the request check reads "Partially verified": the renderer type matches, but the
  Vulkan surface itself is not confirmed. Use `vulkaninfo` and the framework log lines to confirm it.
- GPU name under Vulkan and device limits.

The panel shows "Unavailable" or omits the datum; it never invents one.

**F2** toggles FPS and frame time (update-thread clock). For per-thread statistics, use the framework's
overlay (**Ctrl+F11**); Ctrl+F7 toggles the frame limiter. TaktChase does not impose a resolution,
refresh rate, or FPS limit.

## Performance

- No post-processing, custom shaders, or extra graphics dependencies; only framework drawables.
- A single asset loaded on the home screen (256×256 px logo mark, ~3 KB); the framework's default font.
- The counters only rewrite text at 4 Hz (FPS) and 0.5 Hz (memory, only with the panel open).
- Pulse animation: a few transforms per second.
- Resizable window; content designed for 1920×1080 and scaled proportionally (`DrawSizePreservingFillContainer`).

## CI

| Workflow | Environment | What runs |
|---|---|---|
| `ci-windows.yml` | `windows-latest` (Windows Server x64) | restore, Release build, check for the assemblies of both backends, headless `dotnet test` |
| `ci-arch.yml` | `ubuntu-latest` with the official `archlinux:latest` container + `pacman -S dotnet-sdk-10.0` | same; the kernel is the Ubuntu runner's, the userspace is Arch |

No workflow runs OpenGL/Vulkan on a physical GPU. Restore will still print NuGet audit warnings for
`SixLabors.ImageSharp` until the framework publishes a release that depends on a patched version; see
[`docs/dependency-audit.md`](docs/dependency-audit.md). The warnings are not suppressed.

## Licenses

osu-framework is MIT-licensed. The BASS audio library (a dependency) is a commercial product: free for non-commercial use;
obtain a license for commercial distribution. TaktChase itself is released under the MIT License (see [`LICENSE`](LICENSE)).

## Gameplay prototype (stages 1-2)

Launching the game enters the demonstration map directly (no menu). Controls: **Z / X / mouse buttons** hit,
**Esc / Space** pause and resume, **R** replay. Options: `--audio-offset=<ms>` (positive when audio reaches you late, range ±1000).

Stage 2 moved the circle rules toward osu!standard: judgements are 300 / 100 / 50 / Miss, and Overall Difficulty (OD),
Approach Rate (AR) and Circle Size (CS) drive the windows, the approach time and the circle size. The demonstration map uses
OD 5, AR 7, CS 4 (TaktChase's own choice). **This is not claimed to be exact osu!lazer parity**: see
[`docs/gameplay-rules.md`](docs/gameplay-rules.md) for the formulas, what was implemented, and what is unverified.

- `Gameplay/Judgements`, `Gameplay/GameplayEngine.cs`, `Gameplay/ScoreProcessor.cs`: deterministic rules, no framework, no drawing.
  Windows (inclusive, ms, half-widths): 300 = 80 - 6 OD, 100 = 140 - 8 OD, 50 = 200 - 10 OD; a press up to 400 ms early is a Miss;
  an object not hit by +50 window is missed. Score/combo/accuracy/hit errors come only from judgement events.
- `Gameplay/Beatmaps`: beatmap data, `BeatmapDifficulty` (OD/AR/CS and derived values), `OsuDifficultyMath` (formulas) and the
  deterministic `DemoBeatmap`.
- `Gameplay/GameplaySession.cs`: framework-free flow (pause/resume, which inputs reach the engine, results delay).
- `Gameplay/Timing`: `GameplayClock` (audio track → `DecouplingFramedClock` → `InterpolatingFramedClock`, then the audio offset),
  `AudioOffsetConverter` (the one definition of the offset sign) and `InputTimeEstimator` (input time estimate).
- `Gameplay/GameplayPlayer.cs`, `Gameplay/Drawables`: input wiring, circles with approach circles, HUD, hit error bar.
- `TaktChase.Resources/Tracks/demo-track.wav` is an original synthesized track generated by `tools/generate_demo_track.py`.
- Unit tests for all of the above: `TaktChase.Game.Tests/Unit/Gameplay`.

Status: written without a local .NET SDK. Nothing in stages 1-2 has been built or tested by the author; validate through the
CI workflows (compilation and the NUnit tests) and on real hardware (see `docs/validation-plan.md`).

## Android (platform stage 1)

An Android host runs the same demo gameplay (`TaktChase.Android`, built on `ppy.osu.Framework.Android` 2026.1008.0, `arm64-v8a` only).
Touch input is handled per finger, the HUD respects display cutouts, and the game pauses when the app loses focus. Nothing was built
or run by the author.

- **Build the APK:** push the repository, open the finished **CI Android arm64-v8a** run in the Actions tab and download the
  **`taktchase-android-debug-apk`** artifact (unzip it, then install the APK on the phone).
- CI proves only that the APK **builds**. Touch, audio timing, pause/resume and rendering must be checked on a real phone.
- Details, the touch-input path, the ABI strategy, unverified assumptions and the real-device checklist: [`docs/android.md`](docs/android.md).

## Skin graphics (Stage 2A)

The std::lite hit circle, approach circle and hit-circle numerals are
packaged as embedded PNG resources. Original skin sources and audio
assets are preserved under `TaktChase.Resources/Skins/lite/`.
See [docs/skin-lite.md](docs/skin-lite.md).
