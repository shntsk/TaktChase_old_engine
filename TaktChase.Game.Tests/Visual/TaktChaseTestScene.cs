using osu.Framework.Testing;

namespace TaktChase.Game.Tests.Visual;

/// <summary>
/// Base class for TaktChase's visual test scenes. It does the same as <c>TemplateGameTestScene</c> in the official template:
/// it runs each scene inside a game derived from <see cref="TaktChaseGameBase"/>, so that embedded resources
/// (textures from TaktChase.Resources) and the shared base's screen scaling are available in the tests.
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
