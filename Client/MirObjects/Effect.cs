using Client.MirGraphics;
using Client.MirScenes;
using Client.MirSounds;

namespace Client.MirObjects
{
    public class Effect
    {
        public MLibrary Library;

        public int BaseIndex, Count, Duration;
        public long Start;

        public int CurrentFrame;
        public long NextFrame;

        public Point Source;
        public MapObject Owner;

        public int Light = 6;
        public Color LightColour = Color.White;

        public bool Blend = true;
        public float Rate = 1F;
        public Point DrawLocation;
        public Point DrawOffset = Point.Empty;
        public bool Repeat;
        public long RepeatUntil;

        public bool DrawBehind = false;
        protected virtual int DrawnFrame => BaseIndex + CurrentFrame;

        public long CurrentDelay;
        public long Delay;

        public event EventHandler Complete;
        public event EventHandler Played;

        public Effect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, long starttime = 0, bool drawBehind = false)
        {
            Library = library;
            BaseIndex = baseIndex;
            Count = count == 0 ? 1 : count;
            Duration = duration;
            Start = starttime == 0 ? CMain.Time : starttime;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
            Owner = owner;
            Source = Owner.CurrentLocation;

            DrawBehind = drawBehind;
        }
        public Effect(MLibrary library, int baseIndex, int count, int duration, Point source, long starttime = 0, bool drawBehind = false)
        {
            Library = library;
            BaseIndex = baseIndex;
            Count = count == 0 ? 1 : count;
            Duration = duration;
            Start = starttime == 0 ? CMain.Time : starttime;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
            Source = source;

            DrawBehind = drawBehind;
        }

        public void SetStart(long start)
        {
            Start = start;

            NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
        }

        public virtual void Process()
        {
            if (CurrentFrame == 1)
            {
                if (Played != null)
                    Played(this, EventArgs.Empty);
            }

            if (CMain.Time <= NextFrame) return;

            if (Owner != null && Owner.SkipFrames) CurrentFrame++;

            if (++CurrentFrame >= Count)
            {
                if (Repeat && (RepeatUntil == 0 || CMain.Time < RepeatUntil))
                {
                    CurrentFrame = 0;
                    Start = CMain.Time + Delay;
                    NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);
                }
                else
                    Remove();
            }
            else NextFrame = Start + (Duration / Count) * (CurrentFrame + 1);

