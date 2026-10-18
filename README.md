# TaktChase — Etapa 0 (fundação)

Jogo de ritmo baseado em precisão temporal, mira e leitura de objetos, em C# sobre o
[osu-framework](https://github.com/ppy/osu-framework). Esta etapa entrega apenas a **fundação**:
janela, tela inicial, seleção de backend OpenGL/Vulkan, diagnóstico gráfico e CI. Não há parser de
beatmaps, gameplay, sliders, skins, resultados nem multiplayer.

> **Estado de validação:** nada foi compilado nem testado na criação nem na revisão estática deste pacote.
> Compilação e testes headless são validados pelos workflows de CI; OpenGL/Vulkan em GPU real exigem os
> testes manuais de [`docs/validation-plan.md`](docs/validation-plan.md). **Não há afirmação de
> compatibilidade universal.** A revisão estática que originou esta versão está resumida em
> [`docs/revisao-estatica.md`](docs/revisao-estatica.md).

## Versões e como foram verificadas

Verificadas em 18/10/2026 nas fontes oficiais (páginas do NuGet, repositório `ppy/osu-framework` na tag
`2026.1008.0` e documentação do .NET). Nada foi escolhido por palpite:

| Item | Versão | Fonte |
|---|---|---|
| `ppy.osu.Framework` | **2026.1008.0** (a mais recente no NuGet; alvo `net10.0`) | nuget.org/packages/ppy.osu.Framework |
| `ppy.osu.Framework.Templates` | 2026.1008.0 (mesma versão do framework) | nuget.org/packages/ppy.osu.Framework.Templates |
| .NET | `net10.0`; SDK `10.0.100` com `rollForward: latestFeature` | `global.json` idêntico ao do repositório do framework na tag; o README do framework pede o .NET 10 SDK |
| `Microsoft.NET.Test.Sdk` | **18.8.1** | `TemplateGame.Game.Tests.csproj` do template; versão listada no NuGet |
| `NUnit3TestAdapter` | **6.2.0** | idem; última estável no NuGet |
| `NUnit` | **sem referência direta** (≥ 4.6.1, transitivo) | o template não referencia o NUnit; a dependência vem de `ppy.osu.Framework` (NUnit ≥ 4.6.1 na lista de dependências do NuGet) |

Estrutura (mesma organização do template: Game / Desktop / Game.Tests / Resources):

```
TaktChase.sln
TaktChase.Game/       lógica do jogo, tela inicial, diagnóstico, seleção de backend
TaktChase.Desktop/    ponto de entrada fino (WinExe)
TaktChase.Game.Tests/ test browser visual, cenas de teste e testes unitários (NUnit)
TaktChase.Resources/  texturas embutidas (marca própria do TaktChase)
.github/workflows/    ci-windows.yml, ci-arch.yml
docs/validation-plan.md
```

> O template foi reproduzido à mão (`dotnet new` não foi executado) e depois comparado, de forma estática, com os
> arquivos publicados em `osu.Framework.Templates/templates/template-empty` na tag `2026.1008.0`.
> Diferenças deliberadas em relação ao template: sem projetos iOS/Android, sem `game.ico` (o ícone do template é
> da ppy), sem `.slnf`, `.vscode`, `.idea` e `*.DotSettings`; o executável chama-se `TaktChase.Desktop`
> (no template, `TemplateGame`); o `Main` do Desktop recebe `args`, com `[STAThread]` como no `SampleGame.Desktop`
> do próprio framework; `GenerateDocumentationFile` não é ligado.
> Se preferir, gere um projeto de referência com `dotnet new install ppy.osu.Framework.Templates` e
> `dotnet new osu-framework-game -n Referencia` e compare.

## Como compilar e executar (na sua máquina)

```
dotnet restore
dotnet build -c Release
dotnet run --project TaktChase.Desktop -c Release -- --renderer=opengl --show-fps
```

## Seleção de OpenGL e Vulkan

A seleção acontece em `TaktChaseLauncher.Run`, **antes** de criar o host gráfico, usando apenas os
mecanismos oficiais documentados na wiki "Environment variables" do framework.

| Argumento | O que o TaktChase faz | O que o framework executa |
|---|---|---|
| `--renderer=auto` (padrão) | Não define nada | `GameHost.ChooseAndSetupRenderer` com a ordem de fallback do próprio framework |
| `--renderer=opengl` | `OSU_GRAPHICS_RENDERER=gl`, `OSU_GRAPHICS_SURFACE=opengl` | `GLRenderer` (OpenGL), sem fallback interno |
| `--renderer=vulkan` | `OSU_GRAPHICS_RENDERER=veldrid`, `OSU_GRAPHICS_SURFACE=vulkan` | `VeldridRenderer` com superfície Vulkan, sem fallback interno |

Outros argumentos: `--show-fps` (mostra FPS/frametime na abertura) e `--no-fallback` (desativa o
reinício com OpenGL quando o Vulkan falha).

**Importante sobre `auto`:** a ordem lida em `GameHost.GetPreferredRenderersForCurrentPlatform` é, no
Windows, Direct3D 11 → D3D11 experimental → OpenGL → Vulkan experimental; no Linux, OpenGL → OpenGL
experimental → Vulkan experimental. Portanto, **no Windows `auto` tende a escolher Direct3D 11**
(via Veldrid), não OpenGL; no Linux tende a escolher OpenGL. Use `--renderer=opengl` ou
`--renderer=vulkan` para forçar. Uma configuração `Renderer` já salva pelo framework (`framework.ini`)
também pode alterar a ordem do `auto`.

Detalhes confirmados no código da tag `2026.1008.0` (`FrameworkEnvironment` e `GameHost`):

- `OSU_GRAPHICS_RENDERER` aceita `gl`/`opengl`, `veldrid` (e `deferred`, não usado aqui). O ramo explícito do
  `switch` é `"gl"`; `"opengl"` só funciona por cair no ramo padrão, por isso o TaktChase usa `gl`. Um valor
  desconhecido cai no `GLRenderer` sem aviso.
- `OSU_GRAPHICS_SURFACE` é lido com `Enum.TryParse<GraphicsSurfaceType>` (sem diferenciar maiúsculas); valor
  inválido vira "não definido" sem aviso.
- Se só a superfície for definida, o renderer assumido é `veldrid`; se só o renderer for definido, a superfície
  assumida é OpenGL. Por isso o TaktChase sempre define **as duas**.
- `veldrid` + superfície `vulkan` leva a `GraphicsDevice.CreateVulkan` (`VeldridDevice`); **nada** seleciona o
  Vulkan automaticamente em `VeldridRenderer`: a superfície é que decide o backend.
- No modo automático o framework **não** tenta `VeldridRenderer`+Vulkan: a ordem inclui apenas `Deferred_Vulkan`
  (renderer experimental). `--renderer=vulkan` do TaktChase testa, portanto, um caminho diferente do `auto`.

As variáveis são lidas pelo framework uma única vez por processo (construtor estático de
`FrameworkEnvironment`), por isso são definidas antes de qualquer chamada ao framework. Se
`OSU_GRAPHICS_RENDERER`/`OSU_GRAPHICS_SURFACE` já estiverem definidas no ambiente, o framework as obedece
mesmo com `--renderer=auto`, e o TaktChase registra um aviso.

## Fallback e limitações

- **`auto`:** o fallback é o do framework (tenta cada renderer da ordem acima até um inicializar). Se **todos**
  falharem, `GameHost.Run` **não lança exceção**: registra "No usable renderer was found!" e retorna. O
  `TaktChaseLauncher` detecta esse caso (`host.Renderer == null` sem a tela inicial ter carregado), mostra o
  diagnóstico e encerra com código 2.
- **`--renderer=vulkan`:** com as variáveis oficiais o framework falha de forma definitiva. O TaktChase
  captura a exceção **somente se a falha ocorrer antes de a tela inicial carregar**, escreve o
  diagnóstico (stderr, `%LOCALAPPDATA%/TaktChase/logs/startup-failure.log` ou equivalente em
  `~/.local/share`, e caixa de diálogo no Windows) e **reinicia o processo** com `--renderer=opengl`,
  informando o motivo na tela (painel F3 e indicador âmbar no chip do backend). O fallback só dispara após falha real, nunca
  por suposição sobre drivers.
- `--renderer=opengl` que falha **não** tem fallback: o erro é mostrado. Use `--renderer=auto`.
- **Sem ciclo de reinício:** o processo filho é sempre iniciado com `--renderer=opengl` e com
  `TAKTCHASE_FALLBACK_FROM` definida; nessas condições `ShouldOfferOpenGLFallback` é falso (coberto por teste).
  Se o filho também falhar, o log dele vai para `startup-failure-fallback.log` (não sobrescreve o da falha
  original) e o diagnóstico inclui a causa original.
- **Argumentos repassados no reinício:** apenas `--renderer=opengl` e `--show-fps`. Os demais argumentos do
  TaktChase não têm efeito após a falha (`--no-fallback` já foi considerado; argumentos desconhecidos só geram
  aviso).
- O fallback é por reinício de processo porque as variáveis do framework não podem ser trocadas no mesmo
  processo; o framework não oferece um mecanismo oficial equivalente para "pedir Vulkan e cair para GL"
  nesse modo.
- Falhas que derrubam o processo dentro do driver (crash nativo) não podem ser capturadas por código C#.
- A pré-verificação Vulkan carrega o loader do sistema, cria uma instância e o libera **no mesmo processo** em
  que o framework depois inicializa o Vulkan. Não há como confirmar sem hardware que todos os drivers toleram
  isso; está no plano de validação.
- Uma falha **depois** da tela inicial carregada não é tratada como falha de renderer.

## Compilar vs. executar Vulkan

- **Para compilar:** apenas o SDK .NET e os pacotes NuGet. O `ppy.osu.Framework` já contém o código dos
  dois caminhos de renderização (OpenGL e Veldrid/Vulkan); não existe opção de build que ligue/desligue um
  backend, e nenhum é desativado em silêncio. O CI confirma a presença de `osu.Framework.dll` e `Veldrid*.dll`.
- **Para executar Vulkan:** é necessário, no sistema, um **loader Vulkan** (`libvulkan.so.1` /
  `vulkan-1.dll`) **e** um **driver Vulkan** da GPU. Compilar com sucesso não implica que isso exista.
  O TaktChase não presume nada: com `--renderer=vulkan` ele faz uma pré-verificação (carrega o loader, cria
  uma instância, enumera dispositivos) apenas como diagnóstico; a prova final é a inicialização do framework.

### Pacotes e drivers (verificados nas páginas de pacotes do Arch e na documentação da Microsoft)

**Arch Linux x86_64**

- `dotnet-sdk-10.0` (linha versionada do repositório `extra`; o meta-pacote `dotnet-sdk` mudará de versão maior
  no futuro e quebraria o `global.json`). Em páginas de pacotes consultadas em 18/10/2026 apareceram
  `10.0.10.sdk110-1` (`dotnet-sdk`) e `10.0.8.sdk108-1` (`dotnet-sdk-10.0`, página mais antiga): a versão exata
  muda com o tempo e **não foi confirmada para o instante da compilação**. A faixa `sdk1xx` ≥ 100 satisfaz o
  `global.json` (`10.0.100`, `latestFeature`).
- OpenGL: `mesa` (provê `opengl-driver`).
- Vulkan na Intel UHD (Ice Lake, driver Mesa): `vulkan-intel`, que depende de `vulkan-icd-loader`.
- Opcional para diagnóstico: `vulkan-tools` (ferramentas Vulkan). Confirme o binário com `pacman -Ql vulkan-tools | grep vulkaninfo`.
- O README do framework pede ffmpeg do sistema no Linux apenas para decodificação de vídeo (não usado nesta etapa).

**Windows x64**

- Segundo `release-notes/10.0/supported-os.md` do repositório `dotnet/core` (atualizado em 28/09/2026), o .NET 10
  é suportado em: **Windows 11** 26H1, 25H2, 24H2 e 23H2 (Enterprise/Education); **Windows 10** apenas nas
  edições Enterprise/IoT/LTSC 1607, 1809 e 21H2; **Windows Server** 2025, 23H2, 2022, 2019, 2016, 2012 R2 e 2012
  (as duas últimas com ESU). Windows 7 e 8.1 não constam. Arquitetura alvo do TaktChase: **x64**.
- Esse documento lista as versões **suportadas pela Microsoft para o .NET**; não é uma garantia de que o
  osu-framework, o SDL3 ou o driver gráfico funcionem em cada uma delas.
- Arch Linux **não** consta na lista oficial de distribuições do .NET 10; o .NET no Arch vem dos pacotes da
  própria distribuição.
- OpenGL e Vulkan dependem do **driver gráfico do fabricante** (instalado pelo próprio usuário). **Não verificado:** onde o `vulkan-1.dll` é distribuído e qual versão de driver Intel expõe Vulkan para a UHD G1; confirme na documentação da Intel/Khronos antes de afirmar. O TaktChase não instala nem distribui drivers.

## Diagnóstico em tempo de execução

Tecla **F3** (ou o arquivo `diagnostics.txt` na pasta de dados do jogo) mostra apenas dados reais. O backend
**efetivo** vem sempre do host já inicializado, nunca do argumento: `GameHost.RendererInfo`, o tipo de
`host.Renderer` e `host.Window.GraphicsSurface.Type`. Há alertas quando o pedido (`--renderer`) diverge do que o host
inicializou (superfície e tipo de renderer). Limite da API pública: as classes de renderer são internas ao
framework, então o backend interno do Veldrid (`Device.BackendType`) não é consultável; a superfície `Vulkan`
combinada com `VeldridRenderer` é a evidência disponível.

- Renderer solicitado × backend efetivo (`GameHost.RendererInfo`), tipo do renderer, superfície gráfica da janela.
- `ResolvedRenderer` (somente no modo automático: `GameHost` só o preenche nesse caminho), VSync do renderer.
- Fallback (de qual backend, por quê) e avisos de divergência pedido × ativo.
- Resultado da pré-verificação Vulkan (se pedido): loader, versão de instância, `vkCreateInstance`, nº de dispositivos.
- Sistema, arquitetura, versão do .NET, processadores lógicos.
- Linhas do log do framework com informações gráficas (versão/renderer/vendor do OpenGL etc.). O framework só as
  registra no log; não há propriedade pública, então são exibidas como linhas de log, sem interpretação.
- Memória (working set, heap GC, contagem de coletas), atualizada a cada 2 s com o painel aberto.

**Não disponível via API pública verificada:** nome da GPU no Vulkan e limites do dispositivo. O painel mostra
"n/d" ou omite o dado, nunca inventa. Use `vulkaninfo` e o log do framework para isso.

**F2** liga/desliga FPS e frametime (relógio do thread de atualização). Para estatísticas por thread, use o
overlay do framework (**Ctrl+F11**); Ctrl+F7 alterna o limitador de quadros. O TaktChase não impõe resolução,
taxa de atualização nem limite de FPS.

## Desempenho

- Sem pós-processamento, shaders customizados nem dependências gráficas extras; apenas drawables do framework.
- Um único asset carregado na tela inicial (marca de 256×256 px, ~3 KB); fonte padrão do framework.
- Contadores só reescrevem texto a 4 Hz (FPS) e 0,5 Hz (memória, só com o painel aberto).
- Animação do pulso: poucas transformações por segundo.
- Janela redimensionável; conteúdo desenhado para 1920×1080 e escalado proporcionalmente (`DrawSizePreservingFillContainer`).

## CI

| Workflow | Ambiente | O que roda |
|---|---|---|
| `ci-windows.yml` | `windows-latest` (Windows Server x64) | restore, build Release, verificação de assemblies dos dois backends, `dotnet test` headless |
| `ci-arch.yml` | `ubuntu-latest` com container oficial `archlinux:latest` + `pacman -S dotnet-sdk-10.0` | idem; o kernel é do runner Ubuntu, o userspace é Arch |

Nenhum workflow executa OpenGL/Vulkan em GPU física.

## Licenças

O osu-framework é MIT. A biblioteca de áudio BASS (dependência) é produto comercial: gratuita para uso não
comercial; obtenha licença para distribuição comercial. A licença do TaktChase ainda não foi definida.
