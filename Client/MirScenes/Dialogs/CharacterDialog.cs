using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using C = ClientPackets;
using Client.MirSounds;

namespace Client.MirScenes.Dialogs
{
    public sealed class CharacterDialog : MirPanel
    {
        private const int Margin = 8, GearWidth = 248, StatsWidth = 202, BodyTop = 90, BodyHeight = 282;
        private const int StatsLeft = Margin * 2 + GearWidth, WideWidth = StatsLeft + (StatsWidth + Margin) * 2;

        public readonly MirImageControl CharacterPage, SkillPage;
        public readonly MirItemCell[] Grid;

        private readonly MirButton _characterButton, _skillButton;
        private readonly MirImageControl _classImage;
        private readonly MirLabel _nameLabel, _identityLabel;
        private readonly CharacterStatsPane _combat, _attributes;
        private readonly MagicButton[] _magics;
        private readonly MirButton[] _specialisationButtons;
        private readonly MirLabel _specialisationLabel;
        private readonly UserObject _actor;
        private int _spellOffset;

        public CharacterDialog(MirGridType gridType, UserObject actor)
        {
            _actor = actor;

            // Leave the standard inventory's width clear at both supported layout sizes.
            bool wide = Settings.ScreenWidth >= WideWidth + 318 + Margin;
            AutoSize = false;
            Size = new Size(wide ? WideWidth : StatsLeft + StatsWidth + Margin, BodyTop + BodyHeight + 10);
            Location = new Point(Settings.ScreenWidth - Size.Width, 0);
            Movable = true;
            Sort = true;
            PanelTitle = "";

            BeforeDraw += (_, _) => RefreshInterface();

            CharacterPage = new MirImageControl
            {
                Index = 340,
                Parent = this,
                Library = Libraries.Prguse,
                AutoSize = false,
                Size = new Size(GearWidth, BodyHeight),
                Location = new Point(Margin, BodyTop),
            };
            CharacterPage.AfterDraw += (o, e) =>
            {
                if (Libraries.StateItems == null) return;
                if (Grid[(int)EquipmentSlot.Armour].Item != null)
                {
                    if (actor.WingEffect == 1 || actor.WingEffect == 2)
                    {
                        int wingOffset = actor.WingEffect == 1 ? 2 : 4;

                        int genderOffset = actor.Gender == MirGender.Male ? 0 : 1;

                        Libraries.Prguse2.DrawBlend(1200 + wingOffset + genderOffset, DisplayLocation, Color.White, true, 1F);
                    }

                    ItemInfo realItem = Functions.GetRealItem(Grid[(int)EquipmentSlot.Armour].Item.Info, actor.Level, actor.Class, GameScene.ItemInfoList);
                    Libraries.StateItems.Draw(realItem.Image, DisplayLocation, Color.White, true, 1F);

                }
                if (Grid[(int)EquipmentSlot.Weapon].Item != null)
                {
                    ItemInfo realItem = Functions.GetRealItem(Grid[(int)EquipmentSlot.Weapon].Item.Info, actor.Level, actor.Class, GameScene.ItemInfoList);
                    Libraries.StateItems.Draw(realItem.Image, DisplayLocation, Color.White, true, 1F);

                }

                if (Grid[(int)EquipmentSlot.Helmet].Item != null)
                    Libraries.StateItems.Draw(Grid[(int)EquipmentSlot.Helmet].Item.Info.Image, DisplayLocation, Color.White, true, 1F);
                else
                {
                    int hair = 441 + actor.Hair + (actor.Class == MirClass.Assassin ? 20 : 0) + (actor.Gender == MirGender.Male ? 0 : 40);

                    int offSetX = actor.Class == MirClass.Assassin ? (actor.Gender == MirGender.Male ? 6 : 4) : 0;
                    int offSetY = actor.Class == MirClass.Assassin ? (actor.Gender == MirGender.Male ? 25 : 18) : 0;

                    Libraries.Prguse.Draw(hair, new Point(DisplayLocation.X + offSetX, DisplayLocation.Y + offSetY), Color.White, true, 1F);
                }
            };

            SkillPage = new MirImageControl
            {
                Skin = UiSurface.Inset,
                AutoSize = false,
                Size = new Size(GearWidth, BodyHeight),
                Parent = this,
                Location = new Point(Margin, BodyTop),
                Visible = false
            };

            var header = new MirImageControl
            {
                Parent = this, Skin = UiSurface.Header, NotControl = true, AutoSize = false,
                Location = new Point(Margin, 8), Size = new Size(Size.Width - Margin * 2, 49)
            };
            _nameLabel = AddCaption(header, "", new Rectangle(46, 3, header.Size.Width - 83, 22), false);
            _nameLabel.Font = new Font(FontFamily.GenericSerif, 10f, FontStyle.Bold);
            _nameLabel.DrawFormat |= TextFormatFlags.NoPrefix;
            _identityLabel = AddCaption(header, "", new Rectangle(46, 25, header.Size.Width - 83, 18));
            _identityLabel.DrawFormat |= TextFormatFlags.NoPrefix;
            _classImage = new MirImageControl
            {
                Index = 100, Library = Libraries.Prguse, Parent = header, Location = new Point(8, 13), NotControl = true
            };

            _characterButton = CreateTab("Equipment", Margin, 122);
            _characterButton.Click += (_, _) => ShowCharacterPage();
            _characterButton.Selected = true;
            _skillButton = CreateTab("Skills", Margin + 126, 122);
            _skillButton.Click += (_, _) => ShowSkillPage();

            var close = new MirButton
            {
                Skin = UiSurface.Button, Glyph = UiGlyph.Close, Hint = "Close",
                AutoSize = false, Size = new Size(22, 22), Location = new Point(Size.Width - 36, 14),
                Parent = this, Sound = SoundList.ButtonA
            };
            close.Click += (_, _) => Hide();

            int attributesLeft = wide ? StatsLeft + StatsWidth + Margin : StatsLeft;
            _combat = new CharacterStatsPane(actor, new Rectangle(StatsLeft, BodyTop, StatsWidth, BodyHeight), false)
                { Parent = this };
            _attributes = new CharacterStatsPane(actor, new Rectangle(attributesLeft, BodyTop, StatsWidth, BodyHeight), true)
                { Parent = this, Visible = wide };
            if (wide)
            {
                AddSectionHeading("Combat", StatsLeft);
                AddSectionHeading("Attributes", attributesLeft);
            }
            else
            {
                var combatTab = CreateTab("Combat", StatsLeft, 98);
                var attributeTab = CreateTab("Attributes", StatsLeft + 102, 100);
                combatTab.Selected = true;
                combatTab.Click += (_, _) => ShowAttributes(false);
                attributeTab.Click += (_, _) => ShowAttributes(true);

                void ShowAttributes(bool show)
                {
                    _combat.Visible = combatTab.Selected = !show;
                    _attributes.Visible = attributeTab.Selected = show;
                    Process();
                    Redraw();
                }
            }

            Grid = new MirItemCell[Enum.GetNames(typeof(EquipmentSlot)).Length];

            Grid[(int)EquipmentSlot.Weapon] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Weapon,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(123, 7),
            };


