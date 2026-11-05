using Client.MirSounds;

namespace Client
{
    class Settings
    {
        public const long CleanDelay = 600000;

        public static int ScreenWidth = 1024, ScreenHeight = 768;
        private static InIReader Reader = new InIReader(@".\Mir2Config.ini");
        private static InIReader QuestTrackingReader = new InIReader(Path.Combine(UserDataPath, @".\QuestTracking.ini"));

        private static bool _useTestConfig;
        public static bool UseTestConfig
        {
            get
            {
                return _useTestConfig;
            }
            set
            {
                if (value == true)
                {
                    Reader = new InIReader(@".\Mir2Test.ini");
                }
                _useTestConfig = value;
            }
        }

        public const string DataPath = @".\Data\",
                            MapPath = @".\Map\",
                            SoundPath = @".\Sound\",
                            ExtraDataPath = @".\Data\Extra\",
                            ShadersPath = @".\Data\Shaders\",
                            MonsterPath = @".\Data\Monster\",
                            GatePath = @".\Data\Gate\",
                            FlagPath = @".\Data\Flag\",
                            SiegePath = @".\Data\Siege\",
                            NPCPath = @".\Data\NPC\",
                            CArmourPath = @".\Data\CArmour\",
                            CWeaponPath = @".\Data\CWeapon\",
                            CWeaponEffectPath = @".\Data\CWeaponEffect\",
                            CHairPath = @".\Data\CHair\",
                            AArmourPath = @".\Data\AArmour\",
                            AWeaponPath = @".\Data\AWeapon\",
                            AHairPath = @".\Data\AHair\",
                            ARArmourPath = @".\Data\ARArmour\",
                            ARWeaponPath = @".\Data\ARWeapon\",
                            ARHairPath = @".\Data\ARHair\",
                            CHumEffectPath = @".\Data\CHumEffect\",
                            AHumEffectPath = @".\Data\AHumEffect\",
                            ARHumEffectPath = @".\Data\ARHumEffect\",
                            MountPath = @".\Data\Mount\",
                            FishingPath = @".\Data\Fishing\",
                            PetsPath = @".\Data\Pet\",
                            TransformPath = @".\Data\Transform\",
                            TransformMountsPath = @".\Data\TransformRide2\",
                            TransformEffectPath = @".\Data\TransformEffect\",
                            TransformWeaponEffectPath = @".\Data\TransformWeaponEffect\",
                            MouseCursorPath = @".\Data\Cursors\",
                            ResourcePath = @".\DirectX\",
                            UserDataPath = @".\Data\UserData\",
                            DbLanguageJsonPath = @".\DbLanguage.json";

        //Logs
        public static bool LogErrors = true;
        public static bool LogChat = true;
        public static int RemainingErrorLogs = 100;

        //Graphics
        public static bool FullScreen = true, Borderless = true, TopMost = true, MouseClip = false;
        public static string FontName = "Arial"; //"MS Sans Serif"
        public static float FontSize = 8F;
        public static bool UseMouseCursors = true;

        // Artwork keeps its baked lighting. Extra projected shadows require
        // both this switch and an explicitly authorised source library.
        public static bool PostProcessing = true, DynamicShadows = false;
        public static string ProjectedShadowLibraries { get; private set; } = "";
        internal static CrystalM2.Rendering.Core.ShadowSourcePolicy ShadowSources { get; private set; } = new("");
        public static bool ShadowsFollowLights = true;
        public static int ShadowOpacityPercent = 36, ShadowLengthPercent = 100, ShadowSoftness = 0;
        public static bool EnvironmentAtmosphere = true;
        // Optional atmospheric overlays, pending Windows visual comparison.
        public static bool EnvironmentFogLayers = false;
        public static bool AtmosphereWetSurfaces = false;
        public static bool AtmosphereLightning = false;
        // Opt-in until Windows shader visual validation.
        public static bool LightingBloom = false, MapLightOcclusion = false;
        public static int BloomDivisor = 4;
        public static int BloomIntensityPercent = 100, VignetteIntensityPercent = 100;
        public static int AmbientDarknessPercent = 100, SpellLightIntensityPercent = 100,
            MapLightIntensityPercent = 100, SpellBlendIntensityPercent = 100;
        public static int WeatherParticleCap = 256;
        public static int AtmosphereIntensityPercent = 100;
        public static bool FPSCap = true;
        public static int MaxFPS = 100;
        public static int Resolution = 1024;
        public static bool DebugMode = false;

