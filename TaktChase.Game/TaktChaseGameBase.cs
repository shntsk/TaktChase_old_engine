using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;
using TaktChase.Resources;

namespace TaktChase.Game;

public abstract partial class TaktChaseGameBase : osu.Framework.Game
{
    // Everything here is shared between the game and the test browser (same pattern as the official template).
    protected override Container<Drawable> Content { get; }

    protected TaktChaseGameBase()
    {
        // 1920x1080 reference; content scales proportionally for smaller/larger windows.
        base.Content.Add(Content = new DrawSizePreservingFillContainer
        {
            TargetDrawSize = new Vector2(1920, 1080),
        });
    }

    [BackgroundDependencyLoader]
    private void load()
    {
        Resources.AddStore(new DllResourceStore(TaktChaseResources.ResourceAssembly));
    }
}
