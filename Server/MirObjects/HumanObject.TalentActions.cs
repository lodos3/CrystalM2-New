using System.Drawing;
using Server.MirDatabase;
using Server.MirEnvir;

namespace Server.MirObjects;

public partial class HumanObject
{
    private sealed record TalentTick(Map Map, int Generation, MapObject Target, BuffType Status, object Token, int Remaining, int Power, bool Heal);
    private sealed class SanctuaryPool(Point cell, int capacity)
    {
        public readonly Point Cell = cell;
        public int Capacity = capacity;
    }

    private SanctuaryPool _sanctuary;
    private long _lastMeteorHit = long.MinValue;
    private long _walkingBlightUntil;

    internal int MeteorTalentPower(int power, long start) => start == _lastMeteorHit ? power : ScaleSignature(power, 100 + ComboStacks(Spell.AshSeal) * 12);

    internal void RecordMeteorTalentHit(MapObject enemy, Point centre, long start)
    {
        if (start == _lastMeteorHit) return;
        _lastMeteorHit = start;
        SpendCombo(Spell.AshSeal, enemy, Spell.MeteorStrike, HasOwnedStatus(enemy, BuffType.TalentSinge) ? 1 : 0, tile: centre);
    }

    internal void RecordOrdinarySpiritHit(MonsterObject spirit, MapObject enemy)
    {
        if (OwnsCombo(Spell.SpiritCommand) && IsSignatureCompanion(spirit, Spell.SpiritCommand))
            ObserveRotation(RotationEvent.CompanionHit, Spell.None, enemy);
    }

    private SanctuaryPool FindSanctuaryPool()
    {
        if (!HasBuff(BuffType.TalentSanctuary, out Buff buff) || buff.Caster is not HumanObject source ||
            source.CurrentMap != CurrentMap || buff.Get<SanctuaryPool>("Pool") is not { } pool || source._sanctuary != pool ||
            !Functions.InRange(CurrentLocation, pool.Cell, 1)) return null;
        return pool;
    }

    private void ReactToTalentHit(MapObject attacker)
    {
        if (!CanHitSignatureTarget(attacker)) return;
        if (HasBuff(BuffType.TalentStaticMantle, out Buff mantle) && !mantle.FlagForRemoval)
        {
            RemoveBuff(BuffType.TalentStaticMantle);
            BuildCombo(Spell.StormRelay);
            if (SignatureReach(attacker.CurrentLocation, 3))
                HitSignatureTarget(attacker, Spell.StaticMantle, mantle.Values[0], DefenceType.MAC);
        }
        if (HasBuff(BuffType.TalentPhoenixShroud, out Buff phoenix) && !phoenix.FlagForRemoval)
        {
            RemoveBuff(BuffType.TalentPhoenixShroud);
            HitSignatureArea(Spell.PhoenixShroud, CurrentLocation, 1, 3, phoenix.Values[0], DefenceType.MAC, 65);
        }
    }

    private int TransferTalentDamage(int damage, MapObject attacker)
    {
        if (damage <= 0 || attacker == null || !HasBuff(BuffType.TalentGuardianTether, out Buff tether) || tether.FlagForRemoval ||
            tether.Caster is not HumanObject guard || guard.Dead || guard.CurrentMap != CurrentMap ||
            !Functions.InRange(guard.CurrentLocation, CurrentLocation, 5)) return damage;
        RemoveBuff(BuffType.TalentGuardianTether);
        int portion = Math.Min(damage / 3, tether.Values[0]);
        if (attacker is MonsterObject { Master: null }) guard.ChangeHP(-Math.Min(portion, Math.Max(0, guard.HP - 1)));
        else portion /= 2;
        return damage - portion;
    }