            GameScene.Scene.MapControl.TextureValid = false;
        }

        public virtual void Remove()
        {
            if (Owner != null)
                Owner.Effects.Remove(this);
            else
                MapControl.Effects.Remove(this);

            if (Complete != null)
                Complete(this, EventArgs.Empty);
        }

        public virtual void Draw()
        {
            if (CMain.Time < Start) return;

            if (Owner != null)
            {
                DrawLocation = Owner.DrawLocation;
            }
            else
            {
                DrawLocation = new Point((Source.X - MapObject.User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                                         (Source.Y - MapObject.User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
                DrawLocation.Offset(DrawOffset);
                DrawLocation.Offset(MapObject.User.OffSetMove);
            }


            DrawLocation = GraphicsTuning.Offset(GraphicsLayer.Spell).Apply(DrawLocation, OwnerOffsetLinked);
            using var artwork = DXManager.BlendArtwork(GraphicsLayer.Spell);
            Library.DrawSpellFrame(DrawnFrame, DrawLocation, Color.White, Blend, Rate);
        }

        private bool OwnerOffsetLinked => Owner != null && this is not Missile &&
            GraphicsTuning.Offset(GraphicsTuning.Layer(Owner.Race)).Linked;
        internal Point LightPosition => GraphicsTuning.LightPosition(LightSource.Spell, DrawLocation,
            OwnerOffsetLinked || GraphicsTuning.Offset(GraphicsLayer.Spell).Linked);

        internal bool DrawLightMask(Color tint, float intensity, ref int pixelBudget, out Point centre)
        {
            centre = LightPosition;
            return Library != null && CurrentFrame >= 0 && CurrentFrame < Count &&
                Library.DrawLightShape(DrawnFrame, centre, tint, true, intensity, ref pixelBudget, LightSource.Spell, out centre);
        }

        public void Clear()
        {
            Complete = null;
            Played = null;
        }
    }

    public class Missile : Effect
    {
        public static List<Missile> Missiles = new List<Missile>();
        public MapObject Target;
        public Point Destination;
        public int Interval, FrameCount, Skip;
        public int Direction;
        public bool Explode;
        protected override int DrawnFrame => BaseIndex + CurrentFrame % Math.Max(1, FrameCount) + Direction * (Skip + FrameCount);
        private Spell _particleSpell;
        private int _particleMapGeneration;
        private long _nextParticleTime;

        internal void TrackSpellParticles(Spell spell)
        {
            _particleSpell = spell;
            _particleMapGeneration = GameScene.Scene.MapControl.SpellParticleMapGeneration;
        }

        public Missile(MLibrary library, int baseIndex, int count, int duration, MapObject owner, Point target, bool direction16 = true)
            : base(library, baseIndex, count, duration, owner)
        {
            Missiles.Add(this);
            Source = Owner.CurrentLocation;
            Destination = target;
            Direction = direction16 ? MapControl.Direction16(Source, Destination) : (int)Functions.DirectionFromPoint(Source, Destination);
        }

        public Missile(MLibrary library, int baseIndex, int count, int duration, Point source, Point target)
            : base(library, baseIndex, count, duration, source)
        {
            Missiles.Add(this);
            Destination = target;

            Direction = MapControl.Direction16(Source, Destination);


        }

        public override void Process()
        {
            if (CMain.Time < Start) return;

            if (Target != null) Destination = Target.CurrentLocation;
            else if (!Explode)
            {
                int dist = Functions.MaxDistance(Owner.CurrentLocation, Destination);

                if (dist < Globals.DataRange)
                    Destination.Offset(Destination.X - Source.X, Destination.Y - Source.Y);
            }

            Duration = Functions.MaxDistance(Source, Destination) * 50;
            Count = Duration / Interval;
            if (Count == 0) Count = 1;

            base.Process();

            if (_particleSpell == Spell.None || !Settings.Effect) return;

            MapControl map = GameScene.Scene.MapControl;
            if (_particleMapGeneration != map.SpellParticleMapGeneration) return;

            if (CurrentFrame >= Count)
            {
                if (Target != null || Explode) map.EmitSpellImpact(_particleSpell, Destination);
            }
            else if (CMain.Time >= _nextParticleTime)
            {
                _nextParticleTime = CMain.Time + 32;
                map.EmitSpellTrail(_particleSpell, Source, Destination, (float)CurrentFrame / Count);
            }
        }
        public override void Remove()
        {
            base.Remove();
            Missiles.Remove(this);
        }
        public override void Draw()
        {
            if (CMain.Time < Start) return;


            int index = DrawnFrame;

            DrawLocation = new Point((Source.X - MapObject.User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                                       (Source.Y - MapObject.User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(MapObject.User.OffSetMove);

            int x = (Destination.X - Source.X) * MapControl.CellWidth;
            int y = (Destination.Y - Source.Y) * MapControl.CellHeight;


            DrawLocation.Offset(x * CurrentFrame / Count, y * CurrentFrame / Count);
            DrawLocation.Offset(GraphicsTuning.Offset(GraphicsLayer.Spell).Pixels);

            using var artwork = DXManager.BlendArtwork(GraphicsLayer.Spell);
            Library.DrawSpellFrame(index, DrawLocation, Color.White, Blend, Rate);
        }

    }

    public class InterruptionEffect : Effect
    {
        public static List<InterruptionEffect> effectlist = new List<InterruptionEffect>();
        bool noProcess = false;

        public InterruptionEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, long starttime = 0)
            : base(library, baseIndex, count, duration, owner)
        {
            Repeat = true;
            Blend = blend;
            effectlist.Add(this);
        }

        public override void Process()
        {
            if (!noProcess)
                base.Process();
        }

        public override void Remove()
        {
            base.Remove();
            effectlist.Remove(this);
        }

        public override void Draw()
        {
            if (!((PlayerObject)Owner).Concentrating)
                Remove();
            else if (((PlayerObject)Owner).ConcentrateInterrupted)
                noProcess = true;
            else
                noProcess = false;
            if (!noProcess)
                base.Draw();
        }

        public static int GetOwnerEffectID(uint objectID)
        {
            for (int i = 0; i < effectlist.Count; i++)
            {
                if (effectlist[i].Owner.ObjectID != objectID) continue;
                return i;
            }
            return -1;
        }
    }

    public class ElementsEffect : Effect
    {
        int myType;//1 = green orb, 2 = blue orb, 3 = red orb, 4 = mixed orbs
        long killAt;//holds the exp value for 4 orbs : kills all orbs when myType 4 is reached
        bool loopit = false;//soundloop

        public ElementsEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, int elementType, int killtime, bool loopon = false)
            : base(library, baseIndex, count, duration, owner)
        {
            Repeat = true;
            Blend = blend;
            myType = elementType;
            killAt = killtime;
            //
            loopit = loopon;
            StopSounds();
            StartSound();
        }

        public override void Process()
        {

            base.Process();
        }

        private void StartSound()
        {
            SoundManager.PlaySound(20000 + 126 * 10 + 4 + myType, loopit);
        }

        private void StopSounds()
        {
            for (int i = 0; i <= 3; i++)
                SoundManager.StopSound(20000 + 126 * 10 + 5 + i);
        }

        public override void Remove()
        {
            SoundManager.StopSound(20000 + 126 * 10 + 4 + myType);
            base.Remove();
        }

        public override void Draw()
        {
            if (!((PlayerObject)Owner).HasElements)
                Remove();
            if (((PlayerObject)Owner).ElementsLevel >= killAt && myType < 4)
                Remove();
            base.Draw();
        }
    }

    public class DelayedExplosionEffect : Effect
    {
        public static List<DelayedExplosionEffect> effectlist = new List<DelayedExplosionEffect>();
        public int stage;

        public DelayedExplosionEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, int Stage, long until)
            : base(library, baseIndex, count, duration, owner)
        {
            Repeat = (Stage == 2) ? false : true;
            Blend = blend;
            stage = Stage;
            effectlist.Add(this);
            if (stage == 1)
                SoundManager.PlaySound(20000 + 125 * 10);
            if (stage == 2)
                SoundManager.PlaySound(20000 + 125 * 10 + 5);
        }

        public override void Process()
        {
            base.Process();
        }

        public override void Remove()
        {
            base.Remove();
            effectlist.Remove(this);
        }

        public override void Draw()
        {
            if (Owner.Dead)
            {
                Remove();
                return;
            }
            base.Draw();
        }

        public static int GetOwnerEffectID(uint objectID)
        {
            for (int i = 0; i < effectlist.Count; i++)
            {
                if (effectlist[i].Owner.ObjectID != objectID) continue;
                return i;
            }
            return -1;
        }
    }

    public class SpecialEffect : Effect
    {
        public uint EffectType = 0;

        public SpecialEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, bool drawBehind, uint type)
            : base(library, baseIndex, count, duration, owner, 0, drawBehind)
        {
            Blend = blend;
            DrawBehind = drawBehind;
            EffectType = type;
            Light = -1;
        }

        public override void Process()
        {
            base.Process();
        }
    }

    public class BuffEffect : Effect
    {
        public BuffType BuffType;

        public BuffEffect(MLibrary library, int baseIndex, int count, int duration, MapObject owner, bool blend, BuffType buffType)
            : base(library, baseIndex, count, duration, owner, 0)
        {
            Repeat = true;
            Blend = blend;
            BuffType = buffType;
            Light = -1;
        }

        public override void Process()
        {
            base.Process();
        }
    }

    public class TrackableEffect : Effect
    {
        public static List<TrackableEffect> effectlist = new List<TrackableEffect>();
        public string EffectName = "default";

        public TrackableEffect(Effect baseEffect, string effName = "null")
            : base(baseEffect.Library, baseEffect.BaseIndex, baseEffect.Count, baseEffect.Duration, baseEffect.Owner, baseEffect.Start)
        {
            Repeat = baseEffect.Repeat;
            RepeatUntil = baseEffect.RepeatUntil;
            Blend = baseEffect.Blend;

            EffectName = effName;

            effectlist.Add(this);
        }

        public static int GetOwnerEffectID(uint objectID, string effectName = "null")
        {
            for (int i = 0; i < effectlist.Count; i++)
            {
                if (effectlist[i].Owner.ObjectID != objectID) continue;
                if (effectName != "null" && effectlist[i].EffectName != effectName) continue;
                return i;
            }
            return -1;
        }

        public override void Process()
        {
            base.Process();

            if (Owner == null) Remove();
            else if (Owner.Dead) Remove();
        }

        public override void Remove()
        {
            base.Remove();
            effectlist.Remove(this);
        }

        public void RemoveNoComplete()
        {
            if (Owner != null)
                Owner.Effects.Remove(this);
            else
                MapControl.Effects.Remove(this);
        }

    }

    public class LightEffect : Effect
    {
        public LightEffect(int duration, MapObject owner, long starttime = 0, int lightDistance = 6, Color? lightColour = null)
            : base(null, 0, 0, duration, owner, starttime)
        {
            Light = lightDistance;
            LightColour = lightColour == null ? Color.White : (Color)lightColour;
        }

        public LightEffect(int duration, Point source, long starttime = 0, int lightDistance = 6, Color? lightColour = null)
            : base(null, 0, 0, duration, source, starttime)
        {
            Light = lightDistance;
            LightColour = lightColour == null ? Color.White : (Color)lightColour;
        }

        public override void Process()
        {
            if (CMain.Time >= Start + Duration)
                Remove();
            GameScene.Scene.MapControl.TextureValid = false;

        }

        public override void Draw()
        {

            if (CMain.Time < Start) return;


            if (Owner != null)
            {
                DrawLocation = Owner.DrawLocation;
            }
            else
            {
                DrawLocation = new Point((Source.X - MapObject.User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                                         (Source.Y - MapObject.User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
                DrawLocation.Offset(MapObject.User.OffSetMove);
            }
        }
    }

}
