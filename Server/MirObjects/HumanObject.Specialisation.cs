using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private sealed record SignatureCast(Map Map, int Generation, Point Location, MapObject Target, MonsterObject Companion,
        MirDirection Direction, int Power, bool Concealed, int Stage = 0, bool Trained = false,
        Point LastImpact = default, List<MapObject> RelayTargets = null);

    private int _signatureWard, _signatureGeneration;
    private long _signatureWardUntil, _signatureFieldUntil;
    private readonly object _signatureWardLock = new();

    private void TryCastSpecialisation(UserMagic magic, SpecialisationAbility ability, MirDirection direction, uint targetId, Point location)
    {
        int cost = MagicCost(magic);
        if (Level < ability.UnlockLevel || MP < cost || CurrentMap == null || (byte)direction > 7 ||
            (magic.CastTime != 0 && Envir.Time < magic.CastTime + magic.GetDelay()) ||
            (ability.Spell is Spell.AshSeal or Spell.CinderMine && Envir.Time < _signatureFieldUntil))
        {
            RejectSignature(ability.Spell);
            return;
        }

        int range = magic.Info.Range;
        MapObject target = targetId == ObjectID ? this : targetId == 0 ? null : FindObject(targetId, range);
        MonsterObject companion = null;
        switch (ability.Target)
        {
            case AbilityTarget.Enemy:
                if (!CanHitSignatureTarget(target) || !SignatureReach(target.CurrentLocation, range) ||
                    (ability.Spell == Spell.Distil && FindDistilPoison(target) == null))
                {
                    RejectSignature(ability.Spell);
                    return;
                }

                location = target.CurrentLocation;
                break;
            case AbilityTarget.Ally:
                target = targetId == 0 ? DefaultMagicTarget : target;
                if (!CanShieldSignatureTarget(target) || !SignatureReach(target.CurrentLocation, range))
                {
                    RejectSignature(ability.Spell);
                    return;
                }

                location = target.CurrentLocation;
                break;
            case AbilityTarget.Self:
                target = this;
                location = CurrentLocation;
                break;
            case AbilityTarget.Companion:
                companion = Pets.FirstOrDefault(pet => IsSignatureCompanion(pet, ability.Spell) &&
                    SignatureReach(pet.CurrentLocation, range));
                if (companion == null || CurrentMap.Info.NoPets ||
                    (targetId != 0 && (!CanHitSignatureTarget(target) || !SignatureReach(target.CurrentLocation, range))))
                {
                    RejectSignature(ability.Spell);
                    return;
                }

                location = companion.CurrentLocation;
                break;
            case AbilityTarget.Ground:
                if (!SignatureReach(location, range))
                {
                    RejectSignature(ability.Spell);
                    return;
                }

                target = null;
                break;
        }

        bool concealed = Hidden;
        if (concealed)
        {
            RemoveBuff(BuffType.MoonLight);
            RemoveBuff(BuffType.DarkBody);
        }
        Direction = direction;
        AttackTime = ActionTime = Envir.Time + MoveDelay;
        SpellTime = Envir.Time + 1800;
        LogTime = Envir.Time + Globals.LogDelay;
        RegenTime = Envir.Time + RegenDelay;
        ChangeMP(-cost);
        magic.CastTime = Envir.Time;

        int basePower = ability.Class switch
        {
            MirClass.Wizard => GetAttackPower(Stats[Stat.MinMC], Stats[Stat.MaxMC]),
            MirClass.Taoist => GetAttackPower(Stats[Stat.MinSC], Stats[Stat.MaxSC]),
            _ => GetAttackPower(Stats[Stat.MinDC], Stats[Stat.MaxDC])
        };
        double power = ((double)basePower + magic.GetPower()) * magic.GetMultiplier();
        int castPower = double.IsFinite(power) ? (int)Math.Clamp(power, 1, int.MaxValue / 32) : 1;

        var cast = new SignatureCast(CurrentMap, _signatureGeneration, location, target, companion, direction, castPower, concealed);
        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + ability.Windup, magic, cast));
        if (ability.Spell == Spell.AshSeal) _signatureFieldUntil = Envir.Time + ability.Windup + 1600;
        if (ability.Spell == Spell.CinderMine) _signatureFieldUntil = Envir.Time + ability.Windup + 4000;

        uint resolvedTarget = target?.ObjectID ?? 0;
        Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
        Enqueue(new S.Magic { Spell = ability.Spell, TargetID = resolvedTarget, Target = location, Cast = true, Level = magic.Level });
        Broadcast(new S.ObjectMagic { ObjectID = ObjectID, Direction = Direction, Location = CurrentLocation,
            Spell = ability.Spell, TargetID = resolvedTarget, Target = location, Cast = true, Level = magic.Level });
        if (ability.Spell == Spell.GlacialGate)
        {
            foreach (Point tile in GlacialGateTiles(location, direction))
                if (CurrentMap.ValidPoint(tile)) BroadcastSignatureEffect(ability.Spell, tile, SpellEffect.SignatureWindup);
        }
        else
            BroadcastSignatureEffect(ability.Spell, location, SpellEffect.SignatureWindup);
    }

    private void RejectSignature(Spell spell)
    {
        Enqueue(new S.Magic { Spell = spell, Cast = false });
        Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
    }

    private bool SignatureReach(Point location, int range) => CurrentMap.ValidPoint(location) &&
        Functions.InRange(CurrentLocation, location, range) && CanFly(location);

    private bool CanHitSignatureTarget(MapObject target) => target is HumanObject or MonsterObject &&
        !target.Dead && target.Node != null && target.CurrentMap == CurrentMap &&
        !target.IsFriendlyTarget(this) && target.IsAttackTarget(this);

    private bool CanShieldSignatureTarget(MapObject target) => target is HumanObject && !target.Dead &&
        target.Node != null && target.CurrentMap == CurrentMap && (target == this || target.IsFriendlyTarget(this));

    protected bool IsSignatureCompanion(MonsterObject pet, Spell spell)
    {
        if (pet == null || pet.Race != ObjectType.Monster || pet.Dead || pet.Master != this ||
            pet.Node == null || pet.CurrentMap != CurrentMap) return false;

        string name = pet.Info.Name;
        return spell == Spell.SpiritCommand
            ? name == Settings.SkeletonName || name == Settings.ShinsuName || name == Settings.AngelName
            : name == Settings.VampireName || name == Settings.ToadName || name == Settings.SnakeTotemName;
    }

    protected Poison FindDistilPoison(MapObject target) =>
        target.PoisonList.FirstOrDefault(poison => poison.Source == this && poison.Duration > poison.Time &&
            poison.PType is PoisonType.Green or PoisonType.Red);

    private void CompleteSpecialisationSpell(UserMagic magic, SpecialisationAbility ability, SignatureCast cast)
    {
        if (Dead || Node == null || cast.Map != CurrentMap || cast.Generation != _signatureGeneration ||
            GetMagic(ability.Spell) != magic) return;
        // Short wind-ups can be interrupted. Active fields finish while their caster remains on the map.
        if (cast.Stage == 0 && (CurrentPoison & (PoisonType.Stun | PoisonType.Dazed | PoisonType.Paralysis | PoisonType.Frozen)) != 0)
            return;

        bool advanced = Level >= SpecialisationAbilities.AdvancedTraitLevel;
        bool master = Level >= SpecialisationAbilities.MasterTraitLevel;
        int power = cast.Power;
        Spell spell = ability.Spell;
        MapObject target = cast.Target;
        bool landed = false;

        if (ability.Target == AbilityTarget.Enemy && cast.Stage == 0 &&
            (!CanHitSignatureTarget(target) || !SignatureReach(target.CurrentLocation, magic.Info.Range)))
            return;
        if (ability.Target == AbilityTarget.Ally &&
            (!CanShieldSignatureTarget(target) || !SignatureReach(target.CurrentLocation, magic.Info.Range)))
            return;

        switch (spell)
        {
            case Spell.SteelCut:
            case Spell.QuickCut:
                landed = HitSignatureTarget(target, spell, power, DefenceType.ACAgility);
                break;
            case Spell.SpiritSpark:
                landed = HitSignatureTarget(target, spell, power, DefenceType.MAC);
                break;
            case Spell.Notchbreaker:
            case Spell.Pinpoint:
            {
                int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
                bool far = distance >= (spell == Spell.Notchbreaker ? 2 : 5);
                if (far) power = ScaleSignature(power, advanced ? 125 : 115);
                landed = HitSignatureTarget(target, spell, power, DefenceType.ACAgility);
                if (landed && far && master) ChangeMP(4);
                break;
            }
            case Spell.RedWake:
            {
                BroadcastSignatureEffect(spell, CurrentLocation, SpellEffect.SignatureImpact);
                int hits = HitSignatureArea(spell, CurrentLocation, 1, advanced ? 5 : 4, power, DefenceType.AC, 55);
                landed = hits > 0;
                if (master && hits >= 3) ChangeMP(4);
                break;
            }
            case Spell.Interpose:
            {
                var ally = (HumanObject)target;
                int capacity = advanced ? ScaleSignature(power, 125) : power;
                landed = GrantSignatureWard(ally, spell, capacity, 4000);
                if (landed && master && ally != this) GrantSignatureWard(this, spell, capacity / 3, 3000);
                break;
            }
            case Spell.AshSeal:
            {
                int percent = advanced && cast.Stage == 2 ? 53 : 39;
                landed = HitSignatureArea(spell, cast.Location, 1, 5, ScaleSignature(power, percent), DefenceType.MAC, 80) > 0;
                BroadcastSignatureEffect(spell, cast.Location, SpellEffect.SignatureImpact);
                if (cast.Stage < 2) QueueSignatureStage(magic, cast with { Trained = cast.Trained || landed }, 800);
                else if (landed && master) ChangeMP(4);
                break;
            }
            case Spell.GlacialGate:
            {
                var enemies = new List<MapObject>(4);
                foreach (Point tile in GlacialGateTiles(cast.Location, cast.Direction))
                    AddSignatureTargets(enemies, tile, 4);
                foreach (var enemy in enemies)
                {
                    int damage = advanced && enemy.CurrentPoison.HasFlag(PoisonType.Slow) ? ScaleSignature(power, 120) : power;
                    if (!HitSignatureTarget(enemy, spell, damage, DefenceType.MAC)) continue;
                    SlowSignatureTarget(enemy, 2);
                    landed = true;
                }
                if (landed && master) GrantSignatureWard(this, spell, power / 4, 2500);
                break;
            }
            case Spell.StormRelay:
            {
                if (!CanHitSignatureTarget(target) || (cast.Stage > 0 &&
                    !Functions.InRange(cast.LastImpact, target.CurrentLocation, 3))) break;

                Point impact = target.CurrentLocation;
                landed = HitSignatureTarget(target, spell, ScaleSignature(power, cast.Stage == 0 ? 100 : 55), DefenceType.MAC);
                if (!landed) break;
                if (cast.Stage == 2 && master) ChangeMP(4);
                int max = advanced ? 4 : 3;
                if (cast.Stage + 1 < max)
                {
                    var visited = cast.RelayTargets ?? new List<MapObject>(max);
                    visited.Add(target);
                    var next = GetSignatureTargets(impact, 3, 1, visited).FirstOrDefault();
                    if (next != null) QueueSignatureStage(magic, cast with
                    {
                        Target = next, LastImpact = impact, RelayTargets = visited, Trained = true
                    }, 95);
                }
                break;
            }
            case Spell.LanternLink:
            {
                var ally = (HumanObject)target;
                int before = ally.HP;
                ally.ChangeHP(Math.Min(Math.Max(0, ally.MaxHealth - before), power));
                int effective = Math.Max(0, ally.HP - before);
                landed = GrantSignatureWard(ally, spell, power / 2 + (advanced ? effective / 3 : 0), 3500) || effective > 0;
                if (master)
                {
                    int index = ally.PoisonList.FindIndex(p => p.PType is PoisonType.Green or PoisonType.Red);
                    if (index >= 0)
                    {
                        ally.PoisonList.RemoveAt(index);
                        landed = true;
                    }
                }
                break;
            }
            case Spell.Distil:
            {
                Poison dose = FindDistilPoison(target);
                if (dose == null) break;
                int bonus = (int)Math.Min(dose.Duration - dose.Time, 4) * (advanced ? 8 : 5);
                Point impact = target.CurrentLocation;
                landed = HitSignatureTarget(target, spell, ScaleSignature(power, 100 + bonus), DefenceType.MAC);
                if (!landed) break;
                target.PoisonList.Remove(dose);
                if (master && GetSignatureTargets(impact, 2, 1, new[] { target }).FirstOrDefault() is { } next)
                {
                    next.ApplyPoison(new Poison { Owner = this, PType = dose.PType, Duration = 2,
                        TickSpeed = 1000, Value = Math.Max(1, dose.Value / 2) }, this);
                }
                break;
            }
            case Spell.SpiritCommand:
            case Spell.PackSignal:
            {
                MonsterObject pet = cast.Companion;
                if (!IsSignatureCompanion(pet, spell) || CurrentMap.Info.NoPets ||
                    !SignatureReach(pet.CurrentLocation, magic.Info.Range)) break;
                if (target == null)
                {
                    pet.PetRecall();
                    if (master) ChangeHP(Math.Min(power / 4, Math.Max(0, MaxHealth - HP)));
                }
                else
                {
                    if (!CanHitSignatureTarget(target) || !SignatureReach(target.CurrentLocation, magic.Info.Range) ||
                        !pet.CanFly(target.CurrentLocation)) break;
                    // Mir's pet aggression checks recognise explicit focus through the pet's assigned target.
                    var priorTarget = pet.Target;
                    pet.Target = target;
                    if (!target.IsAttackTarget(pet))
                    {
                        pet.Target = priorTarget;
                        break;
                    }
                }
                pet.ChangeHP(Math.Min(Math.Max(0, pet.MaxHealth - pet.Health), advanced ? ScaleSignature(power, 125) : power));
                BroadcastSignatureEffect(spell, pet.CurrentLocation, SpellEffect.SignatureImpact);
                landed = true;
                break;
            }
            case Spell.VeilStrike:
                landed = HitSignatureTarget(target, spell, cast.Concealed ? ScaleSignature(power, 124) : power, DefenceType.ACAgility);
                if (landed && cast.Concealed)
                {
                    if (!target.Dead)
                        target.ApplyPoison(new Poison { Owner = this, PType = PoisonType.Bleeding, TickSpeed = 1000,
                            Duration = advanced && target is not HumanObject ? 3 : 2, Value = Math.Max(1, power / 12) }, this);
                    if (master) ChangeMP(4);
                }
                break;
            case Spell.Riposte:
                landed = HitSignatureTarget(target, spell, power, DefenceType.ACAgility);
                if (landed)
                {
                    GrantSignatureWard(this, spell, ScaleSignature(power, advanced ? 55 : 40), 2800);
                    if (master) ShortenRiposte(magic);
                }
                break;
            case Spell.SmokeSnare:
            {
                BroadcastSignatureEffect(spell, cast.Location, SpellEffect.SignatureImpact);
                int hits = 0;
                foreach (MapObject enemy in GetSignatureTargets(cast.Location, 1, 4))
                {
                    if (!HitSignatureTarget(enemy, spell, power, DefenceType.AC)) continue;
                    hits++;
                    SlowSignatureTarget(enemy, advanced ? 3 : 2);
                }
                landed = hits > 0;
                if (master && hits >= 2) ChangeMP(4);
                break;
            }
            case Spell.CinderMine:
            {
                var enemies = GetSignatureTargets(cast.Location, 1, advanced ? 5 : 4);
                if (enemies.Count == 0)
                {
                    if (cast.Stage % 3 == 0) BroadcastSignatureEffect(spell, cast.Location, SpellEffect.SignatureArmed);
                    if (cast.Stage < 10) QueueSignatureStage(magic, cast, 400);
                    break;
                }

                BroadcastSignatureEffect(spell, cast.Location, SpellEffect.SignatureImpact);
                int hits = HitSignatureArea(spell, enemies, power, DefenceType.AC, 70);
                landed = hits > 0;
                if (master && hits >= 2) ChangeMP(4);
                break;
            }
        }

        // A multi-pulse cast advances proficiency at most once.
        if (landed && !cast.Trained) LevelMagic(magic);
    }

    private void QueueSignatureStage(UserMagic magic, SignatureCast cast, int delay) =>
        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + delay, magic, cast with { Stage = cast.Stage + 1 }));

    private static IEnumerable<Point> GlacialGateTiles(Point centre, MirDirection direction)
    {
        MirDirection side = Functions.NextDir(Functions.NextDir(direction));
        yield return centre;
        yield return Functions.PointMove(centre, side, 1);
        yield return Functions.PointMove(centre, Functions.ReverseDirection(side), 1);
    }

    protected void ResetSignatureState()
    {
        _signatureGeneration++;
        _signatureFieldUntil = 0;
        lock (_signatureWardLock)
        {
            _signatureWard = 0;
            _signatureWardUntil = 0;
        }
    }

    private void ShortenRiposte(UserMagic magic)
    {
        long reduction = Math.Min(1000, Math.Max(0, magic.GetDelay() - 1800));
        if (reduction == 0) return;
        // Update the client using the existing cooldown timestamp sync.
        magic.CastTime -= reduction;
        SendMagicInfo(magic);
    }

    private bool GrantSignatureWard(HumanObject recipient, Spell spell, int capacity, int milliseconds)
    {
        capacity = Math.Min(capacity, Math.Max(1, recipient.MaxHealth / 6));
        if (capacity <= 0) return false;
        lock (recipient._signatureWardLock)
        {
            if (recipient.Dead || (Envir.Time < recipient._signatureWardUntil && recipient._signatureWard >= capacity))
                return false;

            recipient._signatureWard = capacity;
            recipient._signatureWardUntil = Envir.Time + milliseconds;
        }
        BroadcastSignatureEffect(spell, recipient.CurrentLocation, SpellEffect.SignatureImpact);
        return true;
    }

    private int DamageAfterSignatureWard(int damage)
    {
        if (damage <= 0 || Volatile.Read(ref _signatureWard) <= 0) return damage;
        lock (_signatureWardLock)
        {
            if (Envir.Time >= _signatureWardUntil)
            {
                _signatureWard = 0;
                return damage;
            }

            int absorbed = Math.Min(damage, _signatureWard);
            _signatureWard -= absorbed;
            return damage - absorbed;
        }
    }

    private void SlowSignatureTarget(MapObject target, int pveSeconds)
    {
        if (!target.Dead)
            target.ApplyPoison(new Poison { Owner = this, PType = PoisonType.Slow, TickSpeed = 1000,
                Duration = target is HumanObject ? 1 : pveSeconds }, this);
    }

    private bool HitSignatureTarget(MapObject target, Spell spell, int power, DefenceType defence)
    {
        if (!CanHitSignatureTarget(target)) return false;
        Point impact = target.CurrentLocation;
        if (target is HumanObject) power = ScaleSignature(power, 62);
        if (target.Attacked(this, Math.Max(1, power), defence, false) <= 0) return false;
        BroadcastSignatureEffect(spell, impact, SpellEffect.SignatureImpact);
        return true;
    }

    private int HitSignatureArea(Spell spell, Point centre, int radius, int limit, int power, DefenceType defence, int secondaryPercent)
        => HitSignatureArea(spell, GetSignatureTargets(centre, radius, limit), power, defence, secondaryPercent);

    private int HitSignatureArea(Spell spell, List<MapObject> enemies, int power, DefenceType defence, int secondaryPercent)
    {
        int hits = 0;
        foreach (var enemy in enemies)
            if (HitSignatureTarget(enemy, spell, hits == 0 ? power : ScaleSignature(power, secondaryPercent), defence))
                hits++;

        return hits;
    }

    private List<MapObject> GetSignatureTargets(Point centre, int radius, int limit, IReadOnlyCollection<MapObject> excluded = null)
    {
        var found = new List<MapObject>(limit);
        for (int distance = 0; distance <= radius && found.Count < limit; distance++)
            for (int y = -distance; y <= distance && found.Count < limit; y++)
                for (int x = -distance; x <= distance && found.Count < limit; x++)
                    if (Math.Max(Math.Abs(x), Math.Abs(y)) == distance)
                        AddSignatureTargets(found, new Point(centre.X + x, centre.Y + y), limit, excluded);

        return found;
    }

    private void AddSignatureTargets(List<MapObject> found, Point point, int limit, IReadOnlyCollection<MapObject> excluded = null)
    {
        if (found.Count >= limit || !CurrentMap.ValidPoint(point)) return;
        var objects = CurrentMap.GetCell(point).Objects;
        if (objects == null) return;
        for (int i = 0; i < objects.Count && found.Count < limit; i++)
        {
            MapObject target = objects[i];
            if (CanHitSignatureTarget(target) && !found.Contains(target) && (excluded == null || !excluded.Contains(target)))
                found.Add(target);
        }
    }

    private static int ScaleSignature(int power, int percent) => (int)((long)power * percent / 100);

    private void BroadcastSignatureEffect(Spell spell, Point location, SpellEffect effect) =>
        CurrentMap.Broadcast(new S.MapEffect { Effect = effect, Location = location, Value = (byte)spell }, location);
}
