# CrystalM2 Direct3D11 renderer migration — phase 1

Status: **isolated source-level preview**, not the production game renderer.
The existing SlimDX/Direct3D9 client remains the default and is unchanged by
the new projects. No server, network protocol, database, map format, or existing
gameplay rendering switch has been changed.

## Why preserve baked art?

Mir 2 and Mir 3 sprites contain much of the original artist-authored light,
ambient shading, diffuse detail, highlights and ground shadows. A naive
normal-map/PBR pass that relights every RGB pixel will introduce double
shading, washed-out scenes, incorrect highlights and darkness.

For that reason this migration imposes these rules:

1. **Opaque source pixels are preserved**: the `.Lib` decoder reads original
   gzip-compressed, tightly packed BGRA bytes without colour correction,
   alpha-premultiplication, RGB dithering, sharpening or texture repacking.
2. **Straight alpha compositing**: the D3D11 preview uses BGRA8 UNORM,
   point-clamped sampling, and non-premultiplied SourceAlpha/InverseSourceAlpha
   blending. No implicit sRGB conversion or normal-map shader is used.
3. **Sprite geometry** retains the original LIB X/Y offsets. Optional authored
   mask layers remain separate; they are not merged into baked RGB.
4. **Do not generate replacement shadows** for every sprite: an existing
   texture shadow must not be darkened again. Additional contact shadows and
   casts require an opt-in authored source flag and visual verification.
5. **Additional lighting is additive and separate**, applied to an appropriate
   explicit light/effect texture, not by tinting the original baked sprite.
6. **Layer order is sacred**: batches combine only adjacent compatible
   commands. Global texture sorting is prohibited because it changes
   map->character->foreground transparency order.
7. Environment metadata is opt-in. Image colour is not sufficient to infer
   physically meaningful materials, building heights or wall normals.

This is an art-preservation policy, NOT a claim that current Direct3D9 pixel
output is bitwise identical. GPU blending and filtering need Windows captures
from both renderers for an objective comparison.

## New projects

| Path | Purpose |
|---|---|
| `Client.Rendering.Core/` | Cross-platform .NET 8 baked-art policy, adjacent-only command batching plan, original `.Lib` v2/v3 frame reader |
| `Client.Rendering.D3D11Preview/` | Independent Windows Forms Direct3D11 hardware preview using Vortice.NET and Shader Model 4 HLSL |
| `Tests/ModernRendererTests/` | Pure logic regressions for exact BGRA, alpha, offsets, gzip decoding and layer-safe batches |

The preview uses **Vortice.Direct3D11 3.8.3** and
**Vortice.D3DCompiler 3.8.3** (both official NuGet packages compatible with
.NET 8). It requests a real hardware Direct3D11 device and does not silently
fall back to WARP/CPU rendering.

## Run on Windows x64

From the repository root, with the .NET 8 SDK and Direct3D11-capable GPU:

```powershell
dotnet run --project Tests/ModernRendererTests/ModernRendererTests.csproj
dotnet build Client.Rendering.D3D11Preview/Client.Rendering.D3D11Preview.csproj -c Release
dotnet run --project Client.Rendering.D3D11Preview/Client.Rendering.D3D11Preview.csproj -- "C:\path\to\your\Magic.Lib" 0
```

The window provides an **Open .Lib** action and a frame picker. Select a real
local client `.Lib` asset. The application does **not** download game assets,
copy them into the repository or write to the LIB file.

The preview deliberately draws only the selected real sprite frame against
a neutral background. It is not a full map renderer. Its optional second
artistic mask is parsed by the core decoder but not composited by this first
preview because tint/blend rules must be verified against original art first.

**Important:** the regression suite generates a clearly labeled test-only
binary fixture corresponding to the archive layout confirmed in CrystalM2's
legacy `MLibrary` reader. The fixture is not a substitute for checking
representative actual Mir 2/Mir 3 libraries.

## Next migration gates (do not flip production yet)

1. Compile the new preview and core tests on Windows. Fix actual Vortice API,
   HLSL compilation, and reset errors before expanding the shader pipeline.
2. Run source-identical real assets from Mir 2 and Mir 3 libraries.
   Capture the legacy D3D9 and preview D3D11 versions at identical pixel
   coordinates and composite settings. Check both opaque pixels and
   transparent/antialiased sprite edges.
3. Implement a map layer command collector that matches **the repository's
   actual** back, middle, front, animated, player, monster and spell order.
4. Add bounded texture caching, dynamic batched quad uploads and device-lost
   recovery. Measure latency, draw calls, managed/unmanaged memory and FPS.
5. Add *selective*, artist-authorized lighting/material channels (normal or
   emissive maps only where explicitly supplied), plus separate spell glow,
   rain reflections and occluder-aware shadows. Never globally relight
   legacy sprites with already baked directional highlights.
6. Only after a full baseline and in-game regression suite should a
   `GraphicsBackend` selector be introduced in the production client.

## Compatibility and verification caveat

At the time of writing, GitHub Actions for this repository has failed before
recording any executed steps, even for pure tests; the added Windows build
step may also fail without useful build logs. Therefore **no successful
Windows compile, on-device shader assembly, visual parity or FPS gain is
claimed by this source commit**. On a Windows workstation, run the commands
above and preserve the compiler output and comparison screenshots.