        //Network
        public static bool UseConfig = false;
        public static string IPAddress = "127.0.0.1";
        public static int Port = 7000;
        public const int TimeOut = 5000;

        //Sound
        public static int SoundOverLap = 3;
        private static byte _volume = 100;
        public static int SoundCleanMinutes = 5;

        public static byte Volume
        {
            get { return _volume; }
            set
            {
                switch (value)
                {
                    case > 100:
                        _volume = (byte)100;
                        break;
                    case <= 0:
                        _volume = (byte)0;
                        break;
                    default:
                        _volume = value;
                        break;
                }

                SoundManager.Vol = Convert.ToInt32(_volume);
            }
        }

        private static byte _musicVolume = 100;
        public static byte MusicVolume
        {
            get { return _musicVolume; }
            set
            {
                switch (value)
                {
                    case > 100:
                        _musicVolume = (byte)100;
                        break;
                    case <= 0:
                        _musicVolume = (byte)0;
                        break;
                    default:
                        _musicVolume = value;
                        break;
                }

                SoundManager.MusicVol = Convert.ToInt32(_musicVolume);
            }
        }

        //Game
        public static string AccountID = "",
                             Password = "";

        public static bool
            SkillMode = false,
            SkillBar = true,
            //SkillSet = true,
            Effect = true,
            LevelEffect = true,
            DropView = true,
            NameView = true,
            HPView = true,
            TransparentChat = false,
            ModeView = false,
            DuraView = false,
            DisplayDamage = true,
            TargetDead = false,
            HighlightTarget = true,
            ExpandedBuffWindow = true,
            ExpandedHeroBuffWindow = true,
            DisplayBodyName = false,
            NewMove = false;

        public static string Language = "English";

        public static bool ActionBarsLocked = true;
        public static int[,] SkillbarLocation = { { -1, -1 }, { -1, -1 }, { -1, -1 } };

        //Quests
        public static int[] TrackedQuests = new int[5];

        //Chat
        public static readonly Rectangle[] ChatWindows = new Rectangle[6];
        public static int ActiveChatTab;
        public static bool
            ShowNormalChat = true,
            ShowYellChat = true,
            ShowWhisperChat = true,
            ShowLoverChat = true,
            ShowMentorChat = true,
            ShowGroupChat = true,
            ShowGuildChat = true;

        //Filters
        public static bool
            FilterNormalChat = false,
            FilterWhisperChat = false,
            FilterShoutChat = false,
            FilterSystemChat = false,
            FilterLoverChat = false,
            FilterMentorChat = false,
            FilterGroupChat = false,
            FilterGuildChat = false;


        //AutoPatcher
        public static bool P_Patcher = true;
        public static string P_Host = @"http://mirfiles.com/mir2/cmir/patch/";
        public static string P_PatchFileName = @"PList.gz";
        public static bool P_NeedLogin = false;
        public static string P_Login = string.Empty;
        public static string P_Password = string.Empty;
        public static string P_ServerName = string.Empty;
        public static string P_BrowserAddress = "https://www.lomcn.org/mir2-patchsite/";
        public static string P_Client = Application.StartupPath + "\\";
        public static bool P_AutoStart = false;
        public static int P_Concurrency = 1;

