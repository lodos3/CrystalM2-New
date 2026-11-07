using System.Numerics;
using Client.MirGraphics;
using Client.MirObjects;
using CrystalM2.Rendering.Core;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private const int SpellParticleCapacity = 2048;
    private readonly SpellParticlePool _spellParticles = new(SpellParticleCapacity);
    private readonly SpellParticle[] _groundSpellRow = new SpellParticle[SpellParticleCapacity];
    private readonly List<WizardArc> _wizardArcs = new(16);
    private readonly List<SpellObject> _healingCircleCells = new(25);
    private static float SpellParticleSize => GraphicsTuning.SpellVisualSize.Current / 100f;
    private static bool SpellOffsetLinked => GraphicsTuning.Offset(GraphicsLayer.Spell).Linked || GraphicsTuning.Offset(GraphicsLayer.Particles).Linked;
    internal int SpellParticleMapGeneration { get; private set; }

    private readonly record struct WizardArc(Spell Spell, Point From, Point To, long Start, int Duration,
        MirDirection? Direction = null, long LastCellTime = 0);

    private void ResetSpellParticles()
    {
        if (!_spellParticles.Active.IsEmpty) TextureValid = false;
        _spellParticles.Clear();
        _wizardArcs.Clear();
        _healingCircleCells.Clear();
        SpellParticleMapGeneration++;
    }

    internal void EmitSpellCast(Spell spell, Point location, Point target, MirDirection direction)
    {
        if (SpecialisationAbilities.TryGet(spell, out var ability))
        {
            if (!CanEmitSpellParticles(location) && !CanEmitSpellParticles(target)) return;
            SignatureSpellEffects.Cast(_spellParticles, ability, SpellWorldPoint(location), SpellWorldPoint(target), direction, CMain.Time);
            TextureValid = false;
        }
    }

    internal void EmitSignatureEffect(Spell spell, Point location, SpellEffect phase)
    {
        if (!CanEmitSpellParticles(location) || !SpecialisationAbilities.TryGet(spell, out var ability)) return;
        SignatureSpellEffects.Cell(_spellParticles, ability, SpellWorldPoint(location), phase, CMain.Time);
        TextureValid = false;
    }

    internal void EmitRelayArc(Spell spell, Point from, Point to)
    {
        if (!CanEmitSpellParticles(from) && !CanEmitSpellParticles(to)) return;
        if (SpecialisationAbilities.TryGet(spell, out var ability))
            SignatureSpellEffects.RelayArc(_spellParticles, ability, SpellWorldPoint(from), SpellWorldPoint(to), CMain.Time);
        else if (spell is Spell.Lightning or Spell.ThunderBolt or Spell.Vampirism)
            AddWizardArc(new WizardArc(spell, from, to, CMain.Time, spell == Spell.Vampirism ? 350 : 440));
        TextureValid = false;
    }

    internal void QueueWizardLightning(Point to, MirDirection direction)
    {
        Point from = Functions.PointMove(to, Functions.ReverseDirection(direction), 1);
        long now = CMain.Time;
        // Lightning's server cue is one packet per affected cell. Reassemble the contiguous
        // cells while the cast is arriving so the forks can span the entire travelled path.
        for (int i = _wizardArcs.Count - 1; i >= 0; i--)
        {
            WizardArc arc = _wizardArcs[i];
            if (arc.Direction != direction || arc.To != from || now - arc.LastCellTime > 80 ||
                now - arc.Start > 160 || Functions.MaxDistance(arc.From, arc.To) >= 12) continue;
            _wizardArcs[i] = arc with { To = to, LastCellTime = now };
            return;
        }
        AddWizardArc(new WizardArc(Spell.Lightning, from, to, now, WizardSpellEffects.LightningDuration, direction, now));
    }

    private void AddWizardArc(WizardArc arc)
    {
        if (_wizardArcs.Count >= 32) _wizardArcs.RemoveAt(0);
        _wizardArcs.Add(arc);
    }

    private void ProcessWizardArcs()
    {
        long now = CMain.Time;
        for (int i = _wizardArcs.Count - 1; i >= 0; i--)
        {
            WizardArc arc = _wizardArcs[i];
            if (now - arc.Start >= arc.Duration)
            {
                _wizardArcs.RemoveAt(i);
                continue;
            }
            if (!CanEmitSpellParticles(arc.From) && !CanEmitSpellParticles(arc.To) &&
                !CanEmitSpellParticles(new Point((arc.From.X + arc.To.X) / 2, (arc.From.Y + arc.To.Y) / 2))) continue;
            WizardSpellEffects.DrawArc(_spellParticles, arc.Spell, SpellWorldPoint(arc.From), SpellWorldPoint(arc.To),
                now, arc.Start, arc.Duration, SpellParticleSize, CellSeed(arc.From) ^ (uint)arc.Start);
            TextureValid = false;
        }
    }

    internal void EmitLegacy(SpellVisual visual, Point cell, MirDirection direction = MirDirection.Right, Point drawOffset = default,
        long? start = null, int duration = 0)
    {
        if (!CanEmitSpellParticles(cell) || !visual.HasArt) return;
        if (LegacySpellEffects.HasLiveArt(visual.Spell))
        {
            Effects.Add(new Effect(visual, duration > 0 ? duration : LegacySpellEffects.PacketDuration(visual), cell, start ?? CMain.Time)
                { ParticleDirection = direction, DrawOffset = drawOffset });
            return;
        }
        EmitLegacy(visual, SpellWorldPoint(cell) + new Vector2(drawOffset.X, drawOffset.Y), Facing(direction), start, duration);
    }

    internal void EmitLegacy(SpellVisual visual, MapObject actor, long? start = null, int duration = 0)
    {
        if (!CanEmitSpellParticles(actor.CurrentLocation) || !visual.HasArt) return;
        if (LegacySpellEffects.HasLiveArt(visual.Spell))
        {
            actor.Effects.Add(new Effect(visual, duration > 0 ? duration : LegacySpellEffects.PacketDuration(visual), actor, start ?? CMain.Time));
            return;
        }
        EmitLegacy(visual, SpellActorFeet(actor), Facing(actor.Direction), start, duration);
    }

    private static Vector2 SpellActorFeet(MapObject actor)
    {
        // DrawLocation is already interpolated by the actor and includes its sprite offset.
        Point camera = ToMouseLocation(Point.Empty);
        var feet = new Vector2(actor.DrawLocation.X - camera.X + CellWidth / 2, actor.DrawLocation.Y - camera.Y + CellHeight / 2);
        if (!GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Linked)
        {
            Point offset = GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Pixels;
            feet -= new Vector2(offset.X, offset.Y);
        }
        return feet;
    }

    internal void EmitAuthoredSpellFrame(SpellVisual visual, Effect effect, long start, int duration)
    {
        MapObject actor = effect.Owner;
        if (actor is PlayerObject { Class: MirClass.Warrior } && WarriorSpellEffects.UsesBodyMask(visual.Spell)) return;
        Point cell = actor?.CurrentLocation ?? effect.Source;
        if (!CanEmitSpellParticles(cell)) return;
        Vector2 feet = actor != null ? SpellActorFeet(actor) : SpellWorldPoint(cell) + new Vector2(effect.DrawOffset.X, effect.DrawOffset.Y);
        if (actor != null && SpellOffsetLinked && GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Linked)
        {
            // The shared offset is applied to the particle pass, so keep just one copy of it.
            Point linked = GraphicsTuning.GroupOffset.Pixels;
            feet -= new Vector2(linked.X, linked.Y);
        }
        uint seed = actor?.ObjectID ?? CellSeed(cell);
        if (WarriorSpellEffects.HasArt(visual.Spell))
        {
            Vector2? tip = null;
            if (visual is { Spell: Spell.FlamingSword, Phase: SpellVisualPhase.Prepare } &&
                actor is PlayerObject player && player.EquippedWeaponTrace() is { } trace)
            {
                Point camera = SpellParticleOrigin();
                tip = trace.Tip - new Vector2(camera.X, camera.Y);
            }
            MirDirection direction = visual.Phase == SpellVisualPhase.Swing
                ? effect.ParticleDirection : actor?.Direction ?? effect.ParticleDirection;
            WarriorSpellEffects.DrawFrame(_spellParticles, visual, feet, Facing(direction), CMain.Time, start,
                duration, SpellParticleSize, seed, tip);
        }
        else if (TaoistSpellEffects.HasArt(visual.Spell))
        {
            TaoistSpellEffects.DrawFrame(_spellParticles, visual, feet, Facing(actor?.Direction ?? effect.ParticleDirection),
                CMain.Time, start, duration, SpellParticleSize, seed);
        }
        else
        {
            WizardSpellEffects.DrawFrame(_spellParticles, visual, feet, Facing(actor?.Direction ?? effect.ParticleDirection),
                CMain.Time, start, duration, SpellParticleSize, seed);
        }
        TextureValid = false;
    }

    private void EmitLegacy(SpellVisual visual, Vector2 feet, Vector2 facing, long? start = null, int duration = 0)
    {
        LegacySpellEffects.Emit(_spellParticles, visual, feet, facing, start ?? CMain.Time, duration, CMain.Time);
        TextureValid = false;
    }

    internal void EmitSpellImpact(Spell spell, Point location)
    {
        // The server sends one impact packet for each recipient actually affected by
        // these area spells. Render a compact hit on that recipient; do not collapse
        // all recipients into the talisman's single arrival visual.
        if (spell is Spell.SoulShield or Spell.BlessedArmour or Spell.MassHealing or Spell.MassHiding)
        {
            var hit = new SpellVisual(spell, SpellVisualPhase.Hit);
            if (!CanEmitSpellParticles(location)) return;
            MapObject recipient = null;
            if (M2CellInfo[location.X, location.Y].CellObjects is { } wardOccupants)
            {
                foreach (MapObject actor in wardOccupants)
                {
                    if (actor.CurrentLocation != location ||
                        actor.Race is not (ObjectType.Player or ObjectType.Hero or ObjectType.Monster)) continue;
                    // MapEffect currently identifies the tile, not a unique object ID. If
                    // multiple occupants share it, avoid attaching the cue to the wrong one.
                    if (recipient != null) { recipient = null; break; }
                    recipient = actor;
                }
            }
            if (recipient == null) EmitLegacy(hit, location);
            else EmitLegacy(hit, recipient);
            return;
        }

        // These spells already show the complete field at the cast location. The hit packets
        // confirm contacts elsewhere in that field and only need a small accent on each target.
        var phase = WarriorSpellEffects.HasArt(spell) ||
            spell is Spell.FireBang or Spell.IceStorm or Spell.IceThrust or Spell.FlameField or Spell.ThunderStorm or Spell.StormEscape
            ? SpellVisualPhase.Contact : SpellVisualPhase.Impact;
        var visual = new SpellVisual(spell, phase);
        if (spell == Spell.FlamingSword && CanEmitSpellParticles(location) &&
            M2CellInfo[location.X, location.Y].CellObjects is { } occupants)
        {
            MapObject target = null;
            foreach (MapObject actor in occupants)
            {
                if (actor.CurrentLocation != location || actor.Race is not (ObjectType.Player or ObjectType.Hero or ObjectType.Monster))
                    continue;
                if (target != null)
                {
                    target = null;
                    break;
                }
                target = actor;
            }
            if (target != null)
            {
                EmitLegacy(visual, target);
                return;
            }
        }
        EmitLegacy(visual, location);
    }

    internal void EmitProjectileFrame(Spell spell, Point source, Point destination, LegacyProjectileTimeline timing, bool wake)
    {
        float progress = timing.Progress(CMain.Time);
        Point cell = new((int)MathF.Round(source.X + (destination.X - source.X) * progress),
            (int)MathF.Round(source.Y + (destination.Y - source.Y) * progress));
        if (!CanEmitSpellParticles(cell)) return;

        Vector2 origin = SpellWorldPoint(source);
        Vector2 target = SpellWorldPoint(destination);
        Vector2 heading = target == origin ? Vector2.UnitX : Vector2.Normalize(target - origin);
        Vector2 head = LegacySpellEffects.ProjectileAnchor(spell, Vector2.Lerp(origin, target, progress));
        LegacySpellEffects.ProjectileFrame(_spellParticles, spell, head, heading, CMain.Time, timing.Frame(CMain.Time),
            (int)(timing.Impact - CMain.Time), wake, SpellParticleSize);
        TextureValid = false;
    }

    internal void EmitWeaponSweep(Spell spell, WeaponMaskTrace screenPose, WeaponMaskTrace? previousWorld,
        Point screenFeet, MirDirection direction, float progress, float previousProgress, int remaining, int frameInterval,
        out WeaponMaskTrace worldPose)
    {
        Point camera = SpellParticleOrigin();
        Vector2 offset = new(camera.X, camera.Y);
        worldPose = new WeaponMaskTrace(screenPose.Hilt - offset, screenPose.Tip - offset);
        Vector2 feet = new Vector2(screenFeet.X, screenFeet.Y) - offset;
        LegacySpellEffects.WeaponSweep(_spellParticles, spell, worldPose, previousWorld, CMain.Time, remaining, frameInterval,
            Facing(direction), progress, feet, previousProgress);
        TextureValid = false;
    }

    internal WeaponMaskTrace WeaponPoseInWorld(WeaponMaskTrace screen)
    {
        Point camera = SpellParticleOrigin();
        Vector2 offset = new(camera.X, camera.Y);
        return new WeaponMaskTrace(screen.Hilt - offset, screen.Tip - offset);
    }

    internal void EmitWeaponEnchant(Spell spell, WeaponMaskTrace world, MapObject actor, bool striking)
    {
        if (_spellParticles.Active.Length >= (striking ? SpellParticleCapacity * 7 / 8 : SpellParticleCapacity * 3 / 4)) return;
        WarriorSpellEffects.DrawWeaponEnchant(_spellParticles, spell, world, Facing(actor.Direction), CMain.Time,
            SpellParticleSize, actor.ObjectID, striking, SpellActorFeet(actor).Y);
        TextureValid = false;
    }

    internal WeaponMaskTrace WeaponPoseOnScreen(WeaponMaskTrace world)
    {
        Point camera = SpellParticleOrigin();
        Vector2 offset = new(camera.X, camera.Y);
        return new WeaponMaskTrace(world.Hilt + offset, world.Tip + offset);
    }

    internal void EmitDashTrail(Point from, Point to, MirDirection direction, bool shoulderDash)
    {
        if (!CanEmitSpellParticles(from) && !CanEmitSpellParticles(to)) return;
        Vector2 start = SpellWorldPoint(from);
        Vector2 end = SpellWorldPoint(to);
        if (shoulderDash)
        {
            WarriorSpellEffects.Dash(_spellParticles, start, end, CMain.Time);
            TextureValid = false;
            return;
        }
        for (int i = 0; i < 4; i++)
            EmitLegacy(new SpellVisual(Spell.ShoulderDash, SpellVisualPhase.Flight), Vector2.Lerp(start, end, i / 4f), Facing(direction));
    }

    internal static bool HasGroundSpellParticles(Spell spell) => spell is Spell.FireWall or Spell.PoisonCloud or
        Spell.Blizzard or Spell.MeteorStrike or Spell.HealingCircle or Spell.TrapHexagon or Spell.Reincarnation or
        Spell.ExplosiveTrap or Spell.Trap or Spell.Portal;

    internal void EmitGroundSpellParticles(SpellObject field)
    {
        if (field.Spell == Spell.HealingCircle)
        {
            if (Settings.Effect) _healingCircleCells.Add(field);
            return;
        }
        if (_spellParticles.Active.Length >= SpellParticleCapacity * 5 / 8 || !CanEmitSpellParticles(field.CurrentLocation)) return;
        var visual = new SpellVisual(field.Spell, SpellVisualPhase.Field);
        Vector2 feet = SpellWorldPoint(field.CurrentLocation);
        if (WizardSpellEffects.HasArt(field.Spell))
            WizardSpellEffects.DrawFrame(_spellParticles, visual, feet, Facing(field.Direction), CMain.Time,
                field.ParticleStart, int.MaxValue, SpellParticleSize, CellSeed(field.CurrentLocation), field.ObjectID);
        else if (TaoistSpellEffects.HasArt(field.Spell))
            TaoistSpellEffects.DrawFrame(_spellParticles, visual, feet, Facing(field.Direction), CMain.Time,
                field.ParticleStart, int.MaxValue, SpellParticleSize, CellSeed(field.CurrentLocation), field.ObjectID);
        else
            LegacySpellEffects.Emit(_spellParticles, visual, feet, Facing(field.Direction), CMain.Time, fieldObjectID: field.ObjectID);
        TextureValid = false;
    }

    private void ProcessHealingCircles()
    {
        if (_healingCircleCells.Count == 0) return;
        // The server allocates the cells of a cast in map row order. Combine its
        // adjacent cells into one seal, sized to the usual three- or five-cell field.
        _healingCircleCells.Sort(static (left, right) => left.ObjectID.CompareTo(right.ObjectID));
        for (int i = 0; i < _healingCircleCells.Count && _spellParticles.Active.Length < SpellParticleCapacity * 5 / 8;)
        {
            SpellObject first = _healingCircleCells[i++];
            Point previous = first.CurrentLocation;
            int left = previous.X, right = previous.X, top = previous.Y, bottom = previous.Y;
            long start = first.ParticleStart;
            bool visible = CanEmitSpellParticles(previous);
            while (i < _healingCircleCells.Count)
            {
                SpellObject next = _healingCircleCells[i];
                Point cell = next.CurrentLocation;
                if (next.ObjectID - first.ObjectID >= 25 || Math.Abs(next.ParticleStart - start) > 160 ||
                    cell.Y < previous.Y || cell.Y == previous.Y && cell.X <= previous.X || cell.Y - bottom > 2 ||
                    cell.Y - top > 4 || Math.Max(right, cell.X) - Math.Min(left, cell.X) > 4)
                    break;
                i++;
                previous = cell;
                left = Math.Min(left, cell.X);
                right = Math.Max(right, cell.X);
                bottom = cell.Y;
                visible |= CanEmitSpellParticles(cell);
            }

            if (!visible) continue;
            Vector2 feet = (SpellWorldPoint(new Point(left, top)) + SpellWorldPoint(new Point(right, bottom))) * .5f;
            float width = 354 + Math.Max(0, Math.Max(right - left, bottom - top) - 2) * CellWidth;
            TaoistSpellEffects.DrawFrame(_spellParticles, new SpellVisual(Spell.HealingCircle, SpellVisualPhase.Field),
                feet, Vector2.UnitX, CMain.Time, start, int.MaxValue, SpellParticleSize, first.ObjectID, first.ObjectID, width);
            TextureValid = false;
        }
        _healingCircleCells.Clear();
    }

    internal void EndGroundSpellParticles(uint objectID)
    {
        if (_spellParticles.RemoveField(objectID)) TextureValid = false;
    }

    private static Vector2 Facing(MirDirection direction)
    {
        float angle = ((int)direction - 2) * MathF.PI / 4;
        return Vector2.Normalize(new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .68f));
    }

    private static Vector2 SpellWorldPoint(Point cell) =>
        new(cell.X * CellWidth + CellWidth / 2, cell.Y * CellHeight + CellHeight / 2);

    private static uint CellSeed(Point cell) => unchecked((uint)cell.X * 73856093u ^ (uint)cell.Y * 19349663u);

    private bool CanEmitSpellParticles(Point cell) => Settings.Effect && User != null && M2CellInfo != null &&
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height &&
        Math.Abs(cell.X - User.Movement.X) <= ViewRangeX + 2 &&
        Math.Abs(cell.Y - User.Movement.Y) <= ViewRangeY + 2;

    private void DrawGroundSpellRow(int y, int firstRow, int lastRow)
    {
        if (!Settings.Effect || _spellParticles.Active.IsEmpty) return;
        float start = y == firstRow ? float.NegativeInfinity : y * CellHeight;
        float end = y == lastRow ? float.PositiveInfinity : (y + 1) * CellHeight;
        int count = 0;
        foreach (ref readonly SpellParticle particle in _spellParticles.Active)
            if (particle.Ground && particle.Layer != SpellParticleLayer.Floor && particle.GroundDepth >= start && particle.GroundDepth < end)
                _groundSpellRow[count++] = particle;

        DrawSpellParticles(_groundSpellRow.AsSpan(0, count), ground: true);
    }

    private void DrawFloorSpellParticles()
    {
        int count = 0;
        foreach (ref readonly SpellParticle particle in _spellParticles.Active)
            if (particle.Layer == SpellParticleLayer.Floor) _groundSpellRow[count++] = particle;
        DrawSpellParticles(_groundSpellRow.AsSpan(0, count), ground: true);
    }

    private void DrawSpellParticles(ReadOnlySpan<SpellParticle> particles, bool ground, bool emissiveOverlay = false,
        Texture occlusion = null, int depthOrigin = 0)
    {
        if (!Settings.Effect || particles.IsEmpty) return;

        Point camera = SpellParticleOrigin();
        using var artwork = ground && GraphicsTuning.GroundParticleBlend.Current != 0
            ? DXManager.BlendArtwork(GraphicsLayer.Particles, GraphicsTuning.GroundParticleBlend)
            : DXManager.BlendArtwork(GraphicsLayer.Particles, GraphicsLayer.Spell);
        DXManager.Device.SpellParticles.Draw(particles, new Vector2(camera.X, camera.Y), CMain.Time, ground,
            ground ? 1f : Settings.SpellBlendIntensityPercent / 100f,
            sizeScale: ground ? 1f : SpellParticleSize, emissiveOverlay: emissiveOverlay,
            exposure: GraphicsTuning.SpellExposure.Current / 100f, saturation: GraphicsTuning.SpellSaturation.Current / 100f,
            glow: 1f + GraphicsTuning.SpellGlowStrength.Current / 300f, screenGlow: GraphicsTuning.SpellGlowBlend.Current != 0,
            occlusion: occlusion, depthOrigin: depthOrigin, cellHeight: CellHeight);
    }

    private void DrawSpellParticleLights(float intensity)
    {
        if (_spellParticles.Active.IsEmpty) return;
        Point position = GraphicsTuning.LightPosition(LightSource.Spell, SpellParticleOrigin(), SpellOffsetLinked);
        Vector2 camera = new(position.X, position.Y);
        var light = GraphicsTuning.Light(LightSource.Spell);
        float radius = light.Radius.Current / 100f, warmth = light.Warmth.Current * 0.003f;
        Vector4 tint = RendererDevice.ToVector(light.Colour);
        ShapedLightPass.AddParticles(_spellParticles, camera, CMain.Time, intensity, radius,
            SpellParticleSize, warmth,
            light.CustomColour.Current == 0 ? null : new Vector3(tint.X, tint.Y, tint.Z));
    }

    private Point SpellParticleOrigin() => GraphicsTuning.Offset(GraphicsLayer.Particles).Apply(
        GraphicsTuning.Offset(GraphicsLayer.Spell).Apply(ToMouseLocation(Point.Empty)), GraphicsTuning.Offset(GraphicsLayer.Spell).Linked);
}