    private bool ResolveTalentAction(UserMagic magic, TalentCast cast)
    {
        Spell spell = magic.Spell;
        MapObject target = cast.Target;
        int power = cast.Power;
        int engine = cast.Engine;
        int stage = cast.Stage;
        var defence = Info.Class == MirClass.Warrior ? DefenceType.AC : DefenceType.MAC;
        switch (spell)
        {
            case Spell.RallyingStandard:
            {
                int radius = ClassTalent(6, 1) > 0 ? 3 : 2;
                foreach (HumanObject ally in TalentAllies(cast.Cell, radius))
                    ApplyTalentStatus(ally, BuffType.TalentStandard, radius == 3 ? 2000 : 1200, value: 20);
                return ContinueZone(6, 1000);
            }
            case Spell.ArcaneHammer:
            {
                bool opening = ComboStacks(Spell.Notchbreaker, target) > 0;
                if (!Hit(target, ScaleSignature(power, opening ? 120 + Talent(2, 0) * 4 : 100))) return false;
                if (opening)
                {
                    SpendCombo(Spell.Notchbreaker, target, spell);
                    ApplyTalentStatus(target, BuffType.TalentCrackedPlate, 4000, Stat.MaxAC, Talent(8, 0) > 0 ? -6 : -4);
                    if (Talent(6, 0) > 0) ApplyTalentStatus(target, BuffType.TalentOffBalance, 4000, value: 8);
                    if (Talent(8, 0) > 0) Periodic(target, BuffType.TalentCrackedPlate, power / 3, 1, 350, preserveStatus: true);
                }
                else if (Talent(5, 1, 1) > 0) ChangeMP(2);
                if (HasOwnedStatus(target, BuffType.TalentBrand))
                {
                    target.RemoveBuff(BuffType.TalentBrand);
                    Periodic(target, BuffType.TalentForgedWound, power / 8, 3);
                }
                return true;
            }
            case Spell.Oathbrand:
                if (!Hit(target)) return false;
                ApplyTalentStatus(target, BuffType.TalentBrand, 4000);
                RecordComboHit(spell, target);
                return true;
            case Spell.Oathstep:
            case Spell.PrismStep:
                if (!MoveTalentActor(this, cast.Cell)) return false;
                BroadcastSignatureEffect(spell, cast.Origin, SpellEffect.SignatureImpact);
                BroadcastSignatureEffect(spell, CurrentLocation, SpellEffect.SignatureImpact);
                if (spell == Spell.PrismStep && ClassTalent(4, 0, 0) > 0)
                    foreach (MapObject enemy in GetSignatureTargets(cast.Origin, 1, 3).OfType<MonsterObject>().Where(m => !m.Info.IsBoss))
                        SlowSignatureTarget(enemy, 2);
                if (spell == Spell.PrismStep && ClassTalent(4, 0, 1) > 0 && GetSignatureTargets(cast.Origin, 1, 1).Count == 0)
                {
                    ShortenTalentCooldown(spell, 1800);
                    _localBlinkUntil -= 1800;
                }
                return true;
            case Spell.Forgefall:
                if (!Hit(target, ScaleSignature(power, engine == 0 && HasOwnedStatus(target, BuffType.TalentCrackedPlate) ? 125 : engine == 1 ? 80 : 100))) return false;
                ApplyTalentStatus(target, BuffType.TalentOffBalance, 3000, value: 8);
                if (engine == 0) Periodic(target, BuffType.TalentForgedWound, power / 8, 3);
                if (engine == 1) Ward(this, power / 2, BuffType.TalentOathguard);
                if (engine == 2) ApplyTalentStatus(this, BuffType.TalentFarFocus, 4000, Stat.Accuracy, 3);
                if (Talent(7, 0) > 0) ApplyTalentStatus(this, BuffType.TalentHammerPoise, 4000, value: 130);
                if (Talent(6, 1) > 0) ShortenTalentCooldown(Spell.ArcaneHammer, 1000);
                if (Talent(7, 1) > 0 && Functions.MaxDistance(CurrentLocation, target.CurrentLocation) == 2) ChangeMP(3);
                return true;
            case Spell.GaleCleaver:
            case Spell.BulwarkSlam:
            {
                var enemies = TalentTargets(FrontTalentCells(cast.Origin, cast.Direction), 3);
                if (spell == Spell.GaleCleaver && enemies.Count >= 2) power = ScaleSignature(power, 100 + Talent(2, 0) * 4);
                if (spell == Spell.GaleCleaver && enemies.Count == 1 && Talent(5, 0, 1) > 0) power = ScaleSignature(power, 110);
                var hits = Strike(enemies, power);
                if (hits.Count == 0) return false;
                if (spell == Spell.GaleCleaver)
                {
                    if (Envir.Time < _nextCrescentUntil) { _nextCrescentUntil = 0; ChangeMP(3); }
                    if (hits.Count >= 2)
                    {
                        BuildCombo(Spell.RedWake);
                        if (Talent(6, 0) > 0) Periodic(hits[0], BuffType.TalentRiven, power / 10, 2);
                        if (Talent(8, 0) > 0)
                            Strike(GetSignatureTargets(hits[0].CurrentLocation, 1, 2, hits), power / 3);
                    }
                }
                else foreach (var enemy in hits.OfType<MonsterObject>().Where(mob => !mob.Info.IsBoss))
                {
                    ApplyTalentStatus(enemy, BuffType.TalentShaken, 3000, Stat.MaxDC, -3);
                    if (Talent(1, 0) > 0 || Talent(6, 0) > 0) enemy.Target = this;
                }
                if (spell == Spell.BulwarkSlam && Talent(2, 0) > 0 && hits.Any(enemy => enemy.Target == this))
                    ShortenTalentCooldown(spell, magic.GetDelay() * Talent(2, 0) / 20);
                if (spell == Spell.BulwarkSlam && Talent(8, 0) > 0)
                    StartTalentField(spell, BuffType.TalentHoldLine, CurrentLocation, 2, 3000, 10);
                ObserveRotation(RotationEvent.Hit, spell, hits[0], hits.Count);
                return true;
            }
            case Spell.Bloodrush:
                if (!MoveNear(target, this, 2) || !Hit(target)) return false;
                if (target is MonsterObject) Periodic(target, BuffType.TalentRiven, power / 8, 3);
                return true;
            case Spell.DrakesEcho:
                _echoTarget = null;
                _echoUntil = Envir.Time + 4000;
                BroadcastSignatureEffect(spell, CurrentLocation, SpellEffect.SignatureArmed);
                return true;
            case Spell.Drakefall:
            {
                List<MapObject> hits;
                if (engine == 1)
                {
                    if (!CanHitSignatureTarget(target) || !Functions.InRange(CurrentLocation, target.CurrentLocation, 2) || !Hit(target)) return false;
                    Hit(target, power / 2);
                    hits = [target];
                    ApplyTalentStatus(target, BuffType.TalentWinded, 3000, value: 8);
                    ApplyTalentStatus(this, BuffType.TalentDrakeHaste, 3000, Stat.AttackSpeed, 2);
                    if (Talent(7, 1) > 0) ChangeMP(3);
                }
                else if (stage != 0)
                {
                    Strike(GetSignatureTargets(CurrentLocation, 1, 3), power / 4);
                    return ContinueZone(4, 800);
                }
                else
                {
                    hits = Strike(TalentTargets(FrontTalentCells(cast.Origin, cast.Direction), 3), power);
                    if (hits.Count == 0) return false;
                    if (engine == 0)
                    {
                        Strike(GetSignatureTargets(hits[0].CurrentLocation, 1, Math.Min(2, 5 - hits.Count), hits), power / 2);
                        ApplyTalentStatus(hits[0], BuffType.TalentExposedFlank, 4000, value: 10);
                    }
                    else if (engine == 2)
                    {
                        ApplyTalentStatus(this, BuffType.TalentRushingBlood, 3000, value: 12);
                        ContinueZone(4, 800);
                    }
                }
                ApplyTalentStatus(this, BuffType.TalentRedRhythm, Talent(7, 0) > 0 && hits.Count > 1 ? 6000 : 4000, value: 3);
                if (Talent(7, 2) > 0 && Envir.Time - _lastTalentMove < 3000) ActionTime = Math.Max(Envir.Time, ActionTime - 100);
                return true;
            }
            case Spell.IronChallenge:
                if (target is not MonsterObject { Info.IsBoss: false } mob) return false;
                Taunt(mob);
                if (Talent(5, 0, 0) > 0)
                    foreach (MonsterObject other in GetSignatureTargets(mob.CurrentLocation, 1, 2, [mob]).OfType<MonsterObject>())
                        if (!other.Info.IsBoss) Taunt(other);
                if (Talent(5, 0, 1) > 0) ShortenTalentCooldown(spell, 1500);
                return true;
            case Spell.GuardianTether:
                if (target == this) return false;
                ApplyTalentStatus(target, BuffType.TalentGuardianTether, 3000, value: Math.Max(1, MaxHealth / (Talent(5, 2, 1) > 0 ? 8 : 10)));
                return Impact(target.CurrentLocation);
            case Spell.Gatebreaker:
            {
                var enemies = TalentTargets(FrontTalentCells(cast.Origin, cast.Direction), 3);
                var hits = Strike(enemies, ScaleSignature(power + Math.Min(Stats[Stat.MaxAC] / 4, power / 3), engine == 0 ? 110 : 85));
                if (hits.Count == 0) return false;
                ApplyTalentStatus(hits[0], BuffType.TalentDented, 4000, value: 8);
                if (engine == 0) ApplyTalentStatus(hits[0], BuffType.TalentRattledAim, 3000, Stat.Accuracy, -4);
                if (engine == 1) ApplyTalentStatus(this, BuffType.TalentBastionPlate, 3000, Stat.DamageReductionPercent, 7);
                if (engine == 2 && cast.Ally is HumanObject rescued && CanShieldSignatureTarget(rescued) && SignatureReach(rescued.CurrentLocation, 5))
                    Ward(rescued, power / 2, BuffType.TalentRescueCover);
                if (Talent(7, 0) > 0 && hits.Any(enemy => enemy.Target == this))
                    ApplyTalentStatus(this, BuffType.TalentAnchorPoise, 4000, value: 150);
                if (Talent(7, 1) > 0) _lastTalentGuard = 0;
                return true;
            }
            case Spell.CinderLance:
                if (!Hit(target)) return false;
                int burn = power / (HasOwnFireWall(target.CurrentLocation) ? 5 : 8);
                if (target == _lastBoltTarget && Envir.Time - _lastBoltHit < 3000) burn = ScaleSignature(burn, 100 + Talent(2, 0) * 5);
                if (Talent(8, 0) > 0) burn = ScaleSignature(burn, 118);
                Periodic(target, BuffType.TalentSinge, burn, 3);
                if (Talent(5, 0, 1) > 0 && HasOwnFireWall(target.CurrentLocation)) ShortenTalentCooldown(spell, 850);
                RecordComboHit(spell, target);
                return true;
            case Spell.KilnBurst:
            {
                Impact(cast.Cell);
                var hits = Strike(GetSignatureTargets(cast.Cell, 1, 5), power);
                var singed = hits.FirstOrDefault(enemy => HasOwnedStatus(enemy, BuffType.TalentSinge));
                if (singed != null && hits.Count < 5)
                {
                    singed.RemoveBuff(BuffType.TalentSinge);
                    var splash = GetSignatureTargets(singed.CurrentLocation, 1, 1, hits).FirstOrDefault();
                    Hit(splash, power / 2);
                }
                return hits.Count > 0;
            }
            case Spell.PhoenixShroud:
            case Spell.StaticMantle:
                ApplyTalentStatus(this, spell == Spell.PhoenixShroud ? BuffType.TalentPhoenixShroud : BuffType.TalentStaticMantle,
                    spell == Spell.PhoenixShroud ? 2000 : 3000 + (Talent(5, 2, 1) > 0 ? 1000 : 0), value: power);
                return Impact(CurrentLocation);
            case Spell.FurnaceHeart:
            {
                Impact(cast.Cell);
                var hits = Strike(GetSignatureTargets(cast.Cell, engine == 2 ? 0 : 1, 5), power);
                if (hits.Count == 0) return false;
                ApplyTalentStatus(hits[0], BuffType.TalentHeatFracture, 4000, value: 9);
                if (engine == 0)
                {
                    Periodic(hits[0], BuffType.TalentCinderScar, power / 9, 3);
                    ApplyTalentStatus(this, BuffType.TalentFlashpoint, 4000, value: 200);
                }
                if (engine == 1 || (Talent(7, 1) > 0 && hits.Count >= 2)) ApplyTalentStatus(this, BuffType.TalentKilnFavour, 4000, value: 4);
                if (engine == 2) ApplyTalentStatus(this, BuffType.TalentFurnaceSkin, 3000, Stat.DamageReductionPercent, 6);
                if (Talent(7, 0) > 0) ApplyTalentStatus(this, BuffType.TalentEmberSeal, 4000, value: 2);
                if (Talent(7, 2) > 0 && Envir.Time - _lastTalentMove < 3000) ApplyTalentStatus(this, BuffType.TalentStandard, 2000, value: 15);
                return true;
            }
            case Spell.FractureSpear:
            {
                bool marked = HasRime(target);
                bool far = Functions.MaxDistance(CurrentLocation, target.CurrentLocation) >= 5;
                if (!Hit(target, ScaleSignature(power, marked ? 123 + Talent(2, 0) * 4 : 100))) return false;
                if (marked)
                {
                    SpendRime(target, spell, far);
                    if (Talent(1, 0) > 0) ApplyTalentStatus(target, BuffType.TalentBrittle, 4000, value: 8);
                    if (far && Talent(8, 0) > 0) Strike(GetSignatureTargets(target.CurrentLocation, 1, 2, [target]), power / 3);
                    if (Talent(8, 2) > 0) Ward(this, power / 4, BuffType.TalentRimeWard);
                }
                return true;
            }
            case Spell.SleetSnare:
            {
                var visited = cast.Visited ?? new HashSet<uint>();
                foreach (MapObject enemy in TalentTargets(CrossTalentCells(cast.Cell), 5))
                    if (visited.Count < 12 && visited.Add(enemy.ObjectID)) SlowSignatureTarget(enemy, 2);
                if (stage < 7) QueueTalent(magic, cast with { Visited = visited }, 500);
                return Impact(cast.Cell);
            }
            case Spell.IceglassRefuge:
                return Ward((HumanObject)target, power, BuffType.TalentIceglass, 3000);
            case Spell.WinterglassVolley:
            {
                List<MapObject> hits = engine == 1 ? Strike(TalentTargets(GlacialGateTiles(cast.Cell, cast.Direction), 3), power) :
                    Hit(target, engine == 2 ? power * 3 / 4 : power) ? [target] : [];
                if (hits.Count == 0) return false;
                Periodic(hits[0], BuffType.TalentIceFracture, power / (engine == 0 ? 5 : 8), 1);
                ApplyTalentStatus(this, BuffType.TalentClearCrystal, 4000, value: 200);
                if (engine == 1) ApplyTalentStatus(hits[0], BuffType.TalentSnowblind, 3000, Stat.Accuracy, -4);
                if (engine == 2) Ward(TalentAllies(CurrentLocation, 2, 1).FirstOrDefault() ?? this, power / 2, BuffType.TalentRimeguard);
                if (Talent(7, 0) > 0 && Functions.MaxDistance(CurrentLocation, hits[0].CurrentLocation) >= 5) ChangeMP(3);
                if (Talent(7, 1) > 0 && hits.Count > 1) ApplyTalentStatus(this, BuffType.TalentStillMind, 4000, value: 3);
                return true;
            }
            case Spell.ArcJavelin:
                if (!Hit(target)) return false;
                ApplyTalentStatus(target, BuffType.TalentConductor, Talent(5, 0, 1) > 0 ? 5500 : 4000);
                RecordComboHit(spell, target);
                return true;
            case Spell.FluxPylon:
                _fluxPylon = cast.Cell;
                _fluxUntil = Envir.Time + 5000;
                BroadcastSignatureEffect(spell, cast.Cell, SpellEffect.SignatureArmed);
                return true;
            case Spell.Skywire:
                if (!Hit(target, ScaleSignature(power, engine == 1 ? 135 : 100))) return false;
                ApplyTalentStatus(this, BuffType.TalentChargedHands, 4000, value: 3);
                ApplyTalentStatus(target, BuffType.TalentFusedArmour, 4000, value: 9);
                int secondary = 0;
                if (engine == 0)
                {
                    secondary = Strike(GetSignatureTargets(target.CurrentLocation, 2, 2, [target]), power / 2).Count;
                    Periodic(target, BuffType.TalentStaticScorch, power / 8, 1);
                }
                if (engine == 2)
                {
                    ApplyTalentStatus(this, BuffType.TalentStormstride, 3000, value: 12);
                    ApplyTalentStatus(target, BuffType.TalentScrambledAim, 3000, Stat.Accuracy, -4);
                }
                if (Talent(7, 0) > 0 && secondary > 0) ApplyTalentStatus(this, BuffType.TalentChargedHands, 6000, value: 3);
                if (Talent(7, 1) > 0 && secondary == 0) Ward(this, power / 4, BuffType.TalentWireGuard);
                if (Talent(7, 2) > 0 && HasOwnedStatus(target, BuffType.TalentConductor)) ChangeMP(3);
                return true;
            case Spell.PaperWard:
                bool paper = Ward((HumanObject)target, power, BuffType.TalentQuietLantern, ClassTalent(4, 0, 0) > 0 ? 5500 : 4000);
                if (paper && ClassTalent(4, 0, 1) > 0) ApplyTalentStatus(target, BuffType.TalentPaperScript, 4000);
                return paper;
            case Spell.GuidingWick:
                _wickTarget = target;
                _wickUntil = Envir.Time + 6000;
                ApplyTalentStatus(target, BuffType.TalentWick, 6000);
                ObserveRotation(RotationEvent.Activate, spell, target);
                return Impact(target.CurrentLocation);
            case Spell.TendingBell:
                if (CurrentLocation != cast.Origin || (CurrentPoison & TalentCastLock) != 0) return false;
                foreach (HumanObject ally in TalentAllies(cast.Cell, 2))
                    HealTalentTarget(ally, power, spell, cast.HealCredit);
                return ContinueZone(3, 750);
            case Spell.SanctuaryLantern:
                if (stage == 0) _sanctuary = new SanctuaryPool(cast.Cell, Math.Min(power * 3, MaxHealth / 4));
                if (_sanctuary == null || _sanctuary.Cell != cast.Cell || _sanctuary.Capacity <= 0) return false;
                foreach (HumanObject ally in TalentAllies(cast.Cell, 1))
                    ApplyTalentStatus(ally, BuffType.TalentSanctuary, 800)?.Set("Pool", _sanctuary);
                return ContinueZone(Talent(5, 2, 1) > 0 ? 12 : 10, 500);
            case Spell.DawnCascade:
            {
                var recipient = (HumanObject)target;
                int healed = HealTalentTarget(recipient, ScaleSignature(power, engine == 1 ? 125 : 100), spell);
                if (engine == 2) Ward(recipient, power, BuffType.TalentQuietLantern);
                else
                {
                    HumanObject weakest = null;
                    int count = healed > 0 ? 1 : 0;
                    foreach (HumanObject ally in TalentAllies(recipient.CurrentLocation, 2).Where(ally => ally != recipient && ally.HP < ally.MaxHealth).Take(2))
                    {
                        if (HealTalentTarget(ally, power / 2, spell) <= 0) continue;
                        weakest ??= ally;
                        count++;
                    }
                    if (engine == 0 && count > 0) Periodic(weakest ?? recipient, BuffType.TalentAfterglow, power / 8, 3, heal: true);
                    if (Talent(7, 0) > 0 && count > 1) ChangeMP(3);
                    if (Talent(7, 1) > 0 && count == 1) ApplyTalentStatus(this, BuffType.TalentClearBreath, 4000, value: 3);
                }
                if (engine == 1) ApplyTalentStatus(this, BuffType.TalentClearBreath, 4000, value: 4);
                return Impact(recipient.CurrentLocation);
            }
            case Spell.SerpentNeedle:
                if (!Hit(target)) return false;
                var dose = FindDistilPoison(target, PoisonType.Green);
                if (dose != null && dose.Duration - dose.Time >= 2 && target.TakePoisonTickPower(dose) is > 0 and var tickPower)
                {
                    dose.Time++;
                    if (target is MonsterObject poisonedMonster) poisonedMonster.PoisonDamage(-tickPower, dose.Owner);
                    if (target is HumanObject poisonedHuman)
                    {
                        tickPower = poisonedHuman.DamageAfterSignatureWard(tickPower);
                        poisonedHuman.PoisonDamage(-tickPower, dose.Owner);
                    }
                    if (tickPower > 0) target.BroadcastDamageIndicator(DamageType.Hit, -tickPower);
                    ObserveRotation(RotationEvent.AdvancePoison, spell, target);
                }
                else if (FindDistilPoison(target, PoisonType.Red) != null)
                    ApplyTalentStatus(target, BuffType.TalentScoredMeridian, 3000, value: 6);
                if (dose != null && Talent(2, 0) > 0) ShortenTalentCooldown(spell, magic.GetDelay() * Talent(2, 0) / 20);
                return true;
            case Spell.CausticScript:
                if (!CanHitSignatureTarget(target)) return false;
                if (stage == 0)
                {
                    ApplyTalentStatus(target, BuffType.TalentCausticScript, 2200);
                    BroadcastSignatureEffect(spell, target.CurrentLocation, SpellEffect.SignatureArmed);
                    cast = cast with { TargetDistance = target.VoluntaryDistance };
                    return ContinueZone(8, 275);
                }
                if (!HasOwnedStatus(target, BuffType.TalentCausticScript)) return false;
                bool moved = target.VoluntaryDistance - cast.TargetDistance >= 2;
                if (moved || stage >= 7)
                {
                    target.RemoveBuff(BuffType.TalentCausticScript);
                    return Hit(target, moved ? power : power / 2);
                }
                return ContinueZone(8, 275);
            case Spell.MiasmaVessel:
                _miasmaVessel = cast.Cell;
                _miasmaUntil = Envir.Time + 5000;
                BroadcastSignatureEffect(spell, cast.Cell, SpellEffect.SignatureArmed);
                return true;
            case Spell.ScarletCrucible:
                if (!Hit(target, engine == 1 ? power * 3 / 4 : power)) return false;
                if (cast.Variant == TalentRotations.RedDose)
                    ApplyTalentStatus(target, BuffType.TalentScoredMeridian, 4000, value: engine == 1 ? 12 : 7);
                else Periodic(target, BuffType.TalentCrucibleBurn, power / (engine == 0 ? 5 : 8), 3);
                if (engine == 0) ApplyTalentStatus(this, BuffType.TalentSteadyHand, 4000, value: 3);
                if (engine == 1) Ward(this, power / 2, BuffType.TalentVenomWard);
                if (engine == 2)
                {
                    int spread = SpreadTalentPoison(target.CurrentLocation, target, power / 9, 2);
                    ApplyTalentStatus(this, BuffType.TalentInkskin, 4000, value: 150);
                    if (spread > 0 && Talent(7, 2) > 0) ChangeMP(3);
                }
                if (Talent(7, 1) > 0 && HasOwnedStatus(target, BuffType.TalentSealed))
                    ApplyTalentStatus(target, BuffType.TalentSealed, 4000, Stat.MaxAC, -2);
                return true;
            case Spell.SpiritPounce:
                if (!IsSignatureCompanion(cast.Spirit, Spell.SpiritCommand) || !MoveNear(target, cast.Spirit, 3) ||
                    !Hit(target, ScaleSignature(power, cast.Spirit.Target == target ? 100 + Talent(2, 2) * 4 : 100))) return false;
                if (target.IsAttackTarget(cast.Spirit)) cast.Spirit.Target = target;
                ApplyTalentStatus(target, BuffType.TalentHounded, 4000, value: 8);
                ObserveRotation(RotationEvent.Hit, spell, target);
                if (Talent(2, 0) > 0) ShortenTalentCooldown(spell, magic.GetDelay() * Talent(2, 0) / 20);
                if (Talent(6, 2) > 0) ApplyTalentStatus(target, BuffType.TalentThreadbreak, 3000, value: 6);
                if (Talent(8, 2) > 0) ApplyTalentStatus(target, BuffType.TalentThreadbreak, 4000, value: 9);
                return true;
            case Spell.AncestralRelay:
                if (!IsSignatureCompanion(cast.Spirit, Spell.SpiritCommand) || !SignatureReach(cast.Cell, 8)) return false;
                if (cast.Spirit.CurrentLocation != cast.Cell && !MoveTalentActor(cast.Spirit, cast.Cell)) return false;
                foreach (HumanObject ally in TalentAllies(cast.Cell, 2, 3)) Ward(ally, power / 2, BuffType.TalentRendWard);
                return Impact(cast.Cell);
            case Spell.EchoFang:
                _echoUntil = Envir.Time + 6000;
                ApplyTalentStatus(this, BuffType.TalentEchoFang, 6000, value: power);
                return Impact(CurrentLocation);
            case Spell.AncestorsRend:
                if (!IsSignatureCompanion(cast.Spirit, Spell.SpiritCommand) || !Hit(target, engine == 0 ? power * 3 / 4 : power)) return false;
                Periodic(target, BuffType.TalentSpiritScar, power / 8, Talent(7, 2) > 0 && cast.Spirit.Target == target ? 2 : 1);
                if (Talent(7, 1) > 0) _nextRecallUntil = Envir.Time + 4500;
                if (engine == 0) Ward(TalentAllies(cast.Spirit.CurrentLocation, 2, 1).FirstOrDefault() ?? this, power / 2, BuffType.TalentRendWard);
                if (Functions.InRange(cast.Spirit.CurrentLocation, target.CurrentLocation, 3) && cast.Spirit.CanFly(target.CurrentLocation) &&
                    Hit(target, ScaleSignature(power, engine == 2 ? 62 : 48)))
                {
                    if (engine == 1)
                    {
                        ApplyTalentStatus(cast.Spirit, BuffType.TalentPackFocus, 3000, Stat.Accuracy, 4);
                        ApplyTalentStatus(target, BuffType.TalentHounded, 4000, value: 8);
                    }
                    if (engine == 2) ApplyTalentStatus(this, BuffType.TalentSpiritTempo, 4000, value: 900);
                    if (Talent(7, 0) > 0) ChangeMP(3);
                }
                return true;
            default:
                return false;
        }

        bool Hit(MapObject enemy, int value = -1) => HitSignatureTarget(enemy, spell, value < 0 ? power : Math.Max(1, value), defence);

        List<MapObject> Strike(IEnumerable<MapObject> enemies, int value)
        {
            var hits = new List<MapObject>();
            foreach (MapObject enemy in enemies)
                if (CanFly(enemy.CurrentLocation) && Hit(enemy, hits.Count == 0 ? value : value * 3 / 4)) hits.Add(enemy);
            return hits;
        }

        bool Impact(Point cell)
        {
            BroadcastSignatureEffect(spell, cell, SpellEffect.SignatureImpact);
            return true;
        }

        bool ContinueZone(int count, int interval)
        {
            if (stage + 1 < count) QueueTalent(magic, cast, interval);
            return Impact(cast.Cell);
        }

        bool Ward(HumanObject ally, int amount, BuffType status, int duration = 4000)
        {
            if (!CanShieldSignatureTarget(ally) || !GrantSignatureWard(ally, spell, Math.Max(1, amount), duration)) return false;
            ApplyTalentStatus(ally, status, duration);
            return true;
        }

        void Periodic(MapObject actor, BuffType status, int tickPower, int count, int interval = 1000, bool heal = false, bool preserveStatus = false)
            => StartTalentTicks(magic, actor, status, tickPower, count, interval, heal, preserveStatus);
    }

