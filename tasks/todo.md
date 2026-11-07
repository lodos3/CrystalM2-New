# Talent overhaul implementation sequence

[Design and spell catalogue](plan.md)

Each gameplay slice includes the server effect, client presentation and a focused check. Start with the shared Warrior tree, then prove Iron Vow's control pair and Arcane Hammer → Forgefall sequence. For each later spec, land the guaranteed kit and default proc first. Follow with the two paid spells and test each of the three engine paths against its own encounter.

## Phase 1 · Playable Warrior talent pilot

- [ ] **1. Validate talent graphs and budgets** · Small · Depends on none. Add the common node, rank, choice and edge model with Warrior pilot data. Verify the two budgets, banked points, rank gates, crossovers, one engine, one capstone, the six-spell build and refund cascades in `Tests/SpellCurriculumTests`. Audit deployed byte spell IDs before allocating the 57 original actions. Likely areas are `Shared/SpellCurriculum.cs`, the new shared tree definitions and catalogue tests.
- [ ] **2. Round-trip actor builds** · Medium · Depends on 1. Read and write one bounded build for each player or hero. Derive earned points and preserve selected spec, spell rank, XP, bindings and custom records on upgrade. Verify multiple adjacent records, old 118.0, 119.1 and 120.1 fixtures, invalid saved ranks, and read → save → read on copied data. Likely areas are `CharacterInfo`, `HeroInfo`, a build record and the existing combat save fixtures.
- [ ] **3. Commit a Warrior draft and resolve Shoulder Dash** · Medium · Depends on 1–2. Apply one actor's valid build revision at the trainer and resolve the unmodified Dash, Long Stride, Battering Dash or Crowdbreaker from that build. Verify exact path and damage, a blocked three-target shove, the last legal cell, PvP single-target limits, protected zones and replayed Apply. Likely areas are `PlayerObject`, `HumanObject`, the talent runtime and combat tests.
- [ ] **4. Synchronise actor snapshots and Apply** · Medium · Depends on 2–3. Add an actor-keyed snapshot and draft request with compatibility and revision checks. Verify packet round-trips, stale and duplicate drafts, spoofed hero IDs, rejection without partial writes and a fresh acknowledged snapshot after reconnect. Likely areas are `Shared` packet definitions, `MirConnection` and protocol tests.
- [ ] **5. Build the two-pane Talents window** · Medium · Depends on 4. Show the Warrior tree and three spec previews, paths, ranks, gated states, choice details and dependent refunds. Preview the order and payoff of each rotation engine and the capstone reachable with it. Verify preview then Apply, discard, narrow and wide resolutions, selection from the keyboard, and out-of-combat rejection with the accepted build still visible. Likely areas are Character and Talents dialogs, keybindings and client packet dispatch.
- [ ] **6. Reconcile the skill UI and action bars** · Medium · Depends on 4–5. Use resolved grants for Active/Passive pagination, variant replacement and separate per-spec layouts. Verify locked skills are absent, passives cannot bind, a variant swaps on the same key, and a restored spell does not clear a manually occupied slot. Exercise both player and hero snapshots. Likely areas are the Character and Skill Bar dialogs, `UserObject`, server slot validation and existing protocol tests.

**Checkpoint.** On a local client and server, a Warrior opens Talents, previews all three Dash versions, applies one, casts it against a known arrangement of mobs, then reconnects with the same choice and skill UI. Run the focused suites and build the matched client and server.

## Phase 2 · Warrior combat kits

- [ ] **7. Finish the shared Warrior tree** · Medium · Depends on 3–6. Enable the four user-requested shared skills plus Steel Cut through the new grants. Implement Rallying Standard with one resolved aura per family and the remaining shared passives. Verify all three specs receive the baseline, and overlapping Standards choose the strongest source.
- [ ] **8. Prove control and counter-control** · Medium · Depends on 6–7. Add the status family checks and the narrowly validated counter path with Pommel Lock and Crossguard Break. Verify a controlled Iron Vow can break one allowed state, wrong or scripted states stay active, PvP recovery prevents chaining, and invalid requests spend zero mana. Check that the client shows the active state and allows the counter key while controlled.
- [ ] **9. Prove an ordered proc** · Medium · Depends on 6–7. Implement one recipe and one ready flag per actor, starting with the default Hammer spend and the White Heat route into Forgefall. Verify correct order, target and timing, one credit per event, input retries, a failed preflight, two simultaneous attempts to spend, and cleanup on death, map or build change. Use actual attack and spell resolution in `Tests/SpecialisationCombatTests`.
- [ ] **10. Display the rotation tracker** · Medium · Depends on 8–9. Synchronise steps, target, readiness and cooldown for each actor. Verify the HUD and skill tooltips against server timing with a delayed impact, an expired ready proc, and a switch between player and hero. In Vortice, compare the line and impact to confirmed cells with low particles and high glow.
- [ ] **11. Finish Iron Vow** · Content slice · Depends on 8–10. Deliver six original actions, 12 new passive designs, all three engines and all six listed statuses. Verify the two-tile Thrusting → Hammer → Forgefall and Flaming Sword → Notchbreaker → Forgefall routes have distinct results. Verify Oathbrand retains the Steel Cut bind, Oathstep is learnable alongside it, and an off-column capstone remains legal.
- [ ] **12. Deliver Red Tempest** · Content slice · Depends on 7–11. Deliver its six actions and the full tree. Verify facing, blocked lunges, unique secondary hits and one Momentum build/spend per event. Compare the Rising Gale pack route with Twin Tempo on a lone target. Exercise Heel Hook and Red Slip with an obstructed escape tile.
- [ ] **13. Deliver Stoneguard** · Content slice · Depends on 7–11. Deliver its six actions and the full tree. Verify threat against ordinary and scripted targets, bounded tether transfer and strongest ward resolution. Run Hold Formation and Bodyguard Loop on the same timed attack fixture. Attribute ward absorption to the real attacker and count the incoming cast once.

