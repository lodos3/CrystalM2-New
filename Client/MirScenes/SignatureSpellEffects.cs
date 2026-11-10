using System.Numerics;
using CrystalM2.Rendering.Core;
using Shape = CrystalM2.Rendering.Core.SpellParticleShape;
using Layer = CrystalM2.Rendering.Core.SpellParticleLayer;

namespace Client.MirScenes;

internal static class SignatureSpellEffects
{
    internal enum Form { Cut, Cleave, Hammer, Rush, Bolt, Arc, Eruption, Ice, Ward, Heal, Bind, Veil, Break, Totem }

    internal readonly record struct Look(Form Form, Shape Shape, int Size, Color Colour, Color Core);

    private static Look Profile(SpecialisationAbility ability)
    {
        var tuning = SpellVfxAssets.Current.Signature.Profile;
        var fields = SpellVfxAssets.Fields("Signature", "Profile");
        (Form form, Shape shape, int size) = ability.Spell switch
        {
            Spell.SteelCut or Spell.QuickCut => (fields.Read(tuning.SteelCutForm), fields.Read(tuning.SteelCutShape), fields.Read(tuning.SteelCutSize)),
            Spell.Notchbreaker => (fields.Read(tuning.NotchbreakerForm), fields.Read(tuning.NotchbreakerShape), fields.Read(tuning.NotchbreakerSize)),
            Spell.Oathbrand => (fields.Read(tuning.OathbrandForm), fields.Read(tuning.OathbrandShape), fields.Read(tuning.OathbrandSize)),
            Spell.VeilStrike or Spell.Riposte => (fields.Read(tuning.VeilStrikeForm), fields.Read(tuning.VeilStrikeShape), fields.Read(tuning.VeilStrikeSize)),
            Spell.GaleCleaver => (fields.Read(tuning.GaleCleaverForm), fields.Read(tuning.GaleCleaverShape), fields.Read(tuning.GaleCleaverSize)),
            Spell.RedWake => (fields.Read(tuning.RedWakeForm), fields.Read(tuning.RedWakeShape), fields.Read(tuning.RedWakeSize)),
            Spell.DrakesEcho => (fields.Read(tuning.DrakesEchoForm), fields.Read(tuning.DrakesEchoShape), fields.Read(tuning.DrakesEchoSize)),
            Spell.Drakefall => (fields.Read(tuning.DrakefallForm), fields.Read(tuning.DrakefallShape), fields.Read(tuning.DrakefallSize)),
            Spell.BulwarkSlam => (fields.Read(tuning.BulwarkSlamForm), fields.Read(tuning.BulwarkSlamShape), fields.Read(tuning.BulwarkSlamSize)),
            Spell.ArcaneHammer => (fields.Read(tuning.ArcaneHammerForm), fields.Read(tuning.ArcaneHammerShape), fields.Read(tuning.ArcaneHammerSize)),
            Spell.Forgefall => (fields.Read(tuning.ForgefallForm), fields.Read(tuning.ForgefallShape), fields.Read(tuning.ForgefallSize)),
            Spell.Gatebreaker => (fields.Read(tuning.GatebreakerForm), fields.Read(tuning.GatebreakerShape), fields.Read(tuning.GatebreakerSize)),
            Spell.Bloodrush => (fields.Read(tuning.BloodrushForm), fields.Read(tuning.BloodrushShape), fields.Read(tuning.BloodrushSize)),
            Spell.Oathstep => (fields.Read(tuning.OathstepForm), fields.Read(tuning.OathstepShape), fields.Read(tuning.OathstepSize)),
            Spell.PrismStep => (fields.Read(tuning.PrismStepForm), fields.Read(tuning.PrismStepShape), fields.Read(tuning.PrismStepSize)),
            Spell.SpiritPounce => (fields.Read(tuning.SpiritPounceForm), fields.Read(tuning.SpiritPounceShape), fields.Read(tuning.SpiritPounceSize)),
            Spell.AncestralRelay => (fields.Read(tuning.AncestralRelayForm), fields.Read(tuning.AncestralRelayShape), fields.Read(tuning.AncestralRelaySize)),
            Spell.CinderLance => (fields.Read(tuning.CinderLanceForm), fields.Read(tuning.CinderLanceShape), fields.Read(tuning.CinderLanceSize)),
            Spell.FractureSpear => (fields.Read(tuning.FractureSpearForm), fields.Read(tuning.FractureSpearShape), fields.Read(tuning.FractureSpearSize)),
            Spell.Pinpoint => (fields.Read(tuning.PinpointForm), fields.Read(tuning.PinpointShape), fields.Read(tuning.PinpointSize)),
            Spell.SerpentNeedle => (fields.Read(tuning.SerpentNeedleForm), fields.Read(tuning.SerpentNeedleShape), fields.Read(tuning.SerpentNeedleSize)),
            Spell.SpiritSpark => (fields.Read(tuning.SpiritSparkForm), fields.Read(tuning.SpiritSparkShape), fields.Read(tuning.SpiritSparkSize)),
            Spell.AncestorsRend => (fields.Read(tuning.AncestorsRendForm), fields.Read(tuning.AncestorsRendShape), fields.Read(tuning.AncestorsRendSize)),
            Spell.ArcJavelin => (fields.Read(tuning.ArcJavelinForm), fields.Read(tuning.ArcJavelinShape), fields.Read(tuning.ArcJavelinSize)),
            Spell.StormRelay => (fields.Read(tuning.StormRelayForm), fields.Read(tuning.StormRelayShape), fields.Read(tuning.StormRelaySize)),
            Spell.Skywire => (fields.Read(tuning.SkywireForm), fields.Read(tuning.SkywireShape), fields.Read(tuning.SkywireSize)),
            Spell.ThunderNail => (fields.Read(tuning.ThunderNailForm), fields.Read(tuning.ThunderNailShape), fields.Read(tuning.ThunderNailSize)),
            Spell.AshSeal => (fields.Read(tuning.AshSealForm), fields.Read(tuning.AshSealShape), fields.Read(tuning.AshSealSize)),
            Spell.KilnBurst => (fields.Read(tuning.KilnBurstForm), fields.Read(tuning.KilnBurstShape), fields.Read(tuning.KilnBurstSize)),
            Spell.FurnaceHeart => (fields.Read(tuning.FurnaceHeartForm), fields.Read(tuning.FurnaceHeartShape), fields.Read(tuning.FurnaceHeartSize)),
            Spell.CinderMine => (fields.Read(tuning.CinderMineForm), fields.Read(tuning.CinderMineShape), fields.Read(tuning.CinderMineSize)),
            Spell.ScarletCrucible => (fields.Read(tuning.ScarletCrucibleForm), fields.Read(tuning.ScarletCrucibleShape), fields.Read(tuning.ScarletCrucibleSize)),
            Spell.GlacialGate => (fields.Read(tuning.GlacialGateForm), fields.Read(tuning.GlacialGateShape), fields.Read(tuning.GlacialGateSize)),
            Spell.WinterglassVolley => (fields.Read(tuning.WinterglassVolleyForm), fields.Read(tuning.WinterglassVolleyShape), fields.Read(tuning.WinterglassVolleySize)),
            Spell.IceManacle => (fields.Read(tuning.IceManacleForm), fields.Read(tuning.IceManacleShape), fields.Read(tuning.IceManacleSize)),
            Spell.SleetSnare => (fields.Read(tuning.SleetSnareForm), fields.Read(tuning.SleetSnareShape), fields.Read(tuning.SleetSnareSize)),
            Spell.Interpose => (fields.Read(tuning.InterposeForm), fields.Read(tuning.InterposeShape), fields.Read(tuning.InterposeSize)),
            Spell.GuardianTether => (fields.Read(tuning.GuardianTetherForm), fields.Read(tuning.GuardianTetherShape), fields.Read(tuning.GuardianTetherSize)),
            Spell.PaperWard => (fields.Read(tuning.PaperWardForm), fields.Read(tuning.PaperWardShape), fields.Read(tuning.PaperWardSize)),
            Spell.IceglassRefuge => (fields.Read(tuning.IceglassRefugeForm), fields.Read(tuning.IceglassRefugeShape), fields.Read(tuning.IceglassRefugeSize)),
            Spell.PhoenixShroud => (fields.Read(tuning.PhoenixShroudForm), fields.Read(tuning.PhoenixShroudShape), fields.Read(tuning.PhoenixShroudSize)),
            Spell.StaticMantle => (fields.Read(tuning.StaticMantleForm), fields.Read(tuning.StaticMantleShape), fields.Read(tuning.StaticMantleSize)),
            Spell.EchoFang => (fields.Read(tuning.EchoFangForm), fields.Read(tuning.EchoFangShape), fields.Read(tuning.EchoFangSize)),
            Spell.LanternLink => (fields.Read(tuning.LanternLinkForm), fields.Read(tuning.LanternLinkShape), fields.Read(tuning.LanternLinkSize)),
            Spell.GuidingWick => (fields.Read(tuning.GuidingWickForm), fields.Read(tuning.GuidingWickShape), fields.Read(tuning.GuidingWickSize)),
            Spell.TendingBell => (fields.Read(tuning.TendingBellForm), fields.Read(tuning.TendingBellShape), fields.Read(tuning.TendingBellSize)),
            Spell.DawnCascade => (fields.Read(tuning.DawnCascadeForm), fields.Read(tuning.DawnCascadeShape), fields.Read(tuning.DawnCascadeSize)),
            Spell.SpiritCommand or Spell.PackSignal => (fields.Read(tuning.SpiritCommandForm), fields.Read(tuning.SpiritCommandShape), fields.Read(tuning.SpiritCommandSize)),
            Spell.PommelLock => (fields.Read(tuning.PommelLockForm), fields.Read(tuning.PommelLockShape), fields.Read(tuning.PommelLockSize)),
            Spell.HeelHook => (fields.Read(tuning.HeelHookForm), fields.Read(tuning.HeelHookShape), fields.Read(tuning.HeelHookSize)),
            Spell.ShieldLock => (fields.Read(tuning.ShieldLockForm), fields.Read(tuning.ShieldLockShape), fields.Read(tuning.ShieldLockSize)),
            Spell.IronChallenge => (fields.Read(tuning.IronChallengeForm), fields.Read(tuning.IronChallengeShape), fields.Read(tuning.IronChallengeSize)),
            Spell.LanternEdict => (fields.Read(tuning.LanternEdictForm), fields.Read(tuning.LanternEdictShape), fields.Read(tuning.LanternEdictSize)),
            Spell.TendonSeal => (fields.Read(tuning.TendonSealForm), fields.Read(tuning.TendonSealShape), fields.Read(tuning.TendonSealSize)),
            Spell.AncestorsGrasp => (fields.Read(tuning.AncestorsGraspForm), fields.Read(tuning.AncestorsGraspShape), fields.Read(tuning.AncestorsGraspSize)),
            Spell.CausticScript => (fields.Read(tuning.CausticScriptForm), fields.Read(tuning.CausticScriptShape), fields.Read(tuning.CausticScriptSize)),
            Spell.Distil => (fields.Read(tuning.DistilForm), fields.Read(tuning.DistilShape), fields.Read(tuning.DistilSize)),
            Spell.SmokeChoke or Spell.SmokeSnare => (fields.Read(tuning.SmokeChokeForm), fields.Read(tuning.SmokeChokeShape), fields.Read(tuning.SmokeChokeSize)),
            Spell.CrossguardBreak => (fields.Read(tuning.CrossguardBreakForm), fields.Read(tuning.CrossguardBreakShape), fields.Read(tuning.CrossguardBreakSize)),
            Spell.RedSlip => (fields.Read(tuning.RedSlipForm), fields.Read(tuning.RedSlipShape), fields.Read(tuning.RedSlipSize)),
            Spell.BastionBreak => (fields.Read(tuning.BastionBreakForm), fields.Read(tuning.BastionBreakShape), fields.Read(tuning.BastionBreakSize)),
            Spell.ShedCinders => (fields.Read(tuning.ShedCindersForm), fields.Read(tuning.ShedCindersShape), fields.Read(tuning.ShedCindersSize)),
            Spell.Thawskin => (fields.Read(tuning.ThawskinForm), fields.Read(tuning.ThawskinShape), fields.Read(tuning.ThawskinSize)),
            Spell.GroundingStep => (fields.Read(tuning.GroundingStepForm), fields.Read(tuning.GroundingStepShape), fields.Read(tuning.GroundingStepSize)),
            Spell.UnbindKnot => (fields.Read(tuning.UnbindKnotForm), fields.Read(tuning.UnbindKnotShape), fields.Read(tuning.UnbindKnotSize)),
            Spell.AntivenomVeil => (fields.Read(tuning.AntivenomVeilForm), fields.Read(tuning.AntivenomVeilShape), fields.Read(tuning.AntivenomVeilSize)),
            Spell.SoulUnshackle => (fields.Read(tuning.SoulUnshackleForm), fields.Read(tuning.SoulUnshackleShape), fields.Read(tuning.SoulUnshackleSize)),
            Spell.RallyingStandard => (fields.Read(tuning.RallyingStandardForm), fields.Read(tuning.RallyingStandardShape), fields.Read(tuning.RallyingStandardSize)),
            Spell.FluxPylon => (fields.Read(tuning.FluxPylonForm), fields.Read(tuning.FluxPylonShape), fields.Read(tuning.FluxPylonSize)),
            Spell.MiasmaVessel => (fields.Read(tuning.MiasmaVesselForm), fields.Read(tuning.MiasmaVesselShape), fields.Read(tuning.MiasmaVesselSize)),
            Spell.SanctuaryLantern => (fields.Read(tuning.SanctuaryLanternForm), fields.Read(tuning.SanctuaryLanternShape), fields.Read(tuning.SanctuaryLanternSize)),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability.Spell, "Missing signature particle profile")
        };
        (Color colour, Color core) = ability.Theme switch
        {
            AbilityTheme.Steel => (fields.Read(tuning.SteelColour), fields.Read(tuning.SteelColour2)),
            AbilityTheme.Blood => (fields.Read(tuning.BloodColour), fields.Read(tuning.BloodColour2)),
            AbilityTheme.Ward => (fields.Read(tuning.WardColour), fields.Read(tuning.WardColour2)),
            AbilityTheme.Flame => (fields.Read(tuning.FlameColour), fields.Read(tuning.FlameColour2)),
            AbilityTheme.Frost => (fields.Read(tuning.FrostColour), fields.Read(tuning.FrostColour2)),
            AbilityTheme.Storm => (fields.Read(tuning.StormColour), fields.Read(tuning.StormColour2)),
            AbilityTheme.Spirit => (fields.Read(tuning.SpiritColour), fields.Read(tuning.SpiritColour2)),
            AbilityTheme.Venom => (fields.Read(tuning.VenomColour), fields.Read(tuning.VenomColour2)),
            AbilityTheme.Shadow => (fields.Read(tuning.ShadowColour), fields.Read(tuning.ShadowColour2)),
            _ => (fields.Read(tuning.Colour), fields.Read(tuning.Colour2))
        };
        if (ability.Spell is Spell.ArcaneHammer or Spell.Forgefall) colour = fields.Read(tuning.Colour3);
        if (ability.Spell == Spell.ScarletCrucible) colour = fields.Read(tuning.Colour4);
        return new Look(form, shape, size, colour, core);
    }

    internal static void Cast(SpellParticlePool pool, SpecialisationAbility ability, Vector2 origin, Vector2 target,
        MirDirection direction, long now)
    {
        using var vfxScope = SpellVfxAssets.For(ability.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.Cast);
        var look = Profile(ability);
        var vfx = new Effect(pool, look, now);
        int windup = Math.Max(tuning.Windup, (int)ability.Windup);
        float angle = ((int)direction - tuning.Angle) * MathF.PI / 4;
        Vector2 facing = new(MathF.Cos(angle), MathF.Sin(angle) * tuning.Facing);
        Vector2 hand = origin + facing * tuning.Hand - Vector2.UnitY * tuning.Hand2;
        Vector2 end = target - Vector2.UnitY * tuning.End;
        switch (look.Form)
        {
            case Form.Bolt when target != origin:
                vfx.Projectile(hand, end, windup, look.Size, ability.Spell == Spell.AncestorsRend, emitter: "SignatureSpellEffects.Cast.1");
                break;
            case Form.Arc when target != origin:
                vfx.Lightning(hand, end, windup, look.Size, emitter: "SignatureSpellEffects.Cast.2");
                break;
            case Form.Cut:
            case Form.Cleave:
                vfx.Add(hand + facing * tuning.CleaveSlashOffset, facing * tuning.CleaveSlashVelocity, tuning.CleaveSlashShape, look.Size, tuning.CleaveSlashDuration, tuning.CleaveSlashLayer,
                    (byte)direction, tuning.CleaveSlashAspect, delay: Math.Max(0, windup - tuning.CleaveVal2), emitter: "SignatureSpellEffects.Cast.3");
                if (look.Form == Form.Cleave)
                    vfx.Add(hand + facing * tuning.CleaveAddOffset, facing * tuning.CleaveAddVelocity, look.Shape, look.Size * tuning.CleaveAddSize / 4, tuning.CleaveAddDuration, tuning.CleaveAddLayer,
                        (byte)direction, tuning.CleaveAddAspect, delay: Math.Max(0, windup - tuning.CleaveVal22), core: tuning.CleaveAddCore, emitter: "SignatureSpellEffects.Cast.4");
                break;
            case Form.Hammer:
                vfx.Add(end - Vector2.UnitY * tuning.HammerAddOffset, new Vector2(0, tuning.HammerY), look.Shape, look.Size, Math.Min(windup, tuning.HammerVal2),
                    tuning.HammerAddLayer, aspect: tuning.HammerAddAspect, rotation: tuning.HammerAddRotation + facing.X * tuning.HammerAddRotation2, delay: Math.Max(0, windup - tuning.HammerVal22), linear: true, emitter: "SignatureSpellEffects.Cast.5");
                break;
            case Form.Rush:
                if (target != origin) vfx.Projectile(hand, end, windup, look.Size * tuning.RushSize / 4, false, emitter: "SignatureSpellEffects.Cast.6");
                break;
            case Form.Heal:
            case Form.Ward:
                vfx.Add(hand, -Vector2.UnitY * tuning.WardAddVelocity, look.Shape, tuning.WardAddSize, windup + tuning.WardAddDuration, tuning.WardAddLayer, emitter: "SignatureSpellEffects.Cast.7");
                if (target != origin && ability.Target == AbilityTarget.Ally) vfx.Projectile(hand, end, windup, tuning.WardSize, false, emitter: "SignatureSpellEffects.Cast.8");
                break;
            case Form.Break:
                return; // The instantaneous counter has an impact packet at the actor who breaks free.
            default:
                vfx.Add(hand, -Vector2.UnitY * tuning.GlyphVelocity, tuning.GlyphShape, tuning.GlyphSize, windup + tuning.GlyphDuration, tuning.GlyphLayer, growth: tuning.GlyphGrowth, emitter: "SignatureSpellEffects.Cast.9");
                break;
        }
        vfx.Add(hand, facing * tuning.FlashVelocity, tuning.FlashShape, tuning.FlashSize, Math.Min(windup + tuning.Val1, tuning.Val2), tuning.FlashLayer, core: tuning.FlashCore, emitter: "SignatureSpellEffects.Cast.10");
    }

    internal static void Cell(SpellParticlePool pool, SpecialisationAbility ability, Vector2 cell, SpellEffect phase, long now)
    {
        using var vfxScope = SpellVfxAssets.For(ability.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.Cell);
        var look = Profile(ability);
        var vfx = new Effect(pool, look, now);
        if (phase == SpellEffect.SignatureWindup)
        {
            if (ability.Windup == 0) return;
            vfx.Add(cell, Vector2.Zero, tuning.CellMarkShape, tuning.CellMarkSize, ability.Windup + tuning.CellMarkDuration, tuning.CellMarkLayer, aspect: tuning.CellMarkAspect, alpha: tuning.CellMarkAlpha, emitter: "SignatureSpellEffects.Cell.1");
            return;
        }
        if (phase == SpellEffect.SignatureArmed)
        {
            vfx.Ring(cell, look.Size, tuning.RingDuration, emitter: "SignatureSpellEffects.Cell.2");
            vfx.Add(cell - Vector2.UnitY * tuning.AddOffset, -Vector2.UnitY * tuning.AddVelocity, look.Shape, look.Size * tuning.AddSize / tuning.AddSizeDivisor,
                ability.Spell is Spell.FluxPylon or Spell.MiasmaVessel ? tuning.AddDuration : tuning.AddDuration2, tuning.AddLayer, emitter: "SignatureSpellEffects.Cell.3");
            return;
        }

        Vector2 body = cell - Vector2.UnitY * tuning.Body;
        switch (look.Form)
        {
            case Form.Cut:
            case Form.Cleave:
            case Form.Rush:
                vfx.Add(body, new Vector2(tuning.RushX, tuning.RushY), look.Shape, look.Size, tuning.RushAddDuration, tuning.RushAddLayer, tuning.RushAddDirection, tuning.RushAddAspect, rotation: tuning.RushAddRotation, emitter: "SignatureSpellEffects.Cell.4");
                vfx.Add(body, -Vector2.UnitY * tuning.RushFlashVelocity, tuning.RushFlashShape, look.Size / 2, tuning.RushFlashDuration, tuning.RushFlashLayer, core: tuning.RushFlashCore, emitter: "SignatureSpellEffects.Cell.5");
                vfx.Scatter(body, tuning.RushSparkCount, tuning.RushSparkShape, look.Size * tuning.RushSparkSpread, tuning.RushSparkDuration, emitter: "SignatureSpellEffects.Cell.6");
                break;
            case Form.Hammer:
                vfx.Ring(cell, look.Size + tuning.HammerRingSize, tuning.HammerRingDuration, emitter: "SignatureSpellEffects.Cell.7");
                vfx.Add(body - Vector2.UnitY * tuning.HammerAddOffset, Vector2.UnitY * tuning.HammerAddVelocity, look.Shape, look.Size * tuning.HammerAddSize / 3, tuning.HammerAddDuration, tuning.HammerAddLayer, rotation: tuning.HammerAddRotation, emitter: "SignatureSpellEffects.Cell.8");
                vfx.Add(body, -Vector2.UnitY * tuning.HammerFlashVelocity, tuning.HammerFlashShape, look.Size * tuning.HammerFlashSize / tuning.HammerFlashSizeDivisor, tuning.HammerFlashDuration, tuning.HammerFlashLayer, core: tuning.HammerFlashCore, emitter: "SignatureSpellEffects.Cell.9");
                vfx.Scatter(body, tuning.HammerShardCount, tuning.HammerShardShape, look.Size * tuning.HammerShardSpread, tuning.HammerShardDuration, emitter: "SignatureSpellEffects.Cell.10");
                break;
            case Form.Eruption:
            case Form.Arc:
                vfx.Ring(cell, look.Size, tuning.ArcRingDuration, emitter: "SignatureSpellEffects.Cell.11");
                vfx.Add(body - Vector2.UnitY * tuning.ArcPillarOffset, new Vector2(0, tuning.ArcY), tuning.ArcPillarShape, look.Size * tuning.ArcPillarSize / 3, tuning.ArcPillarDuration, tuning.ArcPillarLayer,
                    aspect: tuning.ArcPillarAspect, growth: tuning.ArcPillarGrowth, emitter: "SignatureSpellEffects.Cell.12");
                vfx.Add(body, -Vector2.UnitY * tuning.ArcAddVelocity, look.Shape, look.Size * tuning.ArcAddSize / 3, tuning.ArcAddDuration, tuning.ArcAddLayer, core: tuning.ArcAddCore, emitter: "SignatureSpellEffects.Cell.13");
                vfx.Scatter(body, tuning.ArcShardCount, look.Form == Form.Arc ? tuning.ArcShardShape : tuning.ArcShardShape2, look.Size * tuning.ArcShardSpread, tuning.ArcShardDuration, emitter: "SignatureSpellEffects.Cell.14");
                break;
            case Form.Ice:
                vfx.Ring(cell, look.Size, tuning.IceRingDuration, emitter: "SignatureSpellEffects.Cell.15");
                for (int i = -1; i <= tuning.IceParticleCount; i++)
                    vfx.Add(body + new Vector2(i * tuning.IceX, Math.Abs(i) * tuning.IceY), new Vector2(i * tuning.IceX2, tuning.IceY2), look.Shape,
                        look.Size / (i == 0 ? tuning.IceAddSize : tuning.IceAddSize2), tuning.IceAddDuration, tuning.IceAddLayer, aspect: tuning.IceAddAspect, rotation: i * tuning.IceAddRotation, core: i == 0, emitter: "SignatureSpellEffects.Cell.16");
                vfx.Scatter(body, tuning.IceShardCount, tuning.IceShardShape, tuning.IceShardSpread, tuning.IceShardDuration, emitter: "SignatureSpellEffects.Cell.17");
                break;
            case Form.Heal:
            case Form.Ward:
            case Form.Totem:
                vfx.Ring(cell, look.Size + tuning.TotemRingSize, tuning.TotemRingDuration, emitter: "SignatureSpellEffects.Cell.18");
                vfx.Add(body - Vector2.UnitY * tuning.TotemAddOffset, new Vector2(0, tuning.TotemY), look.Shape, look.Size * tuning.TotemAddSize / 4, tuning.TotemAddDuration, tuning.TotemAddLayer,
                    aspect: tuning.TotemAddAspect, growth: tuning.TotemAddGrowth, emitter: "SignatureSpellEffects.Cell.19");
                vfx.Add(body - Vector2.UnitY * tuning.TotemFlashOffset, -Vector2.UnitY * tuning.TotemFlashVelocity, tuning.TotemFlashShape, look.Size / 2, tuning.TotemFlashDuration, tuning.TotemFlashLayer, core: tuning.TotemFlashCore, emitter: "SignatureSpellEffects.Cell.20");
                vfx.Scatter(body, tuning.TotemFeatherCount, tuning.TotemFeatherShape, look.Size * tuning.TotemFeatherSpread, tuning.TotemFeatherDuration, rise: true, emitter: "SignatureSpellEffects.Cell.21");
                break;
            case Form.Bind:
            case Form.Break:
                vfx.Ring(cell, look.Size, tuning.BreakRingDuration, emitter: "SignatureSpellEffects.Cell.22");
                vfx.Add(body, new Vector2(0, tuning.BreakY), look.Shape, look.Size, tuning.BreakAddDuration, tuning.BreakAddLayer,
                    growth: look.Form == Form.Break ? tuning.BreakAddGrowth : 0, core: tuning.BreakAddCore, emitter: "SignatureSpellEffects.Cell.23");
                vfx.Scatter(body, tuning.BreakShardCount, tuning.BreakShardShape, look.Size * tuning.BreakShardSpread, tuning.BreakShardDuration, emitter: "SignatureSpellEffects.Cell.24");
                break;
            case Form.Veil:
                vfx.Ring(cell, look.Size, tuning.VeilRingDuration, emitter: "SignatureSpellEffects.Cell.25");
                for (int i = -1; i <= tuning.VeilWispCount; i++)
                    vfx.Add(body + new Vector2(i * tuning.VeilX, tuning.VeilY), new Vector2(i * tuning.VeilX2, tuning.VeilY2), tuning.VeilWispShape, look.Size, tuning.VeilWispDuration,
                        tuning.VeilWispLayer, growth: tuning.VeilWispGrowth, emitter: "SignatureSpellEffects.Cell.26");
                vfx.Add(body, new Vector2(0, tuning.VeilY3), tuning.VeilGlyphShape, look.Size * tuning.VeilGlyphSize / tuning.VeilGlyphSizeDivisor, tuning.VeilGlyphDuration, tuning.VeilGlyphLayer, emitter: "SignatureSpellEffects.Cell.27");
                break;
            default:
                vfx.Add(body, -Vector2.UnitY * tuning.AddVelocity2, look.Shape, look.Size * tuning.AddSize2 / 3, tuning.AddDuration3, tuning.AddLayer2, rotation: tuning.AddRotation, emitter: "SignatureSpellEffects.Cell.28");
                vfx.Add(body, Vector2.Zero, tuning.FlashShape, look.Size * tuning.FlashSize / 3, tuning.FlashDuration, tuning.FlashLayer, core: tuning.FlashCore, emitter: "SignatureSpellEffects.Cell.29");
                vfx.Scatter(body, tuning.ShardCount, tuning.ShardShape, look.Size * tuning.ShardSpread, tuning.ShardDuration, emitter: "SignatureSpellEffects.Cell.30");
                break;
        }
    }

    internal static void RelayArc(SpellParticlePool pool, SpecialisationAbility ability, Vector2 origin, Vector2 destination, long now)
    {
        using var vfxScope = SpellVfxAssets.For(ability.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.RelayArc);
        var effect = new Effect(pool, Profile(ability), now);
        var chest = Vector2.UnitY * tuning.Chest;
        effect.Lightning(origin - chest, destination - chest, 0, tuning.Size, emitter: "SignatureSpellEffects.RelayArc.1");
    }

    internal readonly struct Effect(SpellParticlePool pool, Look look, long now, bool castsLight = true,
        int window = 0, int referenceDuration = 600, long? sampleTime = null, bool frameOnly = false,
        uint fieldObjectID = 0, float? depthY = null)
    {
        public void Add(Vector2 point, Vector2 movement, Shape shape, int size, int duration, Layer layer,
            byte direction = 0, float aspect = 1, float rotation = 0, float growth = 0, int delay = 0,
            int? alpha = null, bool core = false, bool linear = false, string emitter = null)
        {
            using var emitterScope = SpellVfxAssets.UseEmitter(emitter);
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectAdd);
            if (window > 0)
            {
                delay = delay * window / referenceDuration;
                duration = Math.Min(window - delay, Math.Max(1, duration * window / referenceDuration));
            }
            int opacity = Math.Clamp(alpha ?? tuning.DefaultAlpha, 0, 255);
            Color colour = layer == Layer.Smoke
                ? Color.FromArgb(opacity, look.Colour.R / tuning.ColourDivisor, look.Colour.G / tuning.ColourDivisor2, look.Colour.B / tuning.ColourDivisor3)
                : Color.FromArgb(opacity, core ? look.Core : look.Colour);
            var particle = new SpellParticle(point, movement, now + delay, duration, size, colour,
                shape, direction, aspect, rotation, growth, core && layer == Layer.Emissive ? tuning.SpellParticleLayer : layer,
                linear, castsLight && layer != Layer.Smoke, frameOnly, fieldObjectID, DepthY: depthY);
            SpellVfxAssets.Emit(pool, particle, sampleTime: sampleTime ?? now);
        }

        public void Ring(Vector2 point, int size, int duration, string emitter = null)
        {
            using var emitterScope = SpellVfxAssets.UseEmitter(emitter);
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectRing);
            Add(point, Vector2.Zero, tuning.HaloShape, size, duration, tuning.HaloLayer, aspect: tuning.HaloAspect, growth: tuning.HaloGrowth, alpha: tuning.HaloAlpha, emitter: SpellVfxAssets.EmitterPart("Ring"));
            Add(point - Vector2.UnitY * tuning.GlyphOffset, -Vector2.UnitY * tuning.GlyphVelocity, tuning.GlyphShape, size * tuning.GlyphSize / 3, duration * tuning.GlyphDuration / 4,
                tuning.GlyphLayer, aspect: tuning.GlyphAspect, growth: tuning.GlyphGrowth, alpha: tuning.GlyphAlpha, emitter: SpellVfxAssets.EmitterPart("Glyph"));
        }

        public void Projectile(Vector2 start, Vector2 end, int windup, int size, bool fork, string emitter = null)
        {
            using var emitterScope = SpellVfxAssets.UseEmitter(emitter);
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectProjectile);
            Vector2 travel = end - start;
            float angle = MathF.Atan2(travel.Y, travel.X);
            int duration = Math.Max(tuning.Duration, windup - tuning.Duration2);
            var head = look.Shape == Shape.Shard ? tuning.Head : look.Form == Form.Rush ? tuning.Head2 : look.Shape;
            Add(start, travel, tuning.BoltShape, size * tuning.BoltSize / 4, duration, tuning.BoltLayer, aspect: tuning.BoltAspect, rotation: angle, linear: true, emitter: SpellVfxAssets.EmitterPart("Trail"));
            Add(start, travel, head, size / 2, duration, tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle, core: tuning.AddCore, linear: true, emitter: SpellVfxAssets.EmitterPart("Head"));
            for (int i = 1; i <= tuning.FeatherCount; i++)
            {
                Vector2 trail = start + travel * (i / tuning.TrailDivisor);
                Add(trail, -Vector2.UnitY * tuning.FeatherVelocity - travel * tuning.FeatherVelocity2, i % 2 == 0 ? tuning.FeatherShape : tuning.FeatherShape2,
                    size / 2 - i * tuning.FeatherSize, tuning.FeatherDuration + i * tuning.FeatherDuration2, tuning.FeatherLayer, aspect: tuning.FeatherAspect, rotation: angle, delay: duration * i / tuning.FeatherDelayDivisor, emitter: SpellVfxAssets.EmitterPart("Feather"));
                if (fork) Add(trail + Vector2.UnitY * tuning.ClawOffset, new Vector2(0, tuning.Y), tuning.ClawShape, size / 3, tuning.ClawDuration, tuning.ClawLayer,
                    aspect: tuning.ClawAspect, rotation: angle, delay: duration * i / tuning.ClawDelayDivisor + tuning.ClawDelay, alpha: tuning.ClawAlpha, emitter: SpellVfxAssets.EmitterPart("Fork"));
            }
        }

        public void Lightning(Vector2 start, Vector2 end, int windup, int size, string emitter = null)
        {
            using var emitterScope = SpellVfxAssets.UseEmitter(emitter);
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectLightning);
            Vector2 travel = end - start;
            float length = travel.Length();
            if (length < 1) return;
            Vector2 side = new Vector2(-travel.Y, travel.X) / length;
            int segments = Math.Clamp((int)length / tuning.SegmentsDivisor, 3, 10);
            Vector2 last = start;
            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = start + travel * (i / (float)segments);
                if (i != segments) next += side * ((i % 2 == 0 ? -1 : 1) * (tuning.Factor + (i * tuning.Factor2 % 10)));
                Vector2 delta = next - last;
                int span = (int)delta.Length() + tuning.Span;
                Add((last + next) * tuning.BoltOffset, Vector2.Zero, tuning.BoltShape, span, tuning.BoltDuration, tuning.BoltLayer,
                    aspect: Math.Min(tuning.Val1, size * tuning.Val2 / span), rotation: MathF.Atan2(delta.Y, delta.X),
                    delay: Math.Max(0, windup - tuning.Val22) + i * tuning.BoltDelay, core: tuning.BoltCore);
                last = next;
            }
        }

        public void Scatter(Vector2 point, int count, Shape shape, float spread, int duration, bool rise = false, string emitter = null)
        {
            using var emitterScope = SpellVfxAssets.UseEmitter(emitter);
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectScatter);
            if (pool.Active.Length > 660) return;
            for (int i = 0; i < count; i++)
            {
                float angle = i * tuning.Angle + (now % 23) * tuning.Angle2;
                float reach = spread * (tuning.Reach + (i % 4) * tuning.Reach2);
                var movement = new Vector2(MathF.Cos(angle) * reach, MathF.Sin(angle) * reach * tuning.Y - (rise ? tuning.Y2 : tuning.Y3));
                Add(point, movement, shape, tuning.AddSize + i % 4 * tuning.AddSize2, duration - i % 4 * tuning.AddDuration, tuning.AddLayer,
                    aspect: tuning.AddAspect + i % 3 * tuning.AddAspect2, rotation: angle, delay: i % 3 * tuning.AddDelay, alpha: tuning.AddAlpha, core: i % 4 == 0);
            }
        }
    }
}
