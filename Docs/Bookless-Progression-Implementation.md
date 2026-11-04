# Bookless progression foundation

The shared catalog enumerates all 104 seeded base-class spell IDs, grouped into Warrior (17), Wizard (24), Taoist (25), Assassin (17) and Archer (21). FastMove, custom/equipment-only grants and event IDs are excluded. A pure eligibility function is available to the server, along with a stable byte specialisation identity and proposed names for all 15 specialisations. Starter unlock levels apply to five existing spells; other unlocks use the first rank's existing level gate.

This commit is a **foundation only**. It does not modify the save schema, disable books, make spec selection available, grant skills at login, enforce spec casting eligibility, implement passives or update the character UI/VFX. These changes must be integrated together to avoid a playable client/server mismatch.

Run `dotnet run --project Tests/SpellCurriculumTests/SpellCurriculumTests.csproj` on .NET SDK 8. No Windows or dotnet runtime tests were executed in the authoring environment.