        public static void Load()
        {


            if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
            if (!Directory.Exists(MapPath)) Directory.CreateDirectory(MapPath);
            if (!Directory.Exists(SoundPath)) Directory.CreateDirectory(SoundPath);

            //Graphics
            FullScreen = Reader.ReadBoolean("Graphics", "FullScreen", FullScreen);
            Borderless = Reader.ReadBoolean("Graphics", "Borderless", Borderless);
            MouseClip = Reader.ReadBoolean("Graphics", "MouseClip", MouseClip);
            TopMost = Reader.ReadBoolean("Graphics", "AlwaysOnTop", TopMost);
            FPSCap = Reader.ReadBoolean("Graphics", "FPSCap", FPSCap);
            PostProcessing = Reader.ReadBoolean("Graphics", "PostProcessing", PostProcessing);
            DynamicShadows = Reader.ReadBoolean("Graphics", "DynamicShadows", DynamicShadows);
            SetProjectedShadowLibraries(Reader.ReadString("Graphics", "ProjectedShadowLibraries", ProjectedShadowLibraries));
            ShadowsFollowLights = Reader.ReadBoolean("Graphics", "ShadowsFollowLights", ShadowsFollowLights);
            ShadowOpacityPercent = ReadGraphicsRange("ShadowOpacityPercent", ShadowOpacityPercent, 0, 100);
            ShadowLengthPercent = ReadGraphicsRange("ShadowLengthPercent", ShadowLengthPercent, 50, 175);
            ShadowSoftness = ReadGraphicsRange("ShadowSoftness", ShadowSoftness, 0, 8);
            EnvironmentAtmosphere = Reader.ReadBoolean("Graphics", "EnvironmentAtmosphere", EnvironmentAtmosphere);
            EnvironmentFogLayers = Reader.ReadBoolean("Graphics", "EnvironmentFogLayers", EnvironmentFogLayers);
            AtmosphereWetSurfaces = Reader.ReadBoolean("Graphics", "AtmosphereWetSurfaces", AtmosphereWetSurfaces);
            AtmosphereLightning = Reader.ReadBoolean("Graphics", "AtmosphereLightning", AtmosphereLightning);
            LightingBloom = Reader.ReadBoolean("Graphics", "LightingBloom", LightingBloom);
            MapLightOcclusion = Reader.ReadBoolean("Graphics", "MapLightOcclusion", MapLightOcclusion);
            BloomDivisor = Math.Clamp(Reader.ReadInt32("Graphics", "BloomDivisor", BloomDivisor), 2, 8);
            BloomIntensityPercent = ReadGraphicsRange("BloomIntensityPercent", BloomIntensityPercent, 0, 200);
            VignetteIntensityPercent = ReadGraphicsRange("VignetteIntensityPercent", VignetteIntensityPercent, 0, 100);
            AmbientDarknessPercent = ReadGraphicsRange(nameof(AmbientDarknessPercent), AmbientDarknessPercent, 0, 100);
            SpellLightIntensityPercent = ReadGraphicsRange(nameof(SpellLightIntensityPercent), SpellLightIntensityPercent, 0, 200);
            MapLightIntensityPercent = ReadGraphicsRange(nameof(MapLightIntensityPercent), MapLightIntensityPercent, 0, 200);
            SpellBlendIntensityPercent = ReadGraphicsRange(nameof(SpellBlendIntensityPercent), SpellBlendIntensityPercent, 0, 200);
            WeatherParticleCap = Math.Clamp(Reader.ReadInt32("Graphics", "WeatherParticleCap", WeatherParticleCap), 64, 1024);
            AtmosphereIntensityPercent = Math.Clamp(Reader.ReadInt32("Graphics", "AtmosphereIntensityPercent", AtmosphereIntensityPercent), 0, 100);
            Resolution = Reader.ReadInt32("Graphics", "Resolution", Resolution);
            DebugMode = Reader.ReadBoolean("Graphics", "DebugMode", DebugMode);
            UseMouseCursors = Reader.ReadBoolean("Graphics", "UseMouseCursors", UseMouseCursors);

            //Network
            UseConfig = Reader.ReadBoolean("Network", "UseConfig", UseConfig);
            if (UseConfig)
            {
                IPAddress = Reader.ReadString("Network", "IPAddress", IPAddress);
                Port = Reader.ReadInt32("Network", "Port", Port);
            }

            //Logs
            LogErrors = Reader.ReadBoolean("Logs", "LogErrors", LogErrors);
            LogChat = Reader.ReadBoolean("Logs", "LogChat", LogChat);

            //Sound
            Volume = Reader.ReadByte("Sound", "Volume", Volume);
            SoundOverLap = Reader.ReadInt32("Sound", "SoundOverLap", SoundOverLap);
            MusicVolume = Reader.ReadByte("Sound", "Music", MusicVolume);
            var n = Reader.ReadInt32("Sound", "CleanMinutes", SoundCleanMinutes);
            if (n < 1 || n > 60 * 3) n = SoundCleanMinutes;
            SoundCleanMinutes = n;


            //Game
            AccountID = Reader.ReadString("Game", "AccountID", AccountID);
            Password = Reader.ReadString("Game", "Password", Password);

            SkillMode = Reader.ReadBoolean("Game", "SkillMode", SkillMode);
            SkillBar = Reader.ReadBoolean("Game", "SkillBar", SkillBar);
            ActionBarsLocked = Reader.ReadBoolean("Game", "ActionBarsLocked", ActionBarsLocked);
            //SkillSet = Reader.ReadBoolean("Game", "SkillSet", SkillSet);
            Effect = Reader.ReadBoolean("Game", "Effect", Effect);
            LevelEffect = Reader.ReadBoolean("Game", "LevelEffect", Effect);
            DropView = Reader.ReadBoolean("Game", "DropView", DropView);
            NameView = Reader.ReadBoolean("Game", "NameView", NameView);
            HPView = Reader.ReadBoolean("Game", "HPMPView", HPView);
            ModeView = Reader.ReadBoolean("Game", "ModeView", ModeView);
            FontName = Reader.ReadString("Game", "FontName", FontName);
            TransparentChat = Reader.ReadBoolean("Game", "TransparentChat", TransparentChat);
            DisplayDamage = Reader.ReadBoolean("Game", "DisplayDamage", DisplayDamage);
            TargetDead = Reader.ReadBoolean("Game", "TargetDead", TargetDead);
            HighlightTarget = Reader.ReadBoolean("Game", "HighlightTarget", HighlightTarget);
            ExpandedBuffWindow = Reader.ReadBoolean("Game", "ExpandedBuffWindow", ExpandedBuffWindow);
            ExpandedHeroBuffWindow = Reader.ReadBoolean("Game", "ExpandedHeroBuffWindow", ExpandedHeroBuffWindow);
            DuraView = Reader.ReadBoolean("Game", "DuraWindow", DuraView);
            DisplayBodyName = Reader.ReadBoolean("Game", "DisplayBodyName", DisplayBodyName);
            NewMove = Reader.ReadBoolean("Game", "NewMove", NewMove);
            Language = Reader.ReadString("Game", "Language", Language);

            for (int i = 0; i < SkillbarLocation.Length / 2; i++)
            {
                SkillbarLocation[i, 0] = Reader.ReadInt32("Game", "Skillbar" + i.ToString() + "X", SkillbarLocation[i, 0]);
                SkillbarLocation[i, 1] = Reader.ReadInt32("Game", "Skillbar" + i.ToString() + "Y", SkillbarLocation[i, 1]);
            }

            // The original two default positions become the new HUD anchors.
            if (SkillbarLocation[0, 0] == 0 && SkillbarLocation[0, 1] == 0)
                SkillbarLocation[0, 0] = SkillbarLocation[0, 1] = -1;
            if (SkillbarLocation[1, 0] == 216 && SkillbarLocation[1, 1] == 0)
                SkillbarLocation[1, 0] = SkillbarLocation[1, 1] = -1;

            //Chat
            ActiveChatTab = Reader.ReadInt32("ChatLayout", "ActiveTab", 0, false);
            for (int i = 0; i < ChatWindows.Length; i++)
            {
                string key = "Window" + i;
                ChatWindows[i] = new Rectangle(
                    Reader.ReadInt32("ChatLayout", key + "X", 0, false),
                    Reader.ReadInt32("ChatLayout", key + "Y", 0, false),
                    Reader.ReadInt32("ChatLayout", key + "Width", 0, false),
                    Reader.ReadInt32("ChatLayout", key + "Height", 0, false));
            }
            ShowNormalChat = Reader.ReadBoolean("Chat", "ShowNormalChat", ShowNormalChat);
            ShowYellChat = Reader.ReadBoolean("Chat", "ShowYellChat", ShowYellChat);
            ShowWhisperChat = Reader.ReadBoolean("Chat", "ShowWhisperChat", ShowWhisperChat);
            ShowLoverChat = Reader.ReadBoolean("Chat", "ShowLoverChat", ShowLoverChat);
            ShowMentorChat = Reader.ReadBoolean("Chat", "ShowMentorChat", ShowMentorChat);
            ShowGroupChat = Reader.ReadBoolean("Chat", "ShowGroupChat", ShowGroupChat);
            ShowGuildChat = Reader.ReadBoolean("Chat", "ShowGuildChat", ShowGuildChat);

            //Filters
            FilterNormalChat = Reader.ReadBoolean("Filter", "FilterNormalChat", FilterNormalChat);
            FilterWhisperChat = Reader.ReadBoolean("Filter", "FilterWhisperChat", FilterWhisperChat);
            FilterShoutChat = Reader.ReadBoolean("Filter", "FilterShoutChat", FilterShoutChat);
            FilterSystemChat = Reader.ReadBoolean("Filter", "FilterSystemChat", FilterSystemChat);
            FilterLoverChat = Reader.ReadBoolean("Filter", "FilterLoverChat", FilterLoverChat);
            FilterMentorChat = Reader.ReadBoolean("Filter", "FilterMentorChat", FilterMentorChat);
            FilterGroupChat = Reader.ReadBoolean("Filter", "FilterGroupChat", FilterGroupChat);
            FilterGuildChat = Reader.ReadBoolean("Filter", "FilterGuildChat", FilterGuildChat);

            //AutoPatcher
            P_Patcher = Reader.ReadBoolean("Launcher", "Enabled", P_Patcher);
            P_Host = Reader.ReadString("Launcher", "Host", P_Host);
            P_PatchFileName = Reader.ReadString("Launcher", "PatchFile", P_PatchFileName);
            P_NeedLogin = Reader.ReadBoolean("Launcher", "NeedLogin", P_NeedLogin);
            P_Login = Reader.ReadString("Launcher", "Login", P_Login);
            P_Password = Reader.ReadString("Launcher", "Password", P_Password);
            P_AutoStart = Reader.ReadBoolean("Launcher", "AutoStart", P_AutoStart);
            P_ServerName = Reader.ReadString("Launcher", "ServerName", P_ServerName);
            P_BrowserAddress = Reader.ReadString("Launcher", "Browser", P_BrowserAddress);
            P_Concurrency = Reader.ReadInt32("Launcher", "ConcurrentDownloads", P_Concurrency);


            if (!P_Host.EndsWith("/")) P_Host += "/";
            if (P_Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) P_Host = P_Host.Insert(0, "http://");
            if (P_BrowserAddress.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) P_BrowserAddress = P_BrowserAddress.Insert(0, "http://");

            //Temp check to update everyones address
            if (P_Host.ToLower() == "http://mirfiles.co.uk/mir2/cmir/patch/")
            {
                P_Host = "http://mirfiles.com/mir2/cmir/patch/";
            }

            if (P_Concurrency < 1) P_Concurrency = 1;
            if (P_Concurrency > 100) P_Concurrency = 100;

            try
            {
                string languageDirectory = @".\Localization\";
                if (!Directory.Exists(languageDirectory))
                {
                    Directory.CreateDirectory(languageDirectory);
                }
                string settingLanguageFile = Path.Combine(languageDirectory, Language + ".json");
                GameLanguage.LoadClientLanguage(settingLanguageFile);
            }
            catch (Exception ex)
            {
                CMain.SaveError($"Load Client Language Error:{ex.Message}");
            }
            
        }

