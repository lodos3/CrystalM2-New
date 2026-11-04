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
