# Opt-in Mir 2 / Mir 3 environmental material sidecars

The original `.map` and `.Lib` data are unchanged. Map files expose layered
art and collision flags, **not a reliable surface-material database**. Do not
infer a wall, reflective metal, water or rock height from a sprite colour.

This is a NEW versioned authoring format introduced in CrystalM2-New, not an
existing Mir2/Mir3 file format and not a conversion of HeroesM2 map metadata.

## File placement

For a map loaded from `Client/Map/<name>.map` (the configured
`Settings.MapPath`, which may differ), put an optional JSON file beside it:

`<name>.map.materials.json`

A map without this sidecar behaves exactly as before. Sidecars outside the
configured map directory are ignored. Map loads tolerate missing/invalid
sidecars and log validation errors without preventing gameplay.

## Example only — NOT actual Mir map classification

These coordinates and material assignments are **illustrative placeholders**.
An artist must inspect each specific map before supplying real regions.

```json
{
  "version": 1,
  "regions": [
    {
      "x": 10,
      "y": 20,
      "width": 6,
      "height": 4,
      "material": "Stone",
      "wetResponse": 0.7,
      "blocksLight": false
    },
    {
      "x": 20,
      "y": 20,
      "width": 1,
      "height": 5,
      "material": "Wood",
      "blocksLight": true
    }
  ]
}
```

The rectangle coordinates use the same map-cell grid as map rendering,
with inclusive top/left and exclusive right/bottom. `material` must be one of
`Stone`, `Wood`, `Soil`, `Metal`, `Water`, `Foliage`.

Fields and precedence:

- `wetResponse`: optional float between 0 and 1, defaults to 0. This
  activates a **subtle procedural sheen**, not actual planar water reflections,
  only during accumulated rainfall and only if the client setting is enabled.
- `blocksLight`: optional boolean. If provided, overrides the legacy
  impassability-based *visual light-occlusion test* only. It **does not change
  player collision, server walkability or monster pathfinding**. If omitted,
  existing map blocking flags remain the fallback.
- Later rectangles override earlier ones on overlapping cells. This allows
  an artist to annotate a large area and explicitly correct a smaller one.
- Unknown materials, malformed JSON, out-of-bounds regions and unsupported
  versions cause the entire sidecar to be ignored. Limits: 128 regions and
  512 KiB per file. No map-sized material array is constructed.

## Runtime settings

```ini
[Graphics]
EnvironmentAtmosphere=True
AtmosphereWetSurfaces=True
MapLightOcclusion=True
```

All three settings remain independent. `AtmosphereWetSurfaces` and
`MapLightOcclusion` are opt-in because there has been no verified on-device
Direct3D9 screenshot comparison. The weather system maintains wetness briefly
after rain stops, while the material pass draws at most 96 decals per frame.
The only new GPU mask is a tiny 48 × 32 BGRA texture (6 KiB nominal), reused
across cells and released by the Direct3D reset cleanup.

## Design limitations / next stages

This is a **foundation**, not an inferred normal-map or PBR system.
Tagged metal, soil and water do not yet gain bespoke shading unless a
`wetResponse` has been explicitly authored. Actual wall heights and
directional surface normals would require additional, artist-authored data.
An annotation/export tool plus renderer image baselines should precede
automatic classification or lighting changes across entire maps.

## Validation

1. Run `dotnet run --project Tests/VisualPipelineTests/VisualPipelineTests.csproj`.
2. On Windows x64, build `Client/Client.csproj` and launch a known map.
3. Confirm maps without sidecars render identically before/after.
4. Add an explicitly reviewed sidecar, test all four edges, overlapping
   regions, camera movement, map changes, reset and rain stopping.
5. Compare annotated stone/wall areas with `AtmosphereWetSurfaces` and
   `MapLightOcclusion` independently toggled. Check that pathfinding,
   sprite stacking and nameplates do not change. Collect screenshot/FPS/VRAM
   baselines before enabling these features by default.

The current GitHub Actions jobs have historically failed before running
individual steps; source commits are not proof of a successful build or
validated visual output.