        private static int ReadGraphicsRange(string key, int fallback, int minimum, int maximum) =>
            Math.Clamp(Reader.ReadInt32("Graphics", key, fallback), minimum, maximum);

        internal static void SetProjectedShadowLibraries(string libraries)
        {
            ProjectedShadowLibraries = libraries;
            ShadowSources = new(libraries);
        }

        internal static void SaveGraphicsEffects()
        {
            Write(nameof(PostProcessing), PostProcessing);
            Write(nameof(DynamicShadows), DynamicShadows);
            Write(nameof(ProjectedShadowLibraries), ProjectedShadowLibraries);
            Write(nameof(ShadowsFollowLights), ShadowsFollowLights);
            Write(nameof(ShadowOpacityPercent), ShadowOpacityPercent);
            Write(nameof(ShadowLengthPercent), ShadowLengthPercent);
            Write(nameof(ShadowSoftness), ShadowSoftness);
            Write(nameof(EnvironmentAtmosphere), EnvironmentAtmosphere);
            Write(nameof(EnvironmentFogLayers), EnvironmentFogLayers);
            Write(nameof(AtmosphereWetSurfaces), AtmosphereWetSurfaces);
            Write(nameof(AtmosphereLightning), AtmosphereLightning);
            Write(nameof(AtmosphereIntensityPercent), AtmosphereIntensityPercent);
            Write(nameof(VignetteIntensityPercent), VignetteIntensityPercent);
            Write(nameof(LightingBloom), LightingBloom);
            Write(nameof(BloomIntensityPercent), BloomIntensityPercent);
            Write(nameof(BloomDivisor), BloomDivisor);
            Write(nameof(MapLightOcclusion), MapLightOcclusion);
            Write(nameof(AmbientDarknessPercent), AmbientDarknessPercent);
            Write(nameof(SpellLightIntensityPercent), SpellLightIntensityPercent);
            Write(nameof(MapLightIntensityPercent), MapLightIntensityPercent);
            Write(nameof(SpellBlendIntensityPercent), SpellBlendIntensityPercent);
            Reader.Save();

            void Write(string key, object value) => Reader.Write("Graphics", key, value.ToString(), save: false);
        }

