# Multiscale donor armour synthesis (experimental)

g01 remains original GArmour. g02–g99 resolve 98 deterministic recipes using the 11 existing classic CArmour libraries. Installed custom GArmour/13.Lib–99.Lib files retain priority. No packet format, item-stat or server changes. The design identity is fixed by appearance ID, not frame or time.

The donor families are based on the supplied idle sprites: plate 01,02,04,05,06; robes 03,07,08,09,10,11. These are coarse style classifications, not manually segmented material labels. Each recipe uses four distinct donors from its family for chest, shoulders/sleeves, waist and lower armour. All donors are sampled at the corresponding original animation index and aligned to the cell origin.

The CPU decomposes each aligned donor into broad tone, mid-scale relief and fine detail using alpha/skin-aware spatial filtering. At anatomical seams only the broad band is mixed; a single donor supplies both finer bands. Original target contours and coverage are retained. Missing optional donor frames fall back to the fitted source. The head/hands extraction remains heuristic. The GPU separately estimates metal, cloth and warm ornament likelihood using luminance/chroma; four material profiles control palette influence and local directional response. This is not a physically recovered material map, nor a 3D skeleton. No arbitrary line patterns are generated.

The existing lazy 192-frame / 32 MiB GPU cache is retained. CPU synthesis costs more on cache misses; runtime performance must be profiled on the target client. Original files are read only. Exported surface RGB is inspectable in an ordinary LIB viewer.

Export one design:
```powershell
dotnet run --project tasks/ArmourCreate/ArmourCreate.csproj -- --live-one "path/to/Data" "Art/CArmour" "path/to/new-output" 13
```
Use --live with the same three paths to export all 98 pairs. Files are named gNN_HeadHands.Lib and gNN_Surface.Lib. Export is optional and does not override runtime rendering.

Validation: run Tests/ModernRendererTests, Tests/WindowsRendererTests and Tests/ArmourAuthoringTests, build client/server Release and inspect all animation banks and directions in-game. Tests include design uniqueness, family compatibility, alpha conservation and preservation of high-contrast native detail. There is no guarantee of perfect material classification or limb occlusion from flattened 2D pixels.
