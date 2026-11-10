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
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.Profile);
        (Form form, Shape shape, int size) = ability.Spell switch
        {
            Spell.SteelCut or Spell.QuickCut => (tuning.SteelCutForm, tuning.SteelCutShape, tuning.SteelCutSize),
            Spell.Notchbreaker => (tuning.NotchbreakerForm, tuning.NotchbreakerShape, tuning.NotchbreakerSize),
            Spell.Oathbrand => (tuning.OathbrandForm, tuning.OathbrandShape, tuning.OathbrandSize),
            Spell.VeilStrike or Spell.Riposte => (tuning.VeilStrikeForm, tuning.VeilStrikeShape, tuning.VeilStrikeSize),
            Spell.GaleCleaver => (tuning.GaleCleaverForm, tuning.GaleCleaverShape, tuning.GaleCleaverSize),
            Spell.RedWake => (tuning.RedWakeForm, tuning.RedWakeShape, tuning.RedWakeSize),
            Spell.DrakesEcho => (tuning.DrakesEchoForm, tuning.DrakesEchoShape, tuning.DrakesEchoSize),
            Spell.Drakefall => (tuning.DrakefallForm, tuning.DrakefallShape, tuning.DrakefallSize),
            Spell.BulwarkSlam => (tuning.BulwarkSlamForm, tuning.BulwarkSlamShape, tuning.BulwarkSlamSize),
            Spell.ArcaneHammer => (tuning.ArcaneHammerForm, tuning.ArcaneHammerShape, tuning.ArcaneHammerSize),
            Spell.Forgefall => (tuning.ForgefallForm, tuning.ForgefallShape, tuning.ForgefallSize),
            Spell.Gatebreaker => (tuning.GatebreakerForm, tuning.GatebreakerShape, tuning.GatebreakerSize),
            Spell.Bloodrush => (tuning.BloodrushForm, tuning.BloodrushShape, tuning.BloodrushSize),
            Spell.Oathstep => (tuning.OathstepForm, tuning.OathstepShape, tuning.OathstepSize),
            Spell.PrismStep => (tuning.PrismStepForm, tuning.PrismStepShape, tuning.PrismStepSize),
            Spell.SpiritPounce => (tuning.SpiritPounceForm, tuning.SpiritPounceShape, tuning.SpiritPounceSize),
            Spell.AncestralRelay => (tuning.AncestralRelayForm, tuning.AncestralRelayShape, tuning.AncestralRelaySize),
            Spell.CinderLance => (tuning.CinderLanceForm, tuning.CinderLanceShape, tuning.CinderLanceSize),
            Spell.FractureSpear => (tuning.FractureSpearForm, tuning.FractureSpearShape, tuning.FractureSpearSize),
            Spell.Pinpoint => (tuning.PinpointForm, tuning.PinpointShape, tuning.PinpointSize),
            Spell.SerpentNeedle => (tuning.SerpentNeedleForm, tuning.SerpentNeedleShape, tuning.SerpentNeedleSize),
            Spell.SpiritSpark => (tuning.SpiritSparkForm, tuning.SpiritSparkShape, tuning.SpiritSparkSize),
            Spell.AncestorsRend => (tuning.AncestorsRendForm, tuning.AncestorsRendShape, tuning.AncestorsRendSize),
            Spell.ArcJavelin => (tuning.ArcJavelinForm, tuning.ArcJavelinShape, tuning.ArcJavelinSize),
            Spell.StormRelay => (tuning.StormRelayForm, tuning.StormRelayShape, tuning.StormRelaySize),
            Spell.Skywire => (tuning.SkywireForm, tuning.SkywireShape, tuning.SkywireSize),
            Spell.ThunderNail => (tuning.ThunderNailForm, tuning.ThunderNailShape, tuning.ThunderNailSize),
            Spell.AshSeal => (tuning.AshSealForm, tuning.AshSealShape, tuning.AshSealSize),
            Spell.KilnBurst => (tuning.KilnBurstForm, tuning.KilnBurstShape, tuning.KilnBurstSize),
            Spell.FurnaceHeart => (tuning.FurnaceHeartForm, tuning.FurnaceHeartShape, tuning.FurnaceHeartSize),
            Spell.CinderMine => (tuning.CinderMineForm, tuning.CinderMineShape, tuning.CinderMineSize),
            Spell.ScarletCrucible => (tuning.ScarletCrucibleForm, tuning.ScarletCrucibleShape, tuning.ScarletCrucibleSize),
            Spell.GlacialGate => (tuning.GlacialGateForm, tuning.GlacialGateShape, tuning.GlacialGateSize),
            Spell.WinterglassVolley => (tuning.WinterglassVolleyForm, tuning.WinterglassVolleyShape, tuning.WinterglassVolleySize),
            Spell.IceManacle => (tuning.IceManacleForm, tuning.IceManacleShape, tuning.IceManacleSize),
            Spell.SleetSnare => (tuning.SleetSnareForm, tuning.SleetSnareShape, tuning.SleetSnareSize),
            Spell.Interpose => (tuning.InterposeForm, tuning.InterposeShape, tuning.InterposeSize),
            Spell.GuardianTether => (tuning.GuardianTetherForm, tuning.GuardianTetherShape, tuning.GuardianTetherSize),
            Spell.PaperWard => (tuning.PaperWardForm, tuning.PaperWardShape, tuning.PaperWardSize),
            Spell.IceglassRefuge => (tuning.IceglassRefugeForm, tuning.IceglassRefugeShape, tuning.IceglassRefugeSize),
            Spell.PhoenixShroud => (tuning.PhoenixShroudForm, tuning.PhoenixShroudShape, tuning.PhoenixShroudSize),
            Spell.StaticMantle => (tuning.StaticMantleForm, tuning.StaticMantleShape, tuning.StaticMantleSize),
            Spell.EchoFang => (tuning.EchoFangForm, tuning.EchoFangShape, tuning.EchoFangSize),
            Spell.LanternLink => (tuning.LanternLinkForm, tuning.LanternLinkShape, tuning.LanternLinkSize),
            Spell.GuidingWick => (tuning.GuidingWickForm, tuning.GuidingWickShape, tuning.GuidingWickSize),
            Spell.TendingBell => (tuning.TendingBellForm, tuning.TendingBellShape, tuning.TendingBellSize),
            Spell.DawnCascade => (tuning.DawnCascadeForm, tuning.DawnCascadeShape, tuning.DawnCascadeSize),
            Spell.SpiritCommand or Spell.PackSignal => (tuning.SpiritCommandForm, tuning.SpiritCommandShape, tuning.SpiritCommandSize),
            Spell.PommelLock => (tuning.PommelLockForm, tuning.PommelLockShape, tuning.PommelLockSize),
            Spell.HeelHook => (tuning.HeelHookForm, tuning.HeelHookShape, tuning.HeelHookSize),
            Spell.ShieldLock => (tuning.ShieldLockForm, tuning.ShieldLockShape, tuning.ShieldLockSize),
            Spell.IronChallenge => (tuning.IronChallengeForm, tuning.IronChallengeShape, tuning.IronChallengeSize),
            Spell.LanternEdict => (tuning.LanternEdictForm, tuning.LanternEdictShape, tuning.LanternEdictSize),
            Spell.TendonSeal => (tuning.TendonSealForm, tuning.TendonSealShape, tuning.TendonSealSize),
            Spell.AncestorsGrasp => (tuning.AncestorsGraspForm, tuning.AncestorsGraspShape, tuning.AncestorsGraspSize),
            Spell.CausticScript => (tuning.CausticScriptForm, tuning.CausticScriptShape, tuning.CausticScriptSize),
            Spell.Distil => (tuning.DistilForm, tuning.DistilShape, tuning.DistilSize),
            Spell.SmokeChoke or Spell.SmokeSnare => (tuning.SmokeChokeForm, tuning.SmokeChokeShape, tuning.SmokeChokeSize),
            Spell.CrossguardBreak => (tuning.CrossguardBreakForm, tuning.CrossguardBreakShape, tuning.CrossguardBreakSize),
            Spell.RedSlip => (tuning.RedSlipForm, tuning.RedSlipShape, tuning.RedSlipSize),
            Spell.BastionBreak => (tuning.BastionBreakForm, tuning.BastionBreakShape, tuning.BastionBreakSize),
            Spell.ShedCinders => (tuning.ShedCindersForm, tuning.ShedCindersShape, tuning.ShedCindersSize),
            Spell.Thawskin => (tuning.ThawskinForm, tuning.ThawskinShape, tuning.ThawskinSize),
            Spell.GroundingStep => (tuning.GroundingStepForm, tuning.GroundingStepShape, tuning.GroundingStepSize),
            Spell.UnbindKnot => (tuning.UnbindKnotForm, tuning.UnbindKnotShape, tuning.UnbindKnotSize),
            Spell.AntivenomVeil => (tuning.AntivenomVeilForm, tuning.AntivenomVeilShape, tuning.AntivenomVeilSize),
            Spell.SoulUnshackle => (tuning.SoulUnshackleForm, tuning.SoulUnshackleShape, tuning.SoulUnshackleSize),
            Spell.RallyingStandard => (tuning.RallyingStandardForm, tuning.RallyingStandardShape, tuning.RallyingStandardSize),
            Spell.FluxPylon => (tuning.FluxPylonForm, tuning.FluxPylonShape, tuning.FluxPylonSize),
            Spell.MiasmaVessel => (tuning.MiasmaVesselForm, tuning.MiasmaVesselShape, tuning.MiasmaVesselSize),
            Spell.SanctuaryLantern => (tuning.SanctuaryLanternForm, tuning.SanctuaryLanternShape, tuning.SanctuaryLanternSize),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability.Spell, "Missing signature particle profile")
        };
        (Color colour, Color core) = ability.Theme switch
        {
            AbilityTheme.Steel => (tuning.SteelColour, tuning.SteelColour2),
            AbilityTheme.Blood => (tuning.BloodColour, tuning.BloodColour2),
            AbilityTheme.Ward => (tuning.WardColour, tuning.WardColour2),
            AbilityTheme.Flame => (tuning.FlameColour, tuning.FlameColour2),
            AbilityTheme.Frost => (tuning.FrostColour, tuning.FrostColour2),
            AbilityTheme.Storm => (tuning.StormColour, tuning.StormColour2),
            AbilityTheme.Spirit => (tuning.SpiritColour, tuning.SpiritColour2),
            AbilityTheme.Venom => (tuning.VenomColour, tuning.VenomColour2),
            AbilityTheme.Shadow => (tuning.ShadowColour, tuning.ShadowColour2),
            _ => (tuning.Colour, tuning.Colour2)
        };
        if (ability.Spell is Spell.ArcaneHammer or Spell.Forgefall) colour = tuning.Colour3;
        if (ability.Spell == Spell.ScarletCrucible) colour = tuning.Colour4;
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
                vfx.Projectile(hand, end, windup, look.Size, ability.Spell == Spell.AncestorsRend);
                break;
            case Form.Arc when target != origin:
                vfx.Lightning(hand, end, windup, look.Size);
                break;
            case Form.Cut:
            case Form.Cleave:
                vfx.Add(hand + facing * tuning.CleaveSlashOffset, facing * tuning.CleaveSlashVelocity, tuning.CleaveSlashShape, look.Size, tuning.CleaveSlashDuration, tuning.CleaveSlashLayer,
                    (byte)direction, tuning.CleaveSlashAspect, delay: Math.Max(0, windup - tuning.CleaveVal2));
                if (look.Form == Form.Cleave)
                    vfx.Add(hand + facing * tuning.CleaveAddOffset, facing * tuning.CleaveAddVelocity, look.Shape, look.Size * tuning.CleaveAddSize / 4, tuning.CleaveAddDuration, tuning.CleaveAddLayer,
                        (byte)direction, tuning.CleaveAddAspect, delay: Math.Max(0, windup - tuning.CleaveVal22), core: tuning.CleaveAddCore);
                break;
            case Form.Hammer:
                vfx.Add(end - Vector2.UnitY * tuning.HammerAddOffset, new Vector2(0, tuning.HammerY), look.Shape, look.Size, Math.Min(windup, tuning.HammerVal2),
                    tuning.HammerAddLayer, aspect: tuning.HammerAddAspect, rotation: tuning.HammerAddRotation + facing.X * tuning.HammerAddRotation2, delay: Math.Max(0, windup - tuning.HammerVal22), linear: true);
                break;
            case Form.Rush:
                if (target != origin) vfx.Projectile(hand, end, windup, look.Size * tuning.RushSize / 4, false);
                break;
            case Form.Heal:
            case Form.Ward:
                vfx.Add(hand, -Vector2.UnitY * tuning.WardAddVelocity, look.Shape, tuning.WardAddSize, windup + tuning.WardAddDuration, tuning.WardAddLayer);
                if (target != origin && ability.Target == AbilityTarget.Ally) vfx.Projectile(hand, end, windup, tuning.WardSize, false);
                break;
            case Form.Break:
                return; // The instantaneous counter has an impact packet at the actor who breaks free.
            default:
                vfx.Add(hand, -Vector2.UnitY * tuning.GlyphVelocity, tuning.GlyphShape, tuning.GlyphSize, windup + tuning.GlyphDuration, tuning.GlyphLayer, growth: tuning.GlyphGrowth);
                break;
        }
        vfx.Add(hand, facing * tuning.FlashVelocity, tuning.FlashShape, tuning.FlashSize, Math.Min(windup + tuning.Val1, tuning.Val2), tuning.FlashLayer, core: tuning.FlashCore);
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
            vfx.Add(cell, Vector2.Zero, tuning.CellMarkShape, tuning.CellMarkSize, ability.Windup + tuning.CellMarkDuration, tuning.CellMarkLayer, aspect: tuning.CellMarkAspect, alpha: tuning.CellMarkAlpha);
            return;
        }
        if (phase == SpellEffect.SignatureArmed)
        {
            vfx.Ring(cell, look.Size, tuning.RingDuration);
            vfx.Add(cell - Vector2.UnitY * tuning.AddOffset, -Vector2.UnitY * tuning.AddVelocity, look.Shape, look.Size * tuning.AddSize / tuning.AddSizeDivisor,
                ability.Spell is Spell.FluxPylon or Spell.MiasmaVessel ? tuning.AddDuration : tuning.AddDuration2, tuning.AddLayer);
            return;
        }

        Vector2 body = cell - Vector2.UnitY * tuning.Body;
        switch (look.Form)
        {
            case Form.Cut:
            case Form.Cleave:
            case Form.Rush:
                vfx.Add(body, new Vector2(tuning.RushX, tuning.RushY), look.Shape, look.Size, tuning.RushAddDuration, tuning.RushAddLayer, tuning.RushAddDirection, tuning.RushAddAspect, rotation: tuning.RushAddRotation);
                vfx.Add(body, -Vector2.UnitY * tuning.RushFlashVelocity, tuning.RushFlashShape, look.Size / 2, tuning.RushFlashDuration, tuning.RushFlashLayer, core: tuning.RushFlashCore);
                vfx.Scatter(body, tuning.RushSparkCount, tuning.RushSparkShape, look.Size * tuning.RushSparkSpread, tuning.RushSparkDuration);
                break;
            case Form.Hammer:
                vfx.Ring(cell, look.Size + tuning.HammerRingSize, tuning.HammerRingDuration);
                vfx.Add(body - Vector2.UnitY * tuning.HammerAddOffset, Vector2.UnitY * tuning.HammerAddVelocity, look.Shape, look.Size * tuning.HammerAddSize / 3, tuning.HammerAddDuration, tuning.HammerAddLayer, rotation: tuning.HammerAddRotation);
                vfx.Add(body, -Vector2.UnitY * tuning.HammerFlashVelocity, tuning.HammerFlashShape, look.Size * tuning.HammerFlashSize / tuning.HammerFlashSizeDivisor, tuning.HammerFlashDuration, tuning.HammerFlashLayer, core: tuning.HammerFlashCore);
                vfx.Scatter(body, tuning.HammerShardCount, tuning.HammerShardShape, look.Size * tuning.HammerShardSpread, tuning.HammerShardDuration);
                break;
            case Form.Eruption:
            case Form.Arc:
                vfx.Ring(cell, look.Size, tuning.ArcRingDuration);
                vfx.Add(body - Vector2.UnitY * tuning.ArcPillarOffset, new Vector2(0, tuning.ArcY), tuning.ArcPillarShape, look.Size * tuning.ArcPillarSize / 3, tuning.ArcPillarDuration, tuning.ArcPillarLayer,
                    aspect: tuning.ArcPillarAspect, growth: tuning.ArcPillarGrowth);
                vfx.Add(body, -Vector2.UnitY * tuning.ArcAddVelocity, look.Shape, look.Size * tuning.ArcAddSize / 3, tuning.ArcAddDuration, tuning.ArcAddLayer, core: tuning.ArcAddCore);
                vfx.Scatter(body, tuning.ArcShardCount, look.Form == Form.Arc ? tuning.ArcShardShape : tuning.ArcShardShape2, look.Size * tuning.ArcShardSpread, tuning.ArcShardDuration);
                break;
            case Form.Ice:
                vfx.Ring(cell, look.Size, tuning.IceRingDuration);
                for (int i = -1; i <= tuning.IceParticleCount; i++)
                    vfx.Add(body + new Vector2(i * tuning.IceX, Math.Abs(i) * tuning.IceY), new Vector2(i * tuning.IceX2, tuning.IceY2), look.Shape,
                        look.Size / (i == 0 ? tuning.IceAddSize : tuning.IceAddSize2), tuning.IceAddDuration, tuning.IceAddLayer, aspect: tuning.IceAddAspect, rotation: i * tuning.IceAddRotation, core: i == 0);
                vfx.Scatter(body, tuning.IceShardCount, tuning.IceShardShape, tuning.IceShardSpread, tuning.IceShardDuration);
                break;
            case Form.Heal:
            case Form.Ward:
            case Form.Totem:
                vfx.Ring(cell, look.Size + tuning.TotemRingSize, tuning.TotemRingDuration);
                vfx.Add(body - Vector2.UnitY * tuning.TotemAddOffset, new Vector2(0, tuning.TotemY), look.Shape, look.Size * tuning.TotemAddSize / 4, tuning.TotemAddDuration, tuning.TotemAddLayer,
                    aspect: tuning.TotemAddAspect, growth: tuning.TotemAddGrowth);
                vfx.Add(body - Vector2.UnitY * tuning.TotemFlashOffset, -Vector2.UnitY * tuning.TotemFlashVelocity, tuning.TotemFlashShape, look.Size / 2, tuning.TotemFlashDuration, tuning.TotemFlashLayer, core: tuning.TotemFlashCore);
                vfx.Scatter(body, tuning.TotemFeatherCount, tuning.TotemFeatherShape, look.Size * tuning.TotemFeatherSpread, tuning.TotemFeatherDuration, rise: true);
                break;
            case Form.Bind:
            case Form.Break:
                vfx.Ring(cell, look.Size, tuning.BreakRingDuration);
                vfx.Add(body, new Vector2(0, tuning.BreakY), look.Shape, look.Size, tuning.BreakAddDuration, tuning.BreakAddLayer,
                    growth: look.Form == Form.Break ? tuning.BreakAddGrowth : 0, core: tuning.BreakAddCore);
                vfx.Scatter(body, tuning.BreakShardCount, tuning.BreakShardShape, look.Size * tuning.BreakShardSpread, tuning.BreakShardDuration);
                break;
            case Form.Veil:
                vfx.Ring(cell, look.Size, tuning.VeilRingDuration);
                for (int i = -1; i <= tuning.VeilWispCount; i++)
                    vfx.Add(body + new Vector2(i * tuning.VeilX, tuning.VeilY), new Vector2(i * tuning.VeilX2, tuning.VeilY2), tuning.VeilWispShape, look.Size, tuning.VeilWispDuration,
                        tuning.VeilWispLayer, growth: tuning.VeilWispGrowth);
                vfx.Add(body, new Vector2(0, tuning.VeilY3), tuning.VeilGlyphShape, look.Size * tuning.VeilGlyphSize / tuning.VeilGlyphSizeDivisor, tuning.VeilGlyphDuration, tuning.VeilGlyphLayer);
                break;
            default:
                vfx.Add(body, -Vector2.UnitY * tuning.AddVelocity2, look.Shape, look.Size * tuning.AddSize2 / 3, tuning.AddDuration3, tuning.AddLayer2, rotation: tuning.AddRotation);
                vfx.Add(body, Vector2.Zero, tuning.FlashShape, look.Size * tuning.FlashSize / 3, tuning.FlashDuration, tuning.FlashLayer, core: tuning.FlashCore);
                vfx.Scatter(body, tuning.ShardCount, tuning.ShardShape, look.Size * tuning.ShardSpread, tuning.ShardDuration);
                break;
        }
    }

    internal static void RelayArc(SpellParticlePool pool, SpecialisationAbility ability, Vector2 origin, Vector2 destination, long now)
    {
        using var vfxScope = SpellVfxAssets.For(ability.Spell);
        var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.RelayArc);
        var effect = new Effect(pool, Profile(ability), now);
        var chest = Vector2.UnitY * tuning.Chest;
        effect.Lightning(origin - chest, destination - chest, 0, tuning.Size);
    }

    internal readonly struct Effect(SpellParticlePool pool, Look look, long now, bool castsLight = true,
        int window = 0, int referenceDuration = 600, long? sampleTime = null, bool frameOnly = false,
        uint fieldObjectID = 0, float? depthY = null)
    {
        public void Add(Vector2 point, Vector2 movement, Shape shape, int size, int duration, Layer layer,
            byte direction = 0, float aspect = 1, float rotation = 0, float growth = 0, int delay = 0,
            int? alpha = null, bool core = false, bool linear = false)
        {
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
            long sample = sampleTime ?? now;
            if (particle.Start > sample) pool.Schedule(particle);
            else if (sample - particle.Start < particle.Duration) pool.TryAdd(particle);
        }

        public void Ring(Vector2 point, int size, int duration)
        {
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectRing);
            Add(point, Vector2.Zero, tuning.HaloShape, size, duration, tuning.HaloLayer, aspect: tuning.HaloAspect, growth: tuning.HaloGrowth, alpha: tuning.HaloAlpha);
            Add(point - Vector2.UnitY * tuning.GlyphOffset, -Vector2.UnitY * tuning.GlyphVelocity, tuning.GlyphShape, size * tuning.GlyphSize / 3, duration * tuning.GlyphDuration / 4,
                tuning.GlyphLayer, aspect: tuning.GlyphAspect, growth: tuning.GlyphGrowth, alpha: tuning.GlyphAlpha);
        }

        public void Projectile(Vector2 start, Vector2 end, int windup, int size, bool fork)
        {
            var tuning = SpellVfxAssets.Observe(SpellVfxAssets.Current.Signature.EffectProjectile);
            Vector2 travel = end - start;
            float angle = MathF.Atan2(travel.Y, travel.X);
            int duration = Math.Max(tuning.Duration, windup - tuning.Duration2);
            var head = look.Shape == Shape.Shard ? tuning.Head : look.Form == Form.Rush ? tuning.Head2 : look.Shape;
            Add(start, travel, tuning.BoltShape, size * tuning.BoltSize / 4, duration, tuning.BoltLayer, aspect: tuning.BoltAspect, rotation: angle, linear: true);
            Add(start, travel, head, size / 2, duration, tuning.AddLayer, aspect: tuning.AddAspect, rotation: angle, core: tuning.AddCore, linear: true);
            for (int i = 1; i <= tuning.FeatherCount; i++)
            {
                Vector2 trail = start + travel * (i / tuning.TrailDivisor);
                Add(trail, -Vector2.UnitY * tuning.FeatherVelocity - travel * tuning.FeatherVelocity2, i % 2 == 0 ? tuning.FeatherShape : tuning.FeatherShape2,
                    size / 2 - i * tuning.FeatherSize, tuning.FeatherDuration + i * tuning.FeatherDuration2, tuning.FeatherLayer, aspect: tuning.FeatherAspect, rotation: angle, delay: duration * i / tuning.FeatherDelayDivisor);
                if (fork) Add(trail + Vector2.UnitY * tuning.ClawOffset, new Vector2(0, tuning.Y), tuning.ClawShape, size / 3, tuning.ClawDuration, tuning.ClawLayer,
                    aspect: tuning.ClawAspect, rotation: angle, delay: duration * i / tuning.ClawDelayDivisor + tuning.ClawDelay, alpha: tuning.ClawAlpha);
            }
        }

        public void Lightning(Vector2 start, Vector2 end, int windup, int size)
        {
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

        public void Scatter(Vector2 point, int count, Shape shape, float spread, int duration, bool rise = false)
        {
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
