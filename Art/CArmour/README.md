# CArmour authoring masks

Use the character's `DrawFrame + ArmourOffSet` as the index in [poses.json](poses.json). The standard CArmour male bank starts at 0 and the female bank at 808. Each entry supplies a mask sheet number, a source rectangle `[x,y,width,height]`, a cell-relative draw origin `[x,y]`, and attachment points in the order given by `regions`.

Draw the cropped mask at `DrawLocation + origin`. To paint a plate or pin a texture inside that crop, subtract `origin` from the attachment point. To draw an attachment directly in the scene, add its point to `DrawLocation`. Draw the base body first and preserve the client's existing hair, weapon and mount draw order.

Read the mask PNGs as linear data, using these channels independently.

| Channel | Coverage |
|---|---|
| Red | The naked body's alpha after stripping the baked checker shadow. |
| Green | Visible pixels of fitted armour 01 that differ from the aligned base. This also includes sleeve and boot pixels outside the bare silhouette. |
| Blue | Average changed-pixel coverage across cape 03, plate 06 and robe 12. On opaque pixels, about 85 means one example covers the pixel, 170 means two and 255 means all three. Select lower values too when building flared pieces. |

The head and pelvis use the matching CHair/00 silhouette and the base body's coloured garment as landmarks. Other points are estimated from the torso axis and snapped onto visible body pixels. A and B refer to opposite sides of that projected axis. On an upright figure A is screen-left. Overlapping legs, foreshortened limbs and fallen poses can need hand adjustment. Edit a frame's anchor coordinates in the JSON after baking and keep those edits before regenerating.

[Overlay preview](overlay-preview.png) shows a teal test cuirass clipped to the fitted mask across six poses. Cyan marks the estimated head, shoulders and boots. Orange marks the pelvis.

Regenerate from the repository root with `dotnet run --project tasks/ArmourPoseBake -c Release -- Build/Client/Release/Data Art/CArmour`.


## Original black / white / blue armour

`tasks/ArmourCreate` produces a **new** CArmour set, not a recolour of `01.Lib`. The shape is drawn around the existing 11 landmarks for each pose: jointed black enamel torso and greaves, white segmented chest plates, blue central inlays, and restrained gold collar/belt/shoulder details. The original naked frames remain underneath so uncovered skin stays consistent with animations and directional lighting.

The mask atlas limits generated plate coverage using the naked-body alpha (R), fitted silhouette (G) and loose examples (B). Existing armour samples define **where pieces may extend**, not the new art's colour or textures. The masks cannot infer actual depth or correctly resolve every overlapping limb, so dead/fallen and heavily foreshortened poses may need manual anchor corrections.

From the repository root on Windows, with the game's original `Data/CArmour/00.Lib` available, choose a **new unused two-digit number** (for example `NN`, not necessarily `13`):

```powershell
dotnet run --project Tests/ArmourAuthoringTests -c Release -- Art/CArmour
dotnet run --project tasks/ArmourCreate -c Release -- "Build/Client/Release/Data" "Art/CArmour" "Build/Client/Release/Data/CArmour/NN.Lib" "Art/CArmour/NN-preview.png"
```

Replace `NN` with an actual unused CArmour slot. The generator refuses to overwrite an existing library or preview, writes to a temporary file before publishing, and reopens the result through `LegacyMirLibReader`. It writes the same v3 GZip-BGRA layout the current `MLibrary` supports, with the naked source's masks and shadow metadata preserved. The preview contains north-facing, walking, combat and female samples. Restart the client so `Libraries.CArmours` enumerates the new numbered file.

**Using it in-game:** Adding a `.Lib` alone does not equip or register an item. Set up an armour item in the existing server item database with its `Shape` referring to the new CArmour index, using the normal item-editor workflow; do not edit account or world database binaries to force a shape. A client's existing armour shape must also be within its loaded CArmour library array.

**Scope:** This generator targets the standard `CArmour` character bank (warrior/wizard/taoist and compatible shared poses). Assassin `AArmour` and archer alternate `ARArmour` use different frame layouts and need separately baked anchor atlases. It does not replace the live renderer, item schema, or original artwork.

The generated library and preview are local build outputs, not committed binary assets. Validate the appearance in-game by walking/attacking in eight directions on both genders before using the set for live players.
