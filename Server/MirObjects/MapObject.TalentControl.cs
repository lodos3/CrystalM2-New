using S = ServerPackets;

namespace Server.MirObjects;

public abstract partial class MapObject
{
    internal const PoisonType TalentMovementLock = PoisonType.TalentRoot | PoisonType.TalentStun;
    internal const PoisonType TalentCastLock = PoisonType.TalentSilence | PoisonType.TalentStun;
    private long _controlRecovery, _counterRecovery;
    private TalentControl _counterFamily;

    internal static TalentControl ControlOf(PoisonType poison) => poison switch
    {
        PoisonType.TalentRoot => TalentControl.Root,
        PoisonType.TalentSilence => TalentControl.Silence,
        PoisonType.Stun or PoisonType.TalentStun => TalentControl.Stun,
        PoisonType.Paralysis => TalentControl.Paralysis,
        PoisonType.Frozen => TalentControl.Freeze,
        PoisonType.Dazed => TalentControl.Daze,
        PoisonType.Slow => TalentControl.Slow,
        _ => TalentControl.None
    };

    private static bool PvPActor(MapObject actor) => actor is HumanObject or MonsterObject { Master: HumanObject };

    internal bool CanReceiveControl(Poison poison, MapObject caster)
    {
        TalentControl family = ControlOf(poison.PType);
        if (family == TalentControl.None) return true;
        if (Envir.Time < _counterRecovery && (family & _counterFamily) != 0) return false;
        if (family == TalentControl.Slow || !PvPActor(this) || !PvPActor(caster ?? poison.Source)) return true;
        return Envir.Time >= _controlRecovery && !PoisonList.Any(p => IsHardControl(p.PType));
    }

    private static bool IsHardControl(PoisonType poison) => ControlOf(poison) is not (TalentControl.None or TalentControl.Slow);

    internal void RecordControl(Poison poison, MapObject caster)
    {
        if (!IsHardControl(poison.PType) || !PvPActor(this) || !PvPActor(caster ?? poison.Source)) return;
        long duration = Math.Clamp(poison.Duration * poison.TickSpeed, 1, 2000);
        poison.Duration = 1;
        poison.Time = 0;
        poison.TickTime = Envir.Time + duration;
        poison.TickSpeed = duration;
        _controlRecovery = poison.TickTime + 4000;
    }

    internal bool TryApplyTalentControl(HumanObject source, TalentControl kind, int duration)
    {
        if (Dead || InSafeZone || this is MonsterObject { Info.IsBoss: true }) return false;
        PoisonType type = kind switch
        {
            TalentControl.Root => PoisonType.TalentRoot,
            TalentControl.Silence => PoisonType.TalentSilence,
            TalentControl.Stun => PoisonType.TalentStun,
            _ => PoisonType.None
        };
        if (type == PoisonType.None) return false;
        var poison = new Poison { Owner = source, PType = type, Duration = 1, TickTime = Envir.Time + duration, TickSpeed = duration };
        ApplyPoison(poison, source);
        if (!PoisonList.Contains(poison)) return false;
        SyncTalentPoison();
        return true;
    }

    internal Poison FindControlToBreak(TalentControl mask) => PoisonList.FirstOrDefault(poison =>
        poison.Duration > poison.Time && (ControlOf(poison.PType) & mask) != 0);

    internal bool BreakTalentControl(Poison poison)
    {
        if (poison == null || !PoisonList.Remove(poison)) return false;
        BuffType status = TalentStatuses.ControlStatus(poison.PType);
        if (status != BuffType.None) RemoveBuff(status);
        _counterFamily = ControlOf(poison.PType);
        _counterRecovery = Envir.Time + 750;
        if (_controlRecovery > Envir.Time && IsHardControl(poison.PType)) _controlRecovery = Envir.Time + 4000;
        SyncTalentPoison();
        return true;
    }

    private void SyncTalentPoison()
    {
        PoisonType current = PoisonType.None;
        foreach (Poison poison in PoisonList) current |= poison.PType;
        if (CurrentPoison == current) return;
        CurrentPoison = current;
        if (this is PlayerObject player) player.Enqueue(new S.Poisoned { Poison = current });
        if (Node != null) Broadcast(new S.ObjectPoisoned { ObjectID = ObjectID, Poison = current });
    }
}