            Grid[(int)EquipmentSlot.Armour] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Armour,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(163, 7),
            };


            Grid[(int)EquipmentSlot.Helmet] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Helmet,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 7),
            };



            Grid[(int)EquipmentSlot.Torch] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Torch,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 134),
            };


            Grid[(int)EquipmentSlot.Necklace] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Necklace,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 98),
            };


            Grid[(int)EquipmentSlot.BraceletL] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.BraceletL,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(8, 170),
            };

            Grid[(int)EquipmentSlot.BraceletR] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.BraceletR,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 170),
            };

            Grid[(int)EquipmentSlot.RingL] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.RingL,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(8, 206),
            };

            Grid[(int)EquipmentSlot.RingR] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.RingR,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 206),
            };


            Grid[(int)EquipmentSlot.Amulet] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Amulet,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(8, 242),
            };


            Grid[(int)EquipmentSlot.Boots] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Boots,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(48, 242),
            };

            Grid[(int)EquipmentSlot.Belt] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Belt,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(88, 242),
            };


            Grid[(int)EquipmentSlot.Stone] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Stone,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(128, 242),
            };

            Grid[(int)EquipmentSlot.Mount] = new MirItemCell
            {
                ItemSlot = (int)EquipmentSlot.Mount,
                GridType = gridType,
                Parent = CharacterPage,
                Location = new Point(203, 62),
            };

            // The save stores exactly one selection for each actor. Spell rows remain
            // available for review, but inactive spells cannot be dragged or cast.
            _specialisationButtons = new MirButton[3];
            for (int i = 0; i < _specialisationButtons.Length; i++)
            {
                CombatSpecialisation choice = (CombatSpecialisation)(i + 1);
                string name = SpellCurriculum.Name(actor.Class, choice);
                SpecialisationAbility signature = SpecialisationAbilities.Signature(actor.Class, choice);
                var button = new MirButton
                {
                    Parent = SkillPage,
                    Skin = UiSurface.Button,
                    Text = name,
                    Hint = signature == null ? name : SpecialisationPreview(name, signature),
                    AutoSize = false,
                    Size = new Size(74, 24),
                    Location = new Point(8 + i * 76, 8),
                    Sound = SoundList.ButtonA
                };
                button.Click += (_, _) => ChooseSpecialisation(choice);
                _specialisationButtons[i] = button;
            }
            _specialisationLabel = AddCaption(SkillPage, "", new Rectangle(8, 38, 231, 20), false);

            _magics = new MagicButton[5];
            for (int i = 0; i < _magics.Length; i++)
                _magics[i] = new MagicButton
                {
                    Parent = SkillPage,
                    Visible = false,
                    Location = new Point(8, 64 + i * 33),
                    HeroMagic = gridType == MirGridType.HeroEquipment
                };

            AddSpellPageButton(UiGlyph.Left, "Previous spells", 90, -_magics.Length);
            AddSpellPageButton(UiGlyph.Right, "Next spells", 140, _magics.Length);
        }

        private static string SpecialisationPreview(string name, SpecialisationAbility signature) =>
            $"{name}\n{signature.Name} · Level {signature.UnlockLevel}\n{signature.Description}\n" +
            (SpellCombos.For(signature.Spell) is { } combo ? $"{combo.Rotation}\n" : "") +
            string.Join("\n", SpecialisationAbilities.TraitsFor(signature.Spell)
                .Select(trait => $"Level {trait.Level} · {trait.Name}\n{trait.Description}"));

        private void ChooseSpecialisation(CombatSpecialisation requested)
        {
            if (GameScene.Observing || _actor.Specialisation != CombatSpecialisation.Unchosen) return;
            if (!SpellCurriculum.CanSelect(_actor.Class, _actor.Level, _actor.Specialisation, requested))
            {
                GameScene.Scene.OutputMessage("Specialisations unlock at level " +
                    SpellCurriculum.SpecialisationUnlockLevel + ".");
                return;
            }

            string name = SpellCurriculum.Name(_actor.Class, requested);
            var signature = SpecialisationAbilities.Signature(_actor.Class, requested);
            if (signature == null) return;
            var confirm = new MirMessageBox(SpecialisationPreview(name, signature) +
                "\n\nChoose this permanent specialisation?", MirMessageBoxButtons.YesNo);
            confirm.Size = new Size(460, 346);
            confirm.Location = new Point((Settings.ScreenWidth - 460) / 2, (Settings.ScreenHeight - 346) / 2);
            confirm.Label.Size = new Size(412, 246);
            confirm.YesButton.Location = new Point(confirm.YesButton.Location.X, 300);
            confirm.NoButton.Location = new Point(confirm.NoButton.Location.X, 300);
            confirm.YesButton.Click += (_, _) =>
            {
                Network.Enqueue(new C.SelectSpecialisation
                {
                    ForHero = _actor is UserHeroObject,
                    Specialisation = requested
                });
                confirm.Dispose();
            };
            confirm.NoButton.Click += (_, _) => confirm.Dispose();
            confirm.Show();
        }

        private MirButton CreateTab(string text, int left, int width) => new()
        {
            Skin = UiSurface.Button, Text = text, Parent = this, AutoSize = false,
            Location = new Point(left, 65), Size = new Size(width, 22), Sound = SoundList.ButtonA
        };

        private void AddSectionHeading(string text, int left)
        {
            var header = new MirImageControl
            {
                Parent = this, Skin = UiSurface.Header, NotControl = true, AutoSize = false,
                Location = new Point(left, 65), Size = new Size(StatsWidth, 22)
            };
            AddCaption(header, text, new Rectangle(10, 1, StatsWidth - 20, 20), false);
        }

        private void AddSpellPageButton(UiGlyph glyph, string hint, int left, int delta)
        {
            var button = new MirButton
            {
                Skin = UiSurface.Button, Glyph = glyph, Hint = hint, Parent = SkillPage,
                AutoSize = false, Size = new Size(22, 22), Location = new Point(left, 250),
                Sound = SoundList.ButtonA
            };
            button.Click += (_, _) =>
            {
                int next = _spellOffset + delta;
                if (next < 0 || (delta > 0 && next >= _actor.Magics.Count)) return;
                _spellOffset = next;
                RefreshSpells();
            };
        }

        public override void Hide()
        {
            GameScene.Scene.SocketDialog.Hide();
            base.Hide();
        }

        public void ShowCharacterPage() => ShowSkills(false);

        public void ShowSkillPage() => ShowSkills(true);

        private void ShowSkills(bool show)
        {
            CharacterPage.Visible = _characterButton.Selected = !show;
            SkillPage.Visible = _skillButton.Selected = show;
            Redraw();
        }

        public void Process()
        {
            _combat.RefreshValues();
            _attributes.RefreshValues();
        }

        private void RefreshInterface()
        {
            CharacterPage.Index = _actor.Gender == MirGender.Male ? 340 : 341;
            _classImage.Index = _actor.Class switch
            {
                MirClass.Wizard => 101,
                MirClass.Taoist => 102,
                MirClass.Assassin => 103,
                MirClass.Archer => 104,
                _ => 100
            };
            _nameLabel.Text = _actor.Name;
            string identity = $"Level {_actor.Level} · {_actor.Class}";
            if (!string.IsNullOrEmpty(_actor.GuildName))
                identity += $" · {_actor.GuildName} {_actor.GuildRankName}";
            _identityLabel.Text = identity;

            if (SkillPage.Visible)
            {
                if (_actor.Specialisation == CombatSpecialisation.Unchosen)
                    _specialisationLabel.Text = _actor.Level < SpellCurriculum.SpecialisationUnlockLevel
                        ? "Specialisation unlocks at level " + SpellCurriculum.SpecialisationUnlockLevel
                        : "Choose a specialisation";
                else
                    _specialisationLabel.Text = "Specialisation: " +
                        SpellCurriculum.Name(_actor.Class, _actor.Specialisation);

                for (int i = 0; i < _specialisationButtons.Length; i++)
                    _specialisationButtons[i].Selected = _actor.Specialisation == (CombatSpecialisation)(i + 1);

                RefreshSpells();
            }
        }

        private void RefreshSpells()
        {
            for (int i = 0; i < _magics.Length; i++)
            {
                int index = i + _spellOffset;
                _magics[i].Visible = index < _actor.Magics.Count;
                if (_magics[i].Visible)
                {
                    ClientMagic magic = _actor.Magics[index];
                    _magics[i].Update(magic,
                        SpellCurriculum.CanUse(_actor.Class, _actor.Specialisation, magic.Spell));
                }
            }
        }

        public MirItemCell GetCell(ulong id)
        {

            for (int i = 0; i < Grid.Length; i++)
            {
                if (Grid[i].Item == null || Grid[i].Item.UniqueID != id) continue;
                return Grid[i];
            }
            return null;
        }

    }
}
