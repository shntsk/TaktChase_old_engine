using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;

namespace TaktChase.Game.Tests;

public partial class TaktChaseTestBrowser : TaktChaseGameBase
{
    protected override void LoadComplete()
    {
        base.LoadComplete();

        // The test browser discovers the test scenes from this namespace.
        AddRange(new Drawable[]
        {
            new TestBrowser("TaktChase"),
            new CursorContainer(),
        });
    }
}
