# Dependency audit: SixLabors.ImageSharp

Review date: 2026-10-18. Source-level review only; nothing was restored, built, or run.

## Finding

The restore step of the CI logs reports NuGet audit warnings for `SixLabors.ImageSharp` **3.1.11**
(moderate and high severity). The package is not referenced by TaktChase. It arrives transitively:

```
TaktChase.Game  ->  ppy.osu.Framework 2026.1008.0  ->  SixLabors.ImageSharp (>= 3.1.11)
```

The `>= 3.1.11` lower bound is what the NuGet page lists for the two preceding framework releases (2026.616.0 and 2026.921.0),
and the CI resolved exactly 3.1.11 for 2026.1008.0.

## Advisories

Five advisories were published on 2026-10-07 (three high, two moderate): GHSA-j3p4-wp97-rph4, GHSA-j9gm-c75j-xc9q,
GHSA-jjfr-hcj7-qf5w (high), GHSA-gwg2-r3hj-4w44 and GHSA-wmxv-xphr-5c9g (moderate). Public reports on the advisory pages
and downstream issue trackers state that they affect versions up to and including 4.1.1, and that the **first patched
version is 4.1.2**. They also report that **no patched 3.1.x release exists**: 3.1.12 is still listed as vulnerable on NuGet.
At the time of this review the latest ImageSharp release is 4.1.3, and the latest `ppy.osu.Framework` on NuGet is still 2026.1008.0,
which was released the day after the advisories.

Verify the advisory ranges yourself on the GitHub Advisory Database before acting; this document records what was found, not a
guarantee.

## Decision: no override applied

A safe override would have to be 4.1.2 or later, which is a **major-version jump** from the 3.x line the framework is compiled against.

- NuGet allows it (`>= 3.1.11` has no upper bound), but that only means the restore succeeds. The framework's compiled calls to
  ImageSharp 3.x APIs are not guaranteed to exist in 4.x. ImageSharp's previous major change (2.x to 3.x) removed overloads
  and changed property setters, which surfaces as `MissingMethodException` at runtime, not as a build error.
- Texture and font loading in the framework go through ImageSharp, so a mismatch would break the game at startup or on first texture load.
- Compatibility cannot be established by reading source, and this review was not allowed to build or run anything.

Per the project rules, the warnings are not suppressed, NuGet auditing is not disabled, and no package was upgraded blindly.
`TaktChase.Game.csproj` therefore still references only `ppy.osu.Framework 2026.1008.0`.

## Exposure in TaktChase

At this stage the game decodes only its own embedded 256×256 PNG mark and the framework's embedded font assets. It does not open
user-supplied image files. The advisories concern decoders and encoders processing crafted image input, so exposure today appears
limited. This is an assessment from reading the code, not a security guarantee, and it will change once TaktChase loads
skins or beatmap backgrounds.

## Next steps

1. Watch `ppy.osu.Framework` releases and move to the first one that depends on `SixLabors.ImageSharp >= 4.1.2`
   (or on any patched version). That is the intended fix.
2. Optionally test an override on a separate branch first. Add a direct reference to the patched version in `TaktChase.Game.csproj`:

   ```xml
   <PackageReference Include="SixLabors.ImageSharp" Version="4.1.3" />
   ```

   Then run both CI workflows and the visual test browser, and launch the game on real hardware to confirm that textures and
   fonts load. Merge only if that works. If the framework fails with `MissingMethodException`, revert and wait for the framework update.
3. Before TaktChase loads any user-supplied images, either have the patched dependency in place or restrict image loading.
4. After any change, run `dotnet list package --vulnerable --include-transitive` to confirm what resolves.
