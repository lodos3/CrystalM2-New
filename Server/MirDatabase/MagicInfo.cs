using Server.MirEnvir;
using S = ServerPackets;

namespace Server.MirDatabase
{
    public class MagicInfo
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        public string Name;
        public Spell Spell;
        public byte BaseCost, LevelCost, Icon;
        public byte Level1, Level2, Level3;
        public ushort Need1, Need2, Need3;
        public uint DelayBase = 1800, DelayReduction;
        public ushort PowerBase, PowerBonus;
        public ushort MPowerBase, MPowerBonus;
        public float MultiplierBase = 1.0f, MultiplierBonus;
        public byte Range = 9;

        public override string ToString()
        {
            return Name;
        }

        public MagicInfo()
        {

        }

        public MagicInfo (BinaryReader reader, int version = int.MaxValue, int Customversion = int.MaxValue)
        {
            Name = reader.ReadString();
            Spell = (Spell)reader.ReadByte();
            BaseCost = reader.ReadByte();
            LevelCost = reader.ReadByte();
            Icon = reader.ReadByte();
            Level1 = reader.ReadByte();
            Level2 = reader.ReadByte();
            Level3 = reader.ReadByte();
            Need1 = reader.ReadUInt16();
            Need2 = reader.ReadUInt16();
            Need3 = reader.ReadUInt16();
            DelayBase = reader.ReadUInt32();
            DelayReduction = reader.ReadUInt32();
            PowerBase = reader.ReadUInt16();
            PowerBonus = reader.ReadUInt16();
            MPowerBase = reader.ReadUInt16();
            MPowerBonus = reader.ReadUInt16();

            if (version > 66)
                Range = reader.ReadByte();
            if (version > 70)
            {
                MultiplierBase = reader.ReadSingle();
                MultiplierBonus = reader.ReadSingle();
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write((byte)Spell);
            writer.Write(BaseCost);
            writer.Write(LevelCost);
            writer.Write(Icon);
            writer.Write(Level1);
            writer.Write(Level2);
            writer.Write(Level3);
            writer.Write(Need1);
            writer.Write(Need2);
            writer.Write(Need3);
            writer.Write(DelayBase);
            writer.Write(DelayReduction);
            writer.Write(PowerBase);
            writer.Write(PowerBonus);
            writer.Write(MPowerBase);
            writer.Write(MPowerBonus);
            writer.Write(Range);
            writer.Write(MultiplierBase);
            writer.Write(MultiplierBonus);
        }
    }

    public class UserMagic
    {
        protected static Envir Envir
        {
            get { return Envir.Main; }
        }

        public Spell Spell;
        public MagicInfo Info;

        public const byte EffectiveRank = 3;
        public byte Level => EffectiveRank;
        public byte Key;
        public bool IsTempSpell;
        public long CastTime = int.MinValue;
        internal SpellPerkProfile Perks;
        public int Perk(SpellPerk perk) => Perks?.Rank(Spell, perk) ?? 0;
        public int ManaCost
        {
            get
            {
                int cost = Info.BaseCost + EffectiveRank * Info.LevelCost;
                int rank = Perk(SpellPerk.Economy);
                return rank == 0 || cost == 0 ? cost : Math.Max(0, cost - Math.Max(1, cost * rank / 10));
            }
        }
        public byte CastRange => Info.Range == 0 ? (byte)0 : (byte)Math.Min(byte.MaxValue, Info.Range + Perk(SpellPerk.Reach));
        public int Area(int radius) => radius + Perk(SpellPerk.Radius);
        public int EffectDuration(int time) => Scale(time, 100 + 15 * Perk(SpellPerk.Duration));
        public int ScalePower(int power) => Scale(power, 100 + 8 * Perk(SpellPerk.Power));
        public int CastRecovery(int milliseconds) => Scale(milliseconds, 100 - 10 * Perk(SpellPerk.Recharge));
        private static int Scale(int amount, int percent) => (int)Math.Clamp((long)amount * percent / 100, int.MinValue, int.MaxValue);

