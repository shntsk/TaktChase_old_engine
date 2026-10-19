# std::lite skin (Stage 2A: core graphics)

Source: the supplied `lite-main` archive (std::lite 2.7 by sineplusx, CC-BY 4.0; licence and README are kept in `TaktChase.Resources/Skins/lite/`).
The skin's sounds are not by the skin author; see `Skins/lite/audio/LICENSES.md` for their attribution.

## Inspection result

- The pinned `ppy.osu.Framework` 2026.1008.0 loads textures through ImageSharp (PNG/JPG/etc.) and has no SVG loader, so the SVGs had to be converted.
- Only the assets Stage 2A needs were converted: `hitcircle/normal/hitcircle.svg`, `hitcircle/approachcircle.svg` and `fonts/hitcircle/numeral-0..9.svg`.
  Everything else (sliders, spinner, cursor, judgements, follow points, highspeed variants) is untouched and not packaged as graphics.
- The SVGs were rasterised unchanged (no redrawing) at 2x with `tools/render_lite_skin.py` (Chromium, transparent background).
  Exception: `approachcircle.svg` uses `<clipPath><use href="#group"/></clipPath>`, which Inkscape accepts but Chromium ignores (the first render was fully
  transparent). The clip geometry is inlined with identical shapes; the artwork is not altered.

## Converted images

| Texture | Size (px) | Notes |
|---|---|---|
| `hitcircle.png` | 600x600 | white ring + grey gradient body, soft shadow; visible diameter 236 of 300 SVG units |
| `approachcircle.png` | 528x528 | thin opaque white ring, fully transparent centre, ring outer diameter 252 of 264 units. The ring is thin, not faint; centre transparency is intentional |
| `default-0..9.png` | 192x192 (`1`: 180x192) | white numerals with drop shadow |

All are RGBA PNGs. Checks done at conversion time: non-zero alpha, transparent corners/centre, opaque pixels present, bounding boxes match the SVG geometry,
and a visual composite on a dark background. `LiteSkinAssetsTest` repeats the structural checks against the embedded resources in CI.

## Packaging

- `TaktChase.Resources/Textures/Skin/lite/*.png` are `EmbeddedResource`s (same mechanism as `taktchase-mark.png`), read through `DllResourceStore` /
  `TextureStore` as `Skin/lite/<name>`. No downloads and no Android-specific asset folder are needed.
- `TaktChase.Resources/Skins/lite/` is **not embedded**. It keeps, for Stage 2B: `audio/` (all 29 WAVs of the skin, 10 with content, 19 intentionally empty
  files per the skin's own `LICENSES.md`), `source/` (the original SVGs used), `config/numerals.ini` and the licence.
  Content-bearing audio: `{normal,soft,drum}-hit{normal,clap,finish}.wav` (hitclap/finish) and `spinnerbonus.wav`; all 44.1 kHz stereo PCM, 16-bit except
  `soft-hitnormal.wav` (24-bit; check BASS handling in 2B). Note the skin contains the typo'd `nomal-sliderslide.wav` and `nightcore-*` files.
  `*-hitwhistle` and slider sounds are empty files.

## Rendering

`DrawableHitCircle` builds the body (hit circle sprite + digit sprites) and the approach circle from these textures. Size comes from the unchanged
`BeatmapDifficulty.CircleRadius`: the sprite is scaled so the ring's outer edge equals the hit radius (`LiteSkinLayout.ScaleForDiameter`).
Digits use the skin's `HitCircleOverlap` of 7 and are scaled down if a number is wider than 70% of the circle. If a texture cannot be found the
previous plain shapes are drawn instead. Skin combo colours are not applied (the artwork is shown as supplied). Positions, timing, approach scale and alpha
(`HitCircleAnimation`), hit detection and all input code are unchanged.

## Still to verify (CI / device)

Build and unit tests (CI), and the actual on-screen look and sizing on desktop and an Android device. Not tested locally.