    private void StartTalentTicks(UserMagic magic, MapObject actor, BuffType status, int power, int count, int interval = 1000,
        bool heal = false, bool preserveStatus = false)
    {
        if (actor == null || actor.Dead) return;
        var buff = preserveStatus && actor.HasBuff(status, out var existing) ? existing : ApplyTalentStatus(actor, status, count * interval + 100);
        if (buff == null || buff.Caster != this) return;
        var token = new object();
        buff.Set("TalentTick", token);
        ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + interval, magic,
            new TalentTick(CurrentMap, _signatureGeneration, actor, status, token, count, Math.Max(1, power), heal)));
    }

    private void CompleteTalentTick(UserMagic magic, TalentTick tick)
    {
        if (Dead || tick.Map != CurrentMap || tick.Generation != _signatureGeneration ||
            !HasOwnedStatus(tick.Target, tick.Status) || !tick.Target.HasBuff(tick.Status, out var buff) ||
            buff.Get<object>("TalentTick") != tick.Token || tick.Target.Dead || tick.Target.CurrentMap != CurrentMap) return;
        if (tick.Status == BuffType.TalentSmoulder && HasOwnFireWall(tick.Target.CurrentLocation)) return;
        if (tick.Heal && tick.Target is HumanObject ally) HealTalentTarget(ally, tick.Power, magic.Spell);
        else HitSignatureTarget(tick.Target, magic.Spell, tick.Power, DefenceType.None);
        if (tick.Remaining > 1)
            ActionList.Add(new DelayedAction(DelayedType.Magic, Envir.Time + 1000, magic, tick with { Remaining = tick.Remaining - 1 }));
    }

    private bool HasOwnedStatus(MapObject target, BuffType type) => target != null &&
        target.HasBuff(type, out Buff buff) && buff.Caster == this && !buff.FlagForRemoval && buff.ExpireTime > 0 &&
        buff.Values.Length >= 2 && buff.Values[1] == _signatureGeneration;

    private bool HasOwnFireWall(Point cell) => CurrentMap.ValidPoint(cell) && CurrentMap.GetCell(cell).Objects?.Any(
        actor => actor is SpellObject { Spell: Spell.FireWall } wall && wall.Caster == this) == true;

    private bool MoveNear(MapObject target, MapObject mover, int maximum)
    {
        if (!CanHitSignatureTarget(target) || !CanTalentMove(mover)) return false;
        if (Functions.InRange(mover.CurrentLocation, target.CurrentLocation, 1)) return true;
        Point end = mover.CurrentLocation;
        for (int step = 0; step < maximum && !Functions.InRange(end, target.CurrentLocation, 1); step++)
            end.Offset(Math.Sign(target.CurrentLocation.X - end.X), Math.Sign(target.CurrentLocation.Y - end.Y));
        return Functions.InRange(end, target.CurrentLocation, 1) && MoveTalentActor(mover, end);
    }

    private void Taunt(MonsterObject mob)
    {
        if (mob.Master != null || mob.Info.IsBoss || !CanHitSignatureTarget(mob)) return;
        mob.Target = this;
        mob.OperateTime = 0;
        BroadcastSignatureEffect(Spell.IronChallenge, mob.CurrentLocation, SpellEffect.SignatureImpact);
    }

    private void ShortenTalentCooldown(Spell spell, long amount)
    {
        UserMagic magic = GetMagic(spell);
        if (magic == null || magic.CastTime == 0) return;
        long reduction = Math.Min(amount, Math.Max(0, magic.CastTime + magic.GetDelay() - Envir.Time));
        if (reduction == 0) return;
        magic.CastTime -= reduction;
        SendMagicInfo(magic);
    }

    private int HealTalentTarget(HumanObject ally, int power, Spell spell, TalentCastCredit credit = null)
    {
        if (!CanShieldSignatureTarget(ally)) return 0;
        int effective = Math.Min(Math.Max(0, ally.MaxHealth - ally.HP), Math.Max(0, power));
        if (effective == 0) return 0;
        ally.ChangeHP(effective);
        BroadcastSignatureEffect(spell, ally.CurrentLocation, SpellEffect.SignatureImpact);
        if (credit != null) CreditTalentHealing(ally, spell, effective, credit: credit);
        return effective;
    }

    private int SpreadTalentPoison(Point centre, MapObject primary, int power, int count)
    {
        var targets = GetSignatureTargets(centre, 1, count, [primary]);
        foreach (MapObject enemy in targets)
            enemy.ApplyPoison(new Poison { Owner = this, PType = PoisonType.Green, Duration = 2, TickSpeed = 1000,
                TickTime = Envir.Time + 1000, Value = Math.Max(1, power), TalentDose = true, CopiedDose = true }, this);
        return targets.Count;
    }

    internal void OnTalentDoseExpired(MapObject target, Poison dose)
    {
        if (dose.DoseGeneration != _signatureGeneration || !OwnsCombo(Spell.Distil) || Talent(8, 2) == 0 ||
            Envir.Time < _walkingBlightUntil || target is not MonsterObject { Master: null } || !CanHitSignatureTarget(target)) return;
        _walkingBlightUntil = Envir.Time + 8000;
        foreach (MapObject enemy in GetSignatureTargets(target.CurrentLocation, 1, 5, [target]).OfType<MonsterObject>().Take(2))
            HitSignatureTarget(enemy, Spell.ScarletCrucible, Math.Max(1, dose.Value), DefenceType.MAC);
    }

    private void ReleaseTalentVessel(MapObject target, int dosePower)
    {
        if (Envir.Time >= _miasmaUntil || !Functions.InRange(_miasmaVessel, target.CurrentLocation, 1)) return;
        _miasmaUntil = 0;
        SpreadTalentPoison(_miasmaVessel, target, Math.Max(1, dosePower / 2), 3);
        BroadcastSignatureEffect(Spell.MiasmaVessel, _miasmaVessel, SpellEffect.SignatureImpact);
    }

    private bool CleanseTalentTarget(MapObject target, Spell spell)
    {
        var poison = target.PoisonList.FirstOrDefault(p => p.PType is PoisonType.Red or PoisonType.Green or PoisonType.Slow or
            PoisonType.Bleeding or PoisonType.TalentRoot or PoisonType.TalentSilence or PoisonType.TalentStun or PoisonType.Stun or PoisonType.Dazed or PoisonType.Frozen);
        if (poison != null) target.BreakTalentControl(poison);
        else
        {
            var debuff = target.Buffs.FirstOrDefault(buff => !buff.FlagForRemoval && buff.Properties.HasFlag(BuffProperty.Debuff));
            if (debuff == null) return false;
            target.RemoveBuff(debuff.Type);
        }
        ObserveRotation(RotationEvent.Dispel, spell, target);
        if (Class == MirClass.Taoist && ClassTalent(5, 0) > 0 && Envir.Time >= _cleanseCreditUntil)
        {
            _cleanseCreditUntil = Envir.Time + 6000;
            ApplyTalentStatus(this, BuffType.TalentClearBreath, 5000, value: 2);
        }
        if (Talent(2, 1) > 0 && OwnsCombo(Spell.LanternLink)) ChangeMP(Talent(2, 1));
        if (Talent(3, 1, 1) > 0 && target is HumanObject ally && OwnsCombo(Spell.LanternLink))
            GrantSignatureWard(ally, spell, Math.Max(1, Stats[Stat.MaxSC] / 4), 3000);
        return true;
    }
}