        private MagicInfo GetMagicInfo(Spell spell)
        {
            for (int i = 0; i < Envir.MagicInfoList.Count; i++)
            {
                MagicInfo info = Envir.MagicInfoList[i];
                if (info.Spell != spell) continue;
                return info;
            }
            return null;
        }

        public UserMagic(Spell spell)
        {
            Spell = spell;
            
            Info = GetMagicInfo(Spell);
        }
        public UserMagic(BinaryReader reader, int version, int customVersion)
        {
            Spell = (Spell) reader.ReadByte();
            Info = GetMagicInfo(Spell);

            _ = reader.ReadByte(); // Retired proficiency rank.
            Key = reader.ReadByte();
            _ = reader.ReadUInt16();

            if (version < 15) return;
            IsTempSpell = reader.ReadBoolean();

            if (version < 65) return;
            CastTime = reader.ReadInt64();
        }
        public void Save(BinaryWriter writer)
        {
            writer.Write((byte) Spell);

            writer.Write(EffectiveRank);
            writer.Write(Key);
            writer.Write((ushort)0);
            writer.Write(IsTempSpell);
            writer.Write(CastTime);
        }

        public Packet GetInfo(bool hero)
        {
            return new S.NewMagic
                {
                    Magic = CreateClientMagic(),
                    Hero = hero
                };
        }

        public ClientMagic CreateClientMagic()
        {
            int cost = ManaCost;
            byte costPerRank = (byte)Math.Max(0, (cost - byte.MaxValue + EffectiveRank - 1) / EffectiveRank);
            return new ClientMagic
            {
                    Name = Info.Name,
                    Spell = Spell,
                    BaseCost = (byte)(cost - EffectiveRank * costPerRank),
                    LevelCost = costPerRank,
                    Icon = Info.Icon,
                    Level1 = Info.Level1,
                    Level2 = Info.Level2,
                    Level3 = Info.Level3,
                    Need1 = Info.Need1,
                    Need2 = Info.Need2,
                    Need3 = Info.Need3,
                    Level = Level,
                    Key = Key,
                    IsTempSpell = IsTempSpell,
                    Delay = GetDelay(),
                    Range = CastRange,
                    CastTime = CastTime - Envir.Time
            };
        }

        public int GetDamage(int DamageBase)
        {
            return ScalePower((int)((DamageBase + RollPower()) * GetMultiplier()));
        }

        public float GetMultiplier()
        {
            return (Info.MultiplierBase + (Level * Info.MultiplierBonus));
        }

        public int GetPower() => ScalePower(RollPower());

        private int RollPower()
        {
            return (int)Math.Round((MPower() / 4F) * (Level + 1) + DefPower());
        }

        public int MPower()
        {
            if (Info.MPowerBonus > 0)
            {
                return Envir.Random.Next(Info.MPowerBase, Info.MPowerBonus + Info.MPowerBase);
            }
            else
                return Info.MPowerBase;
        }
        public int DefPower()
        {
            if (Info.PowerBonus > 0)
            {
                return Envir.Random.Next(Info.PowerBase, Info.PowerBonus + Info.PowerBase);
            }
            else
                return Info.PowerBase;
        }

        public int GetPower(int power)
        {
            return ScalePower((int)Math.Round(power / 4F * (Level + 1) + DefPower()));
        }

        public long GetDelay()
        {
            long delay = Math.Max(0, Info.DelayBase - (long)Level * Info.DelayReduction);
            if (Spell == Spell.FlamingSword) delay = Math.Max(delay, 10000);
            if (Spell == Spell.CounterAttack) delay = Math.Max(delay, 7000);
            return delay * (100 - 10 * Perk(SpellPerk.Recharge)) / 100;
        }
    }
}
