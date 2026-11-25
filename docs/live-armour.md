# Original-frame multistage armour recolour

g01 remains the original GArmour. g02–g99 use a single installed CArmour/01.Lib–11.Lib source per design. Source identity is unchanged: source=(id-2)%11+1. Each successive group of eleven selects another palette (sapphire, crimson, obsidian, ivory, emerald, frost, amethyst, copper, teal). The final group has ten IDs. Installed custom GArmour/13.Lib–99.Lib still take precedence.

The source frame supplies all geometry, detail, offsets and alpha. Shadow removal uses the existing checker-shadow detector. The naked and sleeveless references are aligned only within the dressed frame. Existing head/hands isolation is used as a conservative protection mask; restored pixels are copied from the original dressed sprite, not pasted from another armour. Uncertain exposed skin can still be missed; material and skin classifications are estimates from flattened pixels.

Independent soft masks derived from original RGB control primary metal, secondary colour, warm trim and dark cloth. A monotone tone curve and gamut-limited chroma mapping retain the source shading. Exact black and white are untouched. The generated layer and restored skin have disjoint coverage. No donor compositing or procedural lines are used by the live path. The previous compositor is retained as unused legacy code for comparison.

Recolouring runs only on cache misses. Existing 192-frame/32 MiB cache and standard artwork shader, scene lighting, greyscale and tint are retained. No additional shader recolouring is applied, preventing double tinting. No protocol, equipment, or server changes.

Export one design:
```powershell
dotnet run --project tasks/ArmourCreate/ArmourCreate.csproj -- --live-one "path/to/Data" "Art/CArmour" "path/to/new-output" 13
```
Use --live to export all 98 pairs. Output filenames gNN_HeadHands.Lib and gNN_Surface.Lib are unchanged; use a new directory. Draw the two complementary layers at their original offset. Source archives are never overwritten.

Validation: Tests/ModernRendererTests includes exact alpha conservation, skin protection, deterministic palette mapping, tone order and gamut tests. Build and inspect the client in all directions and animations; verify the appearance of warm cloth versus metal, skin, and original shadow handling. The chosen palettes are initial artistic tuning, not physically measured materials.

## Local build via GitHub Desktop

Select `CrystalM2-New` and `master`, then Fetch origin and Pull origin when offered. The History tab should show the original-frame recolour implementation and monotonic tone correction. Rebuild the client using the existing local Release configuration. No server changes are needed for these palettes.

