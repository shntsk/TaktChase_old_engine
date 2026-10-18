using osu.Framework.Allocation;

namespace TaktChase.Game.Tests.Visual;

public partial class TestSceneTaktChaseGame : TaktChaseTestScene
{
    [BackgroundDependencyLoader]
    private void load()
    {
        // AddGame already calls SetupLogging and SetHost; calling SetHost manually would register the host handlers twice.
        AddGame(new TaktChaseGame());
    }
}
