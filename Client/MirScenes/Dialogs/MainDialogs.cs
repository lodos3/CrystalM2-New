using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirSounds;
using Font = System.Drawing.Font;
using C = ClientPackets;

namespace Client.MirScenes.Dialogs
{
    public sealed class MainDialog : MirPanel
    {
        public static UserObject User
        {
            get { return MapObject.User; }
            set { MapObject.User = value; }
        }

        public MirImageControl ExperienceBar, WeightBar;
        public MirButton GameShopButton, MenuButton, InventoryButton, CharacterButton, SkillButton, QuestButton, OptionButton;
        public MirControl HealthOrb, ManaOrb;
        private readonly Rectangle _deck;
        private readonly MirButton _bindButton, _lockButton;
        public MirLabel HealthLabel, ManaLabel, LevelLabel, CharacterName, ExperienceLabel, GoldLabel, WeightLabel, SpaceLabel, AModeLabel, PModeLabel, SModeLabel;
        public HeroInfoPanel HeroInfoPanel;
        public HeroBehaviourPanel HeroBehaviourPanel;

        public MirButton HeroMenuButton, HeroSummonButton;

        public bool HPOnly
        {
            get { return User != null && User.Class == MirClass.Warrior && User.Level < 26; }
        }

        public MainDialog()
        {
            AutoSize = false;
            Size = new Size(Settings.ScreenWidth, 154);
            Location = new Point(0, Settings.ScreenHeight - Size.Height);
            DrawImage = false;
            int center = Size.Width / 2;
            _deck = new Rectangle(center - 268, 44, 536, 108);
            BeforeDraw += (_, _) =>
            {
                Rectangle deck = _deck;
                deck.Offset(DisplayLocation);
                DXManager.Device.Ui.Draw(UiSurface.Window, deck);
                DXManager.Device.Ui.Draw(UiSurface.Header,
                    new Rectangle(DisplayLocation.X + center - 180, DisplayLocation.Y + 1, 360, 22));
                DXManager.Device.Ui.Draw(UiSurface.Header, new Rectangle(deck.X + 61, deck.Y + 62, deck.Width - 122, 25));
            };

            InventoryButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Bag,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.InventoryKey), CMain.InputKeys.GetKey(KeybindOptions.Inventory)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center -91, 108),
            };
            InventoryButton.Click += (o, e) =>
            {
                if (GameScene.Scene.InventoryDialog.Visible)
                    GameScene.Scene.InventoryDialog.Hide();
                else
                    GameScene.Scene.InventoryDialog.Show();
            };

            CharacterButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Person,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.CharacterKey), CMain.InputKeys.GetKey(KeybindOptions.Equipment)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center -67, 108),
            };
            CharacterButton.Click += (o, e) =>
            {
                if (GameScene.Scene.CharacterDialog.Visible && GameScene.Scene.CharacterDialog.CharacterPage.Visible)
                    GameScene.Scene.CharacterDialog.Hide();
                else
                {
                    GameScene.Scene.CharacterDialog.Show();
                    GameScene.Scene.CharacterDialog.ShowCharacterPage();
                }
            };

            SkillButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Sword,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.SkillsKey), CMain.InputKeys.GetKey(KeybindOptions.Skills)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center -43, 108),
            };
            SkillButton.Click += (o, e) =>
            {
                if (GameScene.Scene.CharacterDialog.Visible && GameScene.Scene.CharacterDialog.SkillPage.Visible)
                    GameScene.Scene.CharacterDialog.Hide();
                else
                {
                    GameScene.Scene.CharacterDialog.Show();
                    GameScene.Scene.CharacterDialog.ShowSkillPage();
                }
            };

            QuestButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Book,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.QuestsKey), CMain.InputKeys.GetKey(KeybindOptions.Quests)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center -19, 108),
            };
            QuestButton.Click += (o, e) =>
            {
                if (!GameScene.Scene.QuestLogDialog.Visible)
                    GameScene.Scene.QuestLogDialog.Show();
                else GameScene.Scene.QuestLogDialog.Hide();
            };

            OptionButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Gear,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.OptionsKey), CMain.InputKeys.GetKey(KeybindOptions.Options)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center +5, 108),
            };
            OptionButton.Click += (o, e) =>
            {
                if (!GameScene.Scene.OptionDialog.Visible)
                    GameScene.Scene.OptionDialog.Show();
                else GameScene.Scene.OptionDialog.Hide();
            };

            MenuButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Menu,
                Parent = this,
                Sound = SoundList.ButtonC,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.Menu),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center +29, 108),
            };
            MenuButton.Click += (o, e) =>
            {
                if (!GameScene.Scene.MenuDialog.Visible) GameScene.Scene.MenuDialog.Show();
                else GameScene.Scene.MenuDialog.Hide();
            };

            GameShopButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Coin,
                Parent = this,
                Sound = SoundList.ButtonC,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.GameShopKey), CMain.InputKeys.GetKey(KeybindOptions.GameShop)),
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center +53, 108),
            };
            GameShopButton.Click += (o, e) =>
            {
                if (!GameScene.Scene.GameShopDialog.Visible) GameScene.Scene.GameShopDialog.Show();
                else GameScene.Scene.GameShopDialog.Hide();
            };

            _bindButton = new MirButton
            {
                Parent = this, Skin = UiSurface.Button, AutoSize = false, Size = new Size(58, 22),
                Location = new Point(center - 159, 108), Text = "Bind",
                Hint = "Quick keybind mode. Hover a slot and press the shortcut."
            };
            _bindButton.Click += (_, _) => GameScene.Scene.ToggleActionBinding();
            _lockButton = new MirButton
            {
                Parent = this, Skin = UiSurface.Button, AutoSize = false, Size = new Size(24, 22),
                Location = new Point(center + 125, 108), Glyph = UiGlyph.Lock
            };
            _lockButton.Click += (_, _) =>
            {
                Settings.ActionBarsLocked = !Settings.ActionBarsLocked;
                UpdateActionButtons();
            };
            UpdateActionButtons();

            HealthOrb = CreateOrb(_deck.Left - 49, false, out HealthLabel);
            ManaOrb = CreateOrb(_deck.Right - 63, true, out ManaLabel);

            LevelLabel = AddCaption(this, "", new Rectangle(center - 174, 5, 60, 15));
            CharacterName = AddCaption(this, "", new Rectangle(center - 111, 5, 214, 15), false);
            CharacterName.DrawFormat |= TextFormatFlags.HorizontalCenter;

            ExperienceBar = new MirImageControl
            {
                AutoSize = false,
                Size = new Size(346, 7),
                Location = new Point(center - 173, 137),
                Parent = this,
                DrawImage = false,
                NotControl = true,
            };
            ExperienceBar.BeforeDraw += ExperienceBar_BeforeDraw;

            ExperienceLabel = AddCaption(this, "", new Rectangle(center + 113, 5, 61, 15));
            ExperienceLabel.DrawFormat |= TextFormatFlags.Right;

            GoldLabel = new MirLabel
            {
                DrawFormat = TextFormatFlags.VerticalCenter,
                Font = new Font(Settings.FontName, 8F),
                Location = new Point(center + 185, 133),
                Parent = this,
                Size = new Size(79, 14),
                Sound = SoundList.Gold,
            };
            GoldLabel.Click += (o, e) =>
            {
                if (GameScene.SelectedCell == null)
                    GameScene.PickedUpGold = !GameScene.PickedUpGold && GameScene.Gold > 0;
            };

            WeightBar = new MirImageControl
            {
                Skin = UiSurface.ScrollTrack,
                AutoSize = false,
                Size = new Size(81, 6),
                Location = new Point(center - 255, 139),
                Parent = this,
                DrawImage = false,
                NotControl = true,
            };
            WeightBar.BeforeDraw += WeightBar_BeforeDraw;

            WeightLabel = new MirLabel
            {
                Parent = this,
                Location = new Point(center - 252, 125),
                Size = new Size(40, 14),
            };

            SpaceLabel = new MirLabel
            {
                Parent = this,
                Location = new Point(center - 212, 125),
                Size = new Size(26, 14),
            };

            HeroMenuButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Menu,
                Hint = "Hero menu",
                Parent = this,
                Sound = SoundList.ButtonA,
                Visible = false,
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center +77, 108),
            };
            HeroMenuButton.Click += (o, e) =>
            {
                GameScene.Scene.HeroMenuPanel.Toggle();
            };

            HeroSummonButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Person,
                Hint = "Summon hero",
                Parent = this,
                Sound = SoundList.ButtonA,
                Visible = false,
                AutoSize = false,
                Size = new Size(22, 22),
                Location = new Point(center +101, 108),
            };
            HeroSummonButton.Click += (o, e) =>
            {
                Network.Enqueue(new C.Chat
                {
                    Message = "@SUMMONHERO",
                });
            };

            HeroInfoPanel = new HeroInfoPanel { Parent = this, Location = new Point(Size.Width - 202, -158), Visible = false };

            AModeLabel = new MirLabel
            {
                AutoSize = true,
                ForeColour = Color.Yellow,
                OutLineColour = Color.Black,
                Parent = this,
                Location = new Point(Settings.Resolution != 800 ? 899 : 675, Settings.Resolution != 800 ? -448 : -280),
                Visible = Settings.ModeView
            };

            PModeLabel = new MirLabel
            {
                AutoSize = true,
                ForeColour = Color.Orange,
                OutLineColour = Color.Black,
                Parent = this,
                Location = new Point(center - 262, 101),
                Visible = Settings.ModeView
            };

            SModeLabel = new MirLabel
            {
                AutoSize = true,
                ForeColour = Color.LimeGreen,
                OutLineColour = Color.Black,
                Parent = this,
                Location = new Point(Settings.Resolution != 800 ? 899 : 675, Settings.Resolution != 800 ? -463 : -295),
                Visible = Settings.ModeView
            };
        }

        public void Process()
        {
            switch (GameScene.Scene.AMode)
            {
                case AttackMode.Peace:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_Peace);
                    break;
                case AttackMode.Group:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_Group);
                    break;
                case AttackMode.Guild:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_Guild);
                    break;
                case AttackMode.EnemyGuild:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_EnemyGuild);
                    break;
                case AttackMode.RedBrown:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_RedBrown);
                    break;
                case AttackMode.All:
                    AModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AttackMode_All);
                    break;
            }

            switch (GameScene.Scene.PMode)
            {
                case PetMode.Both:
                    PModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PetMode_Both);
                    break;
                case PetMode.MoveOnly:
                    PModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PetMode_MoveOnly);
                    break;
                case PetMode.AttackOnly:
                    PModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PetMode_AttackOnly);
                    break;
                case PetMode.None:
                    PModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PetMode_None);
                    break;
                case PetMode.FocusMasterTarget:
                    PModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PetMode_FocusMasterTarget);
                    break;
            }

            switch (Settings.SkillMode)
            {
                case true:
                    SModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.SkillModeTilde);
                    break;
                case false:
                    SModeLabel.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.SkillModeCtrl);
                    break;
            }

            int maxHealth = User.Stats[Stat.HP], maxMana = User.Stats[Stat.MP];
            HealthLabel.Text = ResourceText(User.HP, maxHealth);
            ManaLabel.Text = HPOnly ? "—" : ResourceText(User.MP, maxMana);

            LevelLabel.Text = $"Level {User.Level}";
            ExperienceLabel.Text = $"{User.Experience / (double)Math.Max(1, User.MaxExperience):0.##%} XP";
            GoldLabel.Text = GameScene.Gold.ToString("N0");
            CharacterName.Text = User.Name;
            SpaceLabel.Text = User.Inventory.Count(t => t == null).ToString();
            WeightLabel.Text = (MapObject.User.Stats[Stat.BagWeight] - MapObject.User.CurrentBagWeight).ToString();
        }

        internal void UpdateActionButtons()
        {
            _bindButton.Text = GameScene.Scene.BindingActionKeys ? "Done" : "Bind";
            _bindButton.Selected = GameScene.Scene.BindingActionKeys;
            _lockButton.Selected = Settings.ActionBarsLocked;
            _lockButton.Hint = Settings.ActionBarsLocked
                ? "Bars locked. Shift-drag to move a spell. Click to unlock."
                : "Bars unlocked. Drag to move a spell. Click to lock.";
            Redraw();
        }

        public override bool IsMouseOver(Point point) => Visible &&
            (_deck.Contains(point.Subtract(DisplayLocation)) || HealthOrb?.IsMouseOver(point) == true ||
                ManaOrb?.IsMouseOver(point) == true || HeroInfoPanel?.IsMouseOver(point) == true);

        private static string ResourceText(int value, int maximum) => Settings.HPView
            ? $"{value:N0} / {maximum:N0}" : $"{value / (double)Math.Max(1, maximum):0%}";

        private MirControl CreateOrb(int x, bool mana, out MirLabel value)
        {
            int size = CrystalM2.Rendering.Core.DarkFantasyAtlas.OrbSize;
            var orb = new MirControl
            {
                Parent = this, Size = new Size(size, size), Location = new Point(x, 22),
                Hint = mana ? "Mana" : "Life"
            };
            orb.BeforeDraw += (_, _) =>
            {
                if (User == null) return;
                double maximum = Math.Max(1, User.Stats[mana ? Stat.MP : Stat.HP]);
                double fraction = (mana ? User.MP : User.HP) / maximum;
                DXManager.Device.Ui.Orb(orb.DisplayLocation, mana && HPOnly ? 0 : fraction, mana);
            };
            value = AddCaption(orb, "", new Rectangle(9, 51, size - 18, 20), false);
            value.DrawFormat |= TextFormatFlags.HorizontalCenter;
            value.OutLine = true;
            MirLabel caption = AddCaption(orb, mana ? "MANA" : "LIFE", new Rectangle(10, 72, size - 20, 14));
            caption.DrawFormat |= TextFormatFlags.HorizontalCenter;
            caption.OutLine = true;
            return orb;
        }

        private void ExperienceBar_BeforeDraw(object sender, EventArgs e)
        {
            if (User == null) return;
            DXManager.Device.Ui.Progress(ExperienceBar.DisplayRectangle,
                User.Experience / (double)Math.Max(1, User.MaxExperience), DarkFantasyPalette.Experience);
        }

        private void WeightBar_BeforeDraw(object sender, EventArgs e)
        {
            if (User == null) return;
            double weight = User.CurrentBagWeight / (double)Math.Max(1, User.Stats[Stat.BagWeight]);
            DXManager.Device.Ui.Progress(WeightBar.DisplayRectangle, weight,
                weight > 0.75 ? DarkFantasyPalette.Health : DarkFantasyPalette.Experience);
        }
    }
    public sealed class MiniMapDialog : MirPanel
    {
        public MirImageControl LightSetting, NewMail;
        public MirButton ToggleButton, BigMapButton, MailButton;
        public MirLabel LocationLabel, MapNameLabel;
        private float _fade = 1F;
        private bool _bigMode = true;

        public MirLabel AModeLabel, PModeLabel;

        public List<MirLabel> QuestIcons = new List<MirLabel>();

        public MiniMapDialog()
        {
            Index = 2090;
            Library = Libraries.Prguse;
            Location = new Point(Settings.ScreenWidth - 126, 0);
            PixelDetect = true;

            BeforeDraw += MiniMap_BeforeDraw;
            AfterDraw += MiniMapDialog_AfterDraw;

            MapNameLabel = new MirLabel
            {
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Parent = this,
                Size = new Size(120, 18),
                Location = new Point(2, 2),
                NotControl = true,
            };

            LocationLabel = new MirLabel
            {
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Parent = this,
                Size = new Size(56, 18),
                Location = new Point(46, 131),
                NotControl = true,
            };

            MailButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Mail,
                Index = 2099,
                HoverIndex = 2100,
                PressedIndex = 2101,
                Parent = this,
                Location = new Point(4, 131),
                Library = Libraries.Prguse,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.Mail)
            };
            MailButton.Click += (o, e) => GameScene.Scene.MailListDialog.Toggle();

            NewMail = new MirImageControl
            {
                Index = 544,
                Location = new Point(5, 132),
                Parent = this,
                Library = Libraries.Prguse,
                Visible = false,
                NotControl = true
            };

            BigMapButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Map,
                Index = 2096,
                HoverIndex = 2097,
                PressedIndex = 2098,
                Parent = this,
                Location = new Point(25, 131),
                Library = Libraries.Prguse,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.BigMapKey), CMain.InputKeys.GetKey(KeybindOptions.Bigmap))
            };
            BigMapButton.Click += (o, e) => GameScene.Scene.BigMapDialog.Toggle();

            ToggleButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Minus,
                Index = 2102,
                HoverIndex = 2103,
                PressedIndex = 2104,
                Parent = this,
                Location = new Point(109, 3),
                Library = Libraries.Prguse,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.MiniMapKey), CMain.InputKeys.GetKey(KeybindOptions.Minimap))
            };
            ToggleButton.Click += (o, e) => Toggle();

            LightSetting = new MirImageControl
            {
                Index = 2093,
                Library = Libraries.Prguse,
                Parent = this,
                Location = new Point(102, 131),
            };


            AModeLabel = new MirLabel
            {
                AutoSize = true,
                ForeColour = Color.Yellow,
                OutLineColour = Color.Black,
                Parent = this,
                Location = new Point(115, 125)
            };

            PModeLabel = new MirLabel
            {
                AutoSize = true,
                ForeColour = Color.Yellow,
                OutLineColour = Color.Black,
                Parent = this,
                Location = new Point(230, 125),
                Visible = false
            };
        }

        private void MiniMapDialog_AfterDraw(object sender, EventArgs e)
        {

        }

        private void MiniMap_BeforeDraw(object sender, EventArgs e)
        {

            foreach (var icon in QuestIcons)
                icon.Dispose();

            QuestIcons.Clear();

            MapControl map = GameScene.Scene.MapControl;
            if (map == null) return;

            if (map.MiniMap == 0 && Index != 2091)
            {
                SetSmallMode();
            }
            else if (map.MiniMap > 0 && _bigMode && Index == 2091)
            {
                SetBigMode();
            }

            if (map.MiniMap <= 0 || Index != 2090 || Libraries.MiniMap == null)
            {
                return;
            }

            Rectangle viewRect = new Rectangle(0, 0, 120, 108);
            Point drawLocation = Location;
            drawLocation.Offset(3, 22);

            Size miniMapSize = Libraries.MiniMap.GetSize(map.MiniMap);
            float scaleX = miniMapSize.Width / (float)map.Width;
            float scaleY = miniMapSize.Height / (float)map.Height;

            viewRect.Location = new Point(
                (int)(scaleX * MapObject.User.CurrentLocation.X) - viewRect.Width / 2,
                (int)(scaleY * MapObject.User.CurrentLocation.Y) - viewRect.Height / 2);

            //   viewRect.Location = viewRect.Location.Subtract(1, 1);
            if (viewRect.Right >= miniMapSize.Width)
                viewRect.X = miniMapSize.Width - viewRect.Width;
            if (viewRect.Bottom >= miniMapSize.Height)
                viewRect.Y = miniMapSize.Height - viewRect.Height;

            if (viewRect.X < 0) viewRect.X = 0;
            if (viewRect.Y < 0) viewRect.Y = 0;

            Libraries.MiniMap.Draw(map.MiniMap, viewRect, drawLocation, Color.FromArgb(255, 255, 255), _fade);


            int startPointX = (int)(viewRect.X / scaleX);
            int startPointY = (int)(viewRect.Y / scaleY);

            foreach (var ob in MapControl.Objects.Values)
            {
                if (ob.Race == ObjectType.Item || ob.Dead || ob.Race == ObjectType.Spell || ob.Sneaking) continue;
                float x = ((ob.CurrentLocation.X - startPointX) * scaleX) + drawLocation.X;
                float y = ((ob.CurrentLocation.Y - startPointY) * scaleY) + drawLocation.Y;

                Color colour;

                if ((GroupDialog.GroupList.Contains(ob.Name) && MapObject.User != ob) || ob.Name.EndsWith(string.Format("({0})", MapObject.User.Name)))
                    colour = Color.FromArgb(0, 0, 255);
                else
                    if (ob is PlayerObject)
                {
                    colour = Color.FromArgb(255, 255, 255);
                }
                else if (ob is NPCObject || ob.AI == 6)
                {
                    colour = Color.FromArgb(0, 255, 50);
                }
                else
                    colour = Color.FromArgb(255, 0, 0);

                DXManager.Draw(DXManager.RadarTexture, new Rectangle(0, 0, 2, 2), new Vector3((float)(x - 0.5), (float)(y - 0.5), 0.0F), colour);

                #region NPC Quest Icons

                if (ob is NPCObject npc && npc.GetAvailableQuests(true).Any())
                {
                    string text = "";
                    Color color = Color.Empty;

                    switch (npc.QuestIcon)
                    {
                        case QuestIcon.ExclamationBlue:
                            color = Color.DodgerBlue;
                            text = "!";
                            break;
                        case QuestIcon.ExclamationYellow:
                            color = Color.Yellow;
                            text = "!";
                            break;
                        case QuestIcon.ExclamationGreen:
                            color = Color.Green;
                            text = "!";
                            break;
                        case QuestIcon.QuestionBlue:
                            color = Color.DodgerBlue;
                            text = "?";
                            break;
                        case QuestIcon.QuestionWhite:
                            color = Color.White;
                            text = "?";
                            break;
                        case QuestIcon.QuestionYellow:
                            color = Color.Yellow;
                            text = "?";
                            break;
                        case QuestIcon.QuestionGreen:
                            color = Color.Green;
                            text = "?";
                            break;
                    }

                    QuestIcons.Add(new MirLabel
                    {
                        AutoSize = true,
                        Parent = GameScene.Scene.MiniMapDialog,
                        Font = new Font(Settings.FontName, 9f, FontStyle.Bold),
                        DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                        Text = text,
                        ForeColour = color,
                        Location = new Point((int)(x - Settings.ScreenWidth + GameScene.Scene.MiniMapDialog.Size.Width) - 6, (int)(y) - 10),
                        NotControl = true,
                        Visible = true,
                        Modal = true
                    });
                }

                #endregion

            }
        }

        public void Toggle()
        {
            if (_fade == 0F)
            {
                _bigMode = true;
                SetBigMode();
                _fade = 1F;
            }
            //else if(_fade == 1F)
            //{
            //    _bigMode = true;
            //    SetBigMode();
            //    _fade = 0.8F;
            //}
            else
            {
                _bigMode = false;
                SetSmallMode();
                _fade = 0;
            }

            Redraw();
        }

        private void SetSmallMode()
        {
            Index = 2091;
            int y = Size.Height - 23;
            MailButton.Location = new Point(4, y);
            NewMail.Location = new Point(5, y + 1);
            BigMapButton.Location = new Point(25, y);
            LocationLabel.Location = new Point(46, y);
            LightSetting.Location = new Point(102, y);

            GameScene.Scene.DuraStatusPanel.Location = new Point(GameScene.Scene.MiniMapDialog.Location.X + 86,
            GameScene.Scene.MiniMapDialog.Size.Height);
        }

        private void SetBigMode()
        {
            Index = 2090;
            int y = Size.Height - 23;
            MailButton.Location = new Point(4, y);
            NewMail.Location = new Point(5, y + 1);
            BigMapButton.Location = new Point(25, y);
            LocationLabel.Location = new Point(46, y);
            LightSetting.Location = new Point(102, y);

            GameScene.Scene.DuraStatusPanel.Location = new Point(GameScene.Scene.MiniMapDialog.Location.X + 86,
            GameScene.Scene.MiniMapDialog.Size.Height);
        }

        public void Process()
        {
            MapControl map = GameScene.Scene.MapControl;
            if (map == null) return;

            MapNameLabel.Text = map.Title;
            LocationLabel.Text = Functions.PointToString(MapObject.User.CurrentLocation);

            GameScene.Scene.MainDialog.SModeLabel.Location = new Point((GameScene.Scene.MiniMapDialog.Location.X - 3) - GameScene.Scene.MainDialog.Location.X,
            (GameScene.Scene.MiniMapDialog.Size.Height + 150) - Settings.ScreenHeight);
            GameScene.Scene.MainDialog.AModeLabel.Location = new Point((GameScene.Scene.MiniMapDialog.Location.X - 3) - GameScene.Scene.MainDialog.Location.X,
            (GameScene.Scene.MiniMapDialog.Size.Height + 165) - Settings.ScreenHeight);
            GameScene.Scene.MainDialog.PModeLabel.Location = new Point((GameScene.Scene.MiniMapDialog.Location.X - 3) - GameScene.Scene.MainDialog.Location.X,
            (GameScene.Scene.MiniMapDialog.Size.Height + 180) - Settings.ScreenHeight);

            if (GameScene.Scene.NewMail)
            {
                double time = (CMain.Time) / 100D;

                if (Math.Round(time) % 10 < 5 || GameScene.Scene.NewMailCounter >= 10)
                {
                    NewMail.Visible = true;
                }
                else
                {
                    if (NewMail.Visible)
                    {
                        GameScene.Scene.NewMailCounter++;
                    }

                    NewMail.Visible = false;
                }
            }
            else
            {
                NewMail.Visible = false;
            }
        }
    }
    public sealed class InspectDialog : MirPanel
    {
        public static UserItem[] Items = new UserItem[14];
        public static uint InspectID;

        public string Name;
        public string GuildName;
        public string GuildRank;
        public MirClass Class;
        public MirGender Gender;
        public byte Hair;
        public ushort Level;
        public string LoverName;
        public bool AllowObserve;

        public MirButton CloseButton, GroupButton, FriendButton, MailButton, TradeButton, LoverButton, ObserveButton;
        public MirImageControl CharacterPage, ClassImage;
        public MirLabel NameLabel;
        public MirLabel GuildLabel, LoverLabel;



        public MirItemCell
            WeaponCell,
            ArmorCell,
            HelmetCell,
            TorchCell,
            NecklaceCell,
            BraceletLCell,
            BraceletRCell,
            RingLCell,
            RingRCell,
            AmuletCell,
            BeltCell,
            BootsCell,
            StoneCell,
            MountCell;

        public InspectDialog()
        {
            Index = 430;
            Library = Libraries.Prguse;
            Location = new Point(536, 0);
            Movable = true;
            Sort = true;

            CharacterPage = new MirImageControl
            {
                Skin = UiSurface.Inset,
                Index = 340,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(8, 70),
            };
            CharacterPage.AfterDraw += (o, e) =>
            {
                if (Libraries.StateItems == null) return;

                ItemInfo RealItem = null;

                if (ArmorCell.Item != null)
                {
                    RealItem = Functions.GetRealItem(ArmorCell.Item.Info, Level, Class, GameScene.ItemInfoList);
                    Libraries.StateItems.Draw(RealItem.Image, new Point(DisplayLocation.X + 0, DisplayLocation.Y + -20), Color.White, true, 1F);

                    if (RealItem.Effect > 0)
                    {
                        int wingOffset = RealItem.Effect == 1 ? 2 : 4;

                        int genderOffset = MapObject.User.Gender == MirGender.Male ? 0 : 1;

                        Libraries.Prguse2.DrawBlend(1200 + wingOffset + genderOffset, new Point(DisplayLocation.X, DisplayLocation.Y - 20), Color.White, true, 1F);
                    }
                }

                if (WeaponCell.Item != null)
                {
                    RealItem = Functions.GetRealItem(WeaponCell.Item.Info, Level, Class, GameScene.ItemInfoList);
                    Libraries.StateItems.Draw(RealItem.Image, new Point(DisplayLocation.X, DisplayLocation.Y - 20),
                    Color.White, true, 1F);

                }

                if (HelmetCell.Item != null)
                    Libraries.StateItems.Draw(HelmetCell.Item.Info.Image, new Point(DisplayLocation.X, DisplayLocation.Y - 20), Color.White, true, 1F);
                else
                {
                    int hair = 441 + Hair + (Class == MirClass.Assassin ? 20 : 0) + (Gender == MirGender.Male ? 0 : 40);

                    int offSetX = Class == MirClass.Assassin ? (Gender == MirGender.Male ? 6 : 4) : 0;
                    int offSetY = Class == MirClass.Assassin ? (Gender == MirGender.Male ? 25 : 18) : 0;

                    Libraries.Prguse.Draw(hair, new Point(DisplayLocation.X + offSetX, DisplayLocation.Y + offSetY - 20), Color.White, true, 1F);
                }
            };


            CloseButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Close,
                Hint = "Close",
                HoverIndex = 361,
                Index = 360,
                Location = new Point(241, 3),
                Library = Libraries.Prguse2,
                Parent = this,
                PressedIndex = 362,
                Sound = SoundList.ButtonA,
            };
            CloseButton.Click += (o, e) => Hide();



            GroupButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Up,
                HoverIndex = 432,
                Index = 431,
                Location = new Point(55, 357),
                Library = Libraries.Prguse,
                Parent = this,
                PressedIndex = 433,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.InviteToGroup),
            };
            GroupButton.Click += (o, e) =>
            {

                if (GroupDialog.GroupList.Count >= Globals.MaxGroup)
                {
                    GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.GroupHasMaxMembers), ChatType.System);
                    return;
                }
                if (GroupDialog.GroupList.Count > 0 && GroupDialog.GroupList[0] != MapObject.User.Name)
                {

                    GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.YouNotGroupLeader), ChatType.System);
                }

                Network.Enqueue(new C.AddMember { Name = Name });
                return;
            };

            FriendButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Group,
                HoverIndex = 435,
                Index = 434,
                Location = new Point(85, 357),
                Library = Libraries.Prguse,
                Parent = this,
                PressedIndex = 436,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.AddToFriendsList),
            };
            FriendButton.Click += (o, e) =>
            {
                Network.Enqueue(new C.AddFriend { Name = Name, Blocked = false });
            };

            MailButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Mail,
                HoverIndex = 438,
                Index = 437,
                Location = new Point(115, 357),
                Library = Libraries.Prguse,
                Parent = this,
                PressedIndex = 439,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.SendMail),
            };
            MailButton.Click += (o, e) => GameScene.Scene.MailComposeLetterDialog.ComposeMail(Name);

            TradeButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Trade",
                HoverIndex = 524,
                Index = 523,
                Location = new Point(145, 357),
                Library = Libraries.Prguse,
                Parent = this,
                PressedIndex = 525,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.Trade),
            };
            TradeButton.Click += (o, e) => Network.Enqueue(new C.TradeRequest());

            ObserveButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Eye,
                Index = 854,
                HoverIndex = 855,
                PressedIndex = 856,
                Location = new Point(175, 357),
                Library = Libraries.Title,
                Parent = this,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.Observe),
            };
            ObserveButton.Click += (o, e) =>
            {
                if (AllowObserve)
                {
                    Network.Enqueue(new C.Observe { Name = Name });
                }
                else
                {
                    GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.PlayerDisabledObservation), ChatType.System);
                }
            };

            NameLabel = new MirLabel
            {
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Parent = this,
                Location = new Point(50, 12),
                Size = new Size(190, 20),
                NotControl = true
            };
            NameLabel.Click += (o, e) =>
            {
                GameScene.Scene.ChatDialog.ChatTextBox.SetFocus();
                GameScene.Scene.ChatDialog.ChatTextBox.Text = string.Format("/{0} ", Name);
                GameScene.Scene.ChatDialog.ChatTextBox.Visible = true;
                GameScene.Scene.ChatDialog.ChatTextBox.TextBox.SelectionLength = 0;
                GameScene.Scene.ChatDialog.ChatTextBox.TextBox.SelectionStart = Name.Length + 2;

            };
            LoverButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Lover",
                Index = 604,
                Location = new Point(17, 17),
                Library = Libraries.Prguse,
                Parent = this,
                Sound = SoundList.None
            };

            GuildLabel = new MirLabel
            {
                DrawFormat = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter,
                Parent = this,
                Location = new Point(50, 33),
                Size = new Size(190, 30),
                NotControl = true,
            };

            ClassImage = new MirImageControl
            {
                Index = 100,
                Library = Libraries.Prguse,
                Location = new Point(15, 33),
                Parent = this,
                NotControl = true,
            };


            WeaponCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Weapon,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(123, 7),
            };

            ArmorCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Armour,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(163, 7),
            };

            HelmetCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Helmet,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 7),
            };


            TorchCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Torch,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 134),
            };

            NecklaceCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Necklace,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 98),
            };

            BraceletLCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.BraceletL,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(8, 170),
            };
            BraceletRCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.BraceletR,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 170),
            };
            RingLCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.RingL,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(8, 206),
            };
            RingRCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.RingR,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 206),
            };

            AmuletCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Amulet,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(8, 242),
            };

            BootsCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Boots,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(48, 242),
            };
            BeltCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Belt,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(88, 242),
            };

            StoneCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Stone,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(128, 242),
            };

            MountCell = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Mount,
                GridType = MirGridType.Inspect,
                Parent = CharacterPage,
                Location = new Point(203, 62),
            };
        }

        public void RefreshInferface(bool IsHero)
        {
            int offSet = Gender == MirGender.Male ? 0 : 1;

            CharacterPage.Index = 340 + offSet;

            switch (Class)
            {
                case MirClass.Warrior:
                    ClassImage.Index = 100;// + offSet * 5;
                    break;
                case MirClass.Wizard:
                    ClassImage.Index = 101;// + offSet * 5;
                    break;
                case MirClass.Taoist:
                    ClassImage.Index = 102;// + offSet * 5;
                    break;
                case MirClass.Assassin:
                    ClassImage.Index = 103;// + offSet * 5;
                    break;
                case MirClass.Archer:
                    ClassImage.Index = 104;// + offSet * 5;
                    break;
            }

            NameLabel.Text = Name;
            GuildLabel.Text = GuildName + " " + GuildRank;
            if (LoverName != "")
            {
                LoverButton.Visible = true;
                LoverButton.Hint = LoverName;
            }
            else
                LoverButton.Visible = false;


            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i] == null) continue;
                GameScene.Bind(Items[i]);
            }

            //ObserveButton.Visible = AllowObserve; //Use this to allow observe button to be shown only if the player has Observe Enabled, pressing this wont bypass Observe being false.

            ObserveButton.Visible = !IsHero;

            TradeButton.Visible = !IsHero;
            MailButton.Visible = !IsHero;
            FriendButton.Visible = !IsHero;
            GroupButton.Visible = !IsHero;

        }


    }
    public sealed class OptionDialog : MirPanel
    {
        private static readonly Size GeneralSize = new(260, 328);
        private static readonly Size GraphicsSize = new(520, 469);
        private readonly MirControl[] _generalControls;
        private readonly GraphicsOptionsPage _graphicsPage;
        private readonly MirButton _generalTab, _graphicsTab;

        public MirButton SkillModeOn, SkillModeOff;
        public MirButton SkillBarOn, SkillBarOff;
        public MirButton EffectOn, EffectOff;
        public MirButton DropViewOn, DropViewOff;
        public MirButton NameViewOn, NameViewOff;
        public MirButton HPViewOn, HPViewOff;
        public MirButton NewMoveOn, NewMoveOff;
        public MirButton ObserveOn, ObserveOff;
        public MirImageControl SoundBar, MusicSoundBar;
        public MirImageControl VolumeBar, MusicVolumeBar;

        public MirButton CloseButton;


        public OptionDialog()
        {
            AutoSize = false;
            Size = GeneralSize;
            Movable = true;
            Sort = true;

            Location = new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2);

            BeforeDraw += OptionPanel_BeforeDraw;

            CloseButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Close,
                Hint = "Close",
                Index = 360,
                HoverIndex = 361,
                Library = Libraries.Prguse2,
                Location = new Point(Size.Width - 26, 5),
                Parent = this,
                Sound = SoundList.ButtonA,
                PressedIndex = 362,
            };
            CloseButton.Click += (o, e) => Hide();

            //tilde option
            SkillModeOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Tilde",
                Library = Libraries.Prguse2,
                Location = new Point(159, 68),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 451,
            };
            SkillModeOn.Click += (o, e) =>
            {
                GameScene.Scene.ChangeSkillMode(false);
            };

            //ctrl option
            SkillModeOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Ctrl",
                Library = Libraries.Prguse2,
                Location = new Point(201, 68),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 454
            };
            SkillModeOff.Click += (o, e) =>
            {
                GameScene.Scene.ChangeSkillMode(true);
            };

            SkillBarOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 93),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 457,
            };
            SkillBarOn.Click += (o, e) => Settings.SkillBar = true;

            SkillBarOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 93),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 460
            };
            SkillBarOff.Click += (o, e) => Settings.SkillBar = false;

            EffectOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 118),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 457,
            };
            EffectOn.Click += (o, e) => Settings.Effect = true;

            EffectOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 118),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 460
            };
            EffectOff.Click += (o, e) => Settings.Effect = false;

            DropViewOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 143),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 457,
            };
            DropViewOn.Click += (o, e) => Settings.DropView = true;

            DropViewOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 143),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 460
            };
            DropViewOff.Click += (o, e) => Settings.DropView = false;

            NameViewOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 168),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 457,
            };
            NameViewOn.Click += (o, e) => Settings.NameView = true;

            NameViewOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 168),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 460
            };
            NameViewOff.Click += (o, e) => Settings.NameView = false;

            HPViewOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 193),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 463,
            };
            HPViewOn.Click += (o, e) =>
            {
                Settings.HPView = true;
                GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.HpMpMode1), ChatType.Hint);
            };

            HPViewOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 193),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 466
            };
            HPViewOff.Click += (o, e) =>
            {
                Settings.HPView = false;
                GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.HpMpMode2), ChatType.Hint);
            };

            SoundBar = new MirImageControl
            {
                Skin = UiSurface.ScrollTrack,
                AutoSize = false,
                Size = new Size(78, 9),
                Location = new Point(159, 225),
                Parent = this,
                DrawImage = false,
            };
            SoundBar.MouseDown += SoundBar_MouseMove;
            SoundBar.MouseMove += SoundBar_MouseMove;
            SoundBar.BeforeDraw += SoundBar_BeforeDraw;

            VolumeBar = new MirImageControl
            {
                Skin = UiSurface.ScrollThumb,
                AutoSize = false,
                Size = new Size(8, 17),
                Location = new Point(155, 218),
                Parent = this,
                NotControl = true,
            };

            MusicSoundBar = new MirImageControl
            {
                Skin = UiSurface.ScrollTrack,
                AutoSize = false,
                Size = new Size(78, 9),
                Location = new Point(159, 251),
                Parent = this,
                DrawImage = false
            };
            MusicSoundBar.MouseDown += MusicSoundBar_MouseMove;
            MusicSoundBar.MouseMove += MusicSoundBar_MouseMove;
            MusicSoundBar.BeforeDraw += MusicSoundBar_BeforeDraw;

            MusicVolumeBar = new MirImageControl
            {
                Skin = UiSurface.ScrollThumb,
                AutoSize = false,
                Size = new Size(8, 17),
                Location = new Point(155, 244),
                Parent = this,
                NotControl = true,
            };

            NewMoveOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Title,
                Location = new Point(159, 296),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 853,
            };
            NewMoveOn.Click += (o, e) =>
            {
                Settings.NewMove = true;
                GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.NewMovementStyle), ChatType.Hint);
            };

            NewMoveOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Title,
                Location = new Point(201, 296),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 850
            };
            NewMoveOff.Click += (o, e) =>
            {
                Settings.NewMove = false;
                GameScene.Scene.ChatDialog.ReceiveChat(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.OldMovementStyle), ChatType.Hint);
            };

            ObserveOn = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "On",
                Library = Libraries.Prguse2,
                Location = new Point(159, 271),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 457,
            };
            ObserveOn.Click += (o, e) => ToggleObserve(true);

            ObserveOff = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Off",
                Library = Libraries.Prguse2,
                Location = new Point(201, 271),
                Parent = this,
                Sound = SoundList.ButtonA,
                Size = new Size(36, 17),
                PressedIndex = 460
            };
            ObserveOff.Click += (o, e) => ToggleObserve(false);

            PanelTitle = "Settings";
            foreach (var row in new[]
            {
                ("Skill modifier", SkillModeOn.Location.Y), ("Skill bar", SkillBarOn.Location.Y),
                ("Spell effects", EffectOn.Location.Y), ("Item names", DropViewOn.Location.Y),
                ("Character names", NameViewOn.Location.Y), ("Health and mana text", HPViewOn.Location.Y),
                ("Sound effects", SoundBar.Location.Y - 4), ("Music", MusicSoundBar.Location.Y - 4),
                ("Allow observing", ObserveOn.Location.Y), ("Modern movement", NewMoveOn.Location.Y)
            }) AddCaption(this, row.Item1, new Rectangle(16, row.Item2, 135, 17));

            _generalControls = Controls.Where(control => control != CloseButton).ToArray();
            _graphicsPage = new GraphicsOptionsPage { Parent = this, Location = new Point(16, 66) };
            _generalTab = AddTab("General", 16, showGraphics: false);
            _graphicsTab = AddTab("Graphics", 100, showGraphics: true);
            _generalTab.Selected = true;
        }

        private MirButton AddTab(string text, int x, bool showGraphics)
        {
            var tab = new MirButton
            {
                Parent = this, Location = new Point(x, 37), Size = new Size(78, 23), AutoSize = false,
                Skin = UiSurface.Button, Text = text, Sound = SoundList.ButtonA
            };
            tab.Click += (_, _) => SelectTab(showGraphics);
            return tab;
        }

        private void SelectTab(bool showGraphics)
        {
            if (_graphicsTab.Selected == showGraphics) return;

            Size previousSize = Size;
            Size = showGraphics ? GraphicsSize : GeneralSize;
            Location = new Point(
                Math.Clamp(Location.X + (previousSize.Width - Size.Width) / 2, 0, Math.Max(0, Settings.ScreenWidth - Size.Width)),
                Math.Clamp(Location.Y, 0, Math.Max(0, Settings.ScreenHeight - Size.Height)));
            CloseButton.Location = new Point(Size.Width - 26, 5);
            foreach (MirControl control in _generalControls) control.Visible = !showGraphics;
            _graphicsPage.Visible = showGraphics;
            _graphicsTab.Selected = showGraphics;
            _generalTab.Selected = !showGraphics;
        }

        private void ToggleObserve(bool allow)
        {
            if (GameScene.AllowObserve == allow) return;

            Network.Enqueue(new C.Chat
            {
                Message = "@ALLOWOBSERVE",
            });
        }

        public void ToggleSkillButtons(bool Ctrl)
        {
            foreach (KeyBind KeyCheck in CMain.InputKeys.Keylist)
            {
                if (KeyCheck.Key == Keys.None)
                    continue;
                if ((KeyCheck.function < KeybindOptions.Bar1Skill1) || (KeyCheck.function > KeybindOptions.Bar2Skill8)) continue;
                //need to test this 
                if ((KeyCheck.RequireCtrl != 1) && (KeyCheck.RequireTilde != 1)) continue;
                KeyCheck.RequireCtrl = (byte)(Ctrl ? 1 : 0);
                KeyCheck.RequireTilde = (byte)(Ctrl ? 0 : 1);
            }
        }

        private void SoundBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || SoundBar != ActiveControl) return;

            Point p = e.Location.Subtract(SoundBar.DisplayLocation);

            byte volume = (byte)Math.Clamp(p.X / (double)Math.Max(1, SoundBar.Size.Width) * 100, 0, 100);
            Settings.Volume = volume;

            double percent = Settings.Volume / 100D;

            SoundBar.Hint = $"{Settings.Volume}%";

            if (percent > 1) percent = 1;

            VolumeBar.Location = percent > 0 ? new Point(159 + (int)((SoundBar.Size.Width - 2) * percent), 218) : new Point(159, 218);
        }

        private void SoundBar_BeforeDraw(object sender, EventArgs e)
        {
            DrawVolume(SoundBar, VolumeBar, Settings.Volume);
        }

        private void MusicSoundBar_BeforeDraw(object sender, EventArgs e)
        {
            DrawVolume(MusicSoundBar, MusicVolumeBar, Settings.MusicVolume);
        }

        private static void DrawVolume(MirImageControl track, MirImageControl thumb, byte value)
        {
            double fraction = Math.Clamp(value / 100d, 0, 1);
            track.Hint = $"{value}%";
            DXManager.Device.Ui.Progress(track.DisplayRectangle, fraction, DarkFantasyPalette.Experience);
            thumb.Location = new Point(track.Location.X + (int)((track.Size.Width - thumb.Size.Width) * fraction), thumb.Location.Y);
        }

        private void MusicSoundBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || MusicSoundBar != ActiveControl) return;

            Point p = e.Location.Subtract(MusicSoundBar.DisplayLocation);

            byte volume = (byte)Math.Clamp(p.X / (double)Math.Max(1, MusicSoundBar.Size.Width) * 100, 0, 100);
            Settings.MusicVolume = volume;

            double percent = Settings.MusicVolume / 100D;

            MusicSoundBar.Hint = $"{Settings.MusicVolume}%";

            if (percent > 1) percent = 1;

            MusicVolumeBar.Location = percent > 0 ? new Point(159 + (int)((MusicSoundBar.Size.Width - 2) * percent), 244) : new Point(159, 244);
        }

        private void OptionPanel_BeforeDraw(object sender, EventArgs e)
        {
            SkillModeOn.Selected = !Settings.SkillMode;
            SkillModeOff.Selected = !SkillModeOn.Selected;
            SkillBarOn.Selected = Settings.SkillBar;
            SkillBarOff.Selected = !SkillBarOn.Selected;
            EffectOn.Selected = Settings.Effect;
            EffectOff.Selected = !EffectOn.Selected;
            DropViewOn.Selected = Settings.DropView;
            DropViewOff.Selected = !DropViewOn.Selected;
            NameViewOn.Selected = Settings.NameView;
            NameViewOff.Selected = !NameViewOn.Selected;
            HPViewOn.Selected = Settings.HPView;
            HPViewOff.Selected = !HPViewOn.Selected;
            ObserveOn.Selected = GameScene.AllowObserve;
            ObserveOff.Selected = !ObserveOn.Selected;
            NewMoveOn.Selected = Settings.NewMove;
            NewMoveOff.Selected = !NewMoveOn.Selected;
        }

    }
    public sealed class MenuDialog : MirPanel
    {
        public MirButton ExitButton,
                         LogOutButton,
                         HelpButton,
                         KeyboardLayoutButton,
                         RankingButton,
                         CraftingButton,
                         IntelligentCreatureButton,
                         RideButton,
                         FishingButton,
                         FriendButton,
                         MentorButton,
                         RelationshipButton,
                         GroupButton,
                         GuildButton;

        public MenuDialog()
        {
            Index = 567;
            Parent = GameScene.Scene;
            Library = Libraries.Title;
            Location = new Point(Settings.ScreenWidth - Size.Width, GameScene.Scene.MainDialog.Location.Y - Size.Height + 6);
            Sort = true;
            Visible = false;
            Movable = true;

            ExitButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Exit",
                HoverIndex = 634,
                Index = 633,
                Parent = this,
                Library = Libraries.Title,
                Location = new Point(3, 12),
                PressedIndex = 635,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.ExitKey), CMain.InputKeys.GetKey(KeybindOptions.Exit))
            };
            ExitButton.Click += (o, e) => GameScene.Scene.QuitGame();

            LogOutButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Log Out",
                HoverIndex = 637,
                Index = 636,
                Parent = this,
                Library = Libraries.Title,
                Location = new Point(3, 31),
                PressedIndex = 638,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.LogOutKey), CMain.InputKeys.GetKey(KeybindOptions.Logout))
            };
            LogOutButton.Click += (o, e) => GameScene.Scene.LogOut();


            HelpButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Help,
                Index = 1970,
                HoverIndex = 1971,
                PressedIndex = 1972,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 50),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.HelpKey), CMain.InputKeys.GetKey(KeybindOptions.Help))
            };
            HelpButton.Click += (o, e) =>
            {
                if (GameScene.Scene.HelpDialog.Visible)
                    GameScene.Scene.HelpDialog.Hide();
                else GameScene.Scene.HelpDialog.Show();
            };

            KeyboardLayoutButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Keyboard Layout",
                Index = 1973,
                HoverIndex = 1974,
                PressedIndex = 1975,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 69),
                Visible = true,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.KeyboardKeybind), CMain.InputKeys.GetKey(KeybindOptions.Keybind))
            };
            KeyboardLayoutButton.Click += (o, e) =>
            {
                if (GameScene.Scene.KeyboardLayoutDialog.Visible)
                    GameScene.Scene.KeyboardLayoutDialog.Hide();
                else GameScene.Scene.KeyboardLayoutDialog.Show();
            };

            RankingButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Ranking",
                Index = 2000,
                HoverIndex = 2001,
                PressedIndex = 2002,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 88),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.RankingKey), CMain.InputKeys.GetKey(KeybindOptions.Ranking))
                //Visible = false
            };
            RankingButton.Click += (o, e) =>
            {
                if (GameScene.Scene.RankingDialog.Visible)
                    GameScene.Scene.RankingDialog.Hide();
                else GameScene.Scene.RankingDialog.Show();
            };

            CraftingButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Crafting",
                Index = 2000,
                HoverIndex = 2001,
                PressedIndex = 2002,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 107),
                Visible = false
            };
            CraftingButton.Click += (o, e) =>
            {

            };

            IntelligentCreatureButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Intelligent Creature",
                Index = 431,
                HoverIndex = 432,
                PressedIndex = 433,
                Parent = this,
                Library = Libraries.Prguse2,
                Location = new Point(3, 126),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.CreaturesKey), CMain.InputKeys.GetKey(KeybindOptions.Creature))
            };
            IntelligentCreatureButton.Click += (o, e) =>
            {
                if (GameScene.Scene.IntelligentCreatureDialog.Visible)
                    GameScene.Scene.IntelligentCreatureDialog.Hide();
                else GameScene.Scene.IntelligentCreatureDialog.Show();
            };
            RideButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Ride",
                Index = 1976,
                HoverIndex = 1977,
                PressedIndex = 1978,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 145),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.MountKey), CMain.InputKeys.GetKey(KeybindOptions.MountWindow))
            };
            RideButton.Click += (o, e) =>
            {
                if (GameScene.Scene.MountDialog.Visible)
                    GameScene.Scene.MountDialog.Hide();
                else GameScene.Scene.MountDialog.Show();
            };

            FishingButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Fishing",
                Index = 1979,
                HoverIndex = 1980,
                PressedIndex = 1981,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 164),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.FishingKey), CMain.InputKeys.GetKey(KeybindOptions.Fishing))
            };
            FishingButton.Click += (o, e) =>
            {
                if (GameScene.Scene.FishingDialog.Visible)
                    GameScene.Scene.FishingDialog.Hide();
                else GameScene.Scene.FishingDialog.Show();
            };

            FriendButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Group,
                Index = 1982,
                HoverIndex = 1983,
                PressedIndex = 1984,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 183),
                Visible = true,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.FriendsKey), CMain.InputKeys.GetKey(KeybindOptions.Friends))
            };
            FriendButton.Click += (o, e) =>
            {
                if (GameScene.Scene.FriendDialog.Visible)
                    GameScene.Scene.FriendDialog.Hide();
                else GameScene.Scene.FriendDialog.Show();
            };

            MentorButton = new MirButton
            {
                Skin = UiSurface.Button,
                Text = "Mentor",
                Index = 1985,
                HoverIndex = 1986,
                PressedIndex = 1987,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 202),
                Visible = true,
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.MentorKey), CMain.InputKeys.GetKey(KeybindOptions.Mentor))
            };
            MentorButton.Click += (o, e) =>
            {
                if (GameScene.Scene.MentorDialog.Visible)
                    GameScene.Scene.MentorDialog.Hide();
                else GameScene.Scene.MentorDialog.Show();
            };


            RelationshipButton = new MirButton  /* lover button */
            {
                Skin = UiSurface.Button,
                Text = "Relationship",
                Index = 1988,
                HoverIndex = 1989,
                PressedIndex = 1990,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 221),
                Visible = true,
                Hint = string.Format(   GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.RelationshipKey), CMain.InputKeys.GetKey(KeybindOptions.Relationship))
            };
            RelationshipButton.Click += (o, e) =>
            {
                if (GameScene.Scene.RelationshipDialog.Visible)
                    GameScene.Scene.RelationshipDialog.Hide();
                else GameScene.Scene.RelationshipDialog.Show();
            };

            GroupButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Up,
                Index = 1991,
                HoverIndex = 1992,
                PressedIndex = 1993,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 240),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.GroupsKey), CMain.InputKeys.GetKey(KeybindOptions.Group))
            };
            GroupButton.Click += (o, e) =>
            {
                if (GameScene.Scene.GroupDialog.Visible)
                    GameScene.Scene.GroupDialog.Hide();
                else GameScene.Scene.GroupDialog.Show();
            };

            GuildButton = new MirButton
            {
                Skin = UiSurface.Button,
                Glyph = UiGlyph.Shield,
                Index = 1994,
                HoverIndex = 1995,
                PressedIndex = 1996,
                Parent = this,
                Library = Libraries.Prguse,
                Location = new Point(3, 259),
                Hint = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.GuildKey), CMain.InputKeys.GetKey(KeybindOptions.Guilds))
            };
            GuildButton.Click += (o, e) =>
            {
                if (GameScene.Scene.GuildDialog.Visible)
                    GameScene.Scene.GuildDialog.Hide();
                else GameScene.Scene.GuildDialog.Show();
            };

        }


    }
    public sealed class MagicButton : MirControl
    {
        public MirButton SkillButton;
        public MirLabel NameLabel, KeyLabel;
        public ClientMagic Magic;
        public MirImageControl CoolDown;
        public bool HeroMagic;
        public bool Usable = true;

        public MagicButton()
        {
            Size = new Size(231, 33);

            SkillButton = new MirButton
            {
                AutoSize = false,
                Size = new Size(31, 31),
                Skin = UiSurface.Slot,
                Hint = "Drag this spell onto an action bar.",
                Parent = this,
                Location = new Point(36, 0),
                Sound = SoundList.ButtonA,
            };
            SkillButton.AfterDraw += (_, _) =>
            {
                if (Magic == null) return;
                Libraries.MagIcon2.Draw(Magic.Icon * 2, SkillButton.DisplayLocation.Add(2, 2), new Size(27, 27),
                    Usable ? Color.White : Color.Gray);
            };
            SkillButton.MouseDown += (_, e) =>
            {
                if (e.Button == MouseButtons.Left && Magic != null && Usable)
                    GameScene.Scene.PressSpellbookSpell(HeroMagic ? GameScene.Hero : GameScene.User, Magic);
            };
            SkillButton.Click += (_, _) =>
            {
                UserObject actor = HeroMagic ? GameScene.Hero : GameScene.User;
                if (GameScene.Observing || Magic == null || !Usable || actor == null ||
                    (HeroMagic && GameScene.Scene.HeroSpawnState != HeroSpawnState.Summoned)) return;
                new AssignKeyPanel(actor, Magic, HeroMagic);
            };

            NameLabel = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(74, 8),
                NotControl = true,
            };

            KeyLabel = new MirLabel
            {
                AutoSize = true,
                Parent = this,
                Location = new Point(2, 2),
                NotControl = true,
            };

            CoolDown = new MirImageControl
            {
                Library = Libraries.Prguse2,
                Parent = this,
                Location = new Point(36, 0),
                Opacity = 0.6F,
                NotControl = true,
                UseOffSet = true,
            };
        }

        public void Update(ClientMagic magic, bool usable = true)
        {
            Magic = magic;
            bool passive = SpellCurriculum.IsPassive(magic.Spell);
            Usable = usable && !passive;
            NameLabel.Text = Magic.Name + (passive ? " [Passive]" : "");
            KeyLabel.Text = Usable && Magic.Key is > 0 and <= 24
                ? GameScene.CompactActionKey(CMain.InputKeys.GetKey(GameScene.ActionKey(Magic.Key))) : "";

            int mana = magic.BaseCost + magic.Level * magic.LevelCost;
            string details = passive ? "Passive ability" : $"{mana} MP · {magic.Delay / 1000f:0.#} s cooldown" +
                (magic.Range > 0 ? $" · {magic.Range} tiles" : "");
            if (Enum.TryParse(magic.Spell + "SkillDescription", out ClientTextKeys text))
            {
                string template = GameLanguage.ClientTextMap.GetLocalization(text);
                string[] description = template.Split('\n').Where(line => !line.Contains("{0}") && !line.Contains("{1}")).ToArray();
                details = string.Join("\n", description).Trim().Replace("{2}", mana.ToString()) + "\n\n" + details;
            }
            SkillButton.Hint = details + (SpellPerks.For(magic.Spell).Count > 0 ? "\nCustomise in Spell Perks." : "") +
                (passive ? "" : "\nDrag to an action bar or click to choose a slot.");
            SetDelay();
        }

        public void SetDelay()
        {
            if (Magic == null) return;

            int totalFrames = 34;

            long timeLeft = Magic.CastTime + Magic.Delay - CMain.Time;

            if (!Usable || timeLeft < 100)
            {
                CoolDown.Visible = false;
                return;
            }

            long delayPerFrame = Math.Max(1, Magic.Delay / totalFrames);
            int startFrame = (int)Math.Clamp(totalFrames - timeLeft / delayPerFrame, 0, totalFrames);

            if ((CMain.Time <= Magic.CastTime + Magic.Delay))
            {
                CoolDown.Visible = true;
                CoolDown.Index = 1290 + startFrame;
            }
        }
    }
    public sealed class AssignKeyPanel : MirPanel
    {
        private readonly MirButton[] _slots;
        private readonly MirButton _clear;
        private int _selected;

        public AssignKeyPanel(UserObject actor, ClientMagic magic, bool hero)
        {
            int offset = hero ? 17 : 1;
            _selected = magic.Key;
            Modal = true;
            AutoSize = false;
            Size = new Size(356, hero ? 122 : 156);
            Location = Center;
            Parent = GameScene.Scene;
            PanelTitle = "";

            AddCaption(this, $"Place {magic.Name} on an action bar", new Rectangle(47, 11, 265, 25), false);
            _ = new MirImageControl
            {
                Parent = this, Library = Libraries.MagIcon2, Index = magic.Icon * 2, Location = new Point(12, 10)
            };
            MirButton close = Button(new Point(324, 9), new Size(22, 22), "", UiGlyph.Close);
            close.Click += (_, _) => Dispose();
            KeyDown += (_, e) =>
            {
                if (e.KeyCode != Keys.Escape) return;
                e.Handled = e.SuppressKeyPress = true;
                Dispose();
            };

            _slots = new MirButton[hero ? 8 : 16];
            for (int i = 0; i < _slots.Length; i++)
            {
                int slot = offset + i;
                string key = CMain.InputKeys.GetKey(GameScene.ActionKey(slot));
                _slots[i] = Button(new Point(13 + 41 * (i % 8), 42 + 34 * (i / 8)), new Size(39, 29),
                    key.Length == 0 ? (i % 8 + 1).ToString() : GameScene.CompactActionKey(key));
                _slots[i].Hint = key.Length == 0 ? $"Slot {slot} · Unbound" : key;
                _slots[i].Click += (_, _) =>
                {
                    _selected = slot;
                    UpdateSelection();
                };
            }

            _clear = Button(new Point(13, Size.Height - 37), new Size(76, 25), "Clear");
            _clear.Click += (_, _) =>
            {
                _selected = 0;
                UpdateSelection();
            };
            MirButton save = Button(new Point(267, Size.Height - 37), new Size(76, 25), "Apply");
            save.Click += (_, _) =>
            {
                GameScene.Scene.AssignActionSpell(actor, magic, _selected, false);
                Dispose();
            };
            UpdateSelection();

            MirButton Button(Point location, Size size, string text, UiGlyph glyph = UiGlyph.None) => new()
            {
                Parent = this, AutoSize = false, Location = location, Size = size,
                Skin = UiSurface.Button, Glyph = glyph, Text = text
            };

            void UpdateSelection()
            {
                for (int i = 0; i < _slots.Length; i++)
                    _slots[i].Selected = i + offset == _selected;
                _clear.Selected = _selected == 0;
                Redraw();
            }
        }
    }

    public sealed class DuraStatusDialog : MirPanel
    {
        public MirButton Character;

        public DuraStatusDialog()
        {
            Size = new Size(40, 19);
            Location = new Point((GameScene.Scene.MiniMapDialog.Location.X + 86), GameScene.Scene.MiniMapDialog.Size.Height);

            Character = new MirButton()
            {
                Index = 2113,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(20, 19),
                Location = new Point(20, 0),
                HoverIndex = 2111,
                PressedIndex = 2112,
                Sound = SoundList.ButtonA,
                Hint = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.DuraPanel)
            };
            Character.Click += (o, e) =>
            {
                if (GameScene.Scene.CharacterDuraPanel.Visible == true)
                {
                    GameScene.Scene.CharacterDuraPanel.Hide();
                    Settings.DuraView = false;
                }
                else
                {
                    GameScene.Scene.CharacterDuraPanel.Show();
                    Settings.DuraView = true;
                }
            };
        }

    }
    public sealed class CharacterDuraPanel : MirImageControl
    {
        public MirImageControl GrayBackground, Background, Helmet, Armour, Belt, Boots, Weapon, Necklace, RightBracelet, LeftBracelet, RightRing, LeftRing, Torch, Stone, Amulet, Mount, Item1, Item2;

        public CharacterDuraPanel()
        {
            Index = 2105;
            Library = Libraries.Prguse;
            Movable = false;
            Location = new Point(Settings.ScreenWidth - 61, 200);

            GrayBackground = new MirImageControl()
            {
                Index = 2161,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(56, 80),
                Location = new Point(3, 3),
                Opacity = 0.4F
            };
            Background = new MirImageControl()
            {
                Skin = UiSurface.Inset,
                Index = 2162,
                Library = Libraries.Prguse,
                Parent = this,
                Size = new Size(56, 80),
                Location = new Point(3, 3),
            };

            #region Pieces

            Helmet = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(24, 3) };
            Belt = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 7), Location = new Point(23, 23) };
            Armour = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(28, 32), Location = new Point(16, 11) };
            Boots = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(24, 9), Location = new Point(17, 43) };
            Weapon = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 33), Location = new Point(4, 5) };
            Necklace = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(3, 67) };
            LeftBracelet = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 8), Location = new Point(3, 43) };
            RightBracelet = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 8), Location = new Point(43, 43) };
            LeftRing = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(3, 54) };
            RightRing = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(43, 54) };
            Torch = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(8, 32), Location = new Point(44, 5) };
            Stone = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(30, 54) };
            Amulet = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(16, 54) };
            Mount = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(12, 12), Location = new Point(43, 68) };
            Item1 = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(8, 12), Location = new Point(19, 67) };
            Item2 = new MirImageControl() { Index = -1, Library = Libraries.Prguse, Parent = Background, Size = new Size(8, 12), Location = new Point(31, 67) };

            #endregion
        }

        public void GetCharacterDura()
        {
            if (GameScene.Scene.CharacterDialog.Grid[0].Item == null) { Weapon.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[1].Item == null) { Armour.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[2].Item == null) { Helmet.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[3].Item == null) { Torch.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[4].Item == null) { Necklace.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[5].Item == null) { LeftBracelet.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[6].Item == null) { RightBracelet.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[7].Item == null) { LeftRing.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[8].Item == null) { RightRing.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[9].Item == null) { Amulet.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[10].Item == null) { Belt.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[11].Item == null) { Boots.Index = -1; }
            if (GameScene.Scene.CharacterDialog.Grid[12].Item == null) { Stone.Index = -1; }

            for (int i = 0; i < MapObject.User.Equipment.Length; i++)
            {
                if (MapObject.User.Equipment[i] == null) continue;
                UpdateCharacterDura(MapObject.User.Equipment[i]);
            }
        }
        public void UpdateCharacterDura(UserItem item)
        {
            int Warning = item.MaxDura / 2;
            int Danger = item.MaxDura / 5;
            ushort AmuletWarning = (ushort)(item.Info.StackSize / 2);
            ushort AmuletDanger = (ushort)(item.Info.StackSize / 5);

            switch (item.Info.Type)
            {
                case ItemType.Amulet: //Based on stacks of 5000
                    if (item.Count > AmuletWarning)
                        Amulet.Index = 2134;
                    if (item.Count <= AmuletWarning)
                        Amulet.Index = 2135;
                    if (item.Count <= AmuletDanger)
                        Amulet.Index = 2136;
                    if (item.Count == 0)
                        Amulet.Index = -1;
                    break;
                case ItemType.Armour:
                    if (item.CurrentDura > Warning)
                        Armour.Index = 2149;
                    if (item.CurrentDura <= Warning)
                        Armour.Index = 2150;
                    if (item.CurrentDura <= Danger)
                        Armour.Index = 2151;
                    if (item.CurrentDura == 0)
                        Armour.Index = -1;
                    break;
                case ItemType.Belt:
                    if (item.CurrentDura > Warning)
                        Belt.Index = 2158;
                    if (item.CurrentDura <= Warning)
                        Belt.Index = 2159;
                    if (item.CurrentDura <= Danger)
                        Belt.Index = 2160;
                    if (item.CurrentDura == 0)
                        Belt.Index = -1;
                    break;
                case ItemType.Boots:
                    if (item.CurrentDura > Warning)
                        Boots.Index = 2152;
                    if (item.CurrentDura <= Warning)
                        Boots.Index = 2153;
                    if (item.CurrentDura <= Danger)
                        Boots.Index = 2154;
                    if (item.CurrentDura == 0)
                        Boots.Index = -1;
                    break;
                case ItemType.Bracelet:
                    if (GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.BraceletR].Item != null && item.UniqueID == GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.BraceletR].Item.UniqueID)
                    {
                        if (item.CurrentDura > Warning)
                            RightBracelet.Index = 2143;
                        if (item.CurrentDura <= Warning)
                            RightBracelet.Index = 2144;
                        if (item.CurrentDura <= Danger)
                            RightBracelet.Index = 2145;
                        if (item.CurrentDura == 0)
                            RightBracelet.Index = -1;
                    }
                    else if (GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.BraceletL].Item != null && item.UniqueID == GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.BraceletL].Item.UniqueID)
                    {
                        if (item.CurrentDura > Warning)
                            LeftBracelet.Index = 2143;
                        if (item.CurrentDura <= Warning)
                            LeftBracelet.Index = 2144;
                        if (item.CurrentDura <= Danger)
                            LeftBracelet.Index = 2145;
                        if (item.CurrentDura == 0)
                            LeftBracelet.Index = -1;
                    }
                    break;
                case ItemType.Helmet:
                    if (item.CurrentDura > Warning)
                        Helmet.Index = 2155;
                    if (item.CurrentDura <= Warning)
                        Helmet.Index = 2156;
                    if (item.CurrentDura <= Danger)
                        Helmet.Index = 2157;
                    if (item.CurrentDura == 0)
                        Helmet.Index = -1;
                    break;
                case ItemType.Necklace:
                    if (item.CurrentDura > Warning)
                        Necklace.Index = 2122;
                    if (item.CurrentDura <= Warning)
                        Necklace.Index = 2123;
                    if (item.CurrentDura <= Danger)
                        Necklace.Index = 2124;
                    if (item.CurrentDura == 0)
                        Necklace.Index = -1;
                    break;
                case ItemType.Ring:
                    if (GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.RingR].Item != null && item.UniqueID == GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.RingR].Item.UniqueID)
                    {
                        if (item.CurrentDura > Warning)
                            RightRing.Index = 2131;
                        if (item.CurrentDura <= Warning)
                            RightRing.Index = 2132;
                        if (item.CurrentDura <= Danger)
                            RightRing.Index = 2133;
                        if (item.CurrentDura == 0)
                            RightRing.Index = -1;
                    }
                    else if (GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.RingL].Item != null && item.UniqueID == GameScene.Scene.CharacterDialog.Grid[(byte)EquipmentSlot.RingL].Item.UniqueID)
                    {
                        if (item.CurrentDura > Warning)
                            LeftRing.Index = 2131;
                        if (item.CurrentDura <= Warning)
                            LeftRing.Index = 2132;
                        if (item.CurrentDura <= Danger)
                            LeftRing.Index = 2133;
                        if (item.CurrentDura == 0)
                            LeftRing.Index = -1;
                    }
                    break;
                case ItemType.Stone:
                    if (item.CurrentDura == 0)
                        Stone.Index = 2137;
                    break;
                case ItemType.Mount:
                    if (item.CurrentDura > Warning)
                        Mount.Index = 2140;
                    if (item.CurrentDura <= Warning)
                        Mount.Index = 2141;
                    if (item.CurrentDura <= Danger)
                        Mount.Index = 2142;
                    if (item.CurrentDura == 0)
                        Mount.Index = -1;
                    break;
                case ItemType.Torch:
                    if (item.CurrentDura > Warning)
                        Torch.Index = 2146;
                    if (item.CurrentDura <= Warning)
                        Torch.Index = 2147;
                    if (item.CurrentDura <= Danger)
                        Torch.Index = 2148;
                    if (item.CurrentDura == 0)
                        Torch.Index = -1;
                    break;
                case ItemType.Weapon:
                    if (item.CurrentDura > Warning)
                        Weapon.Index = 2125;
                    if (item.CurrentDura <= Warning)
                        Weapon.Index = 2126;
                    if (item.CurrentDura <= Danger)
                        Weapon.Index = 2127;
                    if (item.CurrentDura == 0)
                        Weapon.Index = -1;
                    break;
            }
        }

        public override void Hide()
        {
            if (!Visible) return;
            Visible = false;
            GameScene.Scene.DuraStatusPanel.Character.Index = 2113;
        }
        public override void Show()
        {
            if (Visible) return;
            Visible = true;
            GameScene.Scene.DuraStatusPanel.Character.Index = 2110;

            GetCharacterDura();
        }
    }
}
