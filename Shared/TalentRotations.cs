public enum RotationEvent : byte
{
    Hit, Spend, Move, Mitigate, WardHit, Heal, Wick, Dispel, Activate, Poison, AdvancePoison, Recall, Focus, CompanionHit
}

public enum RotationTarget : byte { Any, Capture, Same, Different }

public sealed record RotationStep(RotationEvent Event, Spell Spell, RotationTarget Target, byte Minimum, byte Variant, string Hint)
{
    public bool Matches(RotationEvent kind, Spell spell, int amount, int variant, uint current, uint target) =>
        kind == Event && (Spell == Spell.None || TalentCatalog.Family(spell) == TalentCatalog.Family(Spell) ||
            (Spell == Spell.SpiritSpark && spell == Spell.SoulFireBall)) &&
        amount >= Minimum && (Variant == 0 || Variant == variant) && (Target switch
        {
            RotationTarget.Capture => target != 0,
            RotationTarget.Same => target != 0 && target == current,
            RotationTarget.Different => target != 0 && target != current,
            _ => true
        });
}

public sealed record TalentRotation(Spell Finisher, ushort Window, bool TargetBound, RotationStep[] Steps);

public static class TalentRotations
{
    public const int ReadyTime = 6000;
    public const byte RedDose = 1, GreenDose = 2, FarHit = 3, Isolated = 4;

    public static Spell ActionForFinisher(Spell finisher, int engine) => finisher switch
    {
        Spell.Forgefall => engine is 1 or 2 ? Spell.Notchbreaker : Spell.ArcaneHammer,
        Spell.Drakefall => engine == 2 ? Spell.GaleCleaver : Spell.RedWake,
        Spell.Gatebreaker => Spell.BulwarkSlam,
        Spell.FurnaceHeart => Spell.AshSeal,
        Spell.WinterglassVolley => engine == 1 ? Spell.GlacialGate : Spell.FractureSpear,
        Spell.Skywire => Spell.StormRelay,
        Spell.DawnCascade => Spell.LanternLink,
        Spell.ScarletCrucible => Spell.Distil,
        Spell.AncestorsRend => Spell.SpiritPounce,
        _ => Spell.None
    };

