using Server.MirEnvir;

namespace Server.MirObjects
{
    public enum DelayedType
    {
        Magic,
        /// <summary>
        /// Param0 MapObject (Target) | Param1 Damage | Param2 Defence | Param3 damageWeapon | Param4 UserMagic | Param5 FinalHit
        /// </summary>
        Damage,
        RangeDamage,        
        Spawn,
        Die,
        Recall,
        MapMovement,
        Mine,
        NPC,
        Poison,
        DamageIndicator,
        Quest,

        // Sanjian
        SpellEffect,
    }

    public class DelayedAction
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        public DelayedType Type;
        public long Time;
        public long StartTime;
        public object[] Params;

        public bool FlaggedToRemove;
        public int PerkRevision = -1;
        private TalentCastCredit _credit;
        public TalentCastCredit Credit => _credit ??= new TalentCastCredit();

        public DelayedAction(DelayedType type, long time, params object[] p)
        {
            StartTime = Envir.Time;
            Type = type;
            Time = time;
            Params = p;
            if (type == DelayedType.Magic && p.Length > 1 && p[0] is HumanObject actor && p[1] is MirDatabase.UserMagic &&
                actor.Info != null && SpellPerks.Supports(actor.Info.Class)) PerkRevision = actor.Info.SpellPerks.Revision;
        }
    }

    public sealed class TalentCastCredit
    {
        private static long _lastId;
        private int _claimed;
        private int _healEchoClaimed;
        public readonly long Id = Interlocked.Increment(ref _lastId);
        public bool Claim() => Interlocked.Exchange(ref _claimed, 1) == 0;
        internal bool ClaimHealEcho() => Interlocked.Exchange(ref _healEchoClaimed, 1) == 0;
    }
}
