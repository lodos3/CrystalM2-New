using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private sealed record SignatureCast(Map Map, int Generation, Point Location, MapObject Target, MonsterObject Companion,
        MirDirection Direction, int Power, bool Concealed, int Stage = 0, bool Trained = false,
        Point LastImpact = default, List<MapObject> RelayTargets = null, byte RelayCharge = 0, bool Reinforced = false);

    private sealed record SignatureWard(HumanObject Source, Spell Spell, int Generation, byte Charge, long Until, Point Anchor)
    {
        public int Capacity;
        public bool Observed;
    }

    private int _signatureGeneration;
    private long _signatureFieldUntil, _lastBastionUntil, _lastLanternUntil;
    private List<SignatureWard> _signatureWards;
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
        SpellTime = Envir.Time + ability.CastRecovery;
        LogTime = Envir.Time + Globals.LogDelay;
        RegenTime = Envir.Time + RegenDelay;
        SpendTalentMagicCost(ability.Spell, cost);
        ApplyTalentRecovery(ability.Spell);
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
        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + TalentSignatureWindup(ability.Spell, target, ability.Windup), magic, cast));
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
        if (this is HeroObject hero)
            hero.Owner?.Enqueue(new S.ObjectMagic { ObjectID = ObjectID, Spell = spell, Location = CurrentLocation, Direction = Direction, Cast = false });
        else
        {
            Enqueue(new S.Magic { Spell = spell, Cast = false });
            if (!SpecialisationAbilities.TryGet(spell, out _))
                Enqueue(new S.UserLocation { Direction = Direction, Location = CurrentLocation });
        }
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

    protected Poison FindDistilPoison(MapObject target, PoisonType requested = PoisonType.None)
    {
        var doses = target.PoisonList.Where(poison => poison.Source == this && !poison.CopiedDose && poison.Duration > poison.Time &&
            poison.PType is PoisonType.Green or PoisonType.Red);
        if (requested != PoisonType.None) return doses.FirstOrDefault(poison => poison.PType == requested);
        PoisonType preferred = GetPoison(1)?.Info.Shape == 2 ? PoisonType.Red : PoisonType.Green;
        return doses.FirstOrDefault(poison => poison.PType == preferred) ?? doses.FirstOrDefault();
    }

    private void CompleteSpecialisationSpell(UserMagic magic, SpecialisationAbility ability, SignatureCast cast)
    {
        if (Dead || Node == null || cast.Map != CurrentMap || cast.Generation != _signatureGeneration ||
            GetMagic(ability.Spell) != magic) return;
        // Short wind-ups can be interrupted. Active fields finish while their caster remains on the map.
        if (cast.Stage == 0 && (CurrentPoison & (TalentCastLock | PoisonType.Stun | PoisonType.Dazed | PoisonType.Paralysis | PoisonType.Frozen)) != 0)
            return;

        bool advanced = !TalentCatalog.Supports(Class) && Level >= SpecialisationAbilities.AdvancedTraitLevel;
        bool master = !TalentCatalog.Supports(Class) && Level >= SpecialisationAbilities.MasterTraitLevel;
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
                if (landed && spell == Spell.SteelCut) RecordComboHit(spell, target);
                break;
            case Spell.SpiritSpark:
                landed = HitSignatureTarget(target, spell, power, DefenceType.MAC);
                if (landed) RecordComboHit(spell, target);
                break;
            case Spell.Notchbreaker:
            case Spell.Pinpoint:
            {
                int distance = Functions.MaxDistance(CurrentLocation, target.CurrentLocation);
                bool far = distance >= (spell == Spell.Notchbreaker ? 2 : 5);
                if (far) power = ScaleSignature(power, advanced ? 125 : 115);
                bool opening = spell == Spell.Notchbreaker && ComboStacks(spell, target) > 0;
                if (opening) power = ScaleSignature(power, 118);
                landed = HitSignatureTarget(target, spell, power, DefenceType.ACAgility);
                if (landed && opening)
                {
                    SpendCombo(spell, target);
                    if (!target.Dead && Talent(8, 1) > 0)
                        StartTalentField(spell, BuffType.TalentDuelCircle, target.CurrentLocation, 2, 4000, 8, target);
                }
                if (landed && far && master) ChangeMP(4);
                break;
            }
            case Spell.RedWake:
            {
                BroadcastSignatureEffect(spell, CurrentLocation, SpellEffect.SignatureImpact);
                byte momentum = ComboStacks(spell);
                var enemies = GetSignatureTargets(CurrentLocation, 1, Talent(1, 0) > 0 ? 5 : 4);
                MapObject primary = enemies.FirstOrDefault();
                if (enemies.Count == 1 && momentum > 0)
                    power = ScaleSignature(power, Talent(8, 2) > 0 ? 100 + momentum * 17 : 106);
                int hits = HitSignatureArea(spell, enemies, power, DefenceType.AC, 55 + momentum * 12);
                landed = hits > 0;
                if (landed && momentum > 0)
                {
                    bool spendTwo = hits >= 2 || Talent(8, 2) > 0;
                    SpendCombo(spell, primary, amount: spendTwo ? 2 : 1);
                    if (spendTwo && momentum == 2 && Talent(8, 1) > 0)
                        StartTalentField(spell, BuffType.None, CurrentLocation, 1, 3000, power / 4, this);
                    if (hits >= 2 && Talent(2, 1) > 0) ChangeMP(Talent(2, 1));
                }
                if (master && hits >= 3) ChangeMP(4);
                break;
            }
            case Spell.Interpose:
            {
                var ally = (HumanObject)target;
                int capacity = ScaleSignature(power, 100 + (ally != this && Functions.InRange(CurrentLocation, ally.CurrentLocation, 1) ? Talent(1, 2) * 5 : 0));
                byte guard = ComboStacks(spell);
                capacity = ScaleSignature(capacity, 100 + guard * 15);
                bool lastBastion = guard == 2 && ally == this && PercentHealth < 40 && Talent(8, 1) > 0 && Envir.Time >= _lastBastionUntil;
                if (lastBastion) capacity = ScaleSignature(capacity, 130);
                landed = GrantSignatureWard(ally, spell, capacity, 4000, guard);
                if (landed && lastBastion) _lastBastionUntil = Envir.Time + 25000;
                if (landed && guard > 0) SpendCombo(spell, ally, variant: ally == this ? TalentRotations.Isolated : 1, ally: ally);
                if (landed && guard == 2 && ally != this && Talent(8, 2) > 0)
                {
                    var second = TalentAllies(ally.CurrentLocation, 2).FirstOrDefault(candidate => candidate != ally && candidate != this);
                    if (second != null) GrantSignatureWard(second, spell, capacity / 3, 3000);
                }
                break;
            }
            case Spell.AshSeal:
            {
                int percent = cast.Reinforced && cast.Stage == 2 ? 52 : 39;
                byte embers = cast.Trained ? (byte)0 : ComboStacks(spell);
                int pulse = ScaleSignature(ScaleSignature(power, percent), 100 + embers * 12);
                MapObject primary = null, singed = null;
                foreach (MapObject enemy in GetSignatureTargets(cast.Location, 1, 5))
                {
                    int amount = primary == null ? pulse : ScaleSignature(pulse, 80);
                    if (primary == null && embers > 0 && enemy == _lastBoltTarget && Envir.Time - _lastBoltHit <= 3000)
                        amount = ScaleSignature(amount, 100 + Talent(2, 1) * 4);
                    if (!HitSignatureTarget(enemy, spell, amount, DefenceType.MAC)) continue;
                    primary ??= enemy;
                    if (HasOwnedStatus(enemy, BuffType.TalentSinge)) singed ??= enemy;
                }
                landed = primary != null;
                if (landed && embers > 0) SpendCombo(spell, singed ?? primary, variant: singed != null ? 1 : 0, tile: cast.Location);
                BroadcastSignatureEffect(spell, cast.Location, SpellEffect.SignatureImpact);
                if (cast.Stage < 2)
                {
                    bool reinforced = cast.Reinforced || (landed && embers == 2 && Talent(6, 1) > 0);
                    QueueSignatureStage(magic, cast with { Trained = cast.Trained || landed, Reinforced = reinforced },
                        cast.Stage == 1 && reinforced ? 1050 : 800);
                }
                else if (landed && master) ChangeMP(4);
                break;
            }
            case Spell.GlacialGate:
            {
                var enemies = new List<MapObject>(4);
                foreach (Point tile in GlacialGateTiles(cast.Location, cast.Direction))
                {
                    if (!CurrentMap.ValidPoint(tile)) continue;
                    AddSignatureTargets(enemies, tile, 4);
                    BroadcastSignatureEffect(spell, tile, SpellEffect.SignatureImpact);
                }
                foreach (var enemy in enemies)
                {
                    int damage = advanced && enemy.CurrentPoison.HasFlag(PoisonType.Slow) ? ScaleSignature(power, 120) : power;
                    bool shatter = HasRime(enemy);
                    if (shatter) damage = ScaleSignature(damage, 125);
                    if (!HitSignatureTarget(enemy, spell, damage, DefenceType.MAC, showImpact: false)) continue;
                    if (shatter) SpendRime(enemy);
                    SlowSignatureTarget(enemy, 2);
                    landed = true;
                }
                if (landed && Talent(8, 2) > 0) GrantSignatureWard(this, spell, power / 4, 2500);
                if (Talent(6, 1) > 0 || Talent(8, 1) > 0)
                    StartTalentField(spell, BuffType.None, cast.Location, Talent(8, 1) > 0 ? 1 : 0, 2700, 0);
                break;
            }
            case Spell.StormRelay:
            {
                if (!CanHitSignatureTarget(target) || (cast.Stage > 0 &&
                    !Functions.InRange(cast.LastImpact, target.CurrentLocation, 3))) break;

                Point impact = target.CurrentLocation;
                landed = HitSignatureTarget(target, spell,
                    ScaleSignature(power, cast.Stage == 0 ? 100 : 55 + cast.RelayCharge * 12 + (cast.Stage == 1 ? Talent(2, 0) * 4 : 0)), DefenceType.MAC);
                if (!landed) break;
                if (cast.Stage > 0 && cast.RelayTargets?.LastOrDefault() is { } prior && prior.CurrentLocation == cast.LastImpact)
                    CurrentMap.Broadcast(new S.ObjectProjectile { Spell = spell, Source = prior.ObjectID, Destination = target.ObjectID }, impact);
                if (cast.Stage == 1 && Talent(6, 0) > 0) ShortenTalentCooldown(spell, 1100);
                if (cast.Stage == 2 && master) ChangeMP(4);
                int max = Talent(8, 0) > 0 ? 4 : 3;
                if (cast.Stage + 1 < max)
                {
                    var visited = cast.RelayTargets ?? new List<MapObject>(max);
                    visited.Add(target);
                    var next = GetSignatureTargets(impact, 3, 1, visited).FirstOrDefault();
                    if (next == null && cast.Stage == 0 && Envir.Time < _fluxUntil && Functions.InRange(impact, _fluxPylon, 3))
                    {
                        next = GetSignatureTargets(_fluxPylon, 3, 1, visited).FirstOrDefault();
                        if (next != null) impact = _fluxPylon;
                    }
                    if (next != null)
                    {
                        byte charge = cast.Stage == 0 ? ComboStacks(spell) : cast.RelayCharge;
                        if (cast.Stage == 0 && charge > 0) SpendCombo(spell, target, variant: 1);
                        QueueSignatureStage(magic, cast with
                        {
                            Target = next, LastImpact = impact, RelayTargets = visited, Trained = true, RelayCharge = charge
                        }, 95);
                    }
                    else if (cast.Stage == 0 && HasOwnedStatus(target, BuffType.TalentConductor) && ComboStacks(spell) > 0)
                    {
                        byte charge = ComboStacks(spell);
                        SpendCombo(spell, target, variant: TalentRotations.Isolated);
                        target.RemoveBuff(BuffType.TalentConductor);
                        HitSignatureTarget(target, spell, ScaleSignature(power, 18 + charge * (Talent(6, 1) > 0 ? 13 : 9)), DefenceType.MAC);
                        if (Talent(8, 1) > 0 && charge == 2)
                            QueueSignatureStage(magic, cast with { Stage = max - 1, LastImpact = impact, Power = power / 2, Trained = true }, 340);
                    }
                }
                break;
            }
            case Spell.LanternLink:
            {
                var ally = (HumanObject)target;
                int before = ally.HP;
                ally.ChangeHP(Math.Min(Math.Max(0, ally.MaxHealth - before), power));
                int effective = Math.Max(0, ally.HP - before);
                byte reserve = ComboStacks(spell);
                int ward = power / 2 + (Talent(1, 2) > 0 ? effective / 3 : 0) + ScaleSignature(power, reserve * (15 + Talent(2, 2) * 2));
                bool protectedAlly = GrantSignatureWard(ally, spell, ward, Talent(6, 2) > 0 && ally.PercentHealth < 50 ? 4300 : 3500, reserve);
                if (protectedAlly && reserve > 0) SpendCombo(spell, ally, ally: ally);
                CreditTalentHealing(ally, spell, effective, grantReserve: false);
                landed = protectedAlly || effective > 0;
                if (Talent(8, 1) > 0 && reserve == 2 && protectedAlly)
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
                if (cast.Stage > 0)
                {
                    if (CanHitSignatureTarget(target) && Functions.InRange(cast.Location, target.CurrentLocation, 2))
                        HitSignatureTarget(target, spell, power, DefenceType.MAC);
                    break;
                }
                Poison dose = FindDistilPoison(target);
                if (dose == null) break;
                bool ink = ComboStacks(spell, target) > 0;
                int remaining = (int)Math.Min(dose.Duration - dose.Time, 4);
                int bonus = remaining * (advanced || (dose.PType == PoisonType.Green && Talent(1, 0) > 0) ? 8 : 5) + (ink ? 18 : 0);
                Point impact = target.CurrentLocation;
                landed = HitSignatureTarget(target, spell, ScaleSignature(power, 100 + bonus), DefenceType.MAC);
                if (!landed) break;
                if (ink) SpendCombo(spell, target, variant: dose.PType == PoisonType.Red ? TalentRotations.RedDose : TalentRotations.GreenDose);
                target.PoisonList.Remove(dose);
                if (dose.PType == PoisonType.Red)
                {
                    ApplyTalentStatus(target, BuffType.TalentSealed, 4000, Stat.MaxAC, -2);
                    if (Talent(6, 1) > 0 && FindDistilPoison(target, PoisonType.Green) is { } green) green.NextTickBonus += Math.Max(1, green.Value / 10);
                }
                else
                {
                    ReleaseTalentVessel(target, dose.Value);
                    if (Talent(8, 0) > 0 && remaining >= 3 && !target.Dead)
                    {
                        BroadcastSignatureEffect(spell, impact, SpellEffect.SignatureWindup);
                        QueueSignatureStage(magic, cast with { Power = power / 3, Trained = true }, 350);
                    }
                }
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
                    if (Talent(7, 1) > 0 && Envir.Time < _nextRecallUntil)
                    {
                        _nextRecallUntil = 0;
                        ActionTime = Math.Max(Envir.Time, ActionTime - 150);
                    }
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
                    if (spell == Spell.SpiritCommand) ObserveRotation(RotationEvent.Focus, spell, target);
                }
                int missing = Math.Max(0, pet.MaxHealth - pet.Health);
                int baseMend = advanced || Talent(1, 1) > 0 ? ScaleSignature(power, 125) : power;
                if (Talent(6, 0) > 0 && Functions.InRange(CurrentLocation, pet.CurrentLocation, 2)) baseMend = ScaleSignature(baseMend, 112);
                byte bond = spell == Spell.SpiritCommand ? ComboStacks(spell) : (byte)0;
                int mend = Math.Min(missing, ScaleSignature(baseMend, 100 + bond * 20));
                pet.ChangeHP(mend);
                if (bond > 0 && mend > baseMend)
                {
                    SpendCombo(spell);
                    if (target == null) ObserveRotation(RotationEvent.Recall, spell, pet);
                    if (Talent(2, 1) > 0) ChangeMP(Talent(2, 1));
                    if (Talent(6, 1) > 0 && HP < MaxHealth && pet.HP < pet.MaxHealth)
                        ApplyTalentStatus(pet, BuffType.TalentRimeguard, 3000, Stat.DamageReductionPercent, 7);
                    if (bond == 2 && Talent(8, 0) > 0)
                    {
                        var ally = TalentAllies(pet.CurrentLocation, 2, 1).FirstOrDefault();
                        if (ally != null) GrantSignatureWard(ally, spell, power / 3, 3000);
                    }
                    if (bond == 2 && Talent(8, 1) > 0)
                        StartTalentField(spell, BuffType.TalentAncestralForm, pet.CurrentLocation, 2, 4000, 7, pet);
                }
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
        if (landed && !cast.Trained) ResolveSpellPerks(magic);
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
        ResetCombo();
        ResetTalentRotation();
        ResetTalentMagic();
        _signatureFieldUntil = 0;
        lock (_signatureWardLock)
        {
            _signatureWards?.Clear();
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

    private bool GrantSignatureWard(HumanObject recipient, Spell spell, int capacity, int milliseconds, byte charge = 0)
    {
        capacity = Math.Min(capacity, Math.Max(1, recipient.MaxHealth / 6));
        if (capacity <= 0) return false;
        lock (recipient._signatureWardLock)
        {
            if (recipient.Dead) return false;
            var wards = recipient._signatureWards ??= new List<SignatureWard>(4);
            wards.RemoveAll(ward => !recipient.ValidSignatureWard(ward));
            SignatureWard existing = wards.Find(ward => ward.Source == this && ward.Spell == spell);
            if (existing != null && existing.Capacity >= capacity) return false;
            if (existing != null) wards.Remove(existing);
            if (wards.Count >= 8)
            {
                SignatureWard smallest = wards.MinBy(ward => ward.Capacity);
                if (smallest.Capacity >= capacity) return false;
                wards.Remove(smallest);
            }
            wards.Add(new SignatureWard(this, spell, _signatureGeneration, charge, Envir.Time + milliseconds, recipient.CurrentLocation)
                { Capacity = capacity });
        }
        BroadcastSignatureEffect(spell, recipient.CurrentLocation,
            SpellCurriculum.IsRetired(spell) ? SpellEffect.SignatureImpact : SpellEffect.SpellPerkWard);
        return true;
    }

    private bool ValidSignatureWard(SignatureWard ward) => ward.Capacity > 0 && Envir.Time < ward.Until &&
        ward.Source._signatureGeneration == ward.Generation && ward.Source.CurrentMap == CurrentMap &&
        (ward.Spell != Spell.IceglassRefuge || Functions.InRange(ward.Anchor, CurrentLocation, 1));

    private int DamageAfterSignatureWard(int damage, MapObject attacker = null, DefenceType defence = DefenceType.None)
    {
        if (damage <= 0) return damage;
        if (attacker != null) ReactToTalentHit(attacker);
        SignatureWard ward = null;
        bool observed = false;
        int absorbed = 0;
        SanctuaryPool pool = FindSanctuaryPool();
        lock (_signatureWardLock)
        {
            _signatureWards?.RemoveAll(candidate => !ValidSignatureWard(candidate));
            if (_signatureWards != null)
            {
                foreach (SignatureWard candidate in _signatureWards)
                {
                    if (candidate.Spell is Spell.Skywire or Spell.StormRelay && defence is not (DefenceType.MAC or DefenceType.MACAgility)) continue;
                    if (ward == null || candidate.Capacity > ward.Capacity) ward = candidate;
                }
            }
            if (pool != null && Volatile.Read(ref pool.Capacity) > (ward?.Capacity ?? 0))
            {
                do
                {
                    int available = Volatile.Read(ref pool.Capacity);
                    absorbed = Math.Min(damage, available);
                    if (Interlocked.CompareExchange(ref pool.Capacity, available - absorbed, available) == available) break;
                } while (true);
                if (absorbed > 0) return damage - absorbed;
            }
            if (ward != null)
            {
                if (ward.Spell == Spell.Interpose && attacker != null && !ward.Observed)
                {
                    bool magical = defence is DefenceType.MAC or DefenceType.MACAgility;
                    if (ward.Charge > 0 && !magical && Direction == Functions.DirectionFromPoint(CurrentLocation, attacker.CurrentLocation))
                        ward.Capacity = ScaleSignature(ward.Capacity, 100 + 4 * ward.Source.Talent(2, 1));
                    if (ward.Source != this && Functions.MaxDistance(CurrentLocation, attacker.CurrentLocation) > 1)
                        damage = ScaleSignature(damage, 100 - 3 * ward.Source.Talent(2, 2));
                }
                absorbed = Math.Min(damage, ward.Capacity);
                ward.Capacity -= absorbed;
                observed = ward.Observed;
                if (attacker != null) ward.Observed = true;
            }
        }
        if (ward != null && attacker != null && absorbed > 0)
        {
            if (ward.Charge != 0 && !observed)
                ward.Source.ObserveRotation(RotationEvent.WardHit, ward.Spell, ward.Spell == Spell.Interpose ? attacker : this,
                    ward.Charge, this != ward.Source ? 1 : TalentRotations.Isolated, this);
            if (!observed && ward.Spell == Spell.Interpose && ward.Source != this && ward.Source.Talent(6, 2) > 0)
                ward.Source.ChangeMP(2);
            if (ward.Spell == Spell.LanternLink && ward.Capacity == 0)
            {
                if (ward.Source.Talent(7, 2) > 0) ward.Source.ApplyTalentStatus(attacker, BuffType.TalentDulledFervour, 3500, value: 8);
                if (ward.Source.Talent(8, 2) > 0 && ward.Charge == 2 && Envir.Time >= _lastLanternUntil)
                {
                    _lastLanternUntil = Envir.Time + 20000;
                    ChangeHP(Math.Min(MaxHealth - HP, absorbed / 3));
                }
            }
        }
        return TransferTalentDamage(damage - absorbed, attacker);
    }

    private void SlowSignatureTarget(MapObject target, int pveSeconds)
    {
        if (!target.Dead)
            target.ApplyPoison(new Poison { Owner = this, PType = PoisonType.Slow, TickSpeed = 1000,
                TickTime = Envir.Time + 1000, Duration = target is HumanObject ? 1 : pveSeconds }, this);
    }

    private bool HitSignatureTarget(MapObject target, Spell spell, int power, DefenceType defence, bool showImpact = true)
    {
        if (!CanHitSignatureTarget(target)) return false;
        Point impact = target.CurrentLocation;
        if (target is HumanObject) power = ScaleSignature(power, 62);
        if (TalentSpellDamage(target, spell, Math.Max(1, power), defence) <= 0) return false;
        if (showImpact) BroadcastSignatureEffect(spell, impact, SpellEffect.SignatureImpact);
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
