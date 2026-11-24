# Live classic armour (experimental)
Use existing generated appearance commands: g01 remains the original GArmour design; g02–g12 use the corresponding installed CArmour/01.Lib–11.Lib plus 00.Lib. No packet or item-stat changes. The original draw order for hair, weapons, mounts and wings stays intact. Alternate animation families remain unsupported as before.

The CPU constructs pose coverage, distinct exposed-skin pixels and a material relief texture on demand. It uses the original frame origin and GArmour pose anchors. The GPU shades the material and its encoded surface normals on each draw. No coloured source armour pixels enter the material texture. The skin/hand segmentation is inferred from 2D artwork and requires visual review; do not treat it as ground-truth anatomy. The fitted silhouette constrains design shape, and changing palettes is not equivalent to authoring a new 3D mesh. Cache limit 192 frame pairs / 32 MiB; device replacement/reload invalidates it. Original libraries are not overwritten.

Export all 11 pairs:
```powershell
dotnet run --project tasks/ArmourCreate/ArmourCreate.csproj -- --live "path/to/Data" "Art/CArmour" "path/to/output"
```
The generated Surface.Lib contains packed shader data, not visible armour artwork. The HeadHands.Lib is an ordinary v2 archive without shadow metadata. Source libraries must contain the classic 1616 frames. Original RGB art is read only for anatomical extraction and coverage. The source data is not included in Git. Use local installed game assets.

Validation: run Tests/ModernRendererTests and Tests/WindowsRendererTests, plus Tests/ArmourAuthoringTests to check old GArmour. Inspect exports through a LIB viewer across both genders and every direction, particularly attacks, raised fists, deaths and mounts; review face and glove occlusion before enabling for production.