        public static void Save()
        {
            //Graphics
            Reader.Write("Graphics", "FullScreen", FullScreen);
            Reader.Write("Graphics", "Borderless", Borderless);
            Reader.Write("Graphics", "MouseClip", MouseClip);
            Reader.Write("Graphics", "AlwaysOnTop", TopMost);
            Reader.Write("Graphics", "FPSCap", FPSCap);
            SaveGraphicsEffects();
            Reader.Write("Graphics", "WeatherParticleCap", WeatherParticleCap);
            Reader.Write("Graphics", "Resolution", Resolution);
            Reader.Write("Graphics", "DebugMode", DebugMode);
            Reader.Write("Graphics", "UseMouseCursors", UseMouseCursors);

            //Sound
            Reader.Write("Sound", "Volume", Volume);
            Reader.Write("Sound", "SoundOverLap", SoundOverLap);
            Reader.Write("Sound", "Music", MusicVolume);
            Reader.Write("Sound", "CleanMinutes", SoundCleanMinutes);

            //Game
            Reader.Write("Game", "AccountID", AccountID);
            Reader.Write("Game", "Password", Password);
            Reader.Write("Game", "SkillMode", SkillMode);
            Reader.Write("Game", "SkillBar", SkillBar);
            Reader.Write("Game", "ActionBarsLocked", ActionBarsLocked);
            //Reader.Write("Game", "SkillSet", SkillSet);
            Reader.Write("Game", "Effect", Effect);
            Reader.Write("Game", "LevelEffect", LevelEffect);
            Reader.Write("Game", "DropView", DropView);
            Reader.Write("Game", "NameView", NameView);
            Reader.Write("Game", "HPMPView", HPView);
            Reader.Write("Game", "ModeView", ModeView);
            Reader.Write("Game", "FontName", FontName);
            Reader.Write("Game", "TransparentChat", TransparentChat);
            Reader.Write("Game", "DisplayDamage", DisplayDamage);
            Reader.Write("Game", "TargetDead", TargetDead);
            Reader.Write("Game", "HighlightTarget", HighlightTarget);
            Reader.Write("Game", "ExpandedBuffWindow", ExpandedBuffWindow);
            Reader.Write("Game", "ExpandedHeroBuffWindow", ExpandedBuffWindow);
            Reader.Write("Game", "DuraWindow", DuraView);
            Reader.Write("Game", "DisplayBodyName", DisplayBodyName);
            Reader.Write("Game", "NewMove", NewMove);
            Reader.Write("Game", "Language", Language);

            for (int i = 0; i < SkillbarLocation.Length / 2; i++)
            {

                Reader.Write("Game", "Skillbar" + i.ToString() + "X", SkillbarLocation[i, 0]);
                Reader.Write("Game", "Skillbar" + i.ToString() + "Y", SkillbarLocation[i, 1]);
            }

            //Chat
            Reader.Write("ChatLayout", "ActiveTab", ActiveChatTab);
            for (int i = 0; i < ChatWindows.Length; i++)
            {
                string key = "Window" + i;
                Rectangle window = ChatWindows[i];
                Reader.Write("ChatLayout", key + "X", window.X);
                Reader.Write("ChatLayout", key + "Y", window.Y);
                Reader.Write("ChatLayout", key + "Width", window.Width);
                Reader.Write("ChatLayout", key + "Height", window.Height);
            }
            Reader.Write("Chat", "ShowNormalChat", ShowNormalChat);
            Reader.Write("Chat", "ShowYellChat", ShowYellChat);
            Reader.Write("Chat", "ShowWhisperChat", ShowWhisperChat);
            Reader.Write("Chat", "ShowLoverChat", ShowLoverChat);
            Reader.Write("Chat", "ShowMentorChat", ShowMentorChat);
            Reader.Write("Chat", "ShowGroupChat", ShowGroupChat);
            Reader.Write("Chat", "ShowGuildChat", ShowGuildChat);

            //Filters
            Reader.Write("Filter", "FilterNormalChat", FilterNormalChat);
            Reader.Write("Filter", "FilterWhisperChat", FilterWhisperChat);
            Reader.Write("Filter", "FilterShoutChat", FilterShoutChat);
            Reader.Write("Filter", "FilterSystemChat", FilterSystemChat);
            Reader.Write("Filter", "FilterLoverChat", FilterLoverChat);
            Reader.Write("Filter", "FilterMentorChat", FilterMentorChat);
            Reader.Write("Filter", "FilterGroupChat", FilterGroupChat);
            Reader.Write("Filter", "FilterGuildChat", FilterGuildChat);

            //AutoPatcher
            Reader.Write("Launcher", "Enabled", P_Patcher);
            Reader.Write("Launcher", "Host", P_Host);
            Reader.Write("Launcher", "PatchFile", P_PatchFileName);
            Reader.Write("Launcher", "NeedLogin", P_NeedLogin);
            Reader.Write("Launcher", "Login", P_Login);
            Reader.Write("Launcher", "Password", P_Password);
            Reader.Write("Launcher", "ServerName", P_ServerName);
            Reader.Write("Launcher", "Browser", P_BrowserAddress);
            Reader.Write("Launcher", "AutoStart", P_AutoStart);
            Reader.Write("Launcher", "ConcurrentDownloads", P_Concurrency);
        }

        public static void LoadTrackedQuests(string charName)
        {
            //Quests
            for (int i = 0; i < TrackedQuests.Length; i++)
            {
                TrackedQuests[i] = QuestTrackingReader.ReadInt32(charName, "Quest-" + i.ToString(), -1);
            }
        }

        public static void SaveTrackedQuests(string charName)
        {
            //Quests
            for (int i = 0; i < TrackedQuests.Length; i++)
            {
                QuestTrackingReader.Write(charName, "Quest-" + i.ToString(), TrackedQuests[i]);
            }
        }
    }


}
