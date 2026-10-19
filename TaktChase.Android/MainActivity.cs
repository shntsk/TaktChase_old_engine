using System;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using osu.Framework.Android;
using TaktChase.Game;
using TaktChase.Game.Startup;

namespace TaktChase.Android;

/// <summary>
/// The Android entry point. <see cref="AndroidGameActivity"/> (osu-framework) creates the SDL surface and the
/// <c>AndroidGameHost</c>, and runs the game returned by <see cref="CreateGame"/>. The framework also pauses and resumes the audio
/// device in <c>OnStop</c> / <c>OnRestart</c>; the gameplay itself pauses when the window loses focus (see <c>GameplayPlayer</c>).
/// </summary>
[Activity(
    Label = "TaktChase",
    MainLauncher = true,
    Exported = true,
    LaunchMode = DEFAULT_LAUNCH_MODE,
    ConfigurationChanges = DEFAULT_CONFIG_CHANGES,
    ScreenOrientation = ScreenOrientation.SensorLandscape,
    Theme = "@android:style/Theme.Black.NoTitleBar.Fullscreen")]
public class MainActivity : AndroidGameActivity
{
    // "Game" alone would resolve to the TaktChase.Game namespace here, so the framework type is fully qualified.
    protected override osu.Framework.Game CreateGame() => new TaktChaseGame();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        RuntimeLogCapture.Attach();

        base.OnCreate(savedInstanceState);

        allowDrawingIntoDisplayCutout();
        enterImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        // The system bars come back after a swipe, a dialog or the app switcher; hide them again whenever focus returns.
        if (hasFocus)
            enterImmersiveMode();
    }

    /// <summary>
    /// Lets the window extend into the display cutout (notch / punch hole). The framework's surface reports the cutout and the
    /// rounded corners as the window's safe area, and the gameplay HUD pads itself with it.
    /// </summary>
    private void allowDrawingIntoDisplayCutout()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(28))
            return;

        var window = Window;
        var attributes = window?.Attributes;

        if (window == null || attributes == null)
            return;

        // ShortEdges covers a cutout on the short edges (the left/right sides in landscape); Always (API 30+) also covers the long edges.
        attributes.LayoutInDisplayCutoutMode = OperatingSystem.IsAndroidVersionAtLeast(30)
            ? LayoutInDisplayCutoutMode.Always
            : LayoutInDisplayCutoutMode.ShortEdges;

        window.Attributes = attributes;
    }

    // SystemUiVisibility is deprecated since API 30 but is still honoured on every API level this app supports (21+),
    // and it avoids a second code path that this project cannot test.
#pragma warning disable CA1422, CS0618
    private void enterImmersiveMode()
    {
        var decorView = Window?.DecorView;

        if (decorView == null)
            return;

        const SystemUiFlags flags = SystemUiFlags.LayoutStable
                                    | SystemUiFlags.LayoutHideNavigation
                                    | SystemUiFlags.LayoutFullscreen
                                    | SystemUiFlags.HideNavigation
                                    | SystemUiFlags.Fullscreen
                                    | SystemUiFlags.ImmersiveSticky;

        decorView.SystemUiVisibility = (StatusBarVisibility)(int)flags;
    }
#pragma warning restore CA1422, CS0618
}
