using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using C = ClientPackets;
using S = ServerPackets;

namespace Client.MirScenes.Dialogs;

internal sealed class TalentDialog : MirPanel
{
    private readonly Dictionary<CombatSpecialisation, List<TalentPick>> _drafts = new();
    private readonly MirButton[] _specTabs = new MirButton[3];
    private readonly MirButton _classTab, _specTab, _apply;
    private readonly MirLabel _detail, _message, _rotation;
    private readonly MirButton[] _options = new MirButton[3];
    private MirControl _graph;
    private TalentTree _inspectedTree;
    private TalentNode _inspectedNode;
    private CombatSpecialisation _spec;
    private int _revision;
    private readonly bool _narrow;
    private bool _showClass = true, _pending;

    public UserObject Actor { get; }

    public TalentDialog(UserObject actor, CombatSpecialisation? selected)
    {
        Actor = actor;
        Size = new Size(Math.Min(856, Settings.ScreenWidth - 12), Math.Min(585, Settings.ScreenHeight - 12));
        Location = new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2);
        AutoSize = false;
        Sort = true;
        Movable = true;
        PanelTitle = "Talents · " + actor.Name;
        _narrow = Size.Width < 710;
        _spec = selected ?? (actor.Specialisation == CombatSpecialisation.Unchosen ? CombatSpecialisation.First : actor.Specialisation);
        var close = Button(this, "", new Rectangle(Size.Width - 32, 6, 24, 23), Hide);
        close.Glyph = UiGlyph.Close;
        for (int i = 0; i < _specTabs.Length; i++)
        {
            var choice = (CombatSpecialisation)(i + 1);
            _specTabs[i] = Button(this, SpellCurriculum.Name(actor.Class, choice), new Rectangle(12 + i * 130, 39, 126, 23), () =>
            {
                _spec = choice;
                _showClass = false;
                _inspectedNode = null;
                RefreshGraph();
            });
        }
        _classTab = Button(this, "Class", new Rectangle(Size.Width - 164, 39, 72, 23), () => SetNarrowTab(true));
        _specTab = Button(this, "Spec", new Rectangle(Size.Width - 86, 39, 72, 23), () => SetNarrowTab(false));
        _classTab.Visible = _specTab.Visible = _narrow;
        if (_narrow && Size.Width < 565)
        {
            for (int i = 0; i < 3; i++)
            {
                _specTabs[i].Size = new Size((Size.Width - 186) / 3, 23);
                _specTabs[i].Location = new Point(12 + i * _specTabs[i].Size.Width, 39);
            }
        }

