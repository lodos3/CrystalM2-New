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
