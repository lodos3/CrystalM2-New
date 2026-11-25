# Live classic armour

Use the GM command `@arm PlayerName g02` to wear the original plate set and `@arm PlayerName off` to restore equipped armour. The live slots g02–g12 use one original armour family with different palettes, shoulder widths and split-skirt lengths. The separate g01 generator keeps its existing appearance and file format.

The live renderer uses CArmour/00.Lib for the naked animation poses and exposed skin. The nine painted parts in `Art/CArmour/OriginalPlate.png` supply the front, back and side cuirass, pauldron, bracer, belt, coat-tail, greave and boot. Each frame fits them to the torso and limb anchors. Direction selects the torso view and overlap order. The original cutouts can extend beyond the naked outline at the shoulders and waist. Compositing uses filtered transparent edges at the native sprite resolution.

The shader applies the selected palette and lights each part using its painted value and relief. Cached poses retain the existing limit of 192 frame pairs or 32 MiB. The client releases these resources on device replacement or reload. Live generation uses the classic 1,616-frame male/female bank. Limb tracking, especially hand positions in overlapping or fallen poses, is estimated from two-dimensional sprites.

[Native-size and enlarged g02 renders](../Art/CArmour/original-live-preview.png)

Export the live skin and material libraries with:

```powershell
dotnet run --project tasks/ArmourCreate -c Release -- --live "path/to/Data" "Art/CArmour" "path/to/new/output"
```

The exporter keeps the existing numbered names and verifies the resulting v2 archives. The surface stores normal X/Y in R/G, the material index and painted value in B, and silhouette coverage in A. The skin library contains the exposed head and hands.

To inspect the actual renderer at native size and enlarged, using locally installed game data:

```powershell
dotnet run --project Tests/WindowsRendererTests -c Release -- --original-armour "path/to/Data" "Art/CArmour" "original-armour.png"
```
