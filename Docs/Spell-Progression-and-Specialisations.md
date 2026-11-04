# CrystalM2 spell audit and specialisation proposal

This is a design proposal for the Warrior, Wizard and Taoist. The current-behaviour audit is pinned to the Vortice client and server at commit [`6d8d823`](https://github.com/lodos3/CrystalM2-New/commit/6d8d8238301f63b4cac49ac0603b63cd62761d95). The tables cover 67 class spell IDs and five relevant custom IDs. Of those 72 IDs, 71 have a checked-in seed definition. These are source defaults. The deployed `Server.MirDB`, its book items, and server balance settings were unavailable for this audit. Live learn levels, availability and balance cannot be inferred from the seeds.

Start with the Warrior. Give each class a small automatically learned kit, then let the player choose a specialisation that changes what they do in a fight. Keep the rest of their learned skills and proficiency in the spellbook for future respecs. Introduce one signature action and three small, conditional passives per spec before adding more buttons.

## Warrior and the existing spellbook

- **Learning.** Item class and requirement checks run before the item is used. For books, the item shape is cast to a `Spell` ID, the server adds a `UserMagic` record, sends `NewMagic`, and refreshes stats. Both player and hero have this path. `Level1/2/3` and `Need1/2/3` control practice and ranks after learning. The database loads its own item and spell records first, then fills missing spell definitions. Older databases also receive a spell data upgrade. [Use requirements](../Server/MirObjects/HumanObject.cs#L1022) · [Player book use](../Server/MirObjects/PlayerObject.cs#L6122) · [Hero book use](../Server/MirObjects/HeroObject.cs#L496) · [Rank-up logic](../Server/MirObjects/HumanObject.cs#L6904) · [Database loading](../Server/MirEnvir/Envir.cs#L2873)
- **Casting.** Fencing and Slaying alter stats when learned. Slaying also arms a proc. Thrusting, HalfMoon and CrossHalfMoon are attack toggles. TwinDrakeBlade, FlamingSword and CounterAttack have server-side armed states. Other Warrior abilities go through the usual magic dispatch or a delayed effect. A new eligibility rule has to cover the attack path, toggle path, proc path, direct casts and hero AI. [Passive stats](../Server/MirObjects/HumanObject.cs#L2295) · [Physical attack](../Server/MirObjects/HumanObject.cs#L2849) · [Magic dispatch](../Server/MirObjects/HumanObject.cs#L3401) · [Toggle handling](../Server/MirObjects/HumanObject.cs#L8502)
- **Spellbook and HUD.** The Character window currently paginates seven learned skills from the actor's list. The `MagicButton` displays proficiency, has a large spell-specific tooltip switch, and offers drag and key assignment on every row. The action bar finds a spell by its saved slot. For Fencing, Slaying and SpiritSword, the client accepts the slot assignment and then returns without casting when pressed. Tooltips and the combat path also calculate MP separately. [Character Skills pane](../Client/MirScenes/Dialogs/CharacterDialog.cs#L273) · [Spell row and tooltip](../Client/MirScenes/Dialogs/MainDialogs.cs#L1965) · [HUD spell lookup](../Client/MirScenes/Dialogs/SkillBarDialog.cs#L43) · [Client spell use](../Client/MirScenes/GameScene.cs#L1002)
- **Balance baseline.** `GetDamage` combines a stat roll, spell power and rank multiplier. Individual abilities still choose defence types, secondary hits, target rules and timing in their handlers. The melee attack interval also floors at 550 ms, so a flat attack speed passive can be strong on early gear and do nothing at the cap. Use confirmed outcomes from the server when tuning. [Spell damage and delays](../Server/MirDatabase/MagicInfo.cs#L180) · [Attack speed calculation](../Server/MirObjects/HumanObject.cs#L1749) · [Warrior skill handlers](../Server/MirObjects/HumanObject.cs#L4893)

## Full catalogue

The three levels in each row are the fallback character-level gates for reaching spell ranks 1, 2 and 3. A spell starts at rank 0 when learned. “Proposed home” is a design assignment. Alternatives in the same family share a slot choice and a lockout as appropriate.

### Warrior

Source for identifiers and seed values. Behaviour is verified against attack resolution, delayed spell effects, and the Warrior handlers. [IDs](../Shared/Enums.cs#L1141) · [Seed values](../Server/MirEnvir/Envir.cs#L295) · [Attack patterns and damage](../Server/MirObjects/HumanObject.cs#L2849) · [Warrior handlers](../Server/MirObjects/HumanObject.cs#L4893) · [Map effects](../Server/MirEnvir/Map.cs#L1396)

| ID | Spell | Rank gates | Current behaviour | Proposed home |
| --- | --- | --- | --- | --- |
| 1 | [`Fencing`](../Server/MirEnvir/Envir.cs#L297) | 7 / 9 / 12 | Passive accuracy, +3 per rank. Trains on successful melee damage. | Shared passive |
| 2 | [`Slaying`](../Server/MirEnvir/Envir.cs#L299) | 15 / 17 / 20 | Passive accuracy and maximum DC. An accepted attack can arm a bonus melee strike. | Iron Vow passive |
| 3 | [`Thrusting`](../Server/MirEnvir/Envir.cs#L301) | 22 / 24 / 27 | Toggle for a thrust that can strike the tile beyond the normal melee target. | Iron Vow aimed line |
| 4 | [`HalfMoon`](../Server/MirEnvir/Envir.cs#L317) | 26 / 28 / 31 | Toggle for an adjacent cleave, paid on the attack. | Red Tempest base sweep |
| 5 | [`ShoulderDash`](../Server/MirEnvir/Envir.cs#L334) | 30 / 32 / 34 | Moves forward through checked cells and can push an eligible lower-level target. | Shared mobility |
| 6 | [`TwinDrakeBlade`](../Server/MirEnvir/Envir.cs#L352) | 32 / 34 / 37 | Arms a melee hit with a delayed second strike. A stun has separate chance and target checks. | Red Tempest double hit |
| 7 | [`Entrapment`](../Server/MirEnvir/Envir.cs#L369) | 32 / 35 / 37 | Delayed, chance-based pull and paralysis on eligible monsters. | Stoneguard PvE pull |
| 8 | [`FlamingSword`](../Server/MirEnvir/Envir.cs#L385) | 35 / 37 / 40 | Arms the next melee hit for a DC-based strike against armour. Has a separate arming timer. | Iron Vow charged strike |
| 9 | [`LionRoar`](../Server/MirEnvir/Envir.cs#L402) | 36 / 39 / 41 | Paralyses eligible nearby monsters within a 5×5 footprint. | Stoneguard control choice |
| 10 | [`CrossHalfMoon`](../Server/MirEnvir/Envir.cs#L418) | 38 / 40 / 42 | Toggle with wider adjacent coverage than HalfMoon. | Red Tempest sweep upgrade |
| 11 | [`BladeAvalanche`](../Server/MirEnvir/Envir.cs#L435) | 38 / 41 / 43 | Directional three-column, three-row attack. Far cells have reduced damage and ordinary targets use MAC. | Red Tempest aimed cleave |
| 12 | [`ProtectionField`](../Server/MirEnvir/Envir.cs#L452) | 39 / 42 / 45 | Self buff raises minimum and maximum armour based on current maximum armour. | Stoneguard guard choice |
| 13 | [`Rage`](../Server/MirEnvir/Envir.cs#L468) | 44 / 47 / 50 | Self buff increases minimum and maximum DC. | Red Tempest tempo choice |
| 14 | [`CounterAttack`](../Server/MirEnvir/Envir.cs#L471) | 47 / 51 / 55 | Arms a short armour and magic armour buff and a chance to retaliate against an adjacent attacker. | Stoneguard timed guard |
| 15 | [`SlashingBurst`](../Server/MirEnvir/Envir.cs#L489) | 50 / 53 / 56 | Schedules a directional hit and tries to move the Warrior two tiles. The hit is scheduled before destination checks. | Iron Vow chase choice |
| 16 | [`Fury`](../Server/MirEnvir/Envir.cs#L511) | 45 / 48 / 51 | Self attack speed buff with a long base cooldown. | Red Tempest tempo choice |
| 17 | [`ImmortalSkin`](../Server/MirEnvir/Envir.cs#L529) | 60 / 61 / 62 | Self buff raises maximum armour and lowers maximum DC. | Stoneguard guard choice |

### Wizard

The Wizard has 24 seeded standard spells. FastMove has an ID and a client description, but its seed is commented out and no normal server cast dispatch was found. Blink is listed with the custom spells below. [IDs](../Shared/Enums.cs#L1164) · [Seed values](../Server/MirEnvir/Envir.cs#L547) · [Wizard handlers](../Server/MirObjects/HumanObject.cs#L3939) · [Map effects](../Server/MirEnvir/Map.cs#L949) · [Teleport override](../Server/MirObjects/PlayerObject.cs#L4633)

| ID | Spell | Rank gates | Current behaviour | Proposed home |
| --- | --- | --- | --- | --- |
| 31 | [`FireBall`](../Server/MirEnvir/Envir.cs#L549) | 7 / 9 / 11 | Single-target fire projectile. | Shared starter |
| 32 | [`Repulsion`](../Server/MirEnvir/Envir.cs#L567) | 12 / 15 / 19 | Adjacent push with target and level checks. Shares the push handler with EnergyRepulsor. | Shared spacing |
| 33 | [`ElectricShock`](../Server/MirEnvir/Envir.cs#L584) | 13 / 18 / 24 | Control and tame checks on an eligible monster, with map and pet limits. | Stormwright PvE utility |
| 34 | [`GreatFireBall`](../Server/MirEnvir/Envir.cs#L600) | 15 / 18 / 21 | Higher-tier fire projectile using the same basic delivery as FireBall. | Ashweaver projectile upgrade |
| 35 | [`HellFire`](../Server/MirEnvir/Envir.cs#L618) | 16 / 20 / 24 | Directional fire line. At rank 3 it can add side rays. | Ashweaver line |
| 36 | [`ThunderBolt`](../Server/MirEnvir/Envir.cs#L636) | 17 / 20 / 23 | Single-target lightning with bonus damage against undead. | Stormwright single target |
| 37 | [`Teleport`](../Server/MirEnvir/Envir.cs#L655) | 19 / 22 / 25 | The player handler searches around the bind point on the bind map. Blink is the local targeted teleport. | Shared travel |
| 38 | [`FireBang`](../Server/MirEnvir/Envir.cs#L658) | 22 / 25 / 28 | Delayed burst in a 3×3 area. | Ashweaver burst choice |
| 39 | [`FireWall`](../Server/MirEnvir/Envir.cs#L676) | 24 / 28 / 33 | Persistent fire cells with a per-map cast limit. | Ashweaver zone choice |
| 40 | [`Lightning`](../Server/MirEnvir/Envir.cs#L694) | 26 / 29 / 32 | Directional line extending six cells, with a target check in each cell. | Stormwright line |
| 41 | [`FrostCrunch`](../Server/MirEnvir/Envir.cs#L712) | 28 / 30 / 33 | Single-target ice with separately rolled slow and freeze chances. | Rimecaller Chill setup |
| 42 | [`ThunderStorm`](../Server/MirEnvir/Envir.cs#L730) | 30 / 32 / 34 | Caster-centred 5×5 damage with a large penalty against non-undead. | Stormwright close-range burst |
| 43 | [`MagicShield`](../Server/MirEnvir/Envir.cs#L750) | 31 / 34 / 38 | Self damage reduction that expires faster when hit. | Shared self-defence |
| 44 | [`TurnUndead`](../Server/MirEnvir/Envir.cs#L766) | 32 / 35 / 39 | Conditional instant kill of an eligible undead monster. | Encounter utility, review eligibility |
| 45 | [`Vampirism`](../Server/MirEnvir/Envir.cs#L782) | 33 / 36 / 40 | Targeted damage with self-healing based on damage dealt. | Optional sustain, capped separately |
| 46 | [`IceStorm`](../Server/MirEnvir/Envir.cs#L800) | 35 / 37 / 40 | 3×3 area attack sharing the FireBang damage handler. No explicit chill in that handler. | Rimecaller area hit |
| 47 | [`FlameDisruptor`](../Server/MirEnvir/Envir.cs#L818) | 38 / 40 / 42 | Fire projectile with extra damage against non-undead and an explicit target-type restriction. | Ashweaver projectile choice |
| 48 | [`Mirroring`](../Server/MirEnvir/Envir.cs#L837) | 41 / 43 / 45 | Summons or recalls a configured clone, subject to map pet restrictions. | Rimecaller optional utility |
| 49 | [`FlameField`](../Server/MirEnvir/Envir.cs#L840) | 42 / 43 / 45 | Caster-centred 5×5 fire hit with a longer cast lock. | Ashweaver zone or close burst choice |
| 50 | [`Blizzard`](../Server/MirEnvir/Envir.cs#L858) | 44 / 47 / 50 | Repeated 5×5 ground damage with a slow chance. Uses the caster's shared storm-active flag. | Rimecaller ground pressure |
| 51 | [`MagicBooster`](../Server/MirEnvir/Envir.cs#L878) | 47 / 49 / 52 | Temporarily increases min/max MC and increases mana costs. | Stormwright damage choice |
| 52 | [`MeteorStrike`](../Server/MirEnvir/Envir.cs#L894) | 49 / 52 / 55 | Repeated delayed 5×5 ground hits. Shares the storm-active flag with Blizzard. | Ashweaver zone choice |
| 53 | [`IceThrust`](../Server/MirEnvir/Envir.cs#L914) | 53 / 56 / 59 | Delayed directional ice with distance falloff and slow/freeze rolls. | Rimecaller line payoff |
| 54 | `FastMove` | — | Enum and tooltip entry. No active seed or ordinary server cast handler found. | Hold out of progression |
| 55 | [`StormEscape`](../Server/MirEnvir/Envir.cs#L936) | 60 / 61 / 62 | Caster-centred storm followed by an attempted targeted teleport. | Stormwright mobility choice |

### Taoist

All 25 Taoist IDs have seed entries. ID 66 is unused. Amulets, poison and revive reagents are combat costs, separate from learning books. Their live items and prices need their own audit. [IDs](../Shared/Enums.cs#L1191) · [Seed values](../Server/MirEnvir/Envir.cs#L955) · [Taoist handlers](../Server/MirObjects/HumanObject.cs#L4355) · [Delayed effects](../Server/MirObjects/HumanObject.cs#L5904) · [Ground effects](../Server/MirObjects/SpellObject.cs#L150)

| ID | Spell | Rank gates | Current behaviour | Proposed home |
| --- | --- | --- | --- | --- |
| 61 | [`Healing`](../Server/MirEnvir/Envir.cs#L957) | 7 / 11 / 14 | Queues a heal on an eligible friendly target. | Shared heal |
| 62 | [`SpiritSword`](../Server/MirEnvir/Envir.cs#L974) | 9 / 12 / 15 | Passive melee accuracy and hit-based practice. No active cast. | Shared passive |
| 63 | [`Poisoning`](../Server/MirEnvir/Envir.cs#L977) | 14 / 17 / 20 | Uses the selected poison reagent. Green deals periodic damage; red changes armour effectiveness. | Venom Scribe setup |
| 64 | [`SoulFireBall`](../Server/MirEnvir/Envir.cs#L980) | 18 / 21 / 24 | SC-based projectile that uses an amulet. | Shared damage |
| 65 | [`SummonSkeleton`](../Server/MirEnvir/Envir.cs#L998) | 19 / 22 / 26 | Summons or recalls the configured skeleton, subject to map and pet limits. | Spirit Binder companion form |
| 67 | [`Hiding`](../Server/MirEnvir/Envir.cs#L1014) | 20 / 23 / 26 | Self invisibility using an amulet. | Spirit Binder optional utility |
| 68 | [`MassHiding`](../Server/MirEnvir/Envir.cs#L1017) | 21 / 25 / 29 | Area friendly invisibility. The handler requires an amulet but does not consume it. | Spirit Binder optional utility |
| 69 | [`SoulShield`](../Server/MirEnvir/Envir.cs#L1033) | 22 / 24 / 26 | Area buff to maximum magic armour using an amulet. | Lanternkeeper magic ward |
| 70 | [`Revelation`](../Server/MirEnvir/Envir.cs#L1049) | 23 / 25 / 28 | Chance to reveal target health. | Venom Scribe scouting choice |
| 71 | [`BlessedArmour`](../Server/MirEnvir/Envir.cs#L1065) | 25 / 27 / 29 | Area buff to maximum armour using an amulet. | Lanternkeeper physical ward |
| 72 | [`EnergyRepulsor`](../Server/MirEnvir/Envir.cs#L1081) | 27 / 29 / 31 | Adjacent push with target and level checks, sharing Repulsion's handler. | Shared spacing |
| 73 | [`TrapHexagon`](../Server/MirEnvir/Envir.cs#L1098) | 28 / 30 / 32 | Amulet-based area control against eligible lower-level monsters. | Spirit Binder control choice |
| 74 | [`Purification`](../Server/MirEnvir/Envir.cs#L1114) | 30 / 32 / 35 | Chance-based removal of eligible debuffs and poisons on a friendly target. | Lanternkeeper cleanse |
| 75 | [`MassHealing`](../Server/MirEnvir/Envir.cs#L1130) | 31 / 33 / 36 | Queues healing for eligible allies within a 3×3 area. | Lanternkeeper area heal choice |
| 76 | [`Hallucination`](../Server/MirEnvir/Envir.cs#L1148) | 31 / 34 / 36 | Eligible monster confusion, with chance, level, range and amulet checks. | Venom Scribe PvE control choice |
| 77 | [`UltimateEnhancer`](../Server/MirEnvir/Envir.cs#L1164) | 33 / 35 / 38 | Amulet-based buff that raises the recipient class's relevant maximum attack stat. | Spirit Binder utility choice |
| 78 | [`SummonShinsu`](../Server/MirEnvir/Envir.cs#L1180) | 35 / 37 / 40 | Summons or recalls the configured Shinsu, subject to map and pet limits. | Spirit Binder companion form |
| 79 | [`Reincarnation`](../Server/MirEnvir/Envir.cs#L1196) | 37 / 39 / 41 | Delayed revive request for an eligible dead player, with a revival amulet. | Lanternkeeper situational revive |
| 80 | [`SummonHolyDeva`](../Server/MirEnvir/Envir.cs#L1212) | 38 / 41 / 43 | Summons or recalls the configured Holy Deva, subject to map and pet limits. | Spirit Binder companion form |
| 81 | [`Curse`](../Server/MirEnvir/Envir.cs#L1228) | 40 / 42 / 44 | Amulet-based area slow and power reduction, with chance checks. | Venom Scribe control |
| 82 | [`Plague`](../Server/MirEnvir/Envir.cs#L1245) | 42 / 44 / 47 | Area damage and a rolled status effect, including poison when available. Can drain player MP. | Venom Scribe pressure |
| 83 | [`PoisonCloud`](../Server/MirEnvir/Envir.cs#L1248) | 43 / 45 / 48 | Persistent area damage and green poison. Costs five amulets and five green poison in the current handler. | Venom Scribe ground pressure |
| 84 | [`EnergyShield`](../Server/MirEnvir/Envir.cs#L1268) | 48 / 51 / 54 | Chance for a player recipient to gain HP when hit. | Lanternkeeper reactive ward |
| 85 | [`PetEnhancer`](../Server/MirEnvir/Envir.cs#L1284) | 45 / 48 / 51 | Adds attack and armour to a friendly monster using its level. | Spirit Binder pet buff |
| 86 | [`HealingCircle`](../Server/MirEnvir/Envir.cs#L1300) | 39 / 41 / 43 | Persistent area that heals eligible allies and damages and slows enemies. Current ticks have explicit actor restrictions. | Lanternkeeper area heal choice |

### Custom and equipment grants

The five custom IDs have seed entries. `MagicInfo` has no class-ownership field. A seed or a cast handler proves implementation, while actual access depends on item, script, equipment or admin data. Keep these out of automatic progression until their source and rules are explicit. [IDs](../Shared/Enums.cs#L1260) · [Blink seed](../Server/MirEnvir/Envir.cs#L931) · [Custom seeds](../Server/MirEnvir/Envir.cs#L1867) · [Gear grants](../Server/MirObjects/HumanObject.cs#L1878) · [Custom handlers](../Server/MirObjects/HumanObject.cs#L5791)

| ID | Spell | Rank gates | Current behaviour | Proposed home |
| --- | --- | --- | --- | --- |
| 151 | [`Blink`](../Server/MirEnvir/Envir.cs#L932) | 19 / 22 / 25 | Chance-based local targeted teleport. Current equipment grants can add it temporarily. | Keep equipment entitlement separate |
| 152 | [`Portal`](../Server/MirEnvir/Envir.cs#L1869) | 7 / 11 / 14 | Creates paired cells that can transport the caster or eligible group members. | Separate travel rules; class undecided |
| 153 | [`BattleCry`](../Server/MirEnvir/Envir.cs#L1872) | 48 / 51 / 55 | Nearby monster taunt using one roll per cast. The rank threshold comparison currently runs opposite its comment. | Stoneguard candidate after taunt audit |
| 154 | [`FireBounce`](../Server/MirEnvir/Envir.cs#L1888) | 15 / 18 / 21 | Fire projectile can chain to nearby targets. The selection excludes the last target only. | Ashweaver candidate after bounce limits |
| 155 | [`MeteorShower`](../Server/MirEnvir/Envir.cs#L1906) | 15 / 18 / 21 | Targeted projectile plus secondary damage to up to three nearby eligible monsters. | Hold for cooldown and role review |

## Proposed specialisations

Blizzard's Warrior describes the role contrast between measured strikes, rapid offence and protection. ArenaNet shows two mechanics that translate well to Mir's combat grid. Warrior hits build a resource for a deliberate payoff, and Guardian can give up a personal passive to help allies. These are references for role and trade-off design. The abilities below use CrystalM2's own tiles, timings, resources and equipment. [Blizzard Warrior](https://worldofwarcraft.blizzard.com/en-us/game/classes/warrior) · [ArenaNet Warrior](https://www.guildwars2.com/en/the-game/professions/warrior/) · [ArenaNet Guardian](https://www.guildwars2.com/en/the-game/professions/guardian/)

### Warrior

Shared kit. **Steel Cut** is a proposed adjacent weapon strike that gives new characters an active at level 1. **Fencing** stays a passive. **ShoulderDash** is shared mobility, with any displacement subject to explicit target and tile rules. Keep mana as the first implementation's common cost. Specialisation resources are small temporary counters.

| Spec | Active kit and signature | Three proposed passives | Trade-off |
| --- | --- | --- | --- |
| **Iron Vow** | Thrusting becomes an aimed two-tile line. FlamingSword is a deliberate armed hit. SlashingBurst is an optional chase tool sharing a mobility lockout with ShoulderDash. **Notchbreaker** spends an Opening on one target. | **Measured Grip** grants one Opening when Thrusting lands at its far tile. **Tempered Edge** gives Slaying's proc a bounded payoff on an Open target. **Finish the Line** rewards landing Notchbreaker before the Opening expires with a modest recovery refund. | Strong single-target pressure requires spacing. Give up circular cleave and group protection. |
| **Red Tempest** | HalfMoon upgrades into CrossHalfMoon on the same slot. TwinDrakeBlade becomes the committed double hit. BladeAvalanche supplies a directed pack hit. Rage and Fury are alternative tempo buffs. **Red Wake** spends Momentum on a readable sweep. | **Rising Heat** builds Momentum from a landed primary attack, with a cap per attack. **Wide Arc** gives reduced secondary damage after hitting distinct targets. **Cool Blood** grants a brief efficiency window after a full spend. | Strong against grouped enemies. Avoid increasing raw swing speed beyond the cap, and avoid a long PvP stun on the double hit. |
| **Stoneguard** | ProtectionField upgrades or trades into ImmortalSkin. CounterAttack is a timed guard window. Entrapment pulls eligible monsters. LionRoar and BattleCry are separate control and threat choices in one utility slot. **Interpose** shields one nearby ally with a bounded absorption pool. | **Set Feet** builds Guard from a real mitigated hit, with a short internal lockout. **Brace Line** increases guard value for a short period after a deliberate stop. **Stand Together** spends Guard to extend a single ally shield without chaining damage redirection. | Reliable frontline, group peel and threat. Lower burst and personal pack damage. |

### Wizard

Shared kit. **FireBall** is a modest starter projectile, **Repulsion** is a spacing tool, **MagicShield** is common limited self-defence, and **Teleport** is travel. A specialisation gets its own damage pattern. Fire upgrades may replace the starter in Ashweaver without removing it from the character's learned history.

| Spec | Active kit and signature | Three proposed passives | Trade-off |
| --- | --- | --- | --- |
| **Ashweaver** | GreatFireBall and FlameDisruptor are projectile upgrades or choices. HellFire and FireBang cover line versus burst. FireWall, FlameField and MeteorStrike share a zone budget. **Ash Seal** spends Embers on an owned burn footprint after a visible delay. | **Kindling** earns Ember on a confirmed qualifying cast once per cast. **Banked Heat** transfers remaining zone budget when replacing your own fire zone. **Cauterise** returns a small amount of mana after a successful Ash Seal on a marked target. | Excellent sustained area damage on placed targets. Movement out of the footprint counters it. FireBounce is an optional later choice after fixing bounce bounds. |
| **Rimecaller** | FrostCrunch marks a target with Chill. IceStorm offers quick area damage. IceThrust rewards lining up targets. Blizzard pressures an area over time. Mirroring is optional utility. **Glacial Gate** marks a short strip that applies a bounded slow once per target. | **Cold Snap** applies a predictable short Chill instead of multiple independent freezes. **Fracture** spends Chill for extra damage on an aimed spell. **Winter Margin** gives a small defensive benefit for creating distance from a chilled enemy. | Strong tempo and spacing. Lower immediate burst than a charged Stormwright. Hard freezes remain short, with immunity windows in PvP. |
| **Stormwright** | ThunderBolt is aimed single-target lightning. Lightning is a directional line. ThunderStorm is close-range risk. StormEscape and MagicBooster are mobility versus damage choices. **Storm Relay** discharges a marked tile into a bounded chain of distinct targets. | **Conductive Path** builds Charge from confirmed hits on distinct enemies, capped per cast. **Grounded** allows a limited mana rebate for a successful discharge. **Return Current** rewards a well-aimed first bounce without reopening the same target. | Fast payoff from deliberate positioning. A missed line or poor target cluster limits return. ElectricShock can stay a separate PvE utility with summon and tame limits. |

### Taoist

Shared kit. **Healing** supports solo play and allies, **SoulFireBall** supplies spell damage, **SpiritSword** stays a melee accuracy passive, and **EnergyRepulsor** is optional emergency space. The support path specialises in effective healing and wards, the poison path specialises in attrition, and the summoner specialises in managing one combat spirit.

| Spec | Active kit and signature | Three proposed passives | Trade-off |
| --- | --- | --- | --- |
| **Lanternkeeper** | MassHealing and HealingCircle are burst versus field choices. SoulShield and BlessedArmour cover magic versus physical warding. Purification cleanses. EnergyShield and Reincarnation are situational choices. **Lantern Link** spends a healing reserve to place one short shield on an ally. | **Tended Flame** earns reserve from actual HP restored, with a cap per cast. **Quiet Ward** converts a portion of a useful ward into brief protection, with one effect per recipient. **Clean Hands** grants a short efficiency window after a real cleanse. | High group support. Self-healing, shields and a revive cannot create unlimited sustain or repeat combat resets. |
| **Venom Scribe** | Poisoning chooses red or green by reagent. PoisonCloud, Plague and Curse cover area damage and control with separate budgets. Revelation and Hallucination are optional scouting or PvE control. **Distil** consumes your poison on one marked target for immediate damage. | **Fresh Ink** adds a mark on the first qualifying poison application. **Measured Dose** rewards maintaining a chosen poison without stacking copies. **Spillover** can copy one bounded portion of remaining poison to a new eligible target, once per cast. | Strong attrition and pressure. Cleanse, movement and reagent costs create counterplay. Consuming the poison removes its remaining damage budget. |
| **Spirit Binder** | Skeleton, Shinsu and Holy Deva are three forms in one companion family. PetEnhancer is a command buff. TrapHexagon, Hiding/MassHiding and UltimateEnhancer are utility choices. **Spirit Command** orders the current companion to focus, hold or recall. | **Bound Thread** reinforces one owned combat companion. **Command Bond** rewards a successful owner command with one small shared benefit. **Safe Return** grants a short protective window after a voluntary recall, with a lockout. | The pet provides pressure or support at the cost of personal output. The three forms trade damage, durability and support. Existing summons, tame and map rules must respect the same combat cap. |

### Unlock cadence

Use these as test milestones. Import the live book item requirements before choosing final grant levels. Leave rank gates and already earned practice XP intact for the first rollout so changes to acquisition and tuning can be measured separately.

| Character level | Proposed unlock |
| --- | --- |
| **1** | One reliable shared active. Steel Cut for Warrior, FireBall for Wizard, Healing plus a basic damage option for Taoist. Use a rank-zero SoulFireBall with a deliberately supplied starter reagent budget if retaining its amulet cost. |
| **7** | First shared utility or passive. Grant existing roles with the lowest risk of invalidating early encounters. |
| **15** | Choose one spec in town. Grant its setup action and first passive together so the player can immediately try the loop. |
| **22** | Grant the spec's spender or signature. |
| **30** | Open one utility choice and the second passive. |
| **40** | Grant a contextual upgrade or alternative in an existing action family. |
| **50** | Grant the third passive and a final utility choice, subject to the encounter and PvP tests. |

Most loadouts should end up with six to eight active buttons, one or two optional swaps and three spec passives. Progression can add choices and upgrades across those slots without forcing everyone to bind every legacy variation.


This is the first playable assignment per spec. Other legacy spells remain catalogue choices pending balance review.

| Spec | Level 15 setup | Level 22 payoff | First later choices |
| --- | --- | --- | --- |
| Iron Vow | Thrusting and Measured Grip | Notchbreaker | FlamingSword for burst, SlashingBurst for chase |
| Red Tempest | HalfMoon and Rising Heat | Red Wake | TwinDrakeBlade, CrossHalfMoon upgrade, one of Rage or Fury |
| Stoneguard | ProtectionField and Set Feet | Interpose | CounterAttack, then Entrapment or LionRoar |
| Ashweaver | HellFire and Kindling | Ash Seal | FireWall, then MeteorStrike or FlameField as a zone choice |
| Rimecaller | FrostCrunch and Cold Snap | Glacial Gate | IceStorm, then IceThrust or Blizzard |
| Stormwright | Lightning and Conductive Path | Storm Relay | ThunderBolt, then one of StormEscape or MagicBooster |
| Lanternkeeper | First ward and Tended Flame from shared Healing | Lantern Link | Purification, then one of MassHealing or HealingCircle |
| Venom Scribe | Poisoning and Fresh Ink | Distil | Curse, then one of PoisonCloud or Plague |
| Spirit Binder | SummonSkeleton and Bound Thread | Spirit Command | PetEnhancer, then Shinsu or Holy Deva as alternate forms |

## Bookless progression and spellbook

1. **Separate unlocks, proficiency and usability.** An explicit class curriculum owns character-level unlocks, spec, action kind, target mode, reagent rules and upgrade family. Saved proficiency keeps its stable spell ID, rank and XP. Usability is the current result of class, level, selected spec and the source of the grant. Distinguish curriculum, migrated book, equipment, NPC and GM grants. The existing gear path includes HealRing, FireRing and Blink, so an ID range alone cannot decide eligibility. NPC and GM skill grants also bypass book use today. [Spell record](../Server/MirDatabase/MagicInfo.cs#L87) · [Gear grants](../Server/MirObjects/HumanObject.cs#L1878) · [Temporary skills](../Server/MirObjects/HumanObject.cs#L2257) · [NPC grants](../Server/MirObjects/NPC/NPCSegment.cs#L3647) · [GM grants](../Server/MirObjects/PlayerObject.cs#L3167)
2. **Grant automatically and preserve saves.** Reconcile on character load, creation, level changes and spec selection. Make it safe to run twice and add every eligible grant if a character jumps levels. Backfill level-eligible characters and preserve already earned ranks and XP. Keep historical spells in the record even while another spec is active. Temporary gear grants fall away with the source. Until the pilot passes, use the existing rules for heroes and out-of-scope classes. Then apply the same reconciler to hero bookless progression with a separate spec selection, actor identity and role-aware AI. Hero casting currently requires a nonzero slot, so test that migration before enabling it. [Player level-up](../Server/MirObjects/PlayerObject.cs#L932) · [Hero level-up](../Server/MirObjects/HeroObject.cs#L688) · [Hero cast checks](../Server/MirObjects/HeroObject.cs#L301)
3. **Validate on the server.** Resolve actor, active entitlement, action kind, timing, range, target, map rules and reagents before charging a cast. Return the accepted cast's actual cost and cooldown, or an explicit rejection. Apply the same eligibility rules to direct casts, melee variants, toggles, passive refresh, delayed effects, scripted grants and hero AI. Recheck queued requests when they execute. On a town respec, clear incompatible armed effects, ground spells and companions. Preserve shared cooldowns and lockouts to prevent respec resets. Audit the existing retry queue and map flags for this. [Network dispatch](../Server/MirNetwork/MirConnection.cs#L1566) · [Current MP and timing order](../Server/MirObjects/HumanObject.cs#L3401) · [Attack and armed state](../Server/MirObjects/HumanObject.cs#L2849)
4. **Replace the learned-only list with a catalogue.** Keep it inside Character → Skills beside the live stats. Show the active spec and three selectable previews, with Active and Passive filters. Each row shows availability, unlock condition, proficiency, targeting, range, actual cost, cooldown and reagent where applicable. Lock passives and unavailable rows out of drag/drop and assignment. Keep slot layouts per spec and retain the player's physical keybinds. Use an actor-keyed snapshot or stable spell ID and revision for updates. The current removal packet only carries a list index and the client removes it from the player's list even when a hero script emitted it. Use explicit player or hero identity when assigning slots, and reject an invalid assignment before clearing the occupant. [Spell payload](../Shared/Data/ClientData.cs#L3) · [Removal packet](../Shared/ServerPackets.cs#L3340) · [Client removal](../Client/MirScenes/GameScene.cs#L4628) · [Hero removal](../Server/MirObjects/NPC/NPCSegment.cs#L4656) · [Current slot assignment](../Server/MirNetwork/MirConnection.cs#L1543)
5. **Retire book gates in the economy after the grant path works.** Export book items with their IDs, class flags, spell shapes, requirements, sources and prices. Check drops, shops, quest rewards and the trust merchant filters. Remove the learning requirement only for migrated classes and actors. Preserve physical items during the cutover. Decide a documented use or exchange for affected book items from the export, including duplicates and items already in storage or sale. Reagents remain a separate decision. [Book use](../Server/MirObjects/PlayerObject.cs#L6122) · [Merchant filters](../Client/MirScenes/Dialogs/TrustMerchantDialog.cs#L755)

## Balance before adding power

Correct the following discrepancies before using the existing behaviour as a balance target.

| Evidence in the audited source | Action before tuning |
| --- | --- |
| Generic magic sets several action timers and charges MP before checking many targets and effects. Some toggle abilities separately reject a character whose MP exactly equals the cost. [Generic cast](../Server/MirObjects/HumanObject.cs#L3401) · [Toggles](../Server/MirObjects/HumanObject.cs#L8527) | Test exact mana, out-of-range, protected target, blocked tile, missing reagent and retry cases. Set an explicit charge policy for each successful cast and failed attempt. |
| BattleCry uses one roll and accepts `roll <= threshold`. With the hardcoded thresholds of 90, 70, 50 and 30 over 0–99, conditional success falls from 91% to 31% by rank. [BattleCry](../Server/MirEnvir/Map.cs#L2250) | Define a taunt against eligible monsters, decide whether it is deterministic, and test each rank, boss flag and safe zone. Keep PvP taunt behaviour separate. |
| CounterAttack starts a client cooldown locally and the server uses its own seven-second armed timer. SlashingBurst's fallback delay falls from 14 seconds to 2 seconds while the fallback multiplier rises from 3.25 to 4.00 across ranks. It queues a hit before validating the movement destination. [Client counter timing](../Client/MirScenes/GameScene.cs#L1037) · [Server counter timing](../Server/MirObjects/HumanObject.cs#L8551) · [SlashingBurst seed](../Server/MirEnvir/Envir.cs#L489) · [SlashingBurst handler](../Server/MirObjects/HumanObject.cs#L5160) | Make guard timing authoritative. Test every cell crossed by a movement attack and calibrate damage, cooldown and mobility as a single budget. Clamp cooldown calculations in signed arithmetic before converting values. |
| Plague uses a special SC-based server cost. FireBounce excludes only the immediately hit target when choosing the next one. MeteorShower returns before the common cast completion path. [Server cost](../Server/MirObjects/HumanObject.cs#L3376) · [Plague client cost](../Client/MirScenes/GameScene.cs#L11984) · [FireBounce](../Server/MirObjects/HumanObject.cs#L5937) · [MeteorShower](../Server/MirObjects/HumanObject.cs#L3763) | Give the client authoritative costs. Bound total and per-victim chain hits. Confirm cooldown and result handling for all early-return casts. |
| Red poison changes armour effectiveness by different amounts for humans and monsters. PetEnhancer adds attack and armour based on the target level. Healing uses a queued amount and tick delivery. [Human red poison](../Server/MirObjects/HumanObject.cs#L712) · [Monster red poison](../Server/MirObjects/MonsterObject.cs#L1537) · [PetEnhancer](../Server/MirObjects/HumanObject.cs#L6364) · [Heal delivery](../Server/MirObjects/HumanObject.cs#L606) | Keep separate PvE and PvP budgets. Measure poison's group amplification, real HP restored after overheal, and combined owner plus companion contribution. |

### Reproducible balance gates

Use a read-only export of the deployed magic, item, base-stat, monster and map definitions, plus representative gear sets from the live configuration. Pin the data revision and RNG seeds. Run the same cases on the existing server and the candidate server. Compare accepted casts and outcomes over repeated trials, with per-cast and per-target IDs to expose duplicate hits. Record personal and group contribution separately. No live balance numbers have been validated.

| Scenario | Measure | Proposed first target |
| --- | --- | --- |
| Fresh character, level 15, midgame and endgame. Basic and geared kits. | Time to first useful skill, mana/reagent spend, clear time, damage taken and downtime | Each spec can complete the shared solo route. No mandatory book or rare reagent blocks a starter rotation. |
| 60-second boss and a fixed five-target pull | Sustained damage, burst windows, secondary hit scaling, uptime | Among comparable damage specs, aim for a 10% band in the scenario each is intended to contest. Compare cleave and boss separately. Keep Stoneguard and Lanternkeeper on separate role budgets. |
| Boss tank, timed group heal, cleanse and pet encounters | Damage prevented, time alive, threat retention, effective HPS, party damage increase, owner plus pet output | Each tank/support choice has a distinct use case. Multiple same-family wards, armour debuffs or pets cannot exceed the intended group contribution through stacking. |
| Matched duels and small parties against the other specs plus Assassin and Archer | Median and upper-tail burst, time unable to act, time to kill, sustain, win rates by matchup | Use repeatable gear and comparable play. Reject repeated one-combo kills at equal gear and uninterrupted hard-control chains. Start with a two-second hard-control ceiling for new PvP effects and a post-control immunity window, then calibrate against real encounters. |
| Respec, reconnect, gear swaps, hero ownership, duplicate requests and map changes | Spell identity, saved rank/XP/slots, resource spend, queued cast invalidation, lingering effects | Zero duplicate grants, lost progress, unauthorized casts or cooldown resets. New slots only accept usable active spells. |

These are candidate acceptance gates, not measured results. “Perfect balance” is a playtest outcome to work toward. First record the existing distribution, then adjust a small set of per-mode coefficients and retest. Avoid independently tuning displayed damage, actual damage and VFX timing.

## Original spell effects in the Vortice client

CrystalM2 has a particle engine for screen-space weather and separate spell sprites, missiles and persistent spell objects. Keep weather independent. Add a pooled world-space spell emitter that consumes existing confirmed cast, impact and ground-effect events. Bind each effect to a spell ID and phase such as windup, travel, impact, zone or expiry. Map, actor and cast identity prevent duplicate emissions across network events. Make the damaging tile footprint and timing come from the server. A particle is a visual consequence of an event, so it does not need a network packet. [Weather engine](../Client/MirGraphics/ParticleEngine.cs#L399) · [Cast events](../Client/MirScenes/GameScene.cs#L4688) · [Spell sprites](../Client/MirObjects/PlayerObject.cs#L1537) · [Persistent spells](../Client/MirObjects/SpellObject.cs#L30)

- **Texture source.** Kenney's Particle Pack contains 80 assets under CC0. The author upload and archive include usable starting shapes such as `slash_01.png`, `trace_01.png`, `circle_01.png`, `fire_01.png`, `smoke_01.png` and `spark_01.png`. Import only a selected set into a padded atlas with the source and license recorded. Use the textures as masks for original effects. [Kenney](https://www.kenney.nl/assets/particle-pack) · [Author upload](https://opengameart.org/content/particle-pack-80-sprites) · [CC0](https://creativecommons.org/publicdomain/zero/1.0/)
- **Shaders.** Prototype atlas quads through the current Vortice sprite batch, with alpha for smoke and additive blending for sparks. For procedural telegraphs, try a signed-distance ring or arc with `fwidth` for pixel-scale edge width and `smoothstep` for the soft edge. Submit adjacent effects with the same texture, shader and blend within each world depth layer. Start with the current dynamic sprite buffer. Add instancing only if profiling shows the vertex submission cost matters. Route a small number of glow accents to the existing emission-only bloom so the baked lighting in the artwork remains readable. [Vortice batch](../Client/Rendering/SpriteBatch.cs#L47) · [World shader](../Client/Data/Shaders/World.hlsl#L1) · [HLSL fwidth](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-fwidth) · [HLSL smoothstep](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-smoothstep) · [D3D11 blend factors](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ne-d3d11-d3d11_blend) · [Dynamic resources](https://learn.microsoft.com/en-us/windows/win32/direct3d11/how-to--use-dynamic-resources)
- **Budgets.** Start by testing a 512×512 RGBA8 atlas at 1 MiB before mipmaps, 512 live decorative particles on normal and 1,024 on high. Treat these as caps for a prototype, not measured limits. Cull off-screen emitters, recycle slots, and drop low-priority decoration before telegraphs. Keep local hazards and hostile footprints visible even at minimum effects. Test isolated and dense fights with draw calls, texture bytes, allocations, frame p95/p99 and D3D11 GPU timestamps. Invalidate the dynamic world image while effects animate without rebuilding the cached terrain floor. [World draw and caches](../Client/MirScenes/GameScene.cs#L10829) · [D3D11 timing queries](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/ne-d3d11-d3d11_query)

| Spec | Visual language for the first signature |
| --- | --- |
| Iron Vow | Narrow steel line reaching two cells, then a brief gold notch at the confirmed hit. |
| Red Tempest | Two low ember crescents followed by a larger sweep on the Momentum spend. |
| Stoneguard | Grounded bronze shield wedge facing the source and a distinct rim around the shielded ally. |
| Ashweaver | Ember stencil on owned fire cells. The outline tightens before Ash Seal resolves. |
| Rimecaller | Jagged, pale cell edges and a sharp crack at a successful Chill spend. |
| Stormwright | Angular forks that connect the actual struck targets, with a short-lived mark at the relay tile. |
| Lanternkeeper | Soft lantern ring and paper ribbons around the recipient, with a different outline for harmful zones. |
| Venom Scribe | Stippled vapor for green poison and red engraved ticks for the armour debuff. Use different shapes as well as colours. |
| Spirit Binder | Brushstroke tether and a stamped destination marker for the pet command. |

Blizzard's published Chain Lightning is a useful readability reference for showing chained victims. Golem Mastery is a useful reference for giving a summon path its own identity. Use these as mechanics references with CrystalM2's own art and rules. [Diablo II Chain Lightning](https://classic.battle.net/diablo2exp/skills/sorceress-lightning.shtml#chainlightning) · [Diablo II Golem Mastery](https://classic.battle.net/diablo2exp/skills/necromancer-summoning.shtml#golemmastery)

## First playable slice

Start with a test-server Warrior behind a feature toggle. Build the idempotent unlock reconciler, import existing rank records, expose passive and locked state in Character → Skills, and make active slot eligibility server authoritative. Give the Warrior the shared kit plus the three spec choices with one reliable setup and signature loop each. Prototype the telegraph and impact for those three signatures using one atlas. Record baseline and candidate results on fresh and existing characters before retiring books or rolling the system out to Taoist and Wizard. Use the deployed definition export to set the real unlock levels and tune balance.
