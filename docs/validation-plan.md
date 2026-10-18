# Manual validation plan (real hardware)

Reference: Intel Core i3-1005G1 · Intel UHD Graphics G1 (integrated) · 20 GB RAM · Arch Linux x86_64 and Windows x64.

## What CI covers and what it does not

| Item | CI (GitHub Actions) | Real laptop |
|---|---|---|
| Restore and build (Windows x64 and Arch container) | Yes | — |
| Static packaging check: both renderer types in `osu.Framework.dll`, Veldrid runtime dependencies in `deps.json` and the output folder (not that they work) | Yes | — |
| Headless NUnit tests (visual and unit) | Yes | — |
| OpenGL on a physical GPU | **No** | **Yes** |
| Vulkan on a physical GPU | **No** | **Yes** |
| Vulkan → OpenGL fallback | **No** | **Yes** |
| FPS, frame time, memory, stability | **No** | **Yes** |

In every scenario, record: TaktChase version, `diagnostics.txt`, and the framework log.
Data location: Linux `~/.local/share/TaktChase`, Windows `%APPDATA%\TaktChase`
(check the real path in the framework log). Startup failure log:
`<LocalApplicationData>/TaktChase/logs/startup-failure.log`.

The F3 panel cannot show the graphics surface or the renderer's VSync state: the framework's public API does not expose them, and the
panel reports them as "Unavailable". Confirm the real backend from the framework log lines in the panel and, for Vulkan, from `vulkaninfo`
(Linux) or the driver tools (Windows).

## Prerequisites

**Arch:** `dotnet-sdk-10.0`, `mesa`, `vulkan-intel` (pulls in `vulkan-icd-loader`), optional `vulkan-tools`.
**Windows:** .NET 10 SDK on a Windows version supported by .NET 10 (see README: Windows 11, or Windows 10 only Enterprise/IoT/LTSC 1607, 1809, and 21H2) and the Intel graphics driver installed from the vendor.

Build on the machine: `dotnet build -c Release`.

## Scenarios

### 1. Arch + OpenGL (Intel UHD G1)
1. `dotnet run --project TaktChase.Desktop -c Release -- --renderer=opengl --show-fps`
2. F3: confirm "Renderer type: GLRenderer" and "Request check: Consistent with the request"; no mismatch alerts.
3. In the framework log, confirm the OpenGL version/renderer/vendor lines (expected: Mesa/Intel driver).

### 2. Arch + Vulkan
1. `vulkaninfo --summary` (check that the UHD G1 appears).
2. `dotnet run --project TaktChase.Desktop -c Release -- --renderer=vulkan --show-fps`
3. F3: "Renderer type: VeldridRenderer", "Request check: Partially verified" (the surface itself is not exposed), and a Vulkan pre-check with ≥ 1 device.
   Confirm Vulkan from the framework log lines and `vulkaninfo`, since the panel cannot.

### 3. Windows + OpenGL
`TaktChase.Desktop.exe --renderer=opengl --show-fps` → F3 must show "Renderer type: GLRenderer" and "Request check: Consistent".

### 4. Windows + Vulkan
`TaktChase.Desktop.exe --renderer=vulkan --show-fps` → F3 must show "Renderer type: VeldridRenderer" and "Request check: Partially verified".
If it fails, follow scenario 5.

### 5. Vulkan failure and fallback
Provoke a controlled failure without changing drivers, for example:
- Linux: `VK_DRIVER_FILES=/nonexistent dotnet run --project TaktChase.Desktop -- --renderer=vulkan`
  (a Vulkan loader variable; confirm in the loader documentation for your version).
- Alternative: a machine without a Vulkan driver.

Expected: a diagnosis on stderr/in the log (and a dialog box on Windows if there is no fallback); the game restarts
with OpenGL **exactly once** (confirm in the task manager/`ps` that there are no consecutive restarts); F3 shows
"Fallback: Yes", the fallback reason, and the amber indicator on the backend chip. The original failure's log is in
`startup-failure.log`. Repeat with
`--no-fallback`: it must show the diagnosis and exit with code 2, without restarting.
Also verify that, in this case, the reported backend is the **active** one (OpenGL), not the requested one.

### 5b. Additional failure paths
1. **OpenGL with a forced failure** (if you can provoke one without changing drivers): `--renderer=opengl` must show the
   diagnosis and exit with code 2, without a restart.
2. **`auto` with no usable renderer** (for example, no graphics driver): the framework logs "No usable
   renderer was found!" and `GameHost.Run` returns without an exception; TaktChase must show the diagnosis and exit with
   code 2 (and not exit silently with code 0).
3. **Pre-check + framework in the same process:** with a successful `--renderer=vulkan`, confirm that the window
   opens and that there is no hang/crash after the pre-check (it loads and releases the Vulkan loader before the framework).
4. Confirm that `startup-failure-fallback.log` only appears when the fallback itself fails.

### 6. Performance and stability
For each backend that works (opengl, vulkan, and `auto`):
1. `--show-fps`; record average FPS and frame time at 1920×1080 and in a smaller window; resize the window.
2. Ctrl+F11 (framework statistics) to see per-thread times; Ctrl+F7 toggles the frame limiter.
3. Memory: F3 → working set and GC heap at the start, after 10 min, and after 60 min; gen2 collection counts must not grow continuously.
4. Leave it open for ≥ 60 min; record hangs, visual artifacts, FPS drops, and memory leaks.
5. Compare OpenGL vs. Vulkan (and Direct3D 11 in Windows `auto`) under the same conditions.

## Results log (copy for each test)

```
Date / OS / driver:
Scenario:
Active renderer (F3: Active renderer, Renderer type, Request check):
Backend confirmed by (framework log / vulkaninfo / other):
FPS / frame time:
Initial / final memory:
Stability:
Notes:
```
