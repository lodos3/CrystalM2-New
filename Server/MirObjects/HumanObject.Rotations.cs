using System.Drawing;
using S = ServerPackets;

namespace Server.MirObjects;

public partial class HumanObject
{
    private readonly object _rotationLock = new();
    private MapObject _rotationTarget, _rotationAlly;
    private Point _rotationTile;
    private long _rotationUntil, _lastRotationEvent;
    private int _rotationStep, _rotationMoved, _rotationVariant;
    private bool _rotationReady;

    private TalentRotation Rotation => TalentRotations.Get(Info.Class, Info.Specialisation, Build.Engine);

    internal void ObserveRotation(RotationEvent kind, Spell spell, MapObject target = null, int amount = 1,
        int variant = 0, MapObject ally = null, Point? tile = null, long origin = 0)
    {
        if (Level < 29 || Dead || Rotation is not { } recipe) return;
        var magic = GetMagic(recipe.Finisher);
        if (magic == null || (magic.CastTime != 0 && Envir.Time < magic.CastTime + magic.GetDelay())) return;

        lock (_rotationLock)
        {
            if (Envir.Time >= _rotationUntil) ClearRotation();
            if (_rotationReady) return;
            long eventStamp = origin == 0 ? (Envir.Time << 8) | (byte)spell : -origin;
            if (eventStamp == _lastRotationEvent && eventStamp != 0) return;

            uint current = _rotationTarget?.ObjectID ?? 0;
            uint targetId = target?.ObjectID ?? 0;
            RotationStep step = recipe.Steps[_rotationStep];
            if (kind == RotationEvent.Move && step.Event == RotationEvent.Move)
            {
                _rotationMoved += amount;
                amount = _rotationMoved;
            }
            if (!step.Matches(kind, spell, amount, variant, current, targetId))
            {
                if (_rotationStep == 0 || !recipe.Steps[0].Matches(kind, spell, amount, variant, 0, targetId)) return;
                ClearRotation();
                step = recipe.Steps[0];
            }

            if (_rotationStep == 0) _rotationUntil = Envir.Time + recipe.Window;
            if (step.Target is RotationTarget.Capture or RotationTarget.Different) _rotationTarget = target;
            if (ally != null) _rotationAlly = ally;
            if (tile.HasValue) _rotationTile = tile.Value;
            else if (target != null) _rotationTile = target.CurrentLocation;
            _rotationVariant = variant;
            _lastRotationEvent = eventStamp;
            _rotationStep++;
            if (_rotationStep == recipe.Steps.Length)
            {
                _rotationReady = true;
                int extension = Talent(7, 2) == 0 ? 0 : recipe.Finisher switch
                {
                    Spell.Forgefall => 1000,
                    Spell.Gatebreaker when _rotationAlly != null && Functions.InRange(CurrentLocation, _rotationAlly.CurrentLocation, 5) => 2000,
                    Spell.WinterglassVolley when _rotationUntil - Envir.Time <= 1000 => 1000,
                    _ => 0
                };
                _rotationUntil = Envir.Time + TalentRotations.ReadyTime + extension;
            }
            SendRotation(recipe);
        }
    }

    private void ObserveTalentMovement(Point from, Point to)
    {
        int distance = Functions.MaxDistance(from, to);
        VoluntaryDistance += distance;
        _lastTalentMove = Envir.Time;
        if (Info.Class == MirClass.Wizard && Info.Specialisation == CombatSpecialisation.Second &&
            Build.Engine == 0 && _rotationTarget is { } target &&
            Functions.MaxDistance(from, target.CurrentLocation) >= Functions.MaxDistance(to, target.CurrentLocation)) return;
        if (distance > 0) ObserveRotation(RotationEvent.Move, Spell.None, amount: distance);
    }

    private bool ConsumeRotation(Spell spell, MapObject target, out Point tile, out MapObject ally, out int variant)
    {
        tile = default;
        ally = null;
        variant = 0;
        if (Rotation is not { } recipe || recipe.Finisher != spell) return false;
        lock (_rotationLock)
        {
            if (!_rotationReady || Envir.Time >= _rotationUntil ||
                (recipe.TargetBound && (target != _rotationTarget || target == null || target.Dead || target.CurrentMap != CurrentMap))) return false;
            tile = _rotationTile;
            ally = _rotationAlly;
            variant = _rotationVariant;
            ClearRotation();
            SendRotation(recipe);
            return true;
        }
    }

    private void ResetTalentRotation()
    {
        lock (_rotationLock)
        {
            bool changed = _rotationStep > 0;
            ClearRotation();
            if (changed) SendRotation(Rotation);
        }
    }

    private void ClearRotation()
    {
        _rotationUntil = 0;
        _rotationStep = _rotationMoved = _rotationVariant = 0;
        _rotationReady = false;
        _rotationTarget = _rotationAlly = null;
        _rotationTile = default;
    }

    private void ExpireTalentRotation()
    {
        if (_rotationStep == 0 || (Envir.Time < _rotationUntil &&
            (_rotationTarget == null || (!_rotationTarget.Dead && _rotationTarget.Node != null && _rotationTarget.CurrentMap == CurrentMap)))) return;
        ResetTalentRotation();
    }

    private void SendRotation(TalentRotation recipe)
    {
        var packet = new S.RotationState
        {
            ObjectID = ObjectID,
            TargetID = recipe?.TargetBound == true ? _rotationTarget?.ObjectID ?? 0 : 0,
            Finisher = recipe?.Finisher ?? Spell.None,
            Step = (byte)_rotationStep,
            Ready = _rotationReady,
            Remaining = (ushort)Math.Clamp(_rotationUntil - Envir.Time, 0, ushort.MaxValue)
        };
        if (this is HeroObject hero) hero.Owner?.Enqueue(packet);
        else Enqueue(packet);
    }
}
