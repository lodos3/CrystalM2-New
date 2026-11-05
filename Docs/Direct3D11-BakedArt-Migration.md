# CrystalM2 — one Vortice Direct3D 11 client

`Client/Client.csproj` is the only playable client and uses Vortice.Direct3D11
and Vortice.D3DCompiler 3.8.3 on .NET 8, Windows 10/11 x64. The isolated preview,
SlimDX binaries/reference, Direct3D9 device implementation and shader assembly
files have been removed. There is no renderer selector or fallback client.

## Rendering contract

- Mir 2/Mir 3 LIB files stay intact on disk. World sprite uploads smooth
  recognised transparent checker shadows while preserving the original layer
  offsets, hit-test coverage and draw order.
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

Map, character and monster textures automatically rebuild dense near-black
checker regions over transparency as soft translucent shadows at their authored
positions. This runs when a sprite is uploaded, including its mask layer. Dark
patterns inside opaque artwork require source art or an authored shadow mask to
identify the shadow pixels reliably.

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

Recognised baked checker shadows suppress the extra projection for that frame.
For permitted projection sources, the separate half-resolution masks use linear
filtering and share a 16 MiB budget.

## Environmental and light effects

The HLSL world pass carries forward atmosphere colour transitions, haze,
weather exposure, authored wet-surface overlays and optional lightning. It is
composited before the HUD. No surface normals or materials are inferred from
sprite colour.

Spell sprites and light-marked or additive map animations use the frame drawn
that tick to emit coloured light. Static map lights and torches keep radial
glows. Ambient shade stays separate from emission, and a small additive pass
keeps emission visible during daylight. **Settings → Graphics** exposes ambient
shade, spell light, map light and additive spell blending.

`LightingBloom=True` enables emitted-light extraction followed by horizontal
and vertical Gaussian blur. The two reduced-resolution targets follow
`BloomDivisor` (default 4, clamped 2–8); at 1920×1080 and divisor 4 their combined
BGRA payload is 1,036,800 bytes, excluding driver overhead. Bloom never samples
the baked map artwork. Fog, wet surfaces, lightning and bloom remain optional
until native visual checks have run on actual asset installations.

## Build and checks

```powershell
dotnet build Client/Client.csproj -c Release
dotnet publish Client/Client.csproj -c Release -r win-x64 --self-contained false -o artifacts/client
dotnet run --project Tests/ModernRendererTests/ModernRendererTests.csproj
dotnet run --project Tests/WindowsRendererTests/WindowsRendererTests.csproj -c Release
```

The Windows tests compile the production renderer sources and HLSL, create a
real Microsoft WARP D3D11 device and check alpha through a cached target,
layer order, adjacent batching, drawing after Present, upload disposal and
resize, plus ambient/bloom separation, Gaussian intensity and repaired mask
filtering. WARP is explicitly selected in that test assembly; the game does not
silently fall back to it. Tiny known pixel values are test inputs, not game
assets or a substitute for visual validation with actual libraries.

`Client.exe --validate-shaders` compiles all production shader entry points
without starting the patcher or connecting to a game server.

## Validation limits and remaining acceptance work

The game client and Windows renderer test project compiled successfully for
the Windows target with the .NET SDK from Linux. Windows x64 publish also
succeeded, producing one `Client.exe`, Vortice dependencies and the exact
current HLSL; no retired renderer files or dependency entries were present.
All eight pure rendering and
faction regression suites passed. The ambient test was corrected to compare
ARGB values rather than named-colour identity. Existing WebView2/WindowsBase and source-analysis warnings
remain. The pure geometry/asset-policy tests run without Direct3D. Windows
shader/compositor tests are included in CI but must actually execute before
claiming GPU correctness. No in-game FPS gain, GPU memory reduction or exact
pixel parity with the removed renderer is claimed without hardware results.

The migration's GitHub Actions run failed before either job had a runner or
any executed steps. Its status is not a native shader-test result. Run the
Windows command above locally until CI can start jobs. Build/publish also
removes known retired SlimDX and assembly-shader files from reused outputs.

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