**Checkpoint.** Run all three Warrior specs in a solo route, five-monster pull and boss or tank-spike encounter. Compare at least two engines in each spec. Switch specs, preserve shared picks and each bar, and verify queued effects respect the new revision. Test native CC and a compatible break in a duel.

## Phase 3 · Wizard combat kits

- [ ] **14. Deliver the shared Wizard tree** · Medium · Depends on the Warrior combat checkpoint. Grant the level-based kit, bind the Primary Bolt family, and add Prism Step. Verify target tile and path checks, local teleport lockout, and a Magic Shield modifier on all three specs.
- [ ] **15. Deliver Ashweaver** · Content slice · Depends on 14. Deliver its six actions and the full tree. Verify Fire Wall ownership, first-hit Ember spending, one trigger per field cast, unique Singe splash and the Ash Seal-to-Meteor Strike slot change. Compare a target-bound Singe Sequence with a tile-bound Kiln Cycle. Test silence with movement and weapon attacks still available.
- [ ] **16. Deliver Rimecaller** · Content slice · Depends on 14. Deliver its six actions and the full tree. Verify one target-bound Rime mark, one spend, walkable frost fields and ward expiry on leaving the radius. Check Far Shatter with both Fracture Spear and Ice Thrust. Verify Thawskin clears a permitted freeze without clearing an unrelated silence.
- [ ] **17. Deliver Stormwright** · Content slice · Depends on 14. Deliver its six actions and the full tree. Verify that chains hit each victim once, use the pylon at most once, and spend Charge on the marked isolated target. Compare a Live Conductor boss sequence with Open Circuit on a pack. Test Thunder Nail's actual tile and the counter's legal step.

**Checkpoint.** Compare one boss, one five-target pack and a choke with all three specs and at least two engines per spec. Match the client telegraphs to the server's actual cells and range. Switch away from an active storm and confirm its remaining pulses and any proc eligibility stop.

## Phase 4 · Taoist combat kits

- [ ] **18. Deliver the shared Taoist tree** · Medium · Depends on the Warrior combat checkpoint. Grant basic poisons and cleanse across the class, implement Paper Ward and the two Armour Rites. Verify exact reagent use, target-specific dispel eligibility, and independent physical and magical Rite families.
- [ ] **19. Deliver Lanternkeeper** · Content slice · Depends on 18. Deliver its six actions and the full tree. Verify effective healing builds Reserve once, overheal gives zero credit, and only the winning absorption consumes Sanctuary's shared pool. Compare Wick Weave, which uses an effective transfer as one specific proc step, with Preemptive Light, which requires an incoming hit. Exercise Unbind Knot on self and on a party ally.
- [ ] **20. Deliver Venom Scribe** · Content slice · Depends on 18. Deliver its six actions and the full tree. Verify owner-specific red and green consumption, voluntary movement, partial tick advancement, and capped spread without proc loops. Compare Green Concentrate with Red Prescription. Test that a cleansed, expired or foreign dose cannot earn Crucible.
- [ ] **21. Deliver Spirit Binder** · Content slice · Depends on 18. Deliver its six actions and the full tree. Verify one companion, owner-specific Bond, path and leash restrictions, valid arrival wards, and cleanup of queued orders. Compare the Rescue Circuit mend with the Hunt Order focus sequence. Test all three forms, unavailable-pet maps, Soul Unshackle and owned hero AI.

**Checkpoint.** Test effective healing and absorption, mixed-source poison and a full pet lifecycle. Repeat with two Taoists of the same and different specs. Check amulet and reagent costs, buff icons and party effects in the client.

## Phase 5 · Balance and Release staging

- [ ] **22. Run the balance matrix** · Medium · Depends on 7–21. Verify six original spells, 12 original passive designs, three buffs, three debuffs and both control actions per spec. Export and pin live definitions and representative gear. Exercise the default and all three engine recipes for each spec, including an off-column capstone and a full six-spell build. Compare power, proc cadence, response time, threat, healing, pet contribution and control duration in the relevant PvE and PvP scenarios.
- [ ] **23. Stage and verify matched Release builds** · Medium · Depends on 22. Back up and migrate copies of world, account and hero data. Verify existing high-level actors, older save variants, UI and binding migration, reconnect, actor switches and data preservation. Exercise a complete spec lifecycle in the staged Vortice client. Rehearse rollback using the matching pre-migration binaries and snapshots.

### Verification commands

Run these from the repo root during implementation. Extend the existing test suites with the checks above as each slice lands.

```powershell
dotnet run -c Release --project Tests/SpellCurriculumTests/SpellCurriculumTests.csproj
dotnet run -c Release --project Tests/SpecialisationProtocolTests/SpecialisationProtocolTests.csproj
dotnet run -c Release --project Tests/SpecialisationCombatTests/SpecialisationCombatTests.csproj
dotnet run -c Release --project Tests/UiSkinTests/UiSkinTests.csproj
dotnet run -c Release --project Tests/WindowsRendererTests/WindowsRendererTests.csproj
dotnet build Client/Client.csproj -c Release
dotnet build Server.MirForms/Server.csproj -c Release
```
