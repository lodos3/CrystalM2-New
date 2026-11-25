# Pattern-driven live classic armour (experimental)

g01 retains the original GArmour renderer. g02–g12 use same-index donor frames from installed CArmour/01.Lib–11.Lib. No item-stat, packet or server change. The skin extraction, actor placement, original draw order and 192-frame / 32 MiB GPU cache are retained.

The new surface compositor replaces synthetic plate lines with original artwork. Each base shape has a stable four-section recipe: chest, shoulder/sleeve, waist and lower armour. Plate and robe donors are grouped separately. Anatomical anchor distances and leg segments control the blend; original native pixel coordinates, source values and irregular trim are preserved. Donors are clipped to the base coverage, not stretched from an idle frame. Optional missing donor art falls back to the original fitted layer; exposed skin matches are excluded and uncertain uncovered pixels are filled with a neutral undersuit.

The shader retains source colour and shading with restrained palette influence and directional local detail response. The original pixels already contain baked shading; the added directional response is not a recovered physical normal map or a true 3D relighting model.

Export all 11 pairs:
```powershell
dotnet run --project tasks/ArmourCreate/ArmourCreate.csproj -- --live "path/to/Data" "Art/CArmour" "path/to/new-output"
```
Surface.Lib now stores inspectable donor BGRA artwork, not packed normals. Re-export into a new directory; previous exports are not compatible with the new material interpretation. Source libraries are never overwritten or distributed with Git. Both output layers retain pose offsets and omit baked shadows via the existing repair routine.

Validation: run Tests/ModernRendererTests, Tests/WindowsRendererTests and Tests/ArmourAuthoringTests; visually inspect all designs in both genders, eight directions, attacks, movement, death and mounts. 2D pose landmarks and skin extraction remain estimates; the method does not guarantee perfect limb ownership on overlapping sprite pixels.
