# Android (Stage 1: application foundation)

This stage adds an Android host for the existing demo gameplay. It adds **no** new gameplay, skins, beatmap import, menus, RX or Droid-style
CS. The gameplay rules, the demo music and the desktop controls (mouse, Z/X, Esc/Space, R) are unchanged.

> **Validation status:** nothing here was compiled, built or run by the author. GitHub Actions builds the APK; whether it works on a phone
> has to be checked on a phone. A green Android build does **not** mean the game runs on a device.

## Structure

```
TaktChase.Android/   Android application (net10.0-android): LauncherActivity, MainActivity, startup diagnostics, AndroidManifest.xml
TaktChase.Game/      shared gameplay (unchanged architecture; small input/lifecycle additions, see below)
TaktChase.Resources/ embedded textures and the demo track (loaded the same way on Android)
```

`TaktChase.Android` is **not** part of `TaktChase.sln`. Restoring a `net10.0-android` project needs the .NET Android workload, and the Arch
and Windows workflows (which restore and build the solution) must not install it. Build the Android project by its own path
(`dotnet build TaktChase.Android/TaktChase.Android.csproj`) or through `ci-android.yml`.

## How to build the APK with GitHub Actions

1. Push the repository (or open a pull request). `.github/workflows/ci-android.yml` runs on every push and pull request.
   It can also be started by hand: **Actions** tab, **CI Android arm64-v8a**, **Run workflow**.
2. Open the finished run. At the bottom of the run page, in **Artifacts**, download **`taktchase-android-debug-apk`** (a zip containing one
   `*-Signed.apk`). Artifacts are kept for 14 days.
3. Unzip it, copy the APK to the phone and open it. Android will ask to allow installing apps from that source (a debug-signed APK is not
   from the Play Store). Do not mix it with a differently signed build of the same package (`com.shntsk.taktchase`): uninstall the old one first.

The workflow installs the .NET Android workload and a JDK (Temurin 21), installs the Android SDK packages the build needs into a temporary
directory, builds with `-t:SignAndroidPackage`, checks that exactly one signed APK with `arm64-v8a` native libraries exists, and uploads it.
No step uses `continue-on-error`; any failure fails the job.

## Startup crash report

The native Android launcher checks a small status file in the app's private storage before starting the SDL game activity. If the previous
launch exited before the first game update, reopening TaktChase shows a native Android report with the last recorded startup stage and any managed
unhandled exception that was captured. The screen provides **Copy report** and **Try again** buttons; using ADB is not required. Native crashes and
system-terminated processes may not provide a managed exception, so in those cases the report identifies the last stage only. Once the first game update
completes, the state changes to RUNNING and an old startup report is no longer shown.

## Android host setup

Verified against `ppy/osu-framework` at tag `2026.1008.0` (`SampleGame.Android`, `osu.Framework.Android`, `osu.Framework.Android.props`):

- **Package:** `ppy.osu.Framework.Android` `2026.1008.0` (same version as `ppy.osu.Framework`; the `PackageId` is declared in
  `osu.Framework.Android.csproj` at that tag). The framework version was not changed.
- **Target:** `net10.0-android`, minimum Android 21 (`SupportedOSPlatformVersion` 21.0, as in the framework). The manifest uses
  `targetSdkVersion` 36, as the sample does.
- **Activity:** `MainActivity : AndroidGameActivity` overrides `CreateGame()` and returns `new TaktChaseGame()`, exactly the pattern of
  `SampleGameActivity`. The framework creates the SDL surface and the `AndroidGameHost`.
- **Project properties** copied from `osu.Framework.Android.props` (that file is not shipped in the NuGet package): `AndroidPackageFormat=apk`,
  `AndroidHttpClientHandlerType`, `NullabilityInfoContextSupport`, `EmbedAssembliesIntoApk=true`, `NoWarn XA4218`.
- **Resources and storage:** the textures and the demo track are embedded resources read through `DllResourceStore`, which needs no Android-specific
  code. Logs and `diagnostics.txt` are written through the host's storage, which the framework's `AndroidGameHost` maps to the app's external files
  directory (`GetExternalFilesDir`). Nothing else is stored.
