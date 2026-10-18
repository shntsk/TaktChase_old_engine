# Revisão estática da fundação (etapa 0)

Revisão feita por leitura de código e das fontes oficiais, **sem compilar, sem executar testes e sem instalar
dependências**. Referência: `ppy/osu-framework` na tag `2026.1008.0` (commit `1eb8e95`, 08/10/2026), páginas do NuGet,
wiki oficial e `release-notes/10.0/supported-os.md` de `dotnet/core` (atualizado em 28/09/2026).

## Verificado e confirmado

- `ppy.osu.Framework` **2026.1008.0** é a versão mais recente no NuGet e tem como alvo `net10.0`; o pacote de templates
  tem a mesma versão.
- `global.json` do projeto é idêntico ao do repositório do framework (`10.0.100`, `latestFeature`, sem pré-lançamentos).
- O README do framework nessa tag pede o **.NET 10 SDK**.
- Estrutura Game / Desktop / Game.Tests / Resources, `Host.GetSuitableDesktopHost`, `ScreenStack`, `TestBrowser`,
  `DrawSizePreservingFillContainer`, `DllResourceStore`, `Logger.NewEntry`, `HostOptions.FriendlyGameName`,
  `GameHost.RendererInfo`/`Renderer`/`ResolvedRenderer`, `Storage.GetFullPath`: todos existem com as assinaturas usadas.
- Todos os caracteres não ASCII usados na interface existem na fonte Roboto embutida no framework.

## Suposições removidas ou corrigidas

| # | Problema encontrado | Correção |
|---|---|---|
| 1 | `Microsoft.NET.Test.Sdk 17.14.1`, `NUnit3TestAdapter 5.0.0` e `NUnit 4.6.1` eram palpites | Versões do template oficial: `18.8.1` e `6.2.0`; `NUnit` removido (transitivo, ≥ 4.6.1 via `ppy.osu.Framework`, como no template) |
| 2 | `.sln` usava um GUID de tipo de projeto C# inválido (`...00A0C9930B35`) | GUID oficial `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` (o do template) |
| 3 | Faltava o `app.manifest` do template (DPI, compatibilidade) | Adicionado e ligado por `Directory.Build.props` |
| 4 | `--renderer=opengl` usava `OSU_GRAPHICS_RENDERER=opengl`, que só funciona pelo ramo padrão do `switch` | Passa a usar `gl` (ramo explícito) |
| 5 | Em `auto`, se todos os renderers falham, `GameHost.Run` retorna sem exceção: o processo saía com código 0 e sem diagnóstico | `TaktChaseLauncher` detecta `host.Renderer == null` sem a tela inicial ter carregado e mostra o diagnóstico (código 2) |
| 6 | Fallback: se o filho também falhasse, o log da falha original era sobrescrito e a causa original sumia | Log do filho em arquivo próprio; o diagnóstico inclui a causa original |
| 7 | Comentários e README diziam que a lógica de inicialização era "coberta por testes", mas só existiam cenas visuais | Adicionados testes unitários NUnit (`LaunchOptions`, `RendererEnvironment`, `StartupFailure`, `VulkanProbe`) |
| 8 | `TestSceneTaktChaseGame` chamava `SetHost` e depois `AddGame` (que também chama `SetHost`: handlers do host em duplicidade) | Só `AddGame` |
| 9 | Cenas de teste não usavam uma base que roda o jogo com os recursos do TaktChase (como `TemplateGameTestScene`) | Adicionada `TaktChaseTestScene` |
| 10 | README afirmava que o README do framework cita o .NET 8 | Falso nessa tag (cita .NET 10); texto corrigido |
| 11 | README restringia o Windows sem citar a fonte e o CI do Arch instalava o meta-pacote `dotnet-sdk` | Lista oficial de SOs do .NET 10 citada com data; CI usa `dotnet-sdk-10.0` |
| 12 | Diagnóstico só comparava a superfície com o pedido | Também compara o tipo de renderer (`GLRenderer`/`VeldridRenderer`) |

## Limitações que a API pública não resolve (documentadas, nada inventado)

- As classes `GLRenderer`, `VeldridRenderer` e `DeferredRenderer` são `internal`: o backend interno do Veldrid
  (`Device.BackendType`) não pode ser lido; usa-se `RendererInfo`, o nome do tipo e a superfície da janela.
- `GameHost.ResolvedRenderer` só é preenchido no caminho automático.
- Com as variáveis `OSU_GRAPHICS_*` o framework não faz fallback e não há API pública para "pedir Vulkan e cair para
  OpenGL" nesse modo; por isso o fallback explícito é por reinício de processo.
- Falhas na thread de desenho depois da criação do renderer e crashes nativos de driver não são capturáveis em C#.

## Fora do alcance desta revisão (depende do CI ou do hardware)

Compilação, resultado dos testes, `dotnet restore` no Arch e no Windows, presença das bibliotecas nativas no
runtime, OpenGL e Vulkan na Intel UHD G1, comportamento do fallback e desempenho. Ver `docs/validation-plan.md`.
