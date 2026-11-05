using System.IO.Compression;
using Frame = Client.MirObjects.Frame;
using Client.MirObjects;
using Client.MirScenes;
using CrystalM2.Rendering.Core;
using System.Text.RegularExpressions;

namespace Client.MirGraphics
{
    public static class Libraries
    {
        public static bool Loaded;
        public static int Count, Progress;

        public static readonly MLibrary
            ChrSel = new MLibrary(Settings.DataPath + "ChrSel"),
            Prguse = new MLibrary(Settings.DataPath + "Prguse"),
            Prguse2 = new MLibrary(Settings.DataPath + "Prguse2"),
            Prguse3 = new MLibrary(Settings.DataPath + "Prguse3"),
            UI_32bit = new MLibrary(Settings.DataPath + "UI_32bit"),
            BuffIcon = new MLibrary(Settings.DataPath + "BuffIcon"),
            Help = new MLibrary(Settings.DataPath + "Help"),
            MiniMap = new MLibrary(Settings.DataPath + "MMap"),
            MapLinkIcon = new MLibrary(Settings.DataPath + "MapLinkIcon"),
            Title = new MLibrary(Settings.DataPath + "Title"),
            MagIcon = new MLibrary(Settings.DataPath + "MagIcon"),
            MagIcon2 = new MLibrary(Settings.DataPath + "MagIcon2"),
            Magic = new MLibrary(Settings.DataPath + "Magic"),
            Magic2 = new MLibrary(Settings.DataPath + "Magic2"),
            Magic3 = new MLibrary(Settings.DataPath + "Magic3"),
            Effect = new MLibrary(Settings.DataPath + "Effect"),
            MagicC = new MLibrary(Settings.DataPath + "MagicC"),
            GuildSkill = new MLibrary(Settings.DataPath + "GuildSkill"),
            Weather = new MLibrary(Settings.DataPath + "Weather");

        public static readonly MLibrary
            Background = new MLibrary(Settings.DataPath + "Background");


        public static readonly MLibrary
            Dragon = new MLibrary(Settings.DataPath + "Dragon");

        //Map
        public static readonly MLibrary[] MapLibs = new MLibrary[400];

        //Items
        public static readonly MLibrary
            Items = new MLibrary(Settings.DataPath + "Items"),
            StateItems = new MLibrary(Settings.DataPath + "StateItem"),
            FloorItems = new MLibrary(Settings.DataPath + "DNItems"),
            Items_Tooltip_32bit = new MLibrary(Settings.DataPath + "Items_Tooltip_32bit");

        //Deco
        public static readonly MLibrary
            Deco = new MLibrary(Settings.DataPath + "Deco");

        public static MLibrary[] CArmours,
                                          CWeapons,
										  CWeaponEffect,
										  CHair,
                                          CHumEffect,
                                          AArmours,
                                          AWeaponsL,
                                          AWeaponsR,
                                          AHair,
                                          AHumEffect,
                                          ARArmours,
                                          ARWeapons,
                                          ARWeaponsS,
                                          ARHair,
                                          ARHumEffect,
                                          Monsters,
                                          Gates,
                                          Flags,
                                          Siege,
                                          Mounts,
                                          NPCs,
                                          Fishing,
                                          Pets,
                                          Transform,
                                          TransformMounts,
                                          TransformEffect,
                                          TransformWeaponEffect;

