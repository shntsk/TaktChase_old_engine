using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;

namespace TaktChase.Game.Tests;

public partial class TaktChaseTestBrowser : TaktChaseGameBase
{
    protected override void LoadComplete()
    {
        base.LoadComplete();

        // O test browser descobre as cenas de teste a partir deste namespace.
        AddRange(new Drawable[]
        {
            new TestBrowser("TaktChase"),
            new CursorContainer(),
        });
    }
}
