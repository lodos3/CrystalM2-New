# CrystalM2 — one Vortice Direct3D 11 client

`Client/Client.csproj` is the only playable client and uses Vortice.Direct3D11
and Vortice.D3DCompiler 3.8.3 on .NET 8, Windows x64. The isolated preview,
SlimDX binaries/reference, Direct3D9 device implementation and shader assembly
files have been removed. There is no renderer selector or fallback client.

## Rendering contract

- Original Mir 2/Mir 3 LIB BGRA bytes, independent mask layers, X/Y offsets,
  transparency and painter's order are preserved during upload and drawing.
- Original artwork uses point-clamped BGRA8 UNORM sampling, without automatic
  normal maps, sRGB conversion, inferred materials or per-sprite relighting.
- Intermediate map/UI textures accumulate premultiplied RGB and correct alpha.
  When sampled they are converted back to straight alpha in the shader, so
  cached transparent edges are not multiplied twice.
- Only adjacent compatible sprites share a draw. A fixed dynamic vertex buffer
  holds at most 2,048 quads per upload. Global texture sorting is prohibited.
- Existing map, actors, foreground, weather, ambient lighting and UI retain
  their actual call order. Atmospheric post-processing applies to the world
  texture before the UI.
- Texture upload memory is allocated only while decoding/writing and freed on
  upload completion or disposal. There is no second full-size CPU pixel cache.
- Resize clears context references before releasing backbuffer/RTV resources.
  Device removal invalidates cached textures and recreates the same D3D11
  backend. The production constructor explicitly requests hardware rendering.
- Screenshots use a temporary staging texture and its real row pitch. Ordinary
  frames never read back GPU pixels.

## Baked shadows and checker masks

Projected shadows are disabled by default. Enabling `DynamicShadows=True`
alone does not authorise another shadow on already shaded artwork. List the
exact libraries that an artist has verified contain no baked ground shadow:

```ini
[Graphics]
DynamicShadows=True
ProjectedShadowLibraries=Data/Human/000000.Lib;Data/Monster/01.Lib
```

Those paths are examples of the configuration syntax, not a claim about which
libraries in a particular asset installation are safe. Use the actual path
relative to the client working directory. Matching ignores case and slash
style; similarly named libraries in other folders do not inherit permission.

For explicitly permitted sources, checker-pattern alpha detection and the
16 MiB repaired-shadow mask budget are retained. Repair affects the separate
half-resolution projected mask, never the original artwork or hit-test alpha.
Current animation frames and light-responsive projection continue to be used.

## Build and checks

```powershell
dotnet build Client/Client.csproj -c Release
dotnet run --project Tests/ModernRendererTests/ModernRendererTests.csproj
dotnet run --project Tests/WindowsRendererTests/WindowsRendererTests.csproj -c Release
```

The Windows tests compile the production renderer sources and HLSL, create a
real Microsoft WARP D3D11 device and check alpha through a cached target,
layer order, adjacent batching, drawing after Present, upload disposal and
resize. WARP is explicitly selected in that test assembly; the game does not
silently fall back to it. Tiny known pixel values are test inputs, not game
assets or a substitute for visual validation with actual libraries.

`Client.exe --validate-shaders` compiles all production shader entry points
without starting the patcher or connecting to a game server.

## Validation limits and remaining acceptance work

The game client and Windows renderer test project compiled successfully for
the Windows target with the .NET SDK from Linux. All eight pure rendering and
faction regression suites passed. The ambient test was corrected to compare
ARGB values rather than named-colour identity. Existing WebView2/WindowsBase and source-analysis warnings
remain. The pure geometry/asset-policy tests run without Direct3D. Windows
shader/compositor tests are included in CI but must actually execute before
claiming GPU correctness. No in-game FPS gain, GPU memory reduction or exact
pixel parity with the removed renderer is claimed without hardware results.

The former normal/grayscale/magic assembly files were supplied externally and
are absent from this repository. The HLSL implements the current tint and
blend parameters, but parity with customised external shader files is
unverified. Compare death rendering, magic blending and character tints.

On a Windows workstation with representative real Mir 2/Mir 3 data, check
login/selection/gameplay, animated masks, foreground occlusion, UI text and
borders, weather transitions, optional bloom/occlusion, screenshots, Alt+Enter,
resolution changes and repeated map changes. Compare identical cameras and
record frame-time percentiles, draw calls, process working set and actual GPU
memory. Debug mode exposes real submitted D3D11 draw calls and logical texture
bytes; logical texture bytes are not an actual VRAM allocation measurement.

## Primary references

- [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows)
- [Microsoft flip-model guidance](https://learn.microsoft.com/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model)
- [ResizeBuffers reference lifetime requirements](https://learn.microsoft.com/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-resizebuffers)
