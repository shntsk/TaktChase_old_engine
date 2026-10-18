# Plano de validação manual (hardware real)

Referência: Intel Core i3-1005G1 · Intel UHD Graphics G1 (integrada) · 20 GB RAM · Arch Linux x86_64 e Windows x64.

## O que o CI cobre e o que não cobre

| Item | CI (GitHub Actions) | Notebook real |
|---|---|---|
| Restore e build (Windows x64 e container Arch) | Sim | — |
| Presença dos assemblies dos dois backends no build | Sim | — |
| Testes NUnit headless (visuais e unitários) | Sim | — |
| OpenGL em GPU física | **Não** | **Sim** |
| Vulkan em GPU física | **Não** | **Sim** |
| Fallback Vulkan → OpenGL | **Não** | **Sim** |
| FPS, frametime, memória, estabilidade | **Não** | **Sim** |

Em todos os cenários, anote: versão do TaktChase, `diagnostics.txt` e o log do framework.
Local dos dados: Linux `~/.local/share/TaktChase`, Windows `%APPDATA%\TaktChase`
(confira o caminho real no log do framework). Log de falha de inicialização:
`<LocalApplicationData>/TaktChase/logs/startup-failure.log`.

## Pré-requisitos

**Arch:** `dotnet-sdk-10.0`, `mesa`, `vulkan-intel` (puxa `vulkan-icd-loader`), opcional `vulkan-tools`.
**Windows:** .NET 10 SDK em uma versão de Windows suportada pelo .NET 10 (ver README: Windows 11, ou Windows 10 apenas Enterprise/IoT/LTSC 1607, 1809 e 21H2) e o driver gráfico Intel instalado pelo fabricante.

Compile na máquina: `dotnet build -c Release`.

## Cenários

### 1. Arch + OpenGL (Intel UHD G1)
1. `dotnet run --project TaktChase.Desktop -c Release -- --renderer=opengl --show-fps`
2. F3: confirmar "Superfície gráfica da janela: OpenGL" e tipo `GLRenderer`; sem alertas de divergência.
3. Confirmar no log do framework as linhas de versão/renderer/vendor do OpenGL (esperado: driver Mesa/Intel).

### 2. Arch + Vulkan
1. `vulkaninfo --summary` (ver se a UHD G1 aparece).
2. `dotnet run --project TaktChase.Desktop -c Release -- --renderer=vulkan --show-fps`
3. F3: "Superfície gráfica da janela: Vulkan", tipo `VeldridRenderer`, pré-verificação Vulkan com ≥ 1 dispositivo.

### 3. Windows + OpenGL
`TaktChase.Desktop.exe --renderer=opengl --show-fps` → F3 deve mostrar OpenGL / `GLRenderer`.

### 4. Windows + Vulkan
`TaktChase.Desktop.exe --renderer=vulkan --show-fps` → F3 deve mostrar Vulkan / `VeldridRenderer`.
Se falhar, siga o cenário 5.

### 5. Falha de Vulkan e fallback
Provoque uma falha controlada sem alterar drivers, por exemplo:
- Linux: `VK_DRIVER_FILES=/inexistente dotnet run --project TaktChase.Desktop -- --renderer=vulkan`
  (variável do loader Vulkan; confirme na documentação do loader da sua versão).
- Alternativa: máquina sem driver Vulkan.

Esperado: diagnóstico no stderr/log (e caixa de diálogo no Windows se não houver fallback); o jogo reinicia
com OpenGL **uma única vez** (confirme no gerenciador de tarefas/`ps` que não há reinícios em sequência); F3 mostra
"Fallback: sim", o motivo e o indicador âmbar no chip do backend. O log da falha original fica em
`startup-failure.log`. Repita com
`--no-fallback`: deve exibir o diagnóstico e encerrar com código 2, sem reiniciar.
Verifique também que, nesse caso, o backend informado é o **efetivo** (OpenGL), não o pedido.

### 5b. Caminhos de falha adicionais
1. **OpenGL com falha forçada** (se conseguir provocá-la sem alterar drivers): `--renderer=opengl` deve mostrar o
   diagnóstico e encerrar com código 2, sem reinício.
2. **`auto` sem nenhum renderer utilizável** (por exemplo, sem driver gráfico): o framework registra "No usable
   renderer was found!" e `GameHost.Run` retorna sem exceção; o TaktChase deve mostrar o diagnóstico e encerrar com
   código 2 (e não sair em silêncio com código 0).
3. **Pré-verificação + framework no mesmo processo:** com `--renderer=vulkan` bem-sucedido, confirme que a janela
   abre e que não há travamento/crash após a pré-verificação (ela carrega e libera o loader Vulkan antes do framework).
4. Conferir que `startup-failure-fallback.log` só aparece quando o próprio fallback falha.

### 6. Desempenho e estabilidade
Para cada backend que funcionar (opengl, vulkan, e `auto`):
1. `--show-fps`; anote FPS e frametime médios em 1920×1080 e em janela menor; redimensione a janela.
2. Ctrl+F11 (estatísticas do framework) para ver tempos por thread; Ctrl+F7 alterna o limitador de quadros.
3. Memória: F3 → working set e heap GC no início, após 10 min e após 60 min; contagens de coleta gen2 não devem crescer continuamente.
4. Deixe aberto ≥ 60 min; registre travamentos, artefatos visuais, quedas de FPS e vazamento de memória.
5. Compare OpenGL × Vulkan (e Direct3D 11 no `auto` do Windows) com as mesmas condições.

## Registro de resultados (copiar para cada teste)

```
Data / SO / driver:
Cenário:
Backend efetivo (F3):
FPS / frametime:
Memória inicial / final:
Estabilidade:
Observações:
```
