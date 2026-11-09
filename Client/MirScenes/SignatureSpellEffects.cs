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
        (Form form, Shape shape, int size) = ability.Spell switch
        {
            Spell.SteelCut or Spell.QuickCut => (Form.Cut, Shape.Slash, 84),
            Spell.Notchbreaker => (Form.Cut, Shape.Bolt, 116),
            Spell.Oathbrand => (Form.Cut, Shape.Glyph, 88),
            Spell.VeilStrike or Spell.Riposte => (Form.Cut, Shape.Claw, 91),
            Spell.GaleCleaver => (Form.Cleave, Shape.Slash, 115),
            Spell.RedWake => (Form.Cleave, Shape.Slash, 139),
            Spell.DrakesEcho => (Form.Cleave, Shape.Claw, 103),
            Spell.Drakefall => (Form.Cleave, Shape.Claw, 145),
            Spell.BulwarkSlam => (Form.Cleave, Shape.Shield, 102),
            Spell.ArcaneHammer => (Form.Hammer, Shape.Hammer, 112),
            Spell.Forgefall => (Form.Hammer, Shape.Hammer, 154),
            Spell.Gatebreaker => (Form.Hammer, Shape.Shield, 129),
            Spell.Bloodrush => (Form.Rush, Shape.Slash, 98),
            Spell.Oathstep => (Form.Rush, Shape.Bolt, 68),
            Spell.PrismStep => (Form.Rush, Shape.Prism, 97),
            Spell.SpiritPounce => (Form.Rush, Shape.Claw, 111),
            Spell.AncestralRelay => (Form.Rush, Shape.Feather, 101),
            Spell.CinderLance => (Form.Bolt, Shape.Bolt, 106),
            Spell.FractureSpear => (Form.Bolt, Shape.Shard, 110),
            Spell.Pinpoint => (Form.Bolt, Shape.Bolt, 75),
            Spell.SerpentNeedle => (Form.Bolt, Shape.Bolt, 80),
            Spell.SpiritSpark => (Form.Bolt, Shape.Feather, 83),
            Spell.AncestorsRend => (Form.Bolt, Shape.Claw, 135),
            Spell.ArcJavelin => (Form.Arc, Shape.Bolt, 104),
            Spell.StormRelay => (Form.Arc, Shape.Flash, 115),
            Spell.Skywire => (Form.Arc, Shape.Pillar, 145),
            Spell.ThunderNail => (Form.Arc, Shape.Prism, 90),
            Spell.AshSeal => (Form.Eruption, Shape.Glyph, 111),
            Spell.KilnBurst => (Form.Eruption, Shape.Pillar, 118),
            Spell.FurnaceHeart => (Form.Eruption, Shape.Flash, 148),
            Spell.CinderMine => (Form.Eruption, Shape.Glyph, 106),
            Spell.ScarletCrucible => (Form.Eruption, Shape.Lantern, 115),
            Spell.GlacialGate => (Form.Ice, Shape.Prism, 105),
            Spell.WinterglassVolley => (Form.Ice, Shape.Shard, 133),
            Spell.IceManacle => (Form.Ice, Shape.Glyph, 87),
            Spell.SleetSnare => (Form.Ice, Shape.Halo, 89),
            Spell.Interpose => (Form.Ward, Shape.Shield, 91),
            Spell.GuardianTether => (Form.Ward, Shape.Shield, 81),
            Spell.PaperWard => (Form.Ward, Shape.Glyph, 79),
            Spell.IceglassRefuge => (Form.Ward, Shape.Prism, 105),
            Spell.PhoenixShroud => (Form.Ward, Shape.Feather, 104),
            Spell.StaticMantle => (Form.Ward, Shape.Flash, 96),
            Spell.EchoFang => (Form.Ward, Shape.Claw, 86),
            Spell.LanternLink => (Form.Heal, Shape.Lantern, 101),
            Spell.GuidingWick => (Form.Heal, Shape.Lantern, 79),
            Spell.TendingBell => (Form.Heal, Shape.Halo, 114),
            Spell.DawnCascade => (Form.Heal, Shape.Pillar, 133),
            Spell.SpiritCommand or Spell.PackSignal => (Form.Heal, Shape.Feather, 88),
            Spell.PommelLock => (Form.Bind, Shape.Hammer, 72),
            Spell.HeelHook => (Form.Bind, Shape.Slash, 83),
            Spell.ShieldLock => (Form.Bind, Shape.Shield, 80),
            Spell.IronChallenge => (Form.Bind, Shape.Banner, 90),
            Spell.LanternEdict => (Form.Bind, Shape.Glyph, 94),
            Spell.TendonSeal => (Form.Bind, Shape.Glyph, 87),
            Spell.AncestorsGrasp => (Form.Bind, Shape.Claw, 99),
            Spell.CausticScript => (Form.Bind, Shape.Glyph, 110),
            Spell.Distil => (Form.Bind, Shape.Lantern, 101),
            Spell.SmokeChoke or Spell.SmokeSnare => (Form.Veil, Shape.Wisp, 107),
            Spell.CrossguardBreak => (Form.Break, Shape.Flash, 99),
            Spell.RedSlip => (Form.Break, Shape.Slash, 103),
            Spell.BastionBreak => (Form.Break, Shape.Shield, 107),
            Spell.ShedCinders => (Form.Break, Shape.Feather, 92),
            Spell.Thawskin => (Form.Break, Shape.Prism, 107),
            Spell.GroundingStep => (Form.Break, Shape.Bolt, 100),
            Spell.UnbindKnot => (Form.Break, Shape.Glyph, 99),
            Spell.AntivenomVeil => (Form.Break, Shape.Halo, 102),
            Spell.SoulUnshackle => (Form.Break, Shape.Claw, 104),
            Spell.RallyingStandard => (Form.Totem, Shape.Banner, 117),
            Spell.FluxPylon => (Form.Totem, Shape.Prism, 100),
            Spell.MiasmaVessel => (Form.Totem, Shape.Lantern, 89),
            Spell.SanctuaryLantern => (Form.Totem, Shape.Lantern, 127),
            _ => throw new ArgumentOutOfRangeException(nameof(ability), ability.Spell, "Missing signature particle profile")
        };
        (Color colour, Color core) = ability.Theme switch
        {
            AbilityTheme.Steel => (Color.FromArgb(113, 163, 255), Color.FromArgb(233, 244, 255)),
            AbilityTheme.Blood => (Color.FromArgb(255, 43, 73), Color.FromArgb(255, 202, 157)),
            AbilityTheme.Ward => (Color.FromArgb(255, 169, 53), Color.FromArgb(255, 243, 185)),
            AbilityTheme.Flame => (Color.FromArgb(255, 88, 22), Color.FromArgb(255, 232, 138)),
            AbilityTheme.Frost => (Color.FromArgb(70, 194, 255), Color.FromArgb(215, 251, 255)),
            AbilityTheme.Storm => (Color.FromArgb(125, 108, 255), Color.FromArgb(205, 240, 255)),
            AbilityTheme.Spirit => (Color.FromArgb(48, 226, 156), Color.FromArgb(202, 255, 231)),
            AbilityTheme.Venom => (Color.FromArgb(129, 210, 22), Color.FromArgb(220, 255, 147)),
            AbilityTheme.Shadow => (Color.FromArgb(190, 73, 247), Color.FromArgb(246, 193, 255)),
            _ => (Color.FromArgb(255, 195, 64), Color.FromArgb(255, 240, 182))
        };
        if (ability.Spell is Spell.ArcaneHammer or Spell.Forgefall) colour = Color.FromArgb(132, 81, 255);
        if (ability.Spell == Spell.ScarletCrucible) colour = Color.FromArgb(255, 38, 98);
        return new Look(form, shape, size, colour, core);
    }

    internal static void Cast(SpellParticlePool pool, SpecialisationAbility ability, Vector2 origin, Vector2 target,
        MirDirection direction, long now)
    {
        var look = Profile(ability);
        var vfx = new Effect(pool, look, now);
        int windup = Math.Max(90, (int)ability.Windup);
        float angle = ((int)direction - 2) * MathF.PI / 4;
        Vector2 facing = new(MathF.Cos(angle), MathF.Sin(angle) * .68f);
        Vector2 hand = origin + facing * 12 - Vector2.UnitY * 25;
        Vector2 end = target - Vector2.UnitY * 22;
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
                vfx.Add(hand + facing * 19, facing * 26, Shape.Slash, look.Size, 220, Layer.Emissive,
                    (byte)direction, .74f, delay: Math.Max(0, windup - 110));
                if (look.Form == Form.Cleave)
                    vfx.Add(hand + facing * 31, facing * 30, look.Shape, look.Size * 3 / 4, 230, Layer.Emissive,
                        (byte)direction, .78f, delay: Math.Max(0, windup - 45), core: true);
                break;
            case Form.Hammer:
                vfx.Add(end - Vector2.UnitY * 82, new Vector2(0, 53), look.Shape, look.Size, Math.Min(windup, 330),
                    Layer.Emissive, aspect: 1.06f, rotation: -.32f + facing.X * .25f, delay: Math.Max(0, windup - 310), linear: true);
                break;
            case Form.Rush:
                if (target != origin) vfx.Projectile(hand, end, windup, look.Size * 3 / 4, false);
                break;
            case Form.Heal:
            case Form.Ward:
                vfx.Add(hand, -Vector2.UnitY * 17, look.Shape, 47, windup + 80, Layer.Emissive);
                if (target != origin && ability.Target == AbilityTarget.Ally) vfx.Projectile(hand, end, windup, 48, false);
                break;
            case Form.Break:
                return; // The instantaneous counter has an impact packet at the actor who breaks free.
            default:
                vfx.Add(hand, -Vector2.UnitY * 13, Shape.Glyph, 43, windup + 65, Layer.Emissive, growth: .22f);
                break;
        }
        vfx.Add(hand, facing * 10, Shape.Flash, 40, Math.Min(windup + 100, 280), Layer.Emissive, core: true);
    }

    internal static void Cell(SpellParticlePool pool, SpecialisationAbility ability, Vector2 cell, SpellEffect phase, long now)
    {
        var look = Profile(ability);
        var vfx = new Effect(pool, look, now);
        if (phase == SpellEffect.SignatureWindup)
        {
            if (ability.Windup == 0) return;
            vfx.Add(cell, Vector2.Zero, Shape.CellMark, 51, ability.Windup + 65, Layer.Ground, aspect: .69f, alpha: 220);
            return;
        }
        if (phase == SpellEffect.SignatureArmed)
        {
            vfx.Ring(cell, look.Size, 760);
            vfx.Add(cell - Vector2.UnitY * 32, -Vector2.UnitY * 8, look.Shape, look.Size * 3 / 5,
                ability.Spell is Spell.FluxPylon or Spell.MiasmaVessel ? 4700 : 1100, Layer.Emissive);
            return;
        }

        Vector2 body = cell - Vector2.UnitY * 23;
        switch (look.Form)
        {
            case Form.Cut:
            case Form.Cleave:
            case Form.Rush:
                vfx.Add(body, new Vector2(7, -8), look.Shape, look.Size, 255, Layer.Emissive, 2, .73f, rotation: -.34f);
                vfx.Add(body, -Vector2.UnitY * 12, Shape.Flash, look.Size / 2, 210, Layer.Emissive, core: true);
                vfx.Scatter(body, 8, Shape.Spark, look.Size * .48f, 440);
                break;
            case Form.Hammer:
                vfx.Ring(cell, look.Size + 18, 520);
                vfx.Add(body - Vector2.UnitY * 23, Vector2.UnitY * 15, look.Shape, look.Size * 2 / 3, 225, Layer.Emissive, rotation: -.3f);
                vfx.Add(body, -Vector2.UnitY * 8, Shape.Flash, look.Size * 4 / 5, 190, Layer.Emissive, core: true);
                vfx.Scatter(body, 11, Shape.Shard, look.Size * .6f, 510);
                break;
            case Form.Eruption:
            case Form.Arc:
                vfx.Ring(cell, look.Size, 540);
                vfx.Add(body - Vector2.UnitY * 23, new Vector2(0, -23), Shape.Pillar, look.Size * 2 / 3, 340, Layer.Emissive,
                    aspect: 1.65f, growth: .22f);
                vfx.Add(body, -Vector2.UnitY * 14, look.Shape, look.Size * 2 / 3, 310, Layer.Emissive, core: true);
                vfx.Scatter(body, 10, look.Form == Form.Arc ? Shape.Shard : Shape.Feather, look.Size * .55f, 580);
                break;
            case Form.Ice:
                vfx.Ring(cell, look.Size, 680);
                for (int i = -1; i <= 1; i++)
                    vfx.Add(body + new Vector2(i * 21, Math.Abs(i) * 10), new Vector2(i * 9, -20), look.Shape,
                        look.Size / (i == 0 ? 2 : 3), 530, Layer.Emissive, aspect: 1.52f, rotation: i * .35f, core: i == 0);
                vfx.Scatter(body, 8, Shape.Shard, 47, 540);
                break;
            case Form.Heal:
            case Form.Ward:
            case Form.Totem:
                vfx.Ring(cell, look.Size + 18, 730);
                vfx.Add(body - Vector2.UnitY * 12, new Vector2(0, -12), look.Shape, look.Size * 3 / 4, 620, Layer.Emissive,
                    aspect: 1.13f, growth: .10f);
                vfx.Add(body - Vector2.UnitY * 12, -Vector2.UnitY * 30, Shape.Flash, look.Size / 2, 290, Layer.Emissive, core: true);
                vfx.Scatter(body, 7, Shape.Feather, look.Size * .37f, 650, rise: true);
                break;
            case Form.Bind:
            case Form.Break:
                vfx.Ring(cell, look.Size, 640);
                vfx.Add(body, new Vector2(0, -16), look.Shape, look.Size, 430, Layer.Emissive,
                    growth: look.Form == Form.Break ? .37f : 0, core: true);
                vfx.Scatter(body, 8, Shape.Shard, look.Size * .54f, 520);
                break;
            case Form.Veil:
                vfx.Ring(cell, look.Size, 520);
                for (int i = -1; i <= 1; i++)
                    vfx.Add(body + new Vector2(i * 21, 4), new Vector2(i * 13, -19), Shape.Wisp, look.Size, 680,
                        Layer.World, growth: .4f);
                vfx.Add(body, new Vector2(0, -12), Shape.Glyph, look.Size * 3 / 5, 350, Layer.Emissive);
                break;
            default:
                vfx.Add(body, -Vector2.UnitY * 8, look.Shape, look.Size * 2 / 3, 300, Layer.Emissive, rotation: -.5f);
                vfx.Add(body, Vector2.Zero, Shape.Flash, look.Size * 2 / 3, 230, Layer.Emissive, core: true);
                vfx.Scatter(body, 8, Shape.Shard, look.Size * .52f, 480);
                break;
        }
    }

    internal static void RelayArc(SpellParticlePool pool, SpecialisationAbility ability, Vector2 origin, Vector2 destination, long now)
    {
        var effect = new Effect(pool, Profile(ability), now);
        var chest = Vector2.UnitY * 22;
        effect.Lightning(origin - chest, destination - chest, 0, 96);
    }

    internal readonly struct Effect(SpellParticlePool pool, Look look, long now, bool castsLight = true,
        int window = 0, int referenceDuration = 600, long? sampleTime = null, bool frameOnly = false,
        uint fieldObjectID = 0, float? depthY = null)
    {
        public void Add(Vector2 point, Vector2 movement, Shape shape, int size, int duration, Layer layer,
            byte direction = 0, float aspect = 1, float rotation = 0, float growth = 0, int delay = 0,
            int alpha = 240, bool core = false, bool linear = false)
        {
            if (window > 0)
            {
                delay = delay * window / referenceDuration;
                duration = Math.Min(window - delay, Math.Max(1, duration * window / referenceDuration));
            }
            Color colour = layer == Layer.Smoke
                ? Color.FromArgb(alpha, look.Colour.R / 6, look.Colour.G / 5, look.Colour.B / 5)
                : Color.FromArgb(alpha, core ? look.Core : look.Colour);
            var particle = new SpellParticle(point, movement, now + delay, duration, size, colour,
                shape, direction, aspect, rotation, growth, core && layer == Layer.Emissive ? Layer.EmissiveCore : layer,
                linear, castsLight && layer != Layer.Smoke, frameOnly, fieldObjectID, DepthY: depthY);
            long sample = sampleTime ?? now;
            if (particle.Start > sample) pool.Schedule(particle);
            else if (sample - particle.Start < particle.Duration) pool.TryAdd(particle);
        }

        public void Ring(Vector2 point, int size, int duration)
        {
            Add(point, Vector2.Zero, Shape.Halo, size, duration, Layer.Ground, aspect: .56f, growth: .3f, alpha: 215);
            Add(point - Vector2.UnitY * 5, -Vector2.UnitY * 9, Shape.Glyph, size * 2 / 3, duration * 3 / 4,
                Layer.Emissive, aspect: .48f, growth: .24f, alpha: 190);
        }

        public void Projectile(Vector2 start, Vector2 end, int windup, int size, bool fork)
        {
            Vector2 travel = end - start;
            float angle = MathF.Atan2(travel.Y, travel.X);
            int duration = Math.Max(80, windup - 35);
            var head = look.Shape == Shape.Shard ? Shape.Bolt : look.Form == Form.Rush ? Shape.Claw : look.Shape;
            Add(start, travel, Shape.Bolt, size * 3 / 4, duration, Layer.Emissive, aspect: .28f, rotation: angle, linear: true);
            Add(start, travel, head, size / 2, duration, Layer.Emissive, aspect: .37f, rotation: angle, core: true, linear: true);
            for (int i = 1; i <= 7; i++)
            {
                Vector2 trail = start + travel * (i / 8f);
                Add(trail, -Vector2.UnitY * 13 - travel * .03f, i % 2 == 0 ? Shape.Feather : Shape.Bolt,
                    size / 2 - i * 2, 220 + i * 9, Layer.World, aspect: .53f, rotation: angle, delay: duration * i / 8);
                if (fork) Add(trail + Vector2.UnitY * 17, new Vector2(0, -15), Shape.Claw, size / 3, 205, Layer.Emissive,
                    aspect: .53f, rotation: angle, delay: duration * i / 8 + 24, alpha: 185);
            }
        }

        public void Lightning(Vector2 start, Vector2 end, int windup, int size)
        {
            Vector2 travel = end - start;
            float length = travel.Length();
            if (length < 1) return;
            Vector2 side = new Vector2(-travel.Y, travel.X) / length;
            int segments = Math.Clamp((int)length / 26, 3, 10);
            Vector2 last = start;
            for (int i = 1; i <= segments; i++)
            {
                Vector2 next = start + travel * (i / (float)segments);
                if (i != segments) next += side * ((i % 2 == 0 ? -1 : 1) * (7 + (i * 5 % 10)));
                Vector2 delta = next - last;
                int span = (int)delta.Length() + 9;
                Add((last + next) * .5f, Vector2.Zero, Shape.Bolt, span, 145, Layer.Emissive,
                    aspect: Math.Min(.53f, size * .12f / span), rotation: MathF.Atan2(delta.Y, delta.X),
                    delay: Math.Max(0, windup - 155) + i * 7, core: true);
                last = next;
            }
        }

        public void Scatter(Vector2 point, int count, Shape shape, float spread, int duration, bool rise = false)
        {
            if (pool.Active.Length > 660) return;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f + (now % 23) * .17f;
                float reach = spread * (.52f + (i % 4) * .15f);
                var movement = new Vector2(MathF.Cos(angle) * reach, MathF.Sin(angle) * reach * .58f - (rise ? 41 : 12));
                Add(point, movement, shape, 13 + i % 4 * 4, duration - i % 4 * 45, Layer.World,
                    aspect: .72f + i % 3 * .27f, rotation: angle, delay: i % 3 * 27, alpha: 232, core: i % 4 == 0);
            }
        }
    }
}
