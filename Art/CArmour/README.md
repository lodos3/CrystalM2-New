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


## Generated armour overlays: `g01` – `g99`

The authoring tool creates a **separate transparent GArmour layer**. The client draws the unchanged CArmour/00 naked body, paints the generated armour above it, then draws the normal hair, weapons and wings in the established order. The new armour is original procedural artwork (black enamel, white plates, blue inlays and gold trim). Existing armour libraries are reference silhouettes only: their pixels and material colours are not reused.

The server's `@arm` command is a **GM-only, session-only visual preview**. It does not equip armour or change AC, MAC, DC, durability, item shapes, database records or any combat properties. The player-spawn and player-update packets carry a reserved negative appearance code (-1 through -99) to signal the generated visual; packet length is unchanged. Existing positive armour IDs continue to use the original graphics and item system.

### Build and install generated `g01`

Run in the repository root on **Windows**, with the existing client Data assets present:

```powershell
dotnet run --project Tests/GeneratedArmourVisualTests -c Release
dotnet run --project Tests/ArmourAuthoringTests -c Release -- Art/CArmour
dotnet run --project tasks/ArmourCreate -c Release -- "Build/Client/Release/Data" "Art/CArmour" "Build/Client/Release/Data/GArmour/01.Lib" "Art/CArmour/g01-preview.png"
dotnet build Client/Client.csproj -c Release
dotnet build Server.MirForms/Server.csproj -c Release
```

The `GArmour` directory is created automatically. `01.Lib` maps to `g01`, `02.Lib` to `g02`, and so on through `99.Lib`. Existing numbers in `CArmour` are unaffected. The generator refuses to overwrite an existing generated armour file or preview, writes via a temporary file and verifies sample frames with the same LIB decoder used by production. It also checks the generated frames are **transparent overlays**, not duplicate bodies or inherited baked shadows.

**Restart the new client** after adding an asset so `Libraries.GArmours` discovers the newly added bank. Supply the same `GArmour/NN.Lib` asset to every player's client if others should see it; the server does not transfer these libraries over the network.

In-game, using a GM account:

```text
@arm PlayerName g01
@arm PlayerName g02
@arm PlayerName off
```

The appearance changes immediately for the target player and nearby players. A new viewer receives it from the player-spawn packet. `off` returns to the appearance provided by the character's equipped armour. The override also ends on disconnect; it is deliberately not stored in the character database.

### Constraints

- Generated `gNN` looks are visual-only previews, not traditional item `Shape` IDs. Do **not** add them to normal item tables or overwrite any `CArmour/*.Lib` file.
- Character models that use classic CArmour frame indices are supported. The special alternate animation banks for Assassin and Archer have different frame numbering. Those frames fall back to their existing naked base until separate AArmour/ARArmour mask atlases are authored. Transformed characters do not use the overlay.
- The 11 anatomical anchors and RGB coverage maps are estimates, not rigged 3D bone meshes. Fallen and severely foreshortened poses may require hand-edited anchor positions.
- Source `CArmour/00.Lib` is required **locally** for generation and is not in the GitHub repository. The generated `.Lib` and preview are local build outputs. Preview, build and live appearance must be checked on both genders, all eight directions, movement, attacks and death poses before release.


## Runtime authoring

The client build now copies the existing pose atlas and RGB masks into `Data/ArmourAuthoring`. For g01, the client paints individual poses on demand into an in-memory library; no GArmour file is needed. Each pose is compressed once and cached, and ordinary GPU texture eviction remains active. `@arm PlayerName g01 build` exports all poses into `Data/GArmour/01.Lib` on the issuing GM's client, using the same cached frame records as the preview. It refuses to overwrite an existing library. `@arm PlayerName g01 reload` drops the issuing client's preview cache and reloads its local pose atlas/masks. Source-code changes to ArmourPainter still require recompilation. Other numbered looks require their own authored designs or prebuilt LIBs. Keep both client and server on the same version.

### g01 silhouette-fitting revision

The original sparse attachment painter has been replaced with native-resolution material synthesis. The fitted green mask supplies visible garment geometry, and bare-body red coverage below the collar fills the connected undersuit. Separate visible runs receive their own bevels; the chest, back and side panels use the classic frame bank's facing direction. The new design includes pauldrons, sleeves, waist, hip plates, knees, greaves and boots. No original material colours are copied, and the overlay remains transparent outside its permitted geometry. Direction and limb fitting are approximate for unusual overlapping or fallen poses; visual review remains necessary. The generated library and the runtime preview use the same painter.

### Head protection and plate relief

The head is not an armour material. A conservative head-to-neck collar guard plus a radial head exclusion takes precedence over both body and fitted coverage. It also masks out false positives from the fitted RGB difference map. No implicit helmet is generated. The renderer builds a separate relief surface per pose and derives normals from adjacent heights, adding directional diffuse lighting, metallic highlights, bevels and recessed flexible joints. This is a 2D relief approximation, not an actual rigged 3D mesh; unusual head/limb overlaps may need manual anchor correction. Both the runtime and LIB exporter share this code. Regression checks assert the protected area stays transparent and the remaining fitted coverage and tonal range are maintained.

## Wing intersection diagnostics

Run on Windows with .NET 8 from the repository root:

```powershell
dotnet run --project Tests/WindowsRendererTests -c Release -- --wing-intersections "Build/Client/Release/Data" "wing-intersections" 4
```

Pass a single classic CArmour LIB instead of the Data folder to inspect only that armour. The Data folder mode uses any installed 00, 01, 03, 06 and 12 libraries. The command renders all 1,616 body frames per armour, all 20 wing IDs, eight facings and four time samples per body frame by default. Use 1 instead of 4 for a quicker body-frame-only pass. It emits one GIF per gender, action and wing; the 8 direction cells are ordered Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft. Black is the actual decoded body/armour alpha silhouette with checker shadows stripped; pink is wing coverage captured from the real WARP renderer. Far layers are hidden by body alpha; near layers remain visible. The CSV records overlaps, overlaps outside a 12px socket neighbourhood, root lag and missing legacy wing art. Overlap is not intrinsically incorrect (near wings and orbit auras may legitimately overlap). The test draws wings on death frames to inspect geometry, even though the gameplay renderer hides them after death. Legacy fishing effects have no separate authored frame mapping in FrameSet; these use the actual default effect slot. This exhaustive diagnostic can generate many files and take substantial time. No source LIB is copied to the repository.