    public static bool IsFinisher(Spell spell) => ActionForFinisher(spell, -1) != Spell.None;
    private static readonly TalentRotation[][] Recipes =
    [
        [
            R(Spell.Forgefall, 6500, true, S(RotationEvent.Spend, "Spend Opening with Arcane Hammer", Spell.ArcaneHammer, RotationTarget.Capture)),
            R(Spell.Forgefall, 6500, true, S(RotationEvent.Hit, "Land Thrusting at two tiles", Spell.Thrusting, RotationTarget.Capture, variant: FarHit),
                S(RotationEvent.Spend, "Spend Opening with Arcane Hammer", Spell.ArcaneHammer, RotationTarget.Same)),
            R(Spell.Forgefall, 6500, true, S(RotationEvent.Hit, "Land Flaming Sword", Spell.FlamingSword, RotationTarget.Capture),
                S(RotationEvent.Spend, "Spend Opening with Notchbreaker", Spell.Notchbreaker, RotationTarget.Same)),
            R(Spell.Forgefall, 9000, true, S(RotationEvent.Hit, "Land Steel Cut", Spell.SteelCut, RotationTarget.Capture),
                S(RotationEvent.Hit, "Land Thrusting at two tiles", Spell.Thrusting, RotationTarget.Same, variant: FarHit),
                S(RotationEvent.Spend, "Spend Opening with Notchbreaker", Spell.Notchbreaker, RotationTarget.Same))
        ],
        [
            R(Spell.Drakefall, 8000, false, S(RotationEvent.Spend, "Spend Momentum with Red Wake", Spell.RedWake)),
            R(Spell.Drakefall, 8000, false, S(RotationEvent.Hit, "Land Half Moon", Spell.HalfMoon),
                S(RotationEvent.Hit, "Hit two enemies with Gale Cleaver", Spell.GaleCleaver, minimum: 2),
                S(RotationEvent.Spend, "Spend two Momentum with Red Wake", Spell.RedWake, minimum: 2)),
            R(Spell.Drakefall, 8000, true, S(RotationEvent.Hit, "Land Half Moon", Spell.HalfMoon, RotationTarget.Capture),
                S(RotationEvent.Hit, "Land the Twin Drake opening", Spell.TwinDrakeBlade, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Momentum with Red Wake", Spell.RedWake, RotationTarget.Same)),
            R(Spell.Drakefall, 6000, false, S(RotationEvent.Hit, "Land Gale Cleaver", Spell.GaleCleaver),
                S(RotationEvent.Move, "Walk or run two legal tiles", minimum: 2), S(RotationEvent.Hit, "Land Half Moon", Spell.HalfMoon))
        ],
        [
            R(Spell.Gatebreaker, 8000, false, S(RotationEvent.WardHit, "Absorb a hit with a Guard-powered Interpose", Spell.Interpose)),
            R(Spell.Gatebreaker, 9000, false, S(RotationEvent.Hit, "Land Bulwark Slam", Spell.BulwarkSlam, RotationTarget.Capture),
                S(RotationEvent.Mitigate, "Mitigate the target's attack under Protection Field", Spell.ProtectionField, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Guard with Interpose on yourself", Spell.Interpose, variant: Isolated)),
            R(Spell.Gatebreaker, 10000, false, S(RotationEvent.Activate, "Activate Protection Field", Spell.ProtectionField),
                S(RotationEvent.Mitigate, "Mitigate a hostile attack", Spell.ProtectionField),
                S(RotationEvent.Mitigate, "Mitigate another hostile attack", Spell.ProtectionField),
                S(RotationEvent.Spend, "Spend Guard with Interpose on yourself", Spell.Interpose, variant: Isolated)),
            R(Spell.Gatebreaker, 9000, false, S(RotationEvent.Spend, "Spend Guard with Interpose on an ally", Spell.Interpose, variant: 1),
                S(RotationEvent.WardHit, "Absorb an attack on your ally", Spell.Interpose, RotationTarget.Capture, variant: 1),
                S(RotationEvent.Hit, "Land Bulwark Slam on that attacker", Spell.BulwarkSlam, RotationTarget.Same))
        ],
        [
            R(Spell.FurnaceHeart, 8000, false, S(RotationEvent.Spend, "Hit a Singed enemy with Ember-powered Ash Seal", Spell.AshSeal, variant: 1)),
            R(Spell.FurnaceHeart, 9000, true, S(RotationEvent.Hit, "Apply Singe with Cinder Lance", Spell.CinderLance, RotationTarget.Capture),
                S(RotationEvent.Hit, "Hit that enemy with your fire bolt", Spell.FireBall, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Embers with Ash Seal", Spell.AshSeal, RotationTarget.Same, variant: 1)),
            R(Spell.FurnaceHeart, 10000, false, S(RotationEvent.Hit, "Burn an enemy with Fire Wall", Spell.FireWall),
                S(RotationEvent.Spend, "Spend two Embers with Ash Seal", Spell.AshSeal, minimum: 2)),
            R(Spell.FurnaceHeart, 6000, true, S(RotationEvent.Hit, "Displace an enemy with Repulsion", Spell.Repulsion, RotationTarget.Capture),
                S(RotationEvent.Hit, "Hit that enemy with your fire bolt", Spell.FireBall, RotationTarget.Same),
                S(RotationEvent.Hit, "Land Cinder Lance", Spell.CinderLance, RotationTarget.Same))
        ],
        [
            R(Spell.WinterglassVolley, 6500, true, S(RotationEvent.Spend, "Shatter Rime with the Spear family", Spell.FractureSpear, RotationTarget.Capture)),
            R(Spell.WinterglassVolley, 8000, true, S(RotationEvent.Hit, "Mark with Frost Crunch", Spell.FrostCrunch, RotationTarget.Capture),
                S(RotationEvent.Move, "Take a step away"),
                S(RotationEvent.Spend, "Shatter from the Spear's far band", Spell.FractureSpear, RotationTarget.Same, variant: FarHit)),
            R(Spell.WinterglassVolley, 9000, false, S(RotationEvent.Hit, "Hit two enemies with Ice Storm", Spell.IceStorm, minimum: 2),
                S(RotationEvent.Hit, "Mark a target with Frost Crunch", Spell.FrostCrunch, RotationTarget.Capture),
                S(RotationEvent.Spend, "Shatter that mark with Glacial Gate", Spell.GlacialGate, RotationTarget.Same)),
            R(Spell.WinterglassVolley, 8000, true, S(RotationEvent.Mitigate, "Mitigate a hit under Magic Shield", Spell.MagicShield, RotationTarget.Capture),
                S(RotationEvent.Hit, "Mark the attacker with Frost Crunch", Spell.FrostCrunch, RotationTarget.Same),
                S(RotationEvent.Spend, "Shatter the attacker's Rime", target: RotationTarget.Same))
        ],
        [
            R(Spell.Skywire, 8000, true, S(RotationEvent.Spend, "Spend Charge with Storm Relay", Spell.StormRelay, RotationTarget.Capture)),
            R(Spell.Skywire, 8000, false, S(RotationEvent.Hit, "Land Thunder Bolt", Spell.ThunderBolt),
                S(RotationEvent.Hit, "Hit two enemies with Lightning", Spell.Lightning, minimum: 2),
                S(RotationEvent.Spend, "Spend Charge on a Relay bounce", Spell.StormRelay, variant: 1)),
            R(Spell.Skywire, 4000, true, S(RotationEvent.Hit, "Mark the enemy with Arc Javelin", Spell.ArcJavelin, RotationTarget.Capture),
                S(RotationEvent.Hit, "Land Thunder Bolt on that enemy", Spell.ThunderBolt, RotationTarget.Same),
                S(RotationEvent.Spend, "Discharge Relay on the isolated target", Spell.StormRelay, RotationTarget.Same, variant: Isolated)),
            R(Spell.Skywire, 8000, false, S(RotationEvent.Mitigate, "Mitigate a hit under Magic Shield", Spell.MagicShield),
                S(RotationEvent.Hit, "Land Lightning", Spell.Lightning), S(RotationEvent.Spend, "Spend Charge with Storm Relay", Spell.StormRelay))
        ],
        [
            R(Spell.DawnCascade, 10000, false, S(RotationEvent.WardHit, "Absorb a hit with a Reserve ward", Spell.LanternLink)),
            R(Spell.DawnCascade, 10000, true, S(RotationEvent.Activate, "Mark an ally with Guiding Wick", Spell.GuidingWick, RotationTarget.Capture),
                S(RotationEvent.Wick, "Transfer effective healing to the Wick ally", Spell.GuidingWick, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Reserve on that ally", Spell.LanternLink, RotationTarget.Same)),
            R(Spell.DawnCascade, 8000, true, S(RotationEvent.Dispel, "Remove an eligible effect", target: RotationTarget.Capture),
                S(RotationEvent.Heal, "Restore health to that ally", Spell.Healing, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Reserve on that ally", Spell.LanternLink, RotationTarget.Same)),
            R(Spell.DawnCascade, 10000, true, S(RotationEvent.Heal, "Restore missing health"),
                S(RotationEvent.Heal, "Restore health with another heal"),
                S(RotationEvent.Spend, "Spend two Reserve with Lantern Link", Spell.LanternLink, RotationTarget.Capture, 2),
                S(RotationEvent.WardHit, "Absorb a hostile hit on that ally", Spell.LanternLink, RotationTarget.Same))
        ],
        [
            R(Spell.ScarletCrucible, 6500, true, S(RotationEvent.Spend, "Spend Fresh Ink with Distil", Spell.Distil, RotationTarget.Capture)),
            R(Spell.ScarletCrucible, 9000, true, S(RotationEvent.Poison, "Apply green Poisoning", Spell.Poisoning, RotationTarget.Capture, variant: GreenDose),
                S(RotationEvent.AdvancePoison, "Advance a tick with Serpent Needle", Spell.SerpentNeedle, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Fresh Ink with green Distil", Spell.Distil, RotationTarget.Same, variant: GreenDose)),
            R(Spell.ScarletCrucible, 8000, true, S(RotationEvent.Poison, "Apply red Poisoning", Spell.Poisoning, RotationTarget.Capture, variant: RedDose),
                S(RotationEvent.Hit, "Land Curse", Spell.Curse, RotationTarget.Same),
                S(RotationEvent.Spend, "Spend Fresh Ink with red Distil", Spell.Distil, RotationTarget.Same, variant: RedDose)),
            R(Spell.ScarletCrucible, 9000, true, S(RotationEvent.Poison, "Apply green Poisoning", Spell.Poisoning, RotationTarget.Capture, variant: GreenDose),
                S(RotationEvent.Poison, "Poison a different enemy", Spell.Poisoning, RotationTarget.Different, variant: GreenDose),
                S(RotationEvent.Spend, "Distil the second enemy's poison", Spell.Distil, RotationTarget.Same, variant: GreenDose))
        ],
        [
            R(Spell.AncestorsRend, 6000, true, S(RotationEvent.Hit, "Land Spirit Pounce", Spell.SpiritPounce, RotationTarget.Capture),
                S(RotationEvent.Hit, "Hit the same enemy with an owner bolt", Spell.SpiritSpark, RotationTarget.Same)),
            R(Spell.AncestorsRend, 10000, false, S(RotationEvent.Hit, "Land an owner bolt", Spell.SpiritSpark),
                S(RotationEvent.Recall, "Spend Bond on a Recall mend", Spell.SpiritCommand), S(RotationEvent.Hit, "Land Spirit Pounce", Spell.SpiritPounce)),
            R(Spell.AncestorsRend, 8000, true, S(RotationEvent.Focus, "Order the spirit to focus", Spell.SpiritCommand, RotationTarget.Capture),
                S(RotationEvent.Hit, "Land Spirit Pounce", Spell.SpiritPounce, RotationTarget.Same),
                S(RotationEvent.Hit, "Hit the same enemy with an owner bolt", Spell.SpiritSpark, RotationTarget.Same)),
            R(Spell.AncestorsRend, 7000, true, S(RotationEvent.Hit, "Land Spirit Pounce", Spell.SpiritPounce, RotationTarget.Capture),
                S(RotationEvent.Hit, "Hit the same enemy with an owner bolt", Spell.SpiritSpark, RotationTarget.Same),
                S(RotationEvent.CompanionHit, "Confirm an ordinary spirit attack", target: RotationTarget.Same))
        ]
    ];

    public static TalentRotation Get(MirClass @class, CombatSpecialisation spec, int engine) =>
        TalentCatalog.Supports(@class) && spec is >= CombatSpecialisation.First and <= CombatSpecialisation.Third && engine is >= -1 and <= 2
            ? Recipes[(int)@class * 3 + (int)spec - 1][engine + 1] : null;

    private static TalentRotation R(Spell spell, ushort window, bool bound, params RotationStep[] steps) => new(spell, window, bound, steps);
    private static RotationStep S(RotationEvent kind, string hint, Spell spell = Spell.None,
        RotationTarget target = RotationTarget.Any, byte minimum = 1, byte variant = 0) => new(kind, spell, target, minimum, variant, hint);
}
