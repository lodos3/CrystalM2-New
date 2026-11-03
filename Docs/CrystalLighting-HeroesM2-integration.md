# CrystalM2 rendering and HeroesM2 integration status

This branch is based on `lodos3/CrystalM2-New` (C# .NET 8 Windows, SlimDX/Direct3D9).
Do not transplant the Direct2D DXManager from `lodos3/Crystal-master` over the Direct3D9 implementation without a full backend migration.
The `lodos3/crystalm2` WebGL2 game is a distinct browser renderer: its implementation and shaders cannot be copied into this project unchanged.

## Implemented on this branch
- Decode map-cell lights by tens-digit colour and units-digit radius. The old `Light >= 10` check accidentally eliminated coloured lights, and the new decoder clamps texture indices.
- Static map-cell lights no longer require a nonempty front tile just to be eligible.
- Spell animation frames can illuminate the light render target using their native sprite silhouette instead of only a circular glow.
- Animated spell/effect instances use their actual frame alpha and colour when available; effect-less light sources retain radial fallback. Effect light indices are bounded.
- Optional, **monster-only** faction lookup from `Envir/MonsterFactionList.txt`, using HeroesM2's monster-name/faction-ID idea. Wild mobs assigned different positive IDs are hostile; equal IDs are friendly. Unassigned mobs retain Crystal targeting. Tamed pets and special guard AI retain their existing overrides.
- Two dependency-free .NET console regression projects validate colour decoding and faction parsing/relations.

## Faction configuration

Place `MonsterFactionList.txt` in the server's configured `Settings.EnvirPath`, normally `./Envir/` relative to the server process:

```text
; MonsterName FactionId
Oma 1
ZumaMonster 2
```

Each active line must consist of one monster database **Name** token and one positive integer faction ID. Entries are case insensitive; the last entry for a name wins. The file is optional and loaded once on first monster construction, so restart after editing. Malformed entries produce a descriptive parse exception. Player faction IDs and persistent NPC script switching are **not** part of this port.

## Verified source evidence

- `Client/MirScenes/GameScene.cs`: existing map-light test excluded light values >= 10 immediately before a switch on the tens digit (1 to 4).
- `Client/MirObjects/SpellObject.cs` and `Effect.cs`: existing spell/effect frame libraries, frame indices, and sprite offsets used for shape lighting.
- Uploaded HeroesM2 archive: `Source/M2Engine/LocalDB.pas` reads `MonsterFactionList.txt`; `ObjActor.pas` has faction hostility rules; `ObjNpc.pas` contains `CHECKFACTION` and `CHANGEFACTION`. This is a Delphi codebase, not a drop-in C# library.

## Not implemented or not validated

- Content-dependent ambient darkness remains future work; day/night ambient interpolation is implemented with a 10-second easing transition.
- General HDR/multi-pass bloom and advanced blend modes remain future work. A single-sample pixel-shader post-process and limited sprite-shadow checker repair are now implemented.
- HeroesM2 player factions, saved faction membership, NPC `CHECKFACTION` / `CHANGEFACTION` script support, or client faction UI.
- Windows x64 Direct3D9 rendering and full solution build have **not** been run in this environment (no .NET SDK/Direct3D device available).
- Codex/chat conversation summaries are useful requirements, not recoverable source revisions. This PR should not be treated as a merge of every previous Codex session.

## Reproducible verification

```sh
dotnet run --project Tests/RenderingLightTests/RenderingLightTests.csproj
dotnet run --project Tests/MonsterFactionTests/MonsterFactionTests.csproj
dotnet build "Legend of Mir.sln" -c Release
```

On Windows x64 with the original Mir assets:
1. Enter a dark map containing encoded map lights 1, 21, 32 and 49; confirm white/blue/orange/green and no out-of-range crash. Check a lighted cell with no front sprite.
2. Cast a spell with non-circular frame artwork and inspect whether its illuminated pixels follow the animation. Compare LightEffect (no library) for a soft round fallback.
3. Check night, dawn, evening, and day map transitions for *existing* abrupt switching (not fixed here).
4. Add two monsters with the same configured faction and then two with different faction IDs. Verify that same-faction monsters are not hostile and cross-faction wild monsters target one another; repeat without the file and with pets/guards.
5. Check shader/render-state recovery, screen resizing, missing magic library images, and performance with many simultaneous spells.

The branch is intentionally **draft** until the actual Windows game client and server are built and exercised.


## Client memory / FPS work (follow-up)

- Replaces the stale MImage.Data native pointer (formerly read after UnlockRectangle) with a managed, bit-packed alpha hit-test cache. Transparent pixels remain non-clickable and visible bounds are captured once per texture upload, not repeatedly rescanned. Stores one bit per pixel instead of a full BGRA copy.
- Handles Direct3D9 row pitch explicitly when a GPU texture lock includes row padding, preventing rows from being interpreted at the wrong stride.
- Evicted textures discard the alpha mask. Failed texture creation releases allocated Direct3D resources.
- Periodic texture and control cleanup removes expired entries by index, avoiding repeated linear lookup during bulk eviction.
- SlimDX object tracking is enabled in DEBUG builds only. Existing gameplay timing and online packet formats are unchanged.
- Tests/TextureAlphaTests contains framework-free alpha/hit-test regression cases, including padded-pitch pixels, bit boundaries, full transparency, bounds and invalid buffers.

### Verification required on Windows x64

```sh
dotnet run --project Tests/TextureAlphaTests/TextureAlphaTests.csproj
dotnet build Client/Client.csproj -c Release
```

Test both the client with map assets and any sprites with mask layers. Compare allocations/working set and p50/p95 frame duration before/after at the same resolution, route and number of effects (use a profiler and repeat each scenario). The regressions are committed but not compiled or executed here; GitHub Actions previously failed before running even its first step. No FPS gain or memory reduction has been measured yet.

**Still outstanding:** checkerboard-key pattern detection and runtime shadow cleanup, full post-processing/bloom, smooth day/night, and player factions. Avoid enabling checkerboard removal globally until sample asset pixels demonstrate a safe, low-false-positive rule.

## Ambient-light interpolation (follow-up)

- Adds a deterministic 10-second visual fade between the server's unchanged Day/Dawn/Evening/Night states, with interruption-safe restarts and no new protocol fields.
- Preserves the existing per-map night tint choices (dark grey, LightSlateGray, SkyBlue, Goldenrod).
- On a new map load, initializes to that map's lighting immediately rather than fading from the previous location.
- After a transition reaches pure daylight, avoids the night light render-target pass, unless blindness requires it.
- Tests/AmbientLightTests covers initial light, all map tints, midpoint, completion, interrupted transitions, and map resets.
- Test command: `dotnet run --project Tests/AmbientLightTests/AmbientLightTests.csproj`.
- A 10-second fade is a visual transition, **not** a complete time-of-day simulation or any claimed 48-minute world cycle. This does not implement bloom or checkerboard/shadow cleanup. All GPU changes still require native Windows validation.


## 2026-11-03 master consolidation: DX9 shading and runtime shadows

The client now includes an optional Direct3D9 ps_2_0 post-processing shader in `Client/Data/Shaders/postprocess.ps`. The project copies it into the existing runtime `Data/Shaders` folder. The shader samples the already-rendered map exactly once, applies restrained brightness and radial vignette, and leaves the surrounding UI untouched. Its static constants are reused. The renderer falls back to the unshaded draw if the asset is missing, unsupported, or fails to assemble. This is **not** HDR or multi-pass bloom.

For non-dead player and monster sprites, the active animation frame is reused as a flattened, slanted ground silhouette, recalculated when the map control redraws. The geometry is screen-space sprite projection, **not** a shadow map, environment-aware light direction or occlusion against walls. No texture readback is performed. To bound CPU/GPU state overhead, the pass draws at most 48 dynamic silhouettes per map redraw.

Checkerboard detection is intentionally conservative. A sampled 8x8 patch of alternating opaque/transparent alpha enables a lazily generated half-resolution silhouette mask whose 2x2 alpha averages remove high-frequency holes **in the projected shadow only**. This does not alter sprite artwork or fix checker-patterned RGB pixels, oversized shadow metadata, baked shadows, or foreground tiles. Shadow masks expire after 30 seconds of disuse; main sprite textures keep the existing independent eviction policy.

The graphics configuration in `Mir2Config.ini` has two independent options, both on by default:

```ini
[Graphics]
PostProcessing=True
DynamicShadows=True
```

Set either option to `False` if visual compatibility or performance requires it. For GPU/device compatibility, verify DX9 reset, window resizing, dead-player grayscale, 48+ monsters on screen, animations that change every frame, near-screen-boundary projection, missing shader assets, and a map with substantial checker-dithered sprite artwork.

Regression coverage: `Tests/TextureAlphaTests/Program.cs` now includes 8x8 alternating alpha detection, unchanged source hit testing, smoothed shadow alpha and buffer validation. Run `dotnet run --project Tests/TextureAlphaTests/TextureAlphaTests.csproj` on a .NET 8 development machine and `dotnet build Client/Client.csproj -c Release` on Windows x64 with SlimDX dependencies installed. **Neither the Windows build nor in-game GPU/FPS tests were executed in the assistant environment; GitHub Actions jobs were failing before test-step execution.** Measure p50/p95 frame times, draw calls and process working set with each option disabled/enabled before declaring a performance win.

Future: develop a true light-aware occlusion system (ground occluders/height data), cache-aware shadow batching, HDR/bloom via separately budgeted low-resolution surfaces, external FPS/memory profiling and additional HeroesM2 gameplay parity.


## Master continuation: viewport lighting, direction-aware shadows, and bounded caches

Direct commits to `master` (no new feature branches) extend the DX9 renderer:

- `MapLightViewportCache` indexes only the original `±ViewRange + 24` static map-light area plus an eight-tile guard band. The map cells are decoded when the map changes, the viewport exits the cached region, the view dimensions change, or the light-texture count changes. Each redraw iterates the small emitter list and rechecks the original viewport bounds. It does **not** create a second map-sized array.
- The per-sprite shadow draw first checks the **transformed, slanted projection bounds** against the viewport, preserving shadows that enter the screen from off-camera sprites and skipping entirely offscreen projections before a checkerboard repair mask is created.
- For non-daylight maps, a nearby static map light can steer player/monster projected-shadow direction. The algorithm searches *cached* emitters within 12 tile units and uses a normalized 2D vector away from the closest. The fallback daylight direction remains unchanged. This is **screen-space sprite projection**, not physically correct wall occlusion, height-field shadows, torch/spell occluders, or true shadow-map rendering.
- `ShadowMaskBudget` caps the nominal BGRA payload of lazily generated half-resolution checkerboard shadow textures at **16 MiB**. Failed reservations fall back to source sprite alpha without permanently disabling shadow repair; disposed/expired textures release their reservation. Actual GPU allocation may exceed this nominal count due to driver pitch/alignment.
- `SpriteFileLayout.SkipCompressedLayer` seeks across compressed image payloads while reading optional LIB mask headers instead of allocating a large unused `ReadBytes(Length)` buffer. Secondary mask decoding now uses the second layer's own `MaskWidth`, `MaskHeight` and `MaskLength`, bounded by a 64 MiB uncompressed per-mask safety limit. Test against actual two-layer and tinted legacy sprites before calling this visually compatible.
- Direct3D9 device initialization no longer falls back to the CPU-bound **reference rasterizer** solely because an older GPU lacks shader model 2.0. It chooses the hardware rasterizer, software vertex processing if hardware T&L is unavailable, and excludes the `PureDevice` mode because the renderer queries pixel-shader state.
- When `DebugMode=True`, existing FPS/DPS diagnostics now include projected-shadow draw count (budget 48), shadow-mask logical BGRA MiB and approximate managed heap size. These are diagnostics, **not** a measured performance gain.

### Tests and verification

```sh
dotnet run --project Tests/LightViewportTests/LightViewportTests.csproj
dotnet run --project Tests/RenderingMemoryTests/RenderingMemoryTests.csproj
dotnet run --project Tests/TextureAlphaTests/TextureAlphaTests.csproj
dotnet run --project Tests/RenderingLightTests/RenderingLightTests.csproj
dotnet run --project Tests/AmbientLightTests/AmbientLightTests.csproj
dotnet run --project Tests/MonsterFactionTests/MonsterFactionTests.csproj
```

Windows x64 client build: `dotnet build Client/Client.csproj -c Release` with the project's SlimDX/Windows runtime dependencies. Verify the `postprocess.ps` asset copies to the client's `Data/Shaders` output. The above tests require .NET 8; they do not validate D3D9 GPU behavior.

Compare identical in-game camera routes with `DynamicShadows=False` and then `True`; leave post-processing unchanged for that comparison. Then toggle `PostProcessing` separately. Collect FPS and DPS with debug mode, plus actual process working set in Windows Task Manager. Test at night with several coloured map lights, large/dithered characters, sprite death, map transitions, resize/device reset, and 48+ simultaneous actors. Observe masked/tinted artwork for layer-alignment regressions.

**Verification blocker:** no `dotnet` compiler or Windows D3D9 runtime is present in the assistant execution environment. Current GitHub Actions runs fail before providing executable steps or test logs. All runtime, build, and FPS assertions remain unverified. Do not treat this source commit series as evidence of an actual FPS uplift or pixel-perfect shadows.
