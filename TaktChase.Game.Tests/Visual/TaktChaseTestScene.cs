using osu.Framework.Testing;

namespace TaktChase.Game.Tests.Visual;

/// <summary>
/// Base das cenas de teste visuais do TaktChase. Faz o mesmo que <c>TemplateGameTestScene</c> do template oficial:
/// roda cada cena dentro de um jogo derivado de <see cref="TaktChaseGameBase"/>, para que recursos embutidos
/// (texturas do TaktChase.Resources) e a escala de tela da base compartilhada estejam disponíveis nos testes.
/// </summary>
public abstract partial class TaktChaseTestScene : TestScene
{
    protected override ITestSceneTestRunner CreateRunner() => new TaktChaseTestSceneTestRunner();

    private partial class TaktChaseTestSceneTestRunner : TaktChaseGameBase, ITestSceneTestRunner
    {
        private TestSceneTestRunner.TestRunner runner = null!;

        protected override void LoadAsyncComplete()
        {
            base.LoadAsyncComplete();
            Add(runner = new TestSceneTestRunner.TestRunner());
        }

        public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
    }
}
