# Bookless progression foundation

The shared catalog enumerates all 104 seeded base-class spell IDs, grouped into Warrior (17), Wizard (24), Taoist (25), Assassin (17) and Archer (21). FastMove, custom/equipment-only grants and event IDs are excluded. A pure eligibility function is available to the server, along with a stable byte specialisation identity and proposed names for all 15 specialisations. Starter unlock levels apply to five existing spells; other unlocks use the first rank's existing level gate.

This commit is a **foundation only**. It does not modify the save schema, disable books, make spec selection available, grant skills at login, enforce spec casting eligibility, implement passives or update the character UI/VFX. These changes must be integrated together to avoid a playable client/server mismatch.

Run `dotnet run --project Tests/SpellCurriculumTests/SpellCurriculumTests.csproj` on .NET SDK 8. No Windows or dotnet runtime tests were executed in the authoring environment.

## Migration metadata (118)

Server `Envir.Version` is now 118. Player and hero save records each append a separate selected specialisation byte at their own record boundary. Older (117 or earlier) records default to `Unchosen`, and invalid byte values are normalised on read. The MagicInfo editor now displays automatic unlock level instead of missing-book status. Neither selection UI nor gameplay gating is active yet.

**Deployment:** Back up your live `Server.MirDB` and all character/hero data before starting the upgraded server. The upgraded server writes version 118, which is not compatible with earlier server executables.

## Live server reconciliation

PlayerObject and HeroObject now call `ReconcileCurriculumSpells(false)` immediately after loading or creating the actor, before stats and initial skill snapshots. `HumanObject.LevelUp` reconciles with client notifications after threshold changes. Existing saved spells are retained without resetting rank, XP, keys or cast time; an eligible temporary grant becomes permanent. New spells receive server-generated `UserMagic` records. The old player and hero ItemType.Book handlers now return a failed use response without consuming the book or granting any ability.

This currently unlocks the legacy catalog at each spell's original `Level1` value, not spec-exclusive ownership. Spec UI, slot migration, signature spells, passives and spec-aware server cast enforcement remain incomplete.

## Specialisation selection and enforcement (protocol update)

The shared catalogue now assigns class-specific spells to one of three loadouts per class (15 total), while leaving an explicit basic kit. The player or summoned hero can choose a single specialisation from **Character → Skills** at level 20 or above. Selections are confirmed in the client, validated by the server, and acknowledged before local UI state updates. This is deliberately a one-time decision; a future respecialisation service will need its own rules and persisted state before it can be enabled.

Server magic lookup, melee spell parameters, passive stat refresh, and normal hero AI checks respect the selected loadout. Inactive spell records remain saved, including rank, experience, and keys; inactive spells cannot be dragged from the character panel or shown as usable action bar skills. The player and hero login packets carry specialisation independently. Packet IDs were **appended** to preserve the numeric values of existing messages; **client and server must be deployed together** because the login packet layouts changed.

The five-class catalogue and protocol round-trip tests are at `Tests/SpellCurriculumTests` and `Tests/SpecialisationProtocolTests`. Run `dotnet run --project Tests/SpellCurriculumTests/SpellCurriculumTests.csproj`, `dotnet run --project Tests/SpecialisationProtocolTests/SpecialisationProtocolTests.csproj`, and the complete Windows client/server builds in CI. These have not yet been verified to pass in this authoring environment.

## Signatures, traits and combat particles

All 15 specialisations have a level-20 signature. Steel Cut, Spirit Spark and Quick Cut are shared level-1 attacks for Warrior, Taoist and Assassin. The spellbook previews the signature and traits at levels 20, 32 and 45. The first trait modifies the actor's stats. The next two add spell behaviours such as an extra relay target, poison transfer, ward sharing or mana recovery.

The server validates mana, cooldown, entitlement, range, target and map before accepting a signature. It resolves wind-ups and repeat effects on the server. Spell power, mana cost, range, cooldown and proficiency gates use the existing Magic editor fields. The world database seeds missing definitions when loading and preserves edited rows. These rows use the version-118 layout.

Combat particles use separate caster, target and impact layers with staggered start times. The client clears pending effects on a map reset. Ash Seal and Storm Relay resolve in timed stages. The same client renders terrain, weather and combat effects using the Vortice renderer.

Run `dotnet run --project Tests/SpecialisationCombatTests/SpecialisationCombatTests.csproj` to exercise the server's spell dispatch and Magic database. The defaults are initial PvE and PvP tuning. The encounter and duel comparisons in the design audit require live gear, map and monster data.
