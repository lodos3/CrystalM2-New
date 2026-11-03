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

- Smooth day/night interpolation and screen-content-dependent ambient darkness (the legacy switch remains).
- General HDR/bloom/post-processing pipeline, configurable additive blend modes, checkerboard alpha detection, and GPU shadow cleanup.
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
