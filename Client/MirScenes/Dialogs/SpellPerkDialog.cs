using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using C = ClientPackets;
using S = ServerPackets;

namespace Client.MirScenes.Dialogs;

internal sealed class SpellPerkDialog : MirPanel
{
    private const int ListSize = 9;
    private readonly MirButton[] _spells = new MirButton[ListSize];
    private readonly (MirLabel Name, MirLabel Description, MirButton Minus, MirButton Plus)[] _options = new (MirLabel, MirLabel, MirButton, MirButton)[4];
    private readonly MirLabel _points, _selection, _status, _pages;
    private readonly MirButton _apply;
    private readonly List<SpellPerkPick> _draft = [];
    private ClientMagic[] _learned = [];
    private Spell _selected;
    private int _page;
    private bool _pending;

    internal UserObject Actor { get; }

    internal SpellPerkDialog(UserObject actor)
    {
        Actor = actor;
        Size = new Size(Math.Min(690, Settings.ScreenWidth - 16), 424);
        Location = new Point((Settings.ScreenWidth - Size.Width) / 2, Math.Max(0, (Settings.ScreenHeight - Size.Height) / 2));
        AutoSize = false;
        Sort = true;
        Movable = true;
        PanelTitle = $"Spell Perks · {actor.Name}";
        Button("", Size.Width - 34, 7, 24, Hide).Glyph = UiGlyph.Close;

        _points = Caption("", 12, 41, Size.Width - 24, 20);
        _selection = Caption("", 208, 72, Size.Width - 220, 22);
        for (int i = 0; i < _spells.Length; i++)
        {
            int row = i;
            _spells[i] = Button("", 12, 72 + i * 28, 174, () =>
            {
                int index = _page * ListSize + row;
                if (index >= _learned.Length) return;
                _selected = _learned[index].Spell;
                Refresh();
            });
        }

        for (int i = 0; i < _options.Length; i++)
        {
            int option = i;
            int top = 102 + i * 61;
            var title = Caption("", 208, top, Size.Width - 298, 20);
            var details = Caption("", 208, top + 21, Size.Width - 223, 36);
            details.DrawFormat = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis;
            _options[i] = (title, details,
                Button("−", Size.Width - 72, top, 25, () => Change(option, -1)),
                Button("+", Size.Width - 41, top, 25, () => Change(option, 1)));
        }

        Button("", 12, 329, 25, () => Page(-1)).Glyph = UiGlyph.Left;
        Button("", 161, 329, 25, () => Page(1)).Glyph = UiGlyph.Right;
        _pages = Caption("", 44, 331, 110, 20);
        _status = Caption("", 12, 365, Size.Width - 24, 20);
        Button("Reset spell", 12, 390, 92, () =>
        {
            if (_pending || _draft.RemoveAll(pick => pick.Spell == _selected) == 0) return;
            _status.Text = "";
            Refresh();
        });
        Button("Discard", Size.Width - 224, 390, 98, LoadDraft);
        _apply = Button("Apply changes", Size.Width - 119, 390, 107, Apply);
        LoadDraft();
    }

    internal void Receive(S.SpellPerkState state, bool changed)
    {
        if (_pending && state.Message.Length == 0 && !changed) return;
        if (changed || _pending)
        {
            _pending = false;
            LoadDraft();
        }
        else Refresh();
        if (state.Message.Length != 0) _status.Text = state.Message;
    }

    private void LoadDraft()
    {
        if (_pending) return;
        _draft.Clear();
        _draft.AddRange(Actor.SpellPerkPicks);
        _status.Text = "Apply changes outside combat. Reset a spell to refund its points.";
        Refresh();
    }

    private void Page(int delta)
    {
        _page = Math.Clamp(_page + delta, 0, Math.Max(0, (_learned.Length - 1) / ListSize));
        Refresh();
    }

    private void Change(int index, int delta)
    {
        var options = SpellPerks.For(_selected);
        if (_pending || index >= options.Count) return;
        var option = options[index];
        int rank = Rank(_selected, option.Perk) + delta;
        if (rank < 0 || rank > option.MaxRank) return;

        var picks = _draft.Where(pick => pick.Spell != _selected || pick.Perk != option.Perk).ToList();
        if (rank > 0) picks.Add(new SpellPerkPick(_selected, option.Perk, (byte)rank));
        else picks.RemoveAll(pick => pick.Spell == _selected && options.Any(child => child.Perk == pick.Perk && child.Requires == option.Perk));
        if (!SpellPerks.Validate(Actor.Class, Actor.Level, picks, Knows, out string reason))
        {
            _status.Text = reason;
            return;
        }

        _draft.Clear();
        _draft.AddRange(picks);
        _status.Text = "";
        Refresh();
    }

