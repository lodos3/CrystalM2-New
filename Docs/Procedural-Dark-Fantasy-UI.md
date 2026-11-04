# CrystalM2 procedural UI

The Vortice client generates its window frames, inset containers, buttons,
scroll tracks, slot frames and common icons from one 256 × 384 BGRA atlas.
The palette uses charcoal surfaces, aged bronze borders and warm ivory text.
The corner ornaments stay fixed when a window changes size. Edges stretch
along one axis, and the centre stretches along both. Small controls reduce
their corner widths proportionally rather than overlapping them.

`MirPanel` supplies the generated frame to 77 dialog and panel classes.
Inventory, character stats, sign-in, the options window and confirmation
dialogs have live text. The HUD draws life and mana orbs, experience and bag weight
from the character’s current values. Inventory frames are submitted before the item
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

## HUD controls

- Drag a spell icon from the Skills page onto either eight-slot player bar.
  A summoned hero has a separate eight-slot bar and uses the hero spellbook.
  Shift-drag to rearrange a locked bar. Unlock it for plain dragging.
- Click **Bind**, hover a slot and press a shortcut. For a conflict, release the
  key and repeat the shortcut to confirm reassignment. Delete clears the
  hovered shortcut and Escape exits binding mode.
- Drag a chat tab out of the strip, or right-click it to detach. Drag a
  detached title onto the main strip, or click its close button, to dock it.
  Drag General to move the main window and drag either window’s lower-right
  grip to resize. The windows share 512 messages and scroll independently.
- Press Enter to type. Choose an outgoing channel at the bottom of General.
  Right-click a player’s message to whisper and click an underlined item name
  to inspect its linked item.

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
glyphs, plus circular orb masking and fill bounds. The Windows compositor suite checks a real rendered corner pixel,
adjacent atlas batching and texture allocation stability. Building the
Windows suite does not run its native D3D11 assertions.

With game assets installed on Windows, check sign-in, equipment and stats
tabs, inventory bag tabs and drag/drop, settings sliders, trade lock/unlock,
modal default/cancel actions, focus cycling, minimap and both belt orientations.
Test chat with live Party, Guild and Whisper messages. Scroll a detached tab
away from the latest message while more arrive, then dock it and restart to
check saved window placement. Drag a spell to a vacant slot and swap two
occupied slots, cast from both a click and a shortcut, and relog to verify
the assignments. Check the orb when a resource is full, partial and empty. Repeat at 1024×768 and 1920×1080 and after Alt+Enter. Compare
the in-game draw and texture counters with full inventory slots; the atlas
budget is deterministic, but an FPS change needs a hardware measurement.
