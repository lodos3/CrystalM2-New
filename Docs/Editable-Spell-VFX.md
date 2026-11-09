# Editable spell VFX

The client builds its spell particles from the versioned JSON assets in
[Client/Data/SpellVfx](../Client/Data/SpellVfx). The source directory is the
authoring location. Build and publish copy it to `Data/SpellVfx` next to the
client executable, which reads the files when it starts generating spell art.

- `Warrior.json`, `Wizard.json`, and `Taoist.json` hold class effect settings.
- `Signature.json` holds specialisation and talent settings.
- `Legacy.json` holds legacy effects, projectile travel, and packet cue timing.
- `Frame.json` and `Debris.json` hold the shared frame emitters and impact particles.

Settings are grouped by the effect routine that uses them. For example,
`Warrior.DrakeCharge` controls Twin Drake Blade's charge, `Warrior.ProjectCrescent`
controls Half Moon and Cross Half Moon geometry, and `Taoist.BurningWard` controls the
Soul Shield and Blessed Armour flame and smoke. Changes to a group file apply to
every spell that uses that group. Save a spell override to edit one spell independently.

## A per-spell edit

This source file would be saved as
`Client/Data/SpellVfx/Spells/TwinDrakeBlade.json`. It changes the charge sparks
and impact duration for Twin Drake Blade. Other values come from the group files.

~~~json
{
  "schemaVersion": 1,
  "groups": {
    "Warrior": {
      "DrakeCharge": { "SparkCount": 16 },
      "PacketDuration": { "TwinDrakeBladeDurationMs": 900 }
    }
  }
}
~~~

For a shared group file the outer keys are `schemaVersion` and `nodes`. Colours
use `#RRGGBB` and shapes and draw layers use their names. Colour curves contain
ordered stops with a `position` between 0 and 1 and a `colour`. Timing keys ending
in `Ms` are in milliseconds.

## Studio integration

Use `SpellVfxCatalogue.All` for the catalogue generated from the curriculum,
specialisations, and talent abilities. It includes attack-triggered and passive
entries. The editor can read and write profiles with `SpellVfxAssets`.

- `ReadGroup` reads shared values. `ReadSpell` reads the saved overrides and
  `ReadSpellGroup` resolves effective values for a spell.
- `SaveGroup` and `SaveSpell` validate a document before replacing its file. Save
  only changed settings in a spell override so it keeps inheriting shared updates.
- `Reload` validates and applies a directory to subsequent effect generation
  in the current process. A failed reload keeps the previous settings active.

The existing C# routines define which effects run for a spell event. They also
implement weapon tracking, sprite and rune geometry, and renderer behaviour.
These files expose their tuning and do not define a custom emitter graph.
