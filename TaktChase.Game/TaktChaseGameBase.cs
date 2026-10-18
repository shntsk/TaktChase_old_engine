using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;
using TaktChase.Resources;

namespace TaktChase.Game;

public abstract partial class TaktChaseGameBase : osu.Framework.Game
{
    // Tudo aqui é compartilhado entre o jogo e o test browser (mesmo padrão do template oficial).
    protected override Container<Drawable> Content { get; }

    protected TaktChaseGameBase()
    {
        // Referência de 1920x1080; o conteúdo escala proporcionalmente para janelas menores/maiores.
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
