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


## Visual upgrade 1, 2 and 4 — light bloom, shadow occlusion and atmosphere

### Lighting (1)

`LightBloomPass` supplies an *optional*, shader-driven two-stage bloom path. On dark maps the already composed light texture is sampled to extract bright areas into a single default-pool render target scaled by `BloomDivisor` (default 4). A five-tap cross blur is sampled during upscaling and additively composited on the world, **before the interface**. This is deliberately LDR, not HDR/tonemapping. It runs only when the normal map light pass runs (daylight does not trigger this light pass).

The additional GPU surface is capped at 1,048,576 pixels (~4 MiB nominal BGRA). Shaders are loaded lazily when enabled. Missing shader files, assembly failure or rendering errors disable the optional pass for the current renderer lifetime while leaving standard light rendering intact. The `DXManager.CleanUp` device-reset path disposes the surface and shaders. Both shader assets are copied into `Data/Shaders/` by `Client.csproj`.

### Shadows and occlusion (2)

Player/monster projected shadows already track the active animation frame. Their cast direction now searches nearby static map-light cells **and** up to 48 dynamic moving object/spell lights indexed once per redraw. An optional bounded Bresenham line-of-sight test rejects sources behind tiles carrying the map's own pathfinding-blocking bits (`BackImage & 0x20000000`, `FrontImage & 0x8000`). Rays are capped at 24 cells. An unobstructed nearby light can steer the projected-shadow direction.

**Important limits:** walk-block flags are *not* wall geometry or object heights. These are directional 2D ground silhouettes, not full physical ray-traced occlusion or wall-projected shadows. The occlusion approximation is disabled by default for compatibility until representative maps have been reviewed.

### Atmosphere (4)

`AtmosphereColourGrade` supplies distinct warm dawn, amber evening and blue night tints, preserving the original per-map night ambient colour. `WeatherSetting.Fog`, `Rain`, `Snow` and ember flags influence the tint and add a small screen-space haze. Both day/night tint changes and haze intensity transition over ten seconds with interruption-safe interpolation. The world post-process shader applies colour without colouring text or HUD controls. The existing gameplay weather particles and server lighting rules remain unchanged.

### Graphics settings

The following settings belong under `[Graphics]` in `Mir2Config.ini`:

```ini
PostProcessing=True
EnvironmentAtmosphere=True
LightingBloom=False
BloomDivisor=4
DynamicShadows=True
MapLightOcclusion=False
```

Bloom and static-map occlusion remain **opt-in** until a Windows Direct3D9 game build can be tested; to evaluate them, set `LightingBloom=True` and `MapLightOcclusion=True` separately. `BloomDivisor` is clamped to 2..8; larger divisors allocate smaller buffers. Neither option requires protocol/database changes.

### Validation

Run `dotnet run --project Tests/VisualPipelineTests/VisualPipelineTests.csproj` for tone targets, haze transitions, static-occluder checks and bloom target sizes. In Windows x64, build `Client/Client.csproj` and inspect shader compilation diagnostics in the client's logs. Check dawn, evening, blue/warm night maps, fog/rain/snow maps, animated fire and stationary blue/green lights, screen resizing/reset, moving spells and walls. Repeat with bloom/occlusion independently toggled to detect performance or map-compatibility regressions.

**Not verified:** client build, Direct3D9 shader assembly on target hardware, exact in-game appearances, and FPS/VRAM improvements. Existing GitHub Actions jobs have failed before reporting test steps. These changes are source-level and should not be described as measured visual or performance improvements until tested.


## 2026-11-04 — advanced environment and atmosphere (master)

This extension retains the existing top-down Mir2 tile renderer, original world artwork, legacy particle systems, time-of-day ambient transition, night map tint, and UI. No new server packets or database fields were introduced.

### Environment response

`AtmosphericWeatherController` tracks rainfall-driven wetness, cloud opacity and airborne mist as **separate, smoothly transitioning** values. Abrupt weather changes do not instantly saturate the screen: clouds rise over roughly 16 seconds, mist over 14 seconds, and wetness over 30 seconds. Wetness gradually recedes after rain stops rather than disappearing at the next weather packet. All calculations reuse value types and three existing easing instances, avoiding per-frame heap allocations.

Wind displacement is integrated continuously each draw, with a 250 ms delta limit after pauses so the overlay cannot jump several screens. The field varies slowly using deterministic time functions. Weather transitions reset when a new map is loaded.

