using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private sealed record TalentCast(TalentAbility Ability, Map Map, int Generation, Point Origin, Point Cell,
        MapObject Target, MonsterObject Spirit, MirDirection Direction, int Power, int Engine,
        MapObject Ally = null, int Variant = 0, int Stage = 0, HashSet<uint> Visited = null, TalentCastCredit HealCredit = null,
        long TargetDistance = 0);

    private Point _fluxPylon, _miasmaVessel;
    private long _fluxUntil, _miasmaUntil, _localBlinkUntil;
    private MapObject _wickTarget, _echoTarget;
    private long _wickUntil, _echoUntil;
    internal int TalentEffectGeneration => _signatureGeneration;

    private int Talent(int row, int column, int option = -1) => Build.Rank(false, row, column, option);
    private int ClassTalent(int row, int column, int option = -1) => Build.Rank(true, row, column, option);

    private void TryCastTalentMagic(TalentAbility talent, MirDirection direction, uint targetId, Point location)
    {
        var ability = talent.Spell;
        Spell spell = ability.Spell;
        UserMagic magic = GetMagic(spell);
        bool counter = talent.Kind == TalentAbilityKind.Counter;
        if (magic?.Info == null || Dead || CurrentMap == null || (byte)direction > 7 || (!counter && !CanCast) ||
            (magic.CastTime != 0 && Envir.Time < magic.CastTime + magic.GetDelay()))
        {
            RejectSignature(spell);
            return;
        }

        int range = spell == Spell.Drakefall && Build.Engine == 1 ? 2 : magic.Info.Range;
        MapObject target = targetId == ObjectID ? this : targetId == 0 ? null : FindObject(targetId, Math.Max(1, range));
        MonsterObject spirit = spell is Spell.SpiritPounce or Spell.AncestralRelay or Spell.EchoFang or Spell.AncestorsRend
            ? BoundSpirit() : null;
        if (spell is Spell.SpiritPounce or Spell.AncestralRelay or Spell.EchoFang or Spell.AncestorsRend && spirit == null)
        {
            RejectSignature(spell);
            return;
        }

        Poison control = null;
        if (counter)
        {
            if (FindControlToBreak(talent.Control) != null || target == null || ability.Target is AbilityTarget.Self or AbilityTarget.Ground)
                target = this;
            if (target != this && (CurrentPoison & (TalentCastLock | TalentMovementLock | PoisonType.Stun | PoisonType.Frozen |
                PoisonType.Paralysis | PoisonType.LRParalysis | PoisonType.Dazed)) != 0) target = null;
            if (target == null || !SignatureReach(target.CurrentLocation, range) ||
                (spell == Spell.SoulUnshackle && target != this && target != BoundSpirit()) ||
                (spell == Spell.UnbindKnot && !CanShieldSignatureTarget(target)) ||
                (control = target.FindControlToBreak(talent.Control)) == null)
            {
                RejectSignature(spell);
                return;
            }
        }
        else if (!ResolveTalentTarget(talent, range, ref target, ref location))
        {
            RejectSignature(spell);
            return;
        }

        bool moving = spell is Spell.Oathstep or Spell.PrismStep;
        if (moving && (!CanTalentMove(this) || !CanTravelTalentPath(this, location) ||
            (spell == Spell.PrismStep && (CurrentMap.Info.NoTeleport || Envir.Time < _localBlinkUntil))))
        {
            RejectSignature(spell);
            return;
        }

        UserItem amulet = spell == Spell.CausticScript ? GetAmulet(1) : null;
        int cost = MagicCost(magic);
        if (MP < cost || (spell == Spell.CausticScript && amulet == null) ||
            (spell == Spell.SpiritPounce && !Functions.InRange(spirit.CurrentLocation, location, 6)) ||
            (spell == Spell.AncestralRelay && !Functions.InRange(spirit.CurrentLocation, location, 5)))
        {
            RejectSignature(spell);
            return;
        }

        int engine = Build.Engine, variant = 0;
        MapObject ally = null;
        if (talent.Kind == TalentAbilityKind.Finisher)
        {
            if (spell == Spell.FurnaceHeart && engine == 1) location = _rotationTile;
            if (!SignatureReach(location, range) || !ConsumeRotation(spell, target, out _, out ally, out variant))
            {
                RejectSignature(spell);
                return;
            }
        }
        if (control != null && !target.BreakTalentControl(control))
        {
            RejectSignature(spell);
            return;
        }

        if (amulet != null) ConsumeItem(amulet, 1);
        Direction = direction;
        if (!counter)
        {
            ActionTime = AttackTime = Envir.Time + MoveDelay;
            SpellTime = Envir.Time + ability.CastRecovery;
            LogTime = Envir.Time + Globals.LogDelay;
            RegenTime = Envir.Time + RegenDelay;
        }
        SpendTalentMagicCost(spell, cost);
        ApplyTalentRecovery(spell);
        magic.CastTime = Envir.Time;
        if (spell == Spell.PrismStep) _localBlinkUntil = Envir.Time + magic.GetDelay();

        int basis = Info.Class switch
        {
            MirClass.Wizard => GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]),
            MirClass.Taoist => GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]),
            _ => GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC])
        };
        int power = (int)Math.Clamp(((double)basis + magic.GetPower()) * magic.GetMultiplier(), 1, int.MaxValue / 64);
        uint resolvedTarget = target?.ObjectID ?? 0;
        Enqueue(new S.Magic { Spell = spell, TargetID = resolvedTarget, Target = location, Cast = true, Level = magic.Level });
        Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation, Spell = spell,
            TargetID = resolvedTarget, Target = location, Cast = true, Level = magic.Level });
        if (counter)
        {
            CompleteCounter(talent, target, control, location);
            ResolveSpellPerks(magic);
            return;
        }

        var cast = new TalentCast(talent, CurrentMap, _signatureGeneration, CurrentLocation, location,
            target, spirit, direction, power, engine, ally, variant,
            HealCredit: spell == Spell.TendingBell ? new TalentCastCredit() : null);
        foreach (Point cell in TalentWarningCells(cast))
            if (CurrentMap.ValidPoint(cell)) BroadcastSignatureEffect(spell, cell, SpellEffect.SignatureWindup);
        if (ability.Windup == 0) CompleteTalentSpell(magic, cast);
        else ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + ability.Windup, magic, cast));
    }

    private bool ResolveTalentTarget(TalentAbility talent, int range, ref MapObject target, ref Point location)
    {
        SpecialisationAbility ability = talent.Spell;
        bool lane = ability.Spell == Spell.WinterglassVolley && Build.Engine == 1;
        switch (lane ? AbilityTarget.Ground : ability.Target)
        {
            case AbilityTarget.Enemy:
                if (target == null && talent.Kind == TalentAbilityKind.Finisher) target = _rotationTarget;
                if (!CanHitSignatureTarget(target)) return false;
                location = target.CurrentLocation;
                break;
            case AbilityTarget.Ally:
                target ??= talent.Kind == TalentAbilityKind.Finisher && _rotationTarget is HumanObject ? _rotationTarget : DefaultMagicTarget;
                if (!CanShieldSignatureTarget(target)) return false;
                location = target.CurrentLocation;
                break;
            case AbilityTarget.Self:
                target = this;
                location = CurrentLocation;
                if (ability.Spell == Spell.Drakefall && Build.Engine == 1)
                {
                    target = _rotationTarget;
                    if (!CanHitSignatureTarget(target) || !Functions.InRange(CurrentLocation, target.CurrentLocation, 2)) return false;
                    location = target.CurrentLocation;
                }
                break;
            case AbilityTarget.Ground:
                if (ability.Spell == Spell.FurnaceHeart && Build.Engine is 0 or 2)
                {
                    target = _rotationTarget;
                    if (!CanHitSignatureTarget(target)) return false;
                    location = target.CurrentLocation;
                }
                else target = null;
                break;
        }
        return SignatureReach(location, range);
    }

    private IEnumerable<Point> TalentWarningCells(TalentCast cast)
    {
        Spell spell = cast.Ability.Spell.Spell;
        if (spell is Spell.GaleCleaver or Spell.BulwarkSlam or Spell.Gatebreaker ||
            (spell == Spell.Drakefall && cast.Engine != 1)) return FrontTalentCells(cast.Origin, cast.Direction);
        if (spell is Spell.KilnBurst or Spell.SanctuaryLantern or Spell.FurnaceHeart)
            return SquareTalentCells(cast.Cell, 1);
        if (spell == Spell.WinterglassVolley && cast.Engine == 1) return GlacialGateTiles(cast.Cell, cast.Direction);
        if (spell == Spell.TendingBell) return SquareTalentCells(cast.Cell, 2);
        if (spell == Spell.SleetSnare) return CrossTalentCells(cast.Cell);
        return [cast.Cell];
    }

    private void CompleteTalentSpell(UserMagic magic, TalentCast cast)
    {
        if (Dead || Node == null || cast.Map != CurrentMap || cast.Generation != _signatureGeneration ||
            GetMagic(magic.Spell) != magic) return;
        if (cast.Stage == 0 && (CurrentPoison & (TalentCastLock | PoisonType.Stun | PoisonType.Dazed |
            PoisonType.Paralysis | PoisonType.Frozen)) != 0) return;
        if (cast.Stage == 0 && cast.Ability.Spell.Target == AbilityTarget.Enemy && cast.Target != null &&
            (!CanHitSignatureTarget(cast.Target) || !SignatureReach(cast.Target.CurrentLocation, magic.Info.Range))) return;
        if (cast.Stage == 0 && cast.Ability.Spell.Target == AbilityTarget.Ally &&
            (!CanShieldSignatureTarget(cast.Target) || !SignatureReach(cast.Target.CurrentLocation, magic.Info.Range))) return;

        bool landed = cast.Ability.Kind == TalentAbilityKind.Control ? ResolveTalentControl(cast) :
            ResolveTalentAction(magic, cast);
        if (landed && cast.Stage == 0) ResolveSpellPerks(magic);
    }

    private bool ResolveTalentControl(TalentCast cast)
    {
        Spell spell = cast.Ability.Spell.Spell;
        MapObject target = cast.Target ?? TalentTargets([cast.Cell], 1).FirstOrDefault();
        if (!CanHitSignatureTarget(target) || (spell == Spell.IceManacle && target.CurrentLocation != cast.Cell)) return false;
        if (spell is Spell.HeelHook or Spell.ShieldLock)
        {
            var facing = Functions.DirectionFromPoint(CurrentLocation, target.CurrentLocation);
            if (facing != cast.Direction && facing != Functions.NextDir(cast.Direction) && facing != Functions.PreviousDir(cast.Direction))
                return false;
        }
        int duration = spell switch { Spell.HeelHook or Spell.TendonSeal => 1400, Spell.IceManacle or Spell.AncestorsGrasp => 1500,
            Spell.SmokeChoke => 1200, _ => 1000 };
        if (!target.TryApplyTalentControl(this, cast.Ability.Control, duration)) return false;
        Poison applied = target.FindControlToBreak(cast.Ability.Control);
        if (applied != null && TalentStatuses.ControlStatus(applied.PType) is var status && status != BuffType.None)
            ApplyTalentStatus(target, status, (int)Math.Max(1, applied.TickTime - Envir.Time));
        if (spell == Spell.LanternEdict) ApplyTalentStatus(target, BuffType.TalentLanternsRebuke, 3500, value: 8);
        BroadcastSignatureEffect(spell, target.CurrentLocation, SpellEffect.SignatureImpact);
        return true;
    }

    private void CompleteCounter(TalentAbility ability, MapObject target, Poison removed, Point destination)
    {
        Spell spell = ability.Spell.Spell;
        BroadcastSignatureEffect(spell, target.CurrentLocation, SpellEffect.SignatureImpact);
        if (spell is Spell.RedSlip or Spell.GroundingStep && target == this && Functions.InRange(CurrentLocation, destination, 1))
            MoveTalentActor(this, destination);
        if (spell != Spell.UnbindKnot) return;
        if (CanHitSignatureTarget(removed.Source) && SignatureReach(removed.Source.CurrentLocation, 6))
            ApplyTalentStatus(removed.Source, BuffType.TalentLightbrand, 3000, Stat.Accuracy, -3);
        ObserveRotation(RotationEvent.Dispel, spell, target);
    }

    private void QueueTalent(UserMagic magic, TalentCast cast, int delay) => ActionList.Add(new DelayedAction(
        DelayedType.Magic, Envir.Time + delay, magic, cast with { Stage = cast.Stage + 1 }));

    private MonsterObject BoundSpirit() => CurrentMap?.Info.NoPets == false
        ? Pets.FirstOrDefault(pet => IsSignatureCompanion(pet, Spell.SpiritCommand) && SignatureReach(pet.CurrentLocation, 8)) : null;

    private static bool CanTalentMove(MapObject actor) => !actor.Dead && (actor.CurrentPoison &
        (TalentMovementLock | PoisonType.Paralysis | PoisonType.LRParalysis | PoisonType.Frozen | PoisonType.Stun)) == 0;

    private bool CanTravelTalentPath(MapObject actor, Point destination)
    {
        Point point = actor.CurrentLocation;
        if (point == destination) return false;
        var zone = CurrentMap.GetSafeZone(point);
        while (point != destination)
        {
            point.Offset(Math.Sign(destination.X - point.X), Math.Sign(destination.Y - point.Y));
            if (!CurrentMap.ValidPoint(point) || CurrentMap.GetSafeZone(point) != zone ||
                CurrentMap.GetCell(point).Objects?.Any(other => other != actor && other.Blocking) == true) return false;
        }
        return true;
    }

    private bool MoveTalentActor(MapObject actor, Point destination)
    {
        if (actor.CurrentMap != CurrentMap || !CanTalentMove(actor) || !CanTravelTalentPath(actor, destination)) return false;
        Point original = actor.CurrentLocation;
        MirDirection direction = Functions.DirectionFromPoint(original, destination);
        int distance = Functions.MaxDistance(original, destination);
        if (actor is HumanObject human)
        {
            if (human.InTrapRock || human.CheckMovement(destination)) return false;
            human.RemoveObjects(direction, distance);
        }
        CurrentMap.GetCell(original).Remove(actor);
        actor.CurrentLocation = destination;
        actor.Direction = direction;
        CurrentMap.GetCell(destination).Add(actor);
        if (actor is HumanObject moved)
        {
            moved.AddObjects(direction, distance);
            moved.Enqueue(new S.UserDash { Direction = direction, Location = destination });
            moved.Moved();
        }
        actor.Broadcast(new S.ObjectDash { ObjectID = actor.ObjectID, Direction = direction, Location = destination });
        return true;
    }

    private static IEnumerable<Point> FrontTalentCells(Point origin, MirDirection direction)
    {
        yield return Functions.PointMove(origin, direction, 1);
        yield return Functions.PointMove(origin, Functions.PreviousDir(direction), 1);
        yield return Functions.PointMove(origin, Functions.NextDir(direction), 1);
    }

    private bool TryShoveCrowd(Point front, HashSet<uint> pushed, out MonsterObject first)
    {
        first = null;
        var line = new List<MonsterObject>(3);
        Point point = front;
        while (true)
        {
            if (!CurrentMap.ValidPoint(point) || CurrentMap.GetSafeZone(point) != null) return false;
            var blocking = CurrentMap.GetCell(point).Objects?.Where(actor => actor.Blocking).ToArray() ?? [];
            if (blocking.Length == 0) break;
            if (blocking.Length != 1 || blocking[0] is not MonsterObject mob || mob.Info.IsBoss || !mob.Info.CanPush ||
                mob.Master != null || mob.Level >= Level || !mob.IsAttackTarget(this)) return line.Count == 0;
            if (line.Count == 3) return false;
            line.Add(mob);
            point = Functions.PointMove(point, Direction, 1);
        }
        if (line.Count == 0) return true;
        if (pushed.Count + line.Count(mob => !pushed.Contains(mob.ObjectID)) > 3) return false;
        for (int index = line.Count - 1; index >= 0; index--)
        {
            if (line[index].Pushed(this, Direction, 1) != 1) return false;
            pushed.Add(line[index].ObjectID);
        }
        first = line[0];
        return true;
    }

    private static IEnumerable<Point> SquareTalentCells(Point centre, int radius)
    {
        for (int y = -radius; y <= radius; y++)
            for (int x = -radius; x <= radius; x++) yield return new Point(centre.X + x, centre.Y + y);
    }

    private static IEnumerable<Point> CrossTalentCells(Point centre) => SquareTalentCells(centre, 1)
        .Where(cell => Math.Abs(cell.X - centre.X) + Math.Abs(cell.Y - centre.Y) <= 1);

    private List<MapObject> TalentTargets(IEnumerable<Point> cells, int limit = 5)
    {
        var targets = new List<MapObject>(limit);
        foreach (Point cell in cells)
        {
            AddSignatureTargets(targets, cell, limit);
            if (targets.Count >= limit) break;
        }
        return targets;
    }

    private IEnumerable<HumanObject> TalentAllies(Point point, int radius, int cap = 5) => SquareTalentCells(point, radius)
        .Where(CurrentMap.ValidPoint).SelectMany(cell => CurrentMap.GetCell(cell).Objects ?? [])
        .OfType<HumanObject>().Where(CanShieldSignatureTarget).Distinct()
        .OrderBy(ally => ally.PercentHealth).ThenBy(ally => ally.ObjectID).Take(cap);

    private Buff ApplyTalentStatus(MapObject target, BuffType type, int duration, Stat? stat = null, int value = 0)
    {
        if (target == null || target.Dead || target.CurrentMap != CurrentMap) return null;
        var stats = stat.HasValue ? new Stats { [stat.Value] = value } : null;
        Buff buff = target.AddBuff(type, this, duration, stats, stat.HasValue, false, value, _signatureGeneration);
        if (buff != null) buff.Caster = this;
        return buff;
    }

    private void ResetTalentMagic()
    {
        _fluxUntil = _miasmaUntil = _wickUntil = _echoUntil = 0;
        _wickTarget = _echoTarget = null;
        _sanctuary = null;
        _talentFields?.Clear();
        ResetTalentModifiers();
        foreach (Buff buff in Buffs.Where(buff => TalentStatuses.TryGet(buff.Type, out _)).ToArray()) RemoveBuff(buff.Type);
    }
}