        int bottom = Size.Height;
        _detail = AddCaption(this, "", new Rectangle(15, bottom - 141, Size.Width - 30, 45), false);
        _detail.DrawFormat = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
        for (int i = 0; i < _options.Length; i++)
        {
            int option = i;
            int width = Math.Min(180, (Size.Width - 40) / 3);
            _options[i] = Button(this, "", new Rectangle(15 + i * (width + 3), bottom - 96, width, 21),
                () => Buy(_inspectedTree, _inspectedNode, (byte)option));
        }
        _rotation = AddCaption(this, "", new Rectangle(15, bottom - 72, Size.Width - 30, 18), false);
        _message = AddCaption(this, "", new Rectangle(15, bottom - 53, Size.Width - 30, 18));
        Button(this, "Reset class", new Rectangle(15, bottom - 28, 93, 21), () => ResetTree(CombatSpecialisation.Unchosen));
        Button(this, "Reset spec", new Rectangle(115, bottom - 28, 93, 21), () => ResetTree(_spec));
        Button(this, "Discard", new Rectangle(Size.Width - 222, bottom - 28, 92, 21), () => LoadDrafts());
        _apply = Button(this, "Apply changes", new Rectangle(Size.Width - 124, bottom - 28, 108, 21), Apply);
        LoadDrafts();
    }

    private void SetNarrowTab(bool showClass)
    {
        _showClass = showClass;
        _inspectedNode = null;
        RefreshGraph();
    }

    internal void Receive(S.TalentState state)
    {
        if (_pending && state.Message.Length == 0 && state.Profile.Revision == _revision) return;
        bool update = _pending || _revision != state.Profile.Revision;
        _pending = false;
        if (update) LoadDrafts();
        if (state.Message.Length != 0) _message.Text = state.Message;
        _apply.Enabled = true;
    }

    private void LoadDrafts()
    {
        _revision = Actor.Talents.Revision;
        foreach (var spec in Enum.GetValues<CombatSpecialisation>()) _drafts[spec] = Actor.Talents.Picks(spec).ToList();
        _inspectedNode = null;
        _message.Text = "Left click to learn or choose. Right click to refund.";
        RefreshGraph();
    }

    private void RefreshGraph()
    {
        _graph?.Dispose();
        _graph = new MirControl { Parent = this, Location = new Point(10, 66), Size = new Size(Size.Width - 20, Size.Height - 212) };
        int width = _narrow ? _graph.Size.Width : (_graph.Size.Width - 10) / 2;
        if (!_narrow || _showClass) DrawTree(TalentCatalog.Tree(Actor.Class), 0, width);
        if (!_narrow || !_showClass) DrawTree(TalentCatalog.Tree(Actor.Class, _spec), _narrow ? 0 : width + 10, width);
        for (int i = 0; i < _specTabs.Length; i++) _specTabs[i].Selected = (int)_spec == i + 1;
        _classTab.Selected = _showClass;
        _specTab.Selected = !_showClass;
        int engine = new TalentLoadout(Actor.Class, _spec, _drafts[CombatSpecialisation.Unchosen].ToArray(), _drafts[_spec].ToArray()).Engine;
        var recipe = TalentRotations.Get(Actor.Class, _spec, engine);
        _rotation.Text = recipe == null ? "" : "Rotation · " + string.Join(" → ", recipe.Steps.Select(step => step.Hint));
        _rotation.Hint = _rotation.Text;
        Describe();
        Redraw();
    }

    private void DrawTree(TalentTree tree, int x, int width)
    {
        var pane = new MirImageControl { Parent = _graph, Location = new Point(x, 0), Size = new Size(width, _graph.Size.Height),
            Skin = UiSurface.Inset, AutoSize = false };
        var connectors = new MirControl { Parent = pane, Size = pane.Size, NotControl = true };
        List<TalentPick> picks = _drafts[tree.Spec];
        string title = $"{tree.Title} · {picks.Sum(pick => pick.Rank)} / {TalentCatalog.Points(Actor.Level, tree.Shared)}";
        if (!tree.Shared && Actor.Level < 20) title += " · Unlocks at 20";
        AddCaption(pane, title, new Rectangle(11, 4, width - 22, 19), false);
        int rowPitch = Math.Min(38, (pane.Size.Height - 44) / tree.LastRow);
        int nodeSize = Math.Clamp(rowPitch - 5, 19, 31);
        int columnWidth = (width - 20) / 3;
        for (int column = 0; column < 3; column++)
            AddCaption(pane, tree.Columns[column], new Rectangle(10 + column * columnWidth, 23, columnWidth, 17))
                .DrawFormat |= TextFormatFlags.HorizontalCenter;

        var positions = new Dictionary<ushort, Point>();
        foreach (TalentNode node in tree.Nodes)
        {
            Point centre = new(10 + node.Column * columnWidth + columnWidth / 2, 43 + (node.Row - 1) * rowPitch + nodeSize / 2);
            positions.Add(node.Id, centre);
            TalentPick pick = picks.Find(item => item.Id == node.Id);
            TalentOption option = node.Options[pick.Option];
            bool active = pick.Rank > 0;
            bool canBuy = active || CanBuy(tree, node, 0, out _);
            bool engine = !tree.Shared && node.Row == 4;
            string symbol = node.Options.Length > 1 ? "◇" : engine ? "↻" : node.Row == tree.LastRow ? "✦" : "○";
            var button = Button(pane, symbol, new Rectangle(centre.X - nodeSize / 2, centre.Y - nodeSize / 2, nodeSize, nodeSize),
                () => { Inspect(tree, node); if (node.Options.Length == 1) Buy(tree, node, 0); });
            button.Hint = $"{node.Name} · {pick.Rank}/{node.MaxRank}\n{node.Description}";
            button.Selected = active;
            button.ForeColour = active ? Color.Gold : canBuy ? Color.Wheat : Color.DimGray;
            if (option.Grant != Spell.None && SpecialisationAbilities.TryGet(option.Grant, out var ability))
            {
                button.AfterDraw += (_, _) =>
                {
                    Rectangle icon = button.DisplayRectangle;
                    icon.Inflate(-4, -4);
                    Libraries.MagIcon.Draw(ability.Icon * 2, icon.Location, icon.Size, canBuy ? Color.White : Color.Gray);
                };
            }
            button.MouseUp += (_, e) => { if (e.Button == MouseButtons.Right) Refund(tree, node); };
            var rank = AddCaption(pane, $"{pick.Rank}/{node.MaxRank}", new Rectangle(centre.X + nodeSize / 2 + 2, centre.Y - 7, 30, 14));
            rank.ForeColour = active ? Color.Gold : Color.Gray;
        }
        connectors.BeforeDraw += (_, _) =>
        {
            foreach (TalentNode node in tree.Nodes.Where(node => node.Row > 1))
            {
                DrawEdge(tree.At(node.Row - 1, node.Column), node);
                if (node.Row == 3 && node.Column == 1)
                {
                    DrawEdge(tree.At(2, 0), node);
                    DrawEdge(tree.At(2, 2), node);
                }
                if (node.Column != 1 && (tree.Shared ? node.Row == 5 : node.Row == 6))
                    DrawEdge(tree.At(node.Row - 1, 1), node);
                if (!tree.Shared && node.Row == 5 && tree.Nodes.FirstOrDefault(n => n.Row == 4 && picks.Any(p => p.Id == n.Id)) is { } engine &&
                    engine.Column != node.Column) DrawEdge(engine, node);
            }

            void DrawEdge(TalentNode from, TalentNode to)
            {
                Point a = positions[from.Id], b = positions[to.Id];
                a.Offset(pane.DisplayLocation);
                b.Offset(pane.DisplayLocation);
                bool bought = picks.Any(p => p.Id == from.Id && p.Rank == from.MaxRank) && picks.Any(p => p.Id == to.Id);
                DXManager.DrawLine(a, b, bought ? Color.FromArgb(184, 151, 76) : Color.FromArgb(81, 74, 57));
            }
        };
    }

    private void Inspect(TalentTree tree, TalentNode node)
    {
        _inspectedTree = tree;
        _inspectedNode = node;
        Describe();
    }

    private void Describe()
    {
        _detail.Text = _inspectedNode == null ? "Choose a node to inspect its talent or spell. Preview any spec using the tabs." :
            _inspectedNode.Name + " · " + _inspectedNode.Description;
        for (int i = 0; i < _options.Length; i++)
        {
            bool visible = _inspectedNode != null && _inspectedNode.Options.Length > 1 && i < _inspectedNode.Options.Length;
            _options[i].Visible = visible;
            if (!visible) continue;
            TalentOption option = _inspectedNode.Options[i];
            _options[i].Text = option.Name;
            _options[i].Hint = option.Description;
            _options[i].Selected = _drafts[_inspectedTree.Spec].Any(pick => pick.Id == _inspectedNode.Id && pick.Option == i);
        }
    }

    private bool CanBuy(TalentTree tree, TalentNode node, byte option, out string reason)
    {
        var picks = _drafts[tree.Spec].Where(pick => pick.Id != node.Id).ToList();
        TalentPick current = _drafts[tree.Spec].Find(pick => pick.Id == node.Id);
        picks.Add(new TalentPick(node.Id, (byte)Math.Min(node.MaxRank, current.Rank + 1), option));
        return TalentCatalog.Validate(tree, Actor.Level, picks, out reason);
    }

    private void Buy(TalentTree tree, TalentNode node, byte option)
    {
        if (_pending || node == null) return;
        if (!CanBuy(tree, node, option, out string reason))
        {
            _message.Text = reason;
            return;
        }
        var picks = _drafts[tree.Spec];
        TalentPick current = picks.Find(pick => pick.Id == node.Id);
        picks.RemoveAll(pick => pick.Id == node.Id);
        picks.Add(new TalentPick(node.Id, (byte)Math.Min(node.MaxRank, current.Rank + 1), option));
        _message.Text = "Draft updated. Apply to use these talents.";
        RefreshGraph();
    }

    private void Refund(TalentTree tree, TalentNode node)
    {
        if (_pending) return;
        List<TalentPick> picks = _drafts[tree.Spec];
        TalentPick current = picks.Find(pick => pick.Id == node.Id);
        if (current.Rank == 0) return;
        int before = picks.Sum(pick => pick.Rank);
        picks.RemoveAll(pick => pick.Id == node.Id);
        if (current.Rank > 1) picks.Add(current with { Rank = (byte)(current.Rank - 1) });
        var retained = new List<TalentPick>();
        foreach (TalentPick pick in picks.OrderBy(pick => pick.Id))
        {
            retained.Add(pick);
            if (!TalentCatalog.Validate(tree, Actor.Level, retained, out _)) retained.RemoveAt(retained.Count - 1);
        }
        _drafts[tree.Spec] = retained;
        _message.Text = $"Refunded {before - retained.Sum(pick => pick.Rank)} point(s), including dependent talents.";
        RefreshGraph();
    }

    private void ResetTree(CombatSpecialisation spec)
    {
        if (_pending) return;
        _drafts[spec].Clear();
        _message.Text = "Tree reset in draft. Apply to confirm.";
        RefreshGraph();
    }

    private void Apply()
    {
        if (_pending) return;
        var spec = Actor.Level >= 20 ? _spec : CombatSpecialisation.Unchosen;
        _pending = true;
        _apply.Enabled = false;
        _message.Text = "Waiting for the server…";
        Network.Enqueue(new C.TalentDraft { ObjectID = Actor.ObjectID, Revision = _revision, Spec = spec,
            ClassPicks = _drafts[CombatSpecialisation.Unchosen].ToArray(), SpecPicks = spec == CombatSpecialisation.Unchosen ? [] : _drafts[spec].ToArray() });
    }

    private static MirButton Button(MirControl parent, string text, Rectangle bounds, Action click)
    {
        var button = new MirButton { Parent = parent, Skin = UiSurface.Button, AutoSize = false, Text = text,
            Location = bounds.Location, Size = bounds.Size };
        button.Click += (_, _) => click();
        return button;
    }
}