- **Renderer:** no renderer is forced on Android. `--renderer`, the Vulkan probe and the OpenGL fallback restart belong to the desktop launcher
  (`TaktChaseLauncher`) and are not used; the framework picks its own backend. Which backend it picked appears in the F3 report when a keyboard is
  attached, and in `diagnostics.txt`.

### Android native audio libraries

The managed `ppy.ManagedBass` wrapper calls the native BASS library by name (`bass`). The shared `ppy.osu.Framework` package also
references `ppy.osu.Framework.NativeLibs`, a desktop runtime package containing Linux BASS binaries. Those Linux assets can collide with the Android
files at the same APK paths and be selected first, even when the Android project declares its own `AndroidNativeLibrary` items. The Android project
therefore excludes the desktop package's runtime assets for this app and explicitly includes the matching Android binaries copied unmodified from
`ppy/osu-framework` tag `2026.1008.0`:

- `Native/arm64-v8a/libbass.so`
- `Native/arm64-v8a/libbass_fx.so`
- `Native/arm64-v8a/libbassmix.so`

The Android CI extracts the libraries from the final signed APK, verifies that their SHA-256 hashes exactly match these source files, checks the ELF64
AArch64 machine type, and rejects Linux/glibc dependencies. A successful packaging check still cannot replace launching and testing audio on a physical phone.

## ABI strategy

Only **`arm64-v8a`** is built (`RuntimeIdentifier` `android-arm64`). That covers modern phones and keeps the build fast and the APK small.
The framework's Android package also ships `armeabi-v7a` and `x86` native libraries, and its own props list `android-arm`, `android-arm64` and
`android-x86`, so 32-bit ARM could be added later (set `RuntimeIdentifiers` and drop the single `RuntimeIdentifier`); it is not needed now.
The CI check fails if the APK contains any other ABI. `x86_64` is not listed by the framework at all, so x86_64 emulators cannot run this APK.

## Landscape, system bars and display cutouts

- `MainActivity` is locked to `SensorLandscape` and uses a full-screen no-title theme.
- It hides the status and navigation bars in immersive mode on creation and again whenever the window regains focus.
  It uses `View.SystemUiVisibility`, which is deprecated since API 30 but still available on every supported level; a newer `WindowInsetsController`
  path was not added because it could not be tested here.
- It sets the window's cutout mode to `ShortEdges` (API 28-29) or `Always` (API 30+), so the game can draw into the notch / punch-hole area.
- The framework's `AndroidGameSurface` computes the safe area (cutout insets, rounded-corner radii, status bar in multi-window mode) and publishes
  it as the window's `SafeAreaPadding`. The gameplay HUD (score, accuracy, progress text, combo, hit-error bar, status text) sits inside a
  framework `SafeAreaContainer`, so it stays clear of cutouts and rounded corners. The padding is zero on desktop windows, so the desktop layout
  is unchanged. The playfield is centered and the background and progress bar bleed to the screen edge on purpose.

## Touch-input path

1. Android delivers touch events to the SDL surface, and the framework converts them into per-finger touch states in screen space.
2. `GameplayPlayer.OnTouchDown` handles each finger's touch-down itself and returns `true`.
3. `TouchInputMapper` decides what the tap does: **press** while playing, **resume** while paused, **restart** on the results screen. A tap that resumes
   or restarts is never also a hit.
4. A press goes to `press(screenSpacePosition)`, the same method the mouse uses. It converts the position with `playfield.ToLocalSpace`, which inverts
   the whole scene-graph transform (window pixels, the 1920x1080-preserving content scale and the playfield's own scale) to the logical
   512x384 playfield, and passes it to the unchanged `GameplaySession.Press` and `GameplayEngine` hit testing.

Why touch is handled directly: by default the framework turns touches into **one** mouse button, which stays down while any finger is down. A second finger
pressed while the first is held would therefore produce no new mouse-down, and alternating two-finger tapping (normal in rhythm games) would lose hits. Since
`OnTouchDown` returns `true`, the framework does not also synthesize mouse events for that touch, so there are no double presses. Mouse and Z/X input
are untouched.

No new coordinate-transformation code was written: the display-to-playfield mapping is the framework's own scene-graph transform, so there is nothing
of ours to unit-test there. The framework's behavior (including the claim that touch positions use the same screen space as the mouse) is **not**
verified by CI; it is on the real-device list below.

## Lifecycle

- **Audio:** the framework's `AndroidGameActivity` calls `Bass.Pause()` in `OnStop` and `Bass.Start()` in `OnRestart`; TaktChase relies on that and adds
  nothing to the audio device handling.
- **Gameplay:** on touch platforms (`RuntimeInfo.IsMobile`), `GameplayPlayer` pauses the session when the host's `IsActive` becomes false (window focus
  lost: home button, app switcher, notification shade). Pausing stops the gameplay clock and the audio track, so no circle can be missed while the app is
  away. The session stays paused until the player taps. `GameplaySession.Pause` is idempotent, so repeated focus events never un-pause. On desktop there is
  no automatic pause, as before.
