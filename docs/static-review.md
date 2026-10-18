# Static review of the foundation (stage 0)

Review performed by reading code and the official sources, **without compiling, without running tests, and without installing
dependencies**. Reference: `ppy/osu-framework` at tag `2026.1008.0` (commit `1eb8e95`, 2026-10-08), NuGet pages,
the official wiki, and `release-notes/10.0/supported-os.md` from `dotnet/core` (updated 2026-09-28).

## Verified and confirmed

- `ppy.osu.Framework` **2026.1008.0** is the latest version on NuGet and targets `net10.0`; the templates package has
  the same version.
- The project's `global.json` is identical to the framework repository's (`10.0.100`, `latestFeature`, no prereleases).
- The framework README at that tag requires the **.NET 10 SDK**.
- The Game / Desktop / Game.Tests / Resources structure, `Host.GetSuitableDesktopHost`, `ScreenStack`, `TestBrowser`,
  `DrawSizePreservingFillContainer`, `DllResourceStore`, `Logger.NewEntry`, `HostOptions.FriendlyGameName`,
  `GameHost.RendererInfo`/`Renderer`/`ResolvedRenderer`, `Storage.GetFullPath`: all exist with the signatures used.
- All non-ASCII characters used in the interface exist in the Roboto font embedded in the framework.

## Assumptions removed or corrected

| # | Problem found | Fix |
|---|---|---|
| 1 | `Microsoft.NET.Test.Sdk 17.14.1`, `NUnit3TestAdapter 5.0.0`, and `NUnit 4.6.1` were guesses | Versions from the official template: `18.8.1` and `6.2.0`; `NUnit` removed (transitive, ≥ 4.6.1 through `ppy.osu.Framework`, as in the template) |
| 2 | `.sln` used an invalid C# project type GUID (`...00A0C9930B35`) | Official GUID `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` (the template's) |
| 3 | The template's `app.manifest` was missing (DPI, compatibility) | Added and wired through `Directory.Build.props` |
| 4 | `--renderer=opengl` used `OSU_GRAPHICS_RENDERER=opengl`, which only works through the `switch` default branch | Now uses `gl` (the explicit branch) |
| 5 | In `auto`, if every renderer fails, `GameHost.Run` returns without an exception: the process exited with code 0 and no diagnosis | `TaktChaseLauncher` detects `host.Renderer == null` without the home screen having loaded and shows the diagnosis (code 2) |
| 6 | Fallback: if the child also failed, the original failure's log was overwritten and the original cause was lost | The child's log goes to its own file; the diagnosis includes the original cause |
| 7 | Comments and README said the startup logic was "covered by tests", but only visual scenes existed | Added NUnit unit tests (`LaunchOptions`, `RendererEnvironment`, `StartupFailure`, `VulkanProbe`) |
| 8 | `TestSceneTaktChaseGame` called `SetHost` and then `AddGame` (which also calls `SetHost`: duplicated host handlers) | `AddGame` only |
| 9 | Test scenes did not use a base class that runs the game with TaktChase's resources (like `TemplateGameTestScene`) | Added `TaktChaseTestScene` |
| 10 | README claimed the framework README mentions .NET 8 | False at that tag (it mentions .NET 10); text corrected |
| 11 | README restricted Windows without citing a source, and the Arch CI installed the `dotnet-sdk` meta-package | Official .NET 10 OS list cited with its date; CI uses `dotnet-sdk-10.0` |
| 12 | Diagnostics only compared the surface with the request | Also compared the renderer type (`GLRenderer`/`VeldridRenderer`) |

## Corrections after the first GitHub Actions build (CS0122)

The Arch Linux CI build of the item-12 diagnostics failed with two compiler errors:

| Error | Location | Cause | Fix |
|---|---|---|---|
| CS0122 | `GraphicsDiagnostics.cs(23,60)`: `IWindow.GraphicsSurface` | The member is not accessible from other assemblies in `ppy.osu.Framework` 2026.1008.0 | The code no longer reads it. The report lists **Graphics surface** as "Unavailable" |
| CS0122 | `GraphicsDiagnostics.cs(40,81)`: `IRenderer.VerticalSync` | Same: not accessible (the renderer implementation declares it `protected internal` in the framework source on its master branch) | The code no longer reads it. The report lists **Renderer VSync** as "Unavailable" |

What changed in behavior:

- `GraphicsDiagnostics.Capture` is now the only method that touches the host. It reads `GameHost.RendererInfo`, the type name of
  `GameHost.Renderer`, whether `GameHost.Window` exists, and `GameHost.ResolvedRenderer`: members that the previous
  build already compiled against. Everything else happens in the pure `GraphicsDiagnostics.Build`.
- Nothing is reached through reflection and no framework internals were copied.
- The old alerts "Vulkan was requested but the active surface is X" depended on the inaccessible surface. They are replaced by a
  **request check** against the renderer type: `Consistent` (OpenGL + `GLRenderer`), `PartiallyVerified`
  (Vulkan + `VeldridRenderer`, because that type can also drive Direct3D 11 or Metal), `Mismatch`, `Unavailable`,
  or `NotApplicable` (`auto`).
- The "On Windows, automatic selection prefers Direct3D 11" alert no longer fires from a detected surface. It is now a note that only
  states the framework's documented order and says that the surface cannot be confirmed.
- `GraphicsDiagnosticsTest` covers the new behavior without a GPU or a window.

Not verified by this review: the exact public/internal accessibility of every member of `IWindow`, `IRenderer`, and `GameHost`
(the framework sources at the tag were only partially readable; the fix relies on members that already compiled in CI).
The next GitHub Actions run is the check.

## Limitations the public API does not resolve (documented, nothing invented)

- The `GLRenderer`, `VeldridRenderer`, and `DeferredRenderer` classes are `internal`: Veldrid's internal backend
  (`Device.BackendType`) and the window's graphics surface cannot be read; the diagnostics use `RendererInfo` and the type name.
- The renderer's VSync state is not exposed.
- `GameHost.ResolvedRenderer` is only filled on the automatic path.
- With the `OSU_GRAPHICS_*` variables the framework does not fall back, and there is no public API for "request Vulkan and fall back to
  OpenGL" in that mode; for that reason the explicit fallback is a process restart.
- Failures on the draw thread after the renderer has been created, and native driver crashes, cannot be caught in C#.

## Out of scope for this review (depends on CI or hardware)

Compilation, test results, `dotnet restore` on Arch and Windows, presence of the native libraries in the runtime,
OpenGL and Vulkan on the Intel UHD G1, fallback behavior, and performance. See `docs/validation-plan.md`.
