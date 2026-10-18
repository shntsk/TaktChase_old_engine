using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using TaktChase.Game.Graphics;
using TaktChase.Game.Screens;

namespace TaktChase.Game.Tests.Visual;

public partial class TestSceneHomeScreen : TaktChaseTestScene
{
    private ScreenStack stack = null!;

    [BackgroundDependencyLoader]
    private void load()
    {
        Child = stack = new ScreenStack(new HomeScreen(GraphicsReport.Unavailable))
        {
            RelativeSizeAxes = Axes.Both,
        };
    }

    [Test]
    public void TestHomeScreenIsCurrent()
    {
        AddUntilStep("home screen is current", () => stack.CurrentScreen is HomeScreen);
    }
}
