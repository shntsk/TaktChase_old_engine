using osu.Framework.Allocation;

namespace TaktChase.Game.Tests.Visual;

public partial class TestSceneTaktChaseGame : TaktChaseTestScene
{
    [BackgroundDependencyLoader]
    private void load()
    {
        // AddGame já chama SetupLogging e SetHost; chamar SetHost manualmente registraria os handlers do host em duplicidade.
        AddGame(new TaktChaseGame());
    }
}