        static Libraries()
        {
            //Wiz/War/Tao
            InitLibrary(ref CArmours, Settings.CArmourPath, "00");
            InitLibrary(ref CHair, Settings.CHairPath, "00");
            InitLibrary(ref CWeapons, Settings.CWeaponPath, "00");
            InitLibrary(ref CWeaponEffect, Settings.CWeaponEffectPath, "00");
            InitLibrary(ref CHumEffect, Settings.CHumEffectPath, "00");

            //Assassin
            InitLibrary(ref AArmours, Settings.AArmourPath, "00");
            InitLibrary(ref AHair, Settings.AHairPath, "00");
            InitLibrary(ref AWeaponsL, Settings.AWeaponPath, "00", " L");
            InitLibrary(ref AWeaponsR, Settings.AWeaponPath, "00", " R");
            InitLibrary(ref AHumEffect, Settings.AHumEffectPath, "00");

            //Archer
            InitLibrary(ref ARArmours, Settings.ARArmourPath, "00");
            InitLibrary(ref ARHair, Settings.ARHairPath, "00");
            InitLibrary(ref ARWeapons, Settings.ARWeaponPath, "00");
            InitLibrary(ref ARWeaponsS, Settings.ARWeaponPath, "00", " S");
            InitLibrary(ref ARHumEffect, Settings.ARHumEffectPath, "00");

            //Other
            InitLibrary(ref Monsters, Settings.MonsterPath, "000");
            InitLibrary(ref Gates, Settings.GatePath, "00");
            InitLibrary(ref Flags, Settings.FlagPath, "00");
            InitLibrary(ref Siege, Settings.SiegePath, "00");
            InitLibrary(ref NPCs, Settings.NPCPath, "00");
            InitLibrary(ref Mounts, Settings.MountPath, "00");
            InitLibrary(ref Fishing, Settings.FishingPath, "00");
            InitLibrary(ref Pets, Settings.PetsPath, "00");
            InitLibrary(ref Transform, Settings.TransformPath, "00");
            InitLibrary(ref TransformMounts, Settings.TransformMountsPath, "00");
            InitLibrary(ref TransformEffect, Settings.TransformEffectPath, "00");
            InitLibrary(ref TransformWeaponEffect, Settings.TransformWeaponEffectPath, "00");

            #region Maplibs
            //wemade mir2 (allowed from 0-99)
            MapLibs[0] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Tiles");
            MapLibs[1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Smtiles");
            MapLibs[2] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects");
            for (int i = 2; i < 28; i++)
            {
                MapLibs[i + 1] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects" + i.ToString());
            }
            MapLibs[90] = new MLibrary(Settings.DataPath + "Map\\WemadeMir2\\Objects_32bit");

            //shanda mir2 (allowed from 100-199)
            MapLibs[100] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[100 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Tiles" + (i + 1));
            }
            MapLibs[110] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles");
            for (int i = 1; i < 10; i++)
            {
                MapLibs[110 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\SmTiles" + (i + 1));
            }
            MapLibs[120] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects");
            for (int i = 1; i < 31; i++)
            {
                MapLibs[120 + i] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\Objects" + (i + 1));
            }
            MapLibs[190] = new MLibrary(Settings.DataPath + "Map\\ShandaMir2\\AniTiles1");
            //wemade mir3 (allowed from 200-299)
            string[] Mapstate = { "", "wood\\", "sand\\", "snow\\", "forest\\"};
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[200 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tilesc");
                MapLibs[201 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles30c");
                MapLibs[202 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Tiles5c");
                MapLibs[203 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Smtilesc");
                MapLibs[204 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Housesc");
                MapLibs[205 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Cliffsc");
                MapLibs[206 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Dungeonsc");
                MapLibs[207 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Innersc");
                MapLibs[208 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Furnituresc");
                MapLibs[209 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Wallsc");
                MapLibs[210 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "smObjectsc");
                MapLibs[211 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Animationsc");
                MapLibs[212 +(i*15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object1c");
                MapLibs[213 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\WemadeMir3\\" + Mapstate[i] + "Object2c");
            }
            Mapstate = new string[] { "", "wood", "sand", "snow", "forest"};
            //shanda mir3 (allowed from 300-399)
            for (int i = 0; i < Mapstate.Length; i++)
            {
                MapLibs[300 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tilesc" + Mapstate[i]);
                MapLibs[301 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles30c" + Mapstate[i]);
                MapLibs[302 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Tiles5c" + Mapstate[i]);
                MapLibs[303 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Smtilesc" + Mapstate[i]);
                MapLibs[304 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Housesc" + Mapstate[i]);
                MapLibs[305 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Cliffsc" + Mapstate[i]);
                MapLibs[306 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Dungeonsc" + Mapstate[i]);
                MapLibs[307 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Innersc" + Mapstate[i]);
                MapLibs[308 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Furnituresc" + Mapstate[i]);
                MapLibs[309 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Wallsc" + Mapstate[i]);
                MapLibs[310 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "smObjectsc" + Mapstate[i]);
                MapLibs[311 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Animationsc" + Mapstate[i]);
                MapLibs[312 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object1c" + Mapstate[i]);
                MapLibs[313 + (i * 15)] = new MLibrary(Settings.DataPath + "Map\\ShandaMir3\\" + "Object2c" + Mapstate[i]);
            }
            #endregion

            LoadLibraries();

            Thread thread = new Thread(LoadGameLibraries) { IsBackground = true };
            thread.Start();
        }

        static void InitLibrary(ref MLibrary[] library, string path, string toStringValue, string suffix = "")
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var allFiles = Directory.GetFiles(path, "*" + suffix + MLibrary.Extention, SearchOption.TopDirectoryOnly).OrderBy(x => int.Parse(Regex.Match(x, @"\d+").Value));

            var lastFile = allFiles.Count() > 0 ? Path.GetFileName(allFiles.Last()) : "0";

            var count = int.Parse(Regex.Match(lastFile, @"\d+").Value) + 1;

            library = new MLibrary[count];

            for (int i = 0; i < count; i++)
            {
                library[i] = new MLibrary(path + i.ToString(toStringValue) + suffix);
            }
        }

        static void LoadLibraries()
        {
            ChrSel.Initialize();
            Progress++;

            Prguse.Initialize();
            Progress++;

            Prguse2.Initialize();
            Progress++;

            Prguse3.Initialize();
            Progress++;

            UI_32bit.Initialize();
            Progress++;

            Title.Initialize();
            Progress++;
        }

        private static void LoadGameLibraries()
        {
            Count = MapLibs.Length + Monsters.Length + Gates.Length + Flags.Length + Siege.Length + NPCs.Length + CArmours.Length +
                CHair.Length + CWeapons.Length + CWeaponEffect.Length + AArmours.Length + AHair.Length + AWeaponsL.Length + AWeaponsR.Length +
                ARArmours.Length + ARHair.Length + ARWeapons.Length + ARWeaponsS.Length +
                CHumEffect.Length + AHumEffect.Length + ARHumEffect.Length + Mounts.Length + Fishing.Length + Pets.Length +
                Transform.Length + TransformMounts.Length + TransformEffect.Length + TransformWeaponEffect.Length + 19;

            Dragon.Initialize();
            Progress++;

            BuffIcon.Initialize();
            Progress++;

            Help.Initialize();
            Progress++;

            MiniMap.Initialize();
            Progress++;
            MapLinkIcon.Initialize();
            Progress++;

            MagIcon.Initialize();
            Progress++;
            MagIcon2.Initialize();
            Progress++;

            Magic.Initialize();
            Progress++;
            Magic2.Initialize();
            Progress++;
            Magic3.Initialize();
            Progress++;
            MagicC.Initialize();
            Progress++;

            Effect.Initialize();
            Progress++;

            Weather.Initialize();
            Progress++;

            GuildSkill.Initialize();
            Progress++;

            Background.Initialize();
            Progress++;

            Deco.Initialize();
            Progress++;

            Items.Initialize();
            Progress++;
            StateItems.Initialize();
            Progress++;
            FloorItems.Initialize();
            Progress++;
            Items_Tooltip_32bit.Initialize();
            Progress++;

            for (int i = 0; i < MapLibs.Length; i++)
            {
                if (MapLibs[i] == null)
                    MapLibs[i] = new MLibrary("");
                else
                    MapLibs[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Monsters.Length; i++)
            {
                Monsters[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Gates.Length; i++)
            {
                Gates[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Flags.Length; i++)
            {
                Flags[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Siege.Length; i++)
            {
                Siege[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < NPCs.Length; i++)
            {
                NPCs[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < CArmours.Length; i++)
            {
                CArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHair.Length; i++)
            {
                CHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CWeapons.Length; i++)
            {
                CWeapons[i].Initialize();
                Progress++;
            }

			for (int i = 0; i < CWeaponEffect.Length; i++)
			{
				CWeaponEffect[i].Initialize();
				Progress++;
			}

			for (int i = 0; i < AArmours.Length; i++)
            {
                AArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHair.Length; i++)
            {
                AHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsL.Length; i++)
            {
                AWeaponsL[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AWeaponsR.Length; i++)
            {
                AWeaponsR[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARArmours.Length; i++)
            {
                ARArmours[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHair.Length; i++)
            {
                ARHair[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeapons.Length; i++)
            {
                ARWeapons[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARWeaponsS.Length; i++)
            {
                ARWeaponsS[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < CHumEffect.Length; i++)
            {
                CHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < AHumEffect.Length; i++)
            {
                AHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < ARHumEffect.Length; i++)
            {
                ARHumEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Mounts.Length; i++)
            {
                Mounts[i].Initialize();
                Progress++;
            }


            for (int i = 0; i < Fishing.Length; i++)
            {
                Fishing[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Pets.Length; i++)
            {
                Pets[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < Transform.Length; i++)
            {
                Transform[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformEffect.Length; i++)
            {
                TransformEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformWeaponEffect.Length; i++)
            {
                TransformWeaponEffect[i].Initialize();
                Progress++;
            }

            for (int i = 0; i < TransformMounts.Length; i++)
            {
                TransformMounts[i].Initialize();
                Progress++;
            }
            
            Loaded = true;
        }

    }

    public sealed class MLibrary
    {
        public const string Extention = ".Lib";
        public const int LibVersion = 3;

        private readonly string _fileName;
        private readonly bool _worldArtwork;
        private readonly Lazy<LegacyMirLibReader> _mapShadowBackup;

        private MImage[] _images;
        private FrameSet _frames;
        private int[] _indexList;
        private int _count;
        private bool _initialized;

        private BinaryReader _reader;
        private FileStream _fStream;

        public FrameSet Frames
        {
            get { return _frames; }
        }

        public MLibrary(string filename)
        {
            _fileName = Path.ChangeExtension(filename, Extention);
            string[] parts = filename.TrimStart('.', '/', '\\').Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
            _worldArtwork = parts.Length > 2 && parts[0].Equals("Data", StringComparison.OrdinalIgnoreCase) &&
                parts[1].ToUpperInvariant() is "MAP" or "MONSTER" or "CARMOUR" or "AARMOUR" or "ARARMOUR" or
                    "CHAIR" or "AHAIR" or "ARHAIR" or "CHELMET" or "CWEAPON" or "AWEAPON" or "ARWEAPON" or
                    "MOUNT" or "PET" or "NPC" or "GATE" or "SIEGE" or "FLAG" or "TRANSFORM" or "TRANSFORMRIDE2";
            if (_worldArtwork && parts[1].Equals("Map", StringComparison.OrdinalIgnoreCase))
            {
                string relativePath = Path.GetRelativePath(Settings.DataPath, _fileName);
                _mapShadowBackup = new Lazy<LegacyMirLibReader>(() => OpenMapShadowBackup(relativePath));
            }
        }

        private static LegacyMirLibReader OpenMapShadowBackup(string relativePath)
        {
            string root = Path.Combine(Settings.DataPath, ".shadow-backup");
            if (!Directory.Exists(root)) return null;
            try
            {
                foreach (string snapshot in Directory.GetDirectories(root).OrderByDescending(path => path, StringComparer.Ordinal))
                {
                    string path = Path.Combine(snapshot, relativePath);
                    if (!File.Exists(path)) continue;
                    try
                    {
                        return new LegacyMirLibReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete));
                    }
                    catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            return null;
        }

        public void Initialize()
        {
            _initialized = true;

            if (!File.Exists(_fileName))
                return;

            try
            {
                _fStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read);
                _reader = new BinaryReader(_fStream);
                int currentVersion = _reader.ReadInt32();
                if (currentVersion < 2)
                {
                    System.Windows.Forms.MessageBox.Show("Wrong version, expecting lib version: " + LibVersion.ToString() + " found version: " + currentVersion.ToString() + ".", _fileName, System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error, System.Windows.Forms.MessageBoxDefaultButton.Button1);
                    System.Windows.Forms.Application.Exit();
                    return;
                }
                _count = _reader.ReadInt32();

                int frameSeek = 0;
                if (currentVersion >= 3)
                {
                    frameSeek = _reader.ReadInt32();
                }

                _images = new MImage[_count];
                _indexList = new int[_count];

                for (int i = 0; i < _count; i++)
                    _indexList[i] = _reader.ReadInt32();

                if (currentVersion >= 3)
                {
                    _fStream.Seek(frameSeek, SeekOrigin.Begin);

                    var frameCount = _reader.ReadInt32();

                    if (frameCount > 0)
                    {
                        _frames = new FrameSet();
                        for (int i = 0; i < frameCount; i++)
                        {
                            _frames.Add((MirAction)_reader.ReadByte(), new Frame(_reader));
                        }
                    }
                }
            }
            catch (Exception)
            {
                _initialized = false;
                throw;
            }
        }

        // Read only the frame header until a frame is actually on screen.
        // Header inspection does not create a Direct3D texture or inflate image bytes.
        private MImage GetImageMetadata(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return null;

            MImage image = _images[index];
            if (image == null)
            {
                _fStream.Position = _indexList[index];
                image = new MImage(_reader);
                _images[index] = image;
            }
            return image;
        }

        private bool CheckImage(int index)
        {
            MImage mi = GetImageMetadata(index);
            if (mi == null || mi.Width <= 0 || mi.Height <= 0)
                return false;

            if (!mi.TextureValid)
            {
                _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                mi.CreateTexture(_reader, _worldArtwork, _mapShadowBackup, index);
            }
            return true;
        }

        public Point GetOffSet(int index)
        {
            if (!_initialized) Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Point.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Point(_images[index].X, _images[index].Y);
        }
        public Size GetSize(int index)
        {
            if (!_initialized) Initialize();
            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Seek(_indexList[index], SeekOrigin.Begin);
                _images[index] = new MImage(_reader);
            }

            return new Size(_images[index].Width, _images[index].Height);
        }
        public Size GetTrueSize(int index)
        {
            if (!_initialized)
                Initialize();

            if (_images == null || index < 0 || index >= _images.Length)
                return Size.Empty;

            if (_images[index] == null)
            {
                _fStream.Position = _indexList[index];
                _images[index] = new MImage(_reader);
            }
            MImage mi = _images[index];
            if (mi.TrueSize.IsEmpty)
            {
                if (!mi.TextureValid)
                {
                    if ((mi.Width == 0) || (mi.Height == 0))
                        return Size.Empty;

                    _fStream.Seek(_indexList[index] + 17, SeekOrigin.Begin);
                    mi.CreateTexture(_reader, _worldArtwork, _mapShadowBackup, index);
                }
                return mi.GetTrueSize();
            }
            return mi.TrueSize;
        }

        // Redraw the current animation frame's alpha silhouette at ground level.
        // Only checked/dithered sprites need an additional repaired GPU texture.
        public void DrawProjectedShadow(int index, Point location, Point worldCell)
        {
            if (!Settings.DynamicShadows || Settings.ShadowOpacityPercent == 0 ||
                !Settings.ShadowSources.Allows(_fileName) || DXManager.Blending ||
                DXManager.ProjectedShadowsDrawn >= DXManager.MaxProjectedShadowsPerMapDraw)
                return;

            MImage mi = GetImageMetadata(index);
            if (mi == null || mi.Width <= 0 || mi.Height <= 0 ||
                mi.Width > 320 || mi.Height > 320)
                return; // Reject oversized frames before their texture is uploaded.

            Point imagePoint = location;
            imagePoint.Offset(mi.X, mi.Y);
            ShadowProjection direction = GameScene.Scene?.MapControl?.GetShadowProjection(worldCell)
                                        ?? ShadowProjection.Sunlight;
            float length = Settings.ShadowLengthPercent / 100f;
            direction = new ShadowProjection(direction.Horizontal * length, direction.Vertical * length);
            if (!direction.IsProjectedVisible(imagePoint, mi.Width, mi.Height,
                                              Settings.ScreenWidth, Settings.ScreenHeight))
                return;

            if (!CheckImage(index))
                return;

            if (mi.HasBakedShadow) return;
            Texture repaired = mi.GetRepairedShadowTexture();
            bool halfSize = repaired != null;
            DXManager.DrawProjectedShadow(repaired ?? mi.Image,
                new Rectangle(0, 0, halfSize ? (mi.Width + 1) / 2 : mi.Width,
                                    halfSize ? (mi.Height + 1) / 2 : mi.Height),
                imagePoint, mi.Height, halfSize, direction.Horizontal, direction.Vertical);
            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void Draw(int index, int x, int y)
        {
            MImage mi = GetImageMetadata(index);
            if (mi == null ||
                !SpriteVisibility.IsVisible(x, y, mi.Width, mi.Height,
                                            Settings.ScreenWidth, Settings.ScreenHeight))
                return;

            if (!CheckImage(index))
                return;


            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)x, (float)y, 0.0F), Color.White);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Color colour, bool offSet = false)
        {
            MImage mi = GetImageMetadata(index);
            if (mi == null) return;

            if (offSet) point.Offset(mi.X, mi.Y);

            if (!SpriteVisibility.IsVisible(point.X, point.Y, mi.Width, mi.Height,
                                            Settings.ScreenWidth, Settings.ScreenHeight))
                return;

            if (!CheckImage(index))
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void Draw(int index, Point point, Color colour, bool offSet, float opacity)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            DXManager.DrawOpaque(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawBlend(int index, Point point, Color colour, bool offSet = false, float rate = 1)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            bool oldBlend = DXManager.Blending;
            DXManager.SetBlend(true, rate);

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            DXManager.SetBlend(oldBlend);
            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawEffectBlend(int index, Point point, Color colour, float rate)
        {
            float strength = Math.Clamp(rate * Settings.SpellBlendIntensityPercent / 100f, 0, 1);
            if (strength == 0) return;
            DrawBlend(index, point, Color.FromArgb((int)MathF.Round(colour.A * strength), colour), true);
        }

        public bool DrawLightShape(int index, Point point, Color colour, bool offset, float intensity, ref int pixelBudget)
        {
            MImage image = GetImageMetadata(index);
            if (image == null || image.Width <= 0 || image.Height <= 0) return false;
            if (offset) point.Offset(image.X, image.Y);

            int radius = Math.Clamp(Math.Min(image.Width, image.Height) / 8, 2, 6);
            var bounds = new Rectangle(point, new Size(image.Width, image.Height));
            bounds.Inflate(radius, radius);
            Rectangle visible = Rectangle.Intersect(bounds, new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight));
            int pixels = visible.Width * visible.Height;
            if (pixels <= 0 || pixels > pixelBudget || !CheckImage(index))
                return false;

            DXManager.DrawShapedLight(image.Image, bounds.Location, radius, colour, intensity);
            pixelBudget -= pixels;
            image.CleanTime = CMain.Time + Settings.CleanDelay;
            return true;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, bool offSet)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            if (section.Right > mi.Width)
                section.Width -= section.Right - mi.Width;

            if (section.Bottom > mi.Height)
                section.Height -= section.Bottom - mi.Height;

            DXManager.Draw(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Rectangle section, Point point, Color colour, float opacity)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            if (section.Right > mi.Width)
                section.Width -= section.Right - mi.Width;

            if (section.Bottom > mi.Height)
                section.Height -= section.Bottom - mi.Height;

            DXManager.DrawOpaque(mi.Image, section, new Vector3((float)point.X, (float)point.Y, 0.0F), colour, opacity); 

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void Draw(int index, Point point, Size size, Color colour)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + size.Width < 0 || point.Y + size.Height < 0)
                return;

            float scaleX = (float)size.Width / mi.Width;
            float scaleY = (float)size.Height / mi.Height;

            Matrix matrix = Matrix.CreateScale(scaleX, scaleY, 0);
            DXManager.Sprite.Transform = matrix;
            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X / scaleX, (float)point.Y / scaleY, 0.0F), Color.White); 

            DXManager.Sprite.Transform = Matrix.Identity;

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawTinted(int index, Point point, Color colour, Color Tint, bool offSet = false)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            if (offSet) point.Offset(mi.X, mi.Y);

            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), colour);

            if (mi.MaskImage != null && !mi.MaskImage.Disposed)
            {
                DXManager.Draw(mi.MaskImage, new Rectangle(0, 0, mi.MaskWidth, mi.MaskHeight),
                               new Vector3((float)point.X, (float)point.Y, 0.0F), Tint);
            }

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public void DrawUp(int index, int x, int y)
        {
            if (x >= Settings.ScreenWidth)
                return;

            if (!CheckImage(index))
                return;

            MImage mi = _images[index];
            y -= mi.Height;
            if (y >= Settings.ScreenHeight)
                return;
            if (x + mi.Width < 0 || y + mi.Height < 0)
                return;

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3(x, y, 0.0F), Color.White);

            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }
        public void DrawUpBlend(int index, Point point)
        {
            if (!CheckImage(index))
                return;

            MImage mi = _images[index];

            point.Y -= mi.Height;


            if (point.X >= Settings.ScreenWidth || point.Y >= Settings.ScreenHeight || point.X + mi.Width < 0 || point.Y + mi.Height < 0)
                return;

            bool oldBlend = DXManager.Blending;
            DXManager.SetBlend(true, 1);

            DXManager.Draw(mi.Image, new Rectangle(0, 0, mi.Width, mi.Height), new Vector3((float)point.X, (float)point.Y, 0.0F), Color.White);

            DXManager.SetBlend(oldBlend);
            mi.CleanTime = CMain.Time + Settings.CleanDelay;
        }

        public bool VisiblePixel(int index, Point point, bool accuate)
        {
            if (!CheckImage(index))
                return false;

            if (accuate)
                return _images[index].VisiblePixel(point);

            int accuracy = 2;

            for (int x = -accuracy; x <= accuracy; x++)
                for (int y = -accuracy; y <= accuracy; y++)
                    if (_images[index].VisiblePixel(new Point(point.X + x, point.Y + y)))
                        return true;

            return false;
        }
    }

    public sealed class MImage
    {
        public short Width, Height, X, Y, ShadowX, ShadowY;
        public byte Shadow;
        public int Length;

        public bool TextureValid;
        public Texture Image;
        //layer 2:
        public short MaskWidth, MaskHeight, MaskX, MaskY;
        public int MaskLength;

        public Texture MaskImage;
        public Boolean HasMask;
        public bool HasBakedShadow { get; private set; }

        public long CleanTime;
        public Size TrueSize;

        private AlphaCoverage _alphaCoverage;
        private Texture _repairedShadowTexture;
        private int _repairedShadowBytes;
        private long _repairedShadowLastUsed;
        private bool _shadowAllocationFailed;

        public MImage(BinaryReader reader)
        {
            //read layer 1
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            X = reader.ReadInt16();
            Y = reader.ReadInt16();
            ShadowX = reader.ReadInt16();
            ShadowY = reader.ReadInt16();
            Shadow = reader.ReadByte();
            Length = reader.ReadInt32();

            //check if there's a second layer and read it
            HasMask = ((Shadow >> 7) == 1) ? true : false;
            if (HasMask)
            {
                SpriteFileLayout.SkipCompressedLayer(reader, Length);
                MaskWidth = reader.ReadInt16();
                MaskHeight = reader.ReadInt16();
                MaskX = reader.ReadInt16();
                MaskY = reader.ReadInt16();
                MaskLength = reader.ReadInt32();
            }
        }

        public unsafe void CreateTexture(BinaryReader reader, bool worldArtwork, Lazy<LegacyMirLibReader> mapShadowBackup, int index)
        {
            int width = Width;
            int height = Height;

            try
            {
                Image = new Texture(DXManager.Device, width, height);
                DataRectangle locked = Image.BeginUpload();
                try
                {
                    DecompressBgraRows(reader.ReadBytes(Length), locked.Data, width, height, locked.Pitch);
                    // Snapshot alpha BEFORE UnlockRectangle. Never retain the native pointer.
                    var pixels = new Span<byte>((void*)locked.Data.DataPointer, checked(locked.Pitch * height));
                    _alphaCoverage = AlphaCoverage.Capture(pixels, width, height, locked.Pitch);
                    TrueSize = _alphaCoverage.VisibleSize;
                    HasBakedShadow = worldArtwork && SmoothShadowLayer(pixels, locked.Pitch, mapShadowBackup, index, mask: false);
                }
                finally
                {
                    locked.Data.Dispose();
                    Image.EndUpload();
                }

                if (HasMask)
                {
                    // The second frame header is twelve bytes and has its own
                    // dimensions and compressed length, independent of layer one.
                    SpriteFileLayout.SkipCompressedLayer(reader, 12);
                    if (SpriteFileLayout.HasSafeMask(MaskWidth, MaskHeight, MaskLength))
                    {
                        MaskImage = new Texture(DXManager.Device, MaskWidth, MaskHeight);
                        DataRectangle maskLocked = MaskImage.BeginUpload();
                        try
                        {
                            DecompressBgraRows(reader.ReadBytes(MaskLength), maskLocked.Data,
                                               MaskWidth, MaskHeight, maskLocked.Pitch);
                            if (worldArtwork)
                            {
                                var pixels = new Span<byte>((void*)maskLocked.Data.DataPointer, checked(maskLocked.Pitch * MaskHeight));
                                HasBakedShadow |= SmoothShadowLayer(pixels, maskLocked.Pitch, mapShadowBackup, index, mask: true);
                            }
                        }
                        finally
                        {
                            maskLocked.Data.Dispose();
                            MaskImage.EndUpload();
                        }
                    }
                }

                _shadowAllocationFailed = false;
                DXManager.TextureList.Add(this);
                TextureValid = true;
                CleanTime = CMain.Time + Settings.CleanDelay;
            }
            catch
            {
                Image?.Dispose();
                MaskImage?.Dispose();
                ReleaseRepairedShadow();
                Image = null;
                MaskImage = null;
                _alphaCoverage = null;
                TrueSize = Size.Empty;
                TextureValid = false;
                throw;
            }
        }

        private bool SmoothShadowLayer(Span<byte> pixels, int pitch, Lazy<LegacyMirLibReader> backup, int index, bool mask)
        {
            int width = mask ? MaskWidth : Width, height = mask ? MaskHeight : Height;
            int x = mask ? MaskX : X, y = mask ? MaskY : Y;
            if (backup != null && BakedShadowRepair.HasOfflineRepair(pixels, width, height, pitch) && backup.Value is { } library)
            {
                try
                {
                    if (library.TryReadFrame(index, out var original, includeMask: mask))
                    {
                        var layer = mask ? original.Mask : original;
                        if (layer != null && layer.Width == width && layer.Height == height && layer.OffsetX == x && layer.OffsetY == y &&
                            BakedShadowRepair.TryUseOriginal(pixels, width, height, pitch, layer.Bgra)) return true;
                    }
                }
                catch (Exception ex) when (ex is IOException or InvalidDataException) { }
            }

            return BakedShadowRepair.Smooth(pixels, width, height, pitch, joinTileEdges: backup != null);
        }

        // Generate a compact repair texture once per checked sprite frame. Original
        // texture pixels remain untouched, including intentionally dithered art.
        public unsafe Texture GetRepairedShadowTexture()
        {
            if (_alphaCoverage == null || !_alphaCoverage.HasCheckerboard || _shadowAllocationFailed)
                return null;

            if (_repairedShadowTexture != null && _repairedShadowTexture.Disposed)
                ReleaseRepairedShadow();

            if (_repairedShadowTexture == null)
            {
                int width = _alphaCoverage.ShadowWidth;
                int height = _alphaCoverage.ShadowHeight;
                int bytes = checked(width * height * 4);
                // Reaching the cap is not a permanent failure: use the sprite
                // silhouette until old masks expire and budget is available.
                if (!DXManager.ShadowMasks.TryReserve(bytes))
                    return null;

                Texture candidate = null;
                try
                {
                    candidate = new Texture(DXManager.Device, width, height);
                    DataRectangle locked = candidate.BeginUpload();
                    try
                    {
                        var pixels = new Span<byte>((void*)locked.Data.DataPointer,
                                                    checked(locked.Pitch * height));
                        _alphaCoverage.WriteShadowMask(pixels, locked.Pitch);
                    }
                    finally
                    {
                        locked.Data.Dispose();
                        candidate.EndUpload();
                    }
                    _repairedShadowTexture = candidate;
                    _repairedShadowBytes = bytes;
                }
                catch
                {
                    candidate?.Dispose();
                    DXManager.ShadowMasks.Release(bytes);
                    _shadowAllocationFailed = true; // Fall back to the source sprite.
                    return null;
                }
            }

            _repairedShadowLastUsed = CMain.Time;
            return _repairedShadowTexture;
        }

        private void ReleaseRepairedShadow()
        {
            if (_repairedShadowTexture != null && !_repairedShadowTexture.Disposed)
                _repairedShadowTexture.Dispose();
            _repairedShadowTexture = null;

            if (_repairedShadowBytes != 0)
            {
                DXManager.ShadowMasks.Release(_repairedShadowBytes);
                _repairedShadowBytes = 0;
            }
        }

        public void CleanRepairedShadow(long now)
        {
            if (_repairedShadowTexture == null || now - _repairedShadowLastUsed < 30000)
                return;
            ReleaseRepairedShadow();
        }

        // The caller already knows whether the manager is removing the list entry.
        // Avoid repeatedly searching and shifting a list while expiring many textures.
        public void DisposeTexture(bool unregister = true)
        {
            if (unregister)
                DXManager.TextureList.Remove(this);

            ReleaseRepairedShadow();
            _shadowAllocationFailed = false;

            if (Image != null && !Image.Disposed)
                Image.Dispose();
            if (MaskImage != null && !MaskImage.Disposed)
                MaskImage.Dispose();

            Image = null;
            MaskImage = null;
            TextureValid = false;
            _alphaCoverage = null;
            TrueSize = Size.Empty;
        }

        public bool VisiblePixel(Point point)
        {
            return TextureValid && _alphaCoverage != null &&
                   _alphaCoverage.VisiblePixel(point.X, point.Y);
        }

        public Size GetTrueSize()
        {
            return TrueSize.IsEmpty ? new Size(Width, Height) : TrueSize;
        }

        // GZip data is tightly packed BGRA; an upload rectangle can have
        // padded rows. Stream row-by-row only when its native pitch requires it.
        private static void DecompressBgraRows(byte[] data, Stream destination,
                                               int width, int height, int pitch)
        {
            int rowBytes = checked(width * 4);
            if (pitch < rowBytes)
                throw new InvalidDataException("Invalid BGRA texture pitch.");

            using (var compressed = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                if (pitch == rowBytes)
                {
                    compressed.CopyTo(destination);
                    if (destination.Position != (long)rowBytes * height)
                        throw new EndOfStreamException("Incomplete BGRA texture data.");
                    return;
                }

                byte[] row = new byte[rowBytes];
                for (int y = 0; y < height; y++)
                {
                    int filled = 0;
                    while (filled < rowBytes)
                    {
                        int read = compressed.Read(row, filled, rowBytes - filled);
                        if (read == 0)
                            throw new EndOfStreamException("Incomplete BGRA texture data.");
                        filled += read;
                    }
                    destination.Position = (long)y * pitch;
                    destination.Write(row, 0, row.Length);
                }
            }
        }

        private static byte[] DecompressImage(byte[] image)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(image), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private static void DecompressImage(byte[] data, Stream destination)
        {
            using (var stream = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                stream.CopyTo(destination);
            }
        }
    }
}
