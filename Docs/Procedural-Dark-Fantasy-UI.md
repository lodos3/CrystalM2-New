# CrystalM2 procedural UI

The Vortice client generates its window frames, inset containers, buttons,
scroll tracks, slot frames and common icons from one 256 × 256 BGRA atlas.
The palette uses charcoal surfaces, aged bronze borders and warm ivory text.
The corner ornaments stay fixed when a window changes size. Edges stretch
along one axis, and the centre stretches along both. Small controls reduce
their corner widths proportionally rather than overlapping them.

`MirPanel` supplies the generated frame to 77 dialog and panel classes.
Inventory, character stats, sign-in, the options window and confirmation
dialogs have live text. The HUD draws health, mana, experience and bag-weight
gauges from real game values. Inventory frames are submitted before the item
sprites so an empty grid can stay in the shared atlas batch. Buttons have
normal, hover, pressed, disabled and selected appearances. Movable windows
and modals support Tab, Shift+Tab and Enter/Space on their labelled controls;
persistent HUD panels leave gameplay shortcuts available.

Map images, item and skill art, paper dolls, and specialised graphic-only
controls retain their original content. Several older dialog layouts still
read width and height from the existing LIB headers. Reading these headers
does not decode or upload the full-window image. New layouts can set
`AutoSize = false` and an explicit `Size`, as the inventory, settings,
character, sign-in and confirmation windows do.

## Developer entry points

- `Client.Rendering.Core/DarkFantasyAtlas.cs`: palette, materials and glyphs.
- `Client.Rendering.Core/NineSliceLayout.cs`: geometry and small-size rules.
- `Client/Rendering/UiSkinRenderer.cs`: upload once per D3D11 device and submit
  the atlas patches to the normal sprite batch.
- `Client/MirControls/MirPanel.cs`: frame, live captions and keyboard focus.

## Verify

```powershell
dotnet run --project Tests/UiSkinTests/UiSkinTests.csproj -c Release
dotnet run --project Tests/WindowsRendererTests/WindowsRendererTests.csproj -c Release
dotnet publish Client/Client.csproj -c Release -r win-x64 --self-contained false -o artifacts/client
```

The portable suite checks fixed corner dimensions, complete nonoverlapping
coverage at tiny and normal sizes, deterministic atlas output and visible
glyphs. The Windows compositor suite checks a real rendered corner pixel,
adjacent atlas batching and texture allocation stability. Building the
Windows suite does not run its native D3D11 assertions.

With game assets installed on Windows, check sign-in, equipment and stats
tabs, inventory bag tabs and drag/drop, settings sliders, trade lock/unlock,
modal default/cancel actions, focus cycling, chat, minimap and both belt
orientations. Repeat at 1024×768 and 1920×1080 and after Alt+Enter. Compare
the in-game draw and texture counters with full inventory slots; the atlas
budget is deterministic, but an FPS change needs a hardware measurement.
