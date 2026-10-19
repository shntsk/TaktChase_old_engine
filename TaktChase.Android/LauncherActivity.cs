using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

namespace TaktChase.Android;

/// <summary>
/// Native Android entry point. It checks for an unfinished previous launch before starting the SDL game activity.
/// </summary>
[Activity(
    Label = "TaktChase",
    MainLauncher = true,
    Exported = true,
    Theme = "@android:style/Theme.Material.NoActionBar")]
public sealed class LauncherActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        AndroidStartupDiagnostics.Initialize(this);
        string? previousFailure = AndroidStartupDiagnostics.ReadPreviousFailure();
        if (previousFailure == null)
        {
            launchGame();
            return;
        }

        showFailure(previousFailure);
    }

    private void launchGame()
    {
        AndroidStartupDiagnostics.BeginAttempt();
        StartActivity(new Intent(this, typeof(MainActivity)));
        Finish();
    }

    private void showFailure(string report)
    {
        var root = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical,
        };
        root.SetBackgroundColor(Color.Rgb(20, 22, 29));
        root.SetPadding(dp(24), dp(28), dp(24), dp(20));

        var title = new TextView(this)
        {
            Text = "TaktChase stopped during startup",
            TextSize = 24,
            Typeface = Typeface.Create("sans-serif", TypefaceStyle.Bold),
        };
        title.SetTextColor(Color.White);
        root.AddView(title, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        var explanation = new TextView(this)
        {
            Text = "The previous launch did not reach the first game update. The report below is saved automatically; no ADB is needed. A native crash or system termination may not include a managed exception.",
            TextSize = 14,
        };
        explanation.SetTextColor(Color.Rgb(190, 195, 207));
        explanation.SetPadding(0, dp(12), 0, dp(16));
        root.AddView(explanation, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        var scroll = new ScrollView(this);
        var details = new TextView(this)
        {
            Text = report,
            TextSize = 13,
        };
        details.SetTextColor(Color.Rgb(225, 228, 236));
        details.SetTextIsSelectable(true);
        scroll.AddView(details);
        root.AddView(scroll, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 0, 1f));

        var actions = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal,
        };

        var copy = new Button(this) { Text = "Copy report" };
        copy.Click += (_, _) =>
        {
            var clipboard = GetSystemService(Context.ClipboardService) as ClipboardManager;
            if (clipboard != null)
            {
                clipboard.PrimaryClip = ClipData.NewPlainText("TaktChase startup report", report);
                Toast.MakeText(this, "Report copied", ToastLength.Short)?.Show();
            }
        };

        var retry = new Button(this) { Text = "Try again" };
        retry.Click += (_, _) => launchGame();

        actions.AddView(copy, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
        actions.AddView(retry, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f));
        root.AddView(actions, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        SetContentView(root);
    }

    private int dp(int value) => (int)(value * Resources.DisplayMetrics!.Density + 0.5f);
}