- On touch platforms the on-screen texts say "Tap to resume" and "Tap to play again" instead of mentioning keys.
- Android has no Escape or Space key, so there is no way to pause by hand yet; pausing happens through losing focus. A pause button belongs to a later stage with menus.

## What CI validates, and what it does not

| Validated by `ci-android.yml` | Not validated |
|---|---|
| The .NET Android workload installs on a GitHub-hosted Ubuntu runner | That the APK installs and launches on any real phone |
| `TaktChase.Android` restores and compiles against `ppy.osu.Framework.Android` 2026.1008.0 and the shared game | Rendering (the backend the framework picks, frame rate, scaling, cutout and corner handling) |
| A debug-signed APK with `arm64-v8a` native libraries is produced and uploaded | Touch input, multi-touch and the coordinate mapping on a real screen |
| | Audio output, latency and sync with the circles |
| | Pausing and resuming, and the audio when the app goes to the background |

The platform-independent logic added in this stage (`TouchInputMapper`, `GameplaySession.Pause/Resume`) is covered by NUnit tests, which run in the Arch and
Windows workflows together with all earlier tests. Those tests do not exercise Android, touch hardware or the activity lifecycle.

## Remaining real-device checks

Install the debug APK on an arm64 phone (and note the model and Android version):

1. **Launch.** The game opens in landscape (also after rotating the phone 180 degrees), without status or navigation bars, and starts the demo map.
   If it closes at startup, reopen the app to see the built-in startup report, then use **Copy report** or **Try again**.
2. **Rendering.** The playfield is centered, circles are round and sharp, text is readable. On a phone with a notch or punch hole, no HUD text is
   covered. Check a phone with rounded corners. Record the frame rate if it looks low.
3. **Touch input.** A tap on a circle hits it at the tapped position (tap the edge of a circle and just outside it). Two fingers alternating on two circles
   in quick succession both register. A tap with a finger already held down still counts. No hit is registered twice.
4. **Audio timing.** The demo track plays and the circles reach their target on the kick. Estimate the audio output latency of the phone (Bluetooth
   headphones are much worse than wired or speaker). The audio offset can only be set by the `--audio-offset` command-line option today, which Android has
   no way to pass; this is an open item for a later stage, so on Android treat any consistent early/late bias as a measurement, not a bug.
5. **Pause and resume.** While playing, press the home button, wait 10+ seconds, return: the game shows PAUSED, the music is silent, no circle was missed,
   and a tap resumes from the same point. Repeat with the notification shade, the app switcher, a phone call, and locking and unlocking the screen.
6. **Results and restart.** Let the map finish: the results appear, and a tap starts a new session with exactly one track playing.

## Assumptions that could not be verified

- That the NuGet package `ppy.osu.Framework.Android` 2026.1008.0 exists on nuget.org: the sandbox used to write this stage could read the framework's GitHub
  source at the tag but not nuget.org, so only the `PackageId` in the source was confirmed.
- That the `InstallAndroidDependencies` target with `-p:AcceptAndroidSDKLicenses=True`, and `-t:SignAndroidPackage`, behave as described on the current .NET 10
  Android workload, and that JDK 21 is the right version for it. If the first CI run fails in those steps, the log will say which; no step is skipped.
- That the framework's SDL layer reports window focus loss on Android pause (this is what auto-pause relies on), and that `Window.Attributes` and the
  `LayoutInDisplayCutoutMode` and `SystemUiFlags` members in `MainActivity` exist with exactly these names in the .NET 10 Android bindings. They are standard
  Android binding names, but this project could not compile them.