    private int Rank(Spell spell, SpellPerk perk) => _draft.Find(pick => pick.Spell == spell && pick.Perk == perk).Rank;
    private bool Knows(Spell spell) => _learned.Any(magic => magic.Spell == spell);

    private void Refresh()
    {
        _learned = Actor.Magics.Where(magic => !magic.IsTempSpell && Actor.CanUseBuildSpell(magic.Spell) &&
            SpellPerks.BelongsTo(Actor.Class, magic.Spell) && SpellPerks.For(magic.Spell).Count > 0)
            .OrderBy(magic => magic.Level1).ThenBy(magic => magic.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        _page = Math.Min(_page, Math.Max(0, (_learned.Length - 1) / ListSize));
        if (!Knows(_selected)) _selected = _learned.FirstOrDefault()?.Spell ?? Spell.None;
        int total = SpellPerks.Points(Actor.Level);
        int available = total - _draft.Sum(pick => (int)pick.Rank);
        _points.Text = $"Available points   {available} / {total}     ·     Earn one at each level from 10";
        _pages.Text = $"Page {_page + 1} / {Math.Max(1, (_learned.Length + ListSize - 1) / ListSize)}";
        _selection.Text = _learned.FirstOrDefault(magic => magic.Spell == _selected)?.Name ?? "No spells learned yet";

        for (int i = 0; i < _spells.Length; i++)
        {
            var button = _spells[i];
            int index = i + _page * ListSize;
            button.Visible = index < _learned.Length;
            if (!button.Visible) continue;
            var magic = _learned[index];
            int spent = _draft.Where(pick => pick.Spell == magic.Spell).Sum(pick => pick.Rank);
            button.Text = spent == 0 ? magic.Name : $"{magic.Name}  ·  {spent}";
            button.Selected = _selected == magic.Spell;
        }

        var options = SpellPerks.For(_selected);
        for (int i = 0; i < _options.Length; i++)
        {
            var row = _options[i];
            row.Name.Visible = row.Description.Visible = row.Minus.Visible = row.Plus.Visible = i < options.Count;
            if (!row.Name.Visible) continue;
            var option = options[i];
            int rank = Rank(_selected, option.Perk);
            row.Name.Text = $"{option.Name}  ·  {rank}/{option.MaxRank}";
            row.Description.Text = option.Description;
            if (option.Requires != 0)
                row.Description.Text += $" Requires {options.First(item => item.Perk == option.Requires).Name}.";
            if (option.ExclusiveGroup != 0)
                row.Description.Text += " Choose one dash style.";
            row.Description.Hint = row.Description.Text;
            row.Minus.Enabled = !_pending && rank > 0;
            row.Plus.Enabled = !_pending && available > 0 && rank < option.MaxRank &&
                (option.Requires == 0 || Rank(_selected, option.Requires) > 0) &&
                (option.ExclusiveGroup == 0 || !options.Any(item => item.Perk != option.Perk &&
                    item.ExclusiveGroup == option.ExclusiveGroup && Rank(_selected, item.Perk) > 0));
        }
        _apply.Enabled = !_pending && !_draft.ToHashSet().SetEquals(Actor.SpellPerkPicks);
        Redraw();
    }

    private void Apply()
    {
        if (_pending || GameScene.Observing) return;
        if (!SpellPerks.Validate(Actor.Class, Actor.Level, _draft, Knows, out string reason))
        {
            _status.Text = reason;
            return;
        }
        _pending = true;
        _status.Text = "Applying spell perks…";
        Network.Enqueue(new C.SpellPerkDraft { ObjectID = Actor.ObjectID, Revision = Actor.SpellPerkRevision, Apply = true, Picks = _draft.ToArray() });
        Refresh();
    }

    private MirLabel Caption(string text, int x, int y, int width, int height) =>
        AddCaption(this, text, new Rectangle(x, y, width, height), false);

    private MirButton Button(string text, int x, int y, int width, Action click)
    {
        var button = new MirButton { Text = text, Parent = this, Skin = UiSurface.Button, AutoSize = false,
            Location = new Point(x, y), Size = new Size(width, 24) };
        button.Click += (_, _) => click();
        return button;
    }
}
