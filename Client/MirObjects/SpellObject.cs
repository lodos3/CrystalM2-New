using Client.MirGraphics;
using Client.MirScenes;
using Client.MirSounds;
using S = ServerPackets;

namespace Client.MirObjects
{
    class SpellObject : MapObject
    {
        public override ObjectType Race
        {
            get { return ObjectType.Spell; }
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public Spell Spell;
        public Point AnimationOffset = new(0, 0);
        public int FrameCount, FrameInterval, FrameIndex;
        public bool Repeat, Ended;
        internal long ParticleStart { get; private set; }
        private long _nextParticleTime;
        private bool _emitParticles;
        

        public SpellObject(uint objectID) : base(objectID)
        {
        }

        public void Load(S.ObjectSpell info)
        {
            CurrentLocation = info.Location;
            MapLocation = info.Location;
            GameScene.Scene.MapControl.AddObject(this);
            Spell = info.Spell;
            Direction = info.Direction;
            Repeat = true;
            Ended = false;
            _emitParticles = MapControl.HasGroundSpellParticles(Spell);
            ParticleStart = _nextParticleTime = CMain.Time;

            switch (Spell)
            {
                case Spell.TrapHexagon:
                case Spell.FireWall:
                case Spell.PoisonCloud:
                case Spell.Blizzard:
                case Spell.MeteorStrike:
                case Spell.Reincarnation:
                case Spell.Trap:
                case Spell.Portal:
                case Spell.HealingCircle:
                    FrameCount = 1;
                    FrameInterval = LegacySpellEffects.FieldInterval(Spell);
                    break;
                case Spell.DigOutZombie:
                    BodyLibrary = (ushort)Monster.DigOutZombie < Libraries.Monsters.Count() ? Libraries.Monsters[(ushort)Monster.DigOutZombie] : null;
                    DrawFrame = 304 + (byte)Direction;
                    FrameCount = 0;
                    Blend = false;
                    break;
                case Spell.Rubble:
                    if (Direction == 0)
                        BodyLibrary = null;
                    else
                    {
                        BodyLibrary = Libraries.Effect;
                        DrawFrame = 64 + Math.Min(4, (int)(Direction - 1));
                        FrameCount = 1;
                        FrameInterval = 10000;
                    }
                    break;
                case Spell.ExplosiveTrap:
                    FrameCount = 1;
                    FrameInterval = LegacySpellEffects.FieldInterval(Spell);
                    if (info.Param)
                    {
                        _emitParticles = false;
                        SoundManager.PlaySound(20000 + 124 * 10 + 5);
                        GameScene.Scene.MapControl.EmitSpellImpact(Spell, CurrentLocation);
                    }
                    break;
                case Spell.MapLightning:
                    MapControl.Effects.Add(new Effect(Libraries.Dragon, 400 + (CMain.Random.Next(3) * 10), 5, 600, CurrentLocation));
                    SoundManager.PlaySound(8301);
                    break;
                case Spell.MapLava:
                    MapControl.Effects.Add(new Effect(Libraries.Dragon, 440, 20, 1600, CurrentLocation) { Blend = false });
                    MapControl.Effects.Add(new Effect(Libraries.Dragon, 470, 10, 800, CurrentLocation));
                    SoundManager.PlaySound(8302);
                    break;
                case Spell.MapQuake1:
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 27, 12, 1200, CurrentLocation) { Blend = false });
                    SoundManager.PlaySound(8304);
                    break;
                case Spell.MapQuake2:
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HellLord], 39, 13, 1300, CurrentLocation) { Blend = false });
                    SoundManager.PlaySound(8304);
                    break;
                case Spell.DigOutArmadillo:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.Armadillo];
                    DrawFrame = 472 + (byte)Direction;
                    FrameCount = 0;
                    Blend = false;
                    break;
                case Spell.GeneralMeowMeowThunder:                
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.GeneralMeowMeow], 522, 7, 700, CurrentLocation) { Blend = true });
                    SoundManager.PlaySound(8321);
                    break;
                case Spell.StoneGolemQuake:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.StoneGolem];
                    DrawFrame = 368 + (int)Direction * 8;
                    FrameInterval = 100;
                    FrameCount = 8;
                    Light = 0;
                    Blend = false;
                    Repeat = false;
                    SoundManager.PlaySound(8304);
                    break;
                case Spell.EarthGolemPile:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.EarthGolem];
                    DrawFrame = 441;
                    FrameInterval = 100;
                    FrameCount = 8;
                    Light = 0;
                    Blend = false;
                    Repeat = false;
                    SoundManager.PlaySound(8331);
                    break;
                case Spell.TreeQueenMassRoots:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.TreeQueen];
                    DrawFrame = 82;
                    FrameInterval = 100;
                    FrameCount = 15;
                    Blend = false;
                    Repeat = false;
                    SoundManager.PlaySound(8341);
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TreeQueen], 97, 14, 1400, CurrentLocation) { Blend = true });
                    break;
                case Spell.TreeQueenGroundRoots:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.TreeQueen];
                    DrawFrame = 48;
                    FrameInterval = 100;
                    FrameCount = 9;
                    Blend = false;
                    Repeat = false;
                    SoundManager.PlaySound(8342);
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TreeQueen], 57, 9, 900, CurrentLocation) { Blend = true });
                    break;
                case Spell.TreeQueenRoot:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.TreeQueen];
                    DrawFrame = 111;
                    FrameInterval = 100;
                    FrameCount = 15;
                    Blend = false;
                    Repeat = false;
                    SoundManager.PlaySound(8343);
                    break;
                case Spell.TucsonGeneralRock:
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.TucsonGeneral], 552, 20, 2000, CurrentLocation) { Repeat = false, Blend = false });
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.TucsonGeneral];
                    DrawFrame = 572;
                    FrameInterval = 100;
                    FrameCount = 20;
                    Light = 1;
                    Blend = true;
                    Repeat = false;
                    break;
                case Spell.FlyingStatueIceTornado:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.FlyingStatue];
                    DrawFrame = 314;
                    FrameInterval = 100;
                    FrameCount = 20;
                    Blend = true;
                    Repeat = false;
                    SoundManager.PlaySound(8303);
                    break;
                case Spell.DarkOmaKingNuke:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.DarkOmaKing];
                    DrawFrame = 1630 + (int)Direction * 9;
                    FrameInterval = 100;
                    FrameCount = 9;
                    Blend = true;
                    Repeat = false;
                    SoundManager.PlaySound(((ushort)Monster.DarkOmaKing * 10) + 9);
                    break;
                case Spell.HornedSorcererDustTornado:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.HornedSorceror];
                    DrawFrame = 634;
                    FrameInterval = 100;
                    FrameCount = 10;
                    Blend = true;
                    Repeat = true;
                    SoundManager.PlaySound(8306);
                    break;
                case Spell.HornedCommanderRockFall:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.HornedCommander];
                    DrawFrame = 1066;
                    FrameInterval = 100;
                    FrameCount = 12;
                    Blend = true;
                    Repeat = true;
                    SoundManager.PlaySound(8456);
                    break;
                case Spell.HornedCommanderRockSpike:
                    BodyLibrary = Libraries.Monsters[(ushort)Monster.HornedCommander];
                    DrawFrame = 1190;
                    FrameInterval = 100;
                    FrameCount = 9;
                    Blend = false;
                    Repeat = true;
                    SoundManager.PlaySound(8457);
                    MapControl.Effects.Add(new Effect(Libraries.Monsters[(ushort)Monster.HornedCommander], 1199, 9, 900, CurrentLocation) { Blend = true });
                    break;
            }

            if (_emitParticles) Light = 0;

            NextMotion = CMain.Time + FrameInterval;
            NextMotion -= NextMotion % 100;
        }

        public override void Process()
        {
            if (_emitParticles && (Repeat || FrameIndex < FrameCount))
            {
                if (LegacySpellEffects.HasLiveArt(Spell) || CMain.Time >= _nextParticleTime)
                {
                    _nextParticleTime = CMain.Time + FrameInterval * 2;
                    GameScene.Scene.MapControl.EmitGroundSpellParticles(this);
                }
            }

            if (CMain.Time >= NextMotion)
            {
                if (++FrameIndex >= FrameCount && Repeat)
                {
                    FrameIndex = 0;
                    Ended = true;
                }

                NextMotion = CMain.Time + FrameInterval;

                switch (Spell)
                {
                    case Spell.TucsonGeneralRock:
                        if (FrameIndex == 10) SoundManager.PlaySound(8305);
                        break;
                    case Spell.HornedSorcererDustTornado:
                        if (FrameIndex == 0 && CMain.Random.Next(3) == 0) SoundManager.PlaySound(8306);
                        break;
                    case Spell.HornedCommanderRockSpike:
                        if (Ended)
                        {
                            DrawFrame = 1198;
                            FrameCount = 1;
                            FrameIndex = 0;
                        }
                        break;
                }
            }

            DrawLocation = new Point((CurrentLocation.X - User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth, (CurrentLocation.Y - User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);
            DrawLocation.Offset(GlobalDisplayLocationOffset);
            DrawLocation.Offset(GraphicsTuning.Offset(GraphicsLayer.Spell).Pixels);
            DrawLocation.Offset(User.OffSetMove);
        }

        public override void Draw()
        {
            using var artwork = BlendArtwork();
            if (FrameIndex >= FrameCount && !Repeat) return;
            if (BodyLibrary == null) return;

            BodyLibrary.DrawSpellFrame(DrawFrame + FrameIndex,
                AnimationOffset == default ? DrawLocation : GetDrawWithOffset(), DrawColour, Blend, 0.8f);
        }

        // The spell's actual animated frame contributes its own alpha silhouette
        // to the light map, rather than being represented solely by a round glow.
        // This is called only while the light render target is active.
        internal Point LightPosition => GraphicsTuning.LightPosition(LightSource.Spell,
            AnimationOffset == default ? DrawLocation : GetDrawWithOffset(), GraphicsTuning.Offset(GraphicsLayer.Spell).Linked);

        internal bool DrawLightMask(Color tint, float intensity, ref int pixelBudget, out Point centre)
        {
            centre = LightPosition;
            if (BodyLibrary == null || (FrameIndex >= FrameCount && !Repeat))
                return false;

            return BodyLibrary.DrawLightShape(DrawFrame + FrameIndex,
                centre, tint, true, intensity, ref pixelBudget, LightSource.Spell, out centre);
        }

        public override bool MouseOver(Point p)
        {
            return false;
        }

        public override void DrawBehindEffects(bool effectsEnabled)
        {
        }

        public override void DrawEffects(bool effectsEnabled)
        { 
        }

        private Point GetDrawWithOffset()
        {
            Point newDrawLocation = new (
                (CurrentLocation.X + AnimationOffset.X - User.Movement.X + MapControl.OffSetX) * MapControl.CellWidth,
                (CurrentLocation.Y + AnimationOffset.Y - User.Movement.Y + MapControl.OffSetY) * MapControl.CellHeight);

            newDrawLocation.Offset(GlobalDisplayLocationOffset);
            newDrawLocation.Offset(GraphicsTuning.Offset(GraphicsLayer.Spell).Pixels);
            newDrawLocation.Offset(User.OffSetMove);

            return newDrawLocation;
        }
    }
}