The existing weather flags drive the presentation:
- **Rain:** cooler cloud cover, optional persistent wet highlights, drifting mist
- **Snow:** cool pale atmosphere with slow-moving wind and distinct mist palette
- **Fog:** thicker white/blue-gray parallax mist
- **Red embers/fire particles:** ochre ash haze and warm moving cloud shadows
- **Yellow embers, purple foliage and autumn leaves:** subtle matching palette corrections
- **Clear weather:** no procedural cloud overlay and no wet sheen

The old weather particle textures and particle motion remain intact.

### Low-memory atmospheric layers

`AtmosphereFogOverlay` creates a single, *lazily allocated* **128×128 A8R8G8B8** managed texture (64 KiB nominal raw pixels) from `AtmosphereTileNoise`. The tile repeats seamlessly and is reused for **two independent wind-driven layers**: translucent fog and broad cloud shadow. The fog is blended **inside the world render target**, before the existing ambient darkness pass and before UI/nameplates. It does not introduce an additional full-screen framebuffer or a GPU readback.

`AtmosphereOverlayLayout` limits each layer to **48 sprite tile draws**. On exceptionally large resolutions a layer exceeding this bound is skipped. If the optional texture allocation fails, the renderer logs the error, disables that layer, and continues with the existing scene. Device reset/cleanup releases its texture.

### Enhanced screen-space atmosphere shader

The existing post-processing pixel shader remains a single world-texture fetch with constant-register input and now accepts:

- A vertical **distance-haze approximation** (stronger near the upper part of the top-down screen)
- Weather and time dependent tinting
- Smooth overcast **exposure dimming**, bounded to a maximum reduction of 0.12 brightness units
- **Optional** rain-wet highlight enhancement, persisting after showers
- **Optional** low-intensity lightning tint on rain-enabled maps that explicitly allow lightning

The deterministic lightning flash peaks at **0.12 intensity**, lasts at most **150 ms**, and repeats no faster than **35 seconds** per map seed. It is **disabled by default**, including for photosensitivity considerations.

These are *screen-space approximations*. They do **not** create geometric sky/cloud volumes, surface normals, puddle meshes, true specular reflections, light shafts through 3D fog, or physically correct shadows on buildings.

### Weather memory and FPS hygiene

`MapControl.UpdateWeather()` now disposes **every** previous weather particle engine (including index zero, which the legacy loop skipped) before clearing the list. `ParticleEngine.Process()` enforces a configurable **per-engine particle cap** (default 256, allowed 64–1024) for continuously emitted weather effects; pre-existing initial weather grids remain intact. This addresses potential weather-transition retention and uncontrolled emitter growth without suppressing the original weather artwork.

The existing debug FPS display also reports current cloud, mist and wetness state for side-by-side checks.

### Configuration

The following example enables high-detail atmospheric effects. They remain **opt-in** by default until the Windows DX9 client has been tested.

```ini
[Graphics]
PostProcessing=True
EnvironmentAtmosphere=True
EnvironmentFogLayers=True
AtmosphereWetSurfaces=True
AtmosphereLightning=False
AtmosphereIntensityPercent=75
WeatherParticleCap=256
LightingBloom=True
BloomDivisor=4
```

Set `EnvironmentFogLayers=False` to eliminate the extra 1–2 procedural overlays. Set `AtmosphereWetSurfaces=False` to eliminate wet highlight response. Set `AtmosphereIntensityPercent=0` to remove all newly introduced shader/overlay grading without changing existing precipitation particles. `LightingBloom` is an independently controlled older feature.

### Validation path / limitations

Run the pure regression project:

```sh
dotnet run --project Tests/VisualPipelineTests/VisualPipelineTests.csproj
```

It now covers weather persistence, interruption, wind drift, ash/foliage colour, tileable procedural noise and padded buffer safety, tile budgets, particle caps, lightning opt-in and overcast exposure. Also run the existing texture, map lighting, day-night and faction test projects.

Then build and run `Client/Client.csproj` on a Windows x64 machine with the repository's SlimDX dependencies. Confirm `Data/Shaders/postprocess.ps` loads, compare clear/rain/snow/fog/ash maps at dawn/day/evening/night, move the camera and resize window, inspect actual shadows and sprite alpha edges, test weather transitions at different frame rates, and check repeated map transitions for leaked particle resources. Measure frame times, draw calls, working-set, and GPU memory with each effect enabled/disabled independently.

**Status:** source changes are committed, but not compiled or visually executed on Windows here. GitHub Actions is currently reporting jobs that terminate without runner steps; do not treat those results as passing CI or measured FPS/VRAM changes.
