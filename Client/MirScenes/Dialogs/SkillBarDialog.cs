using Client.MirControls;
using Client.MirGraphics;
using Client.MirObjects;

namespace Client.MirScenes.Dialogs;

public sealed class SkillBarDialog : MirPanel
{
    public const int SlotsPerBar = 8;
    private const int Pitch = 43;
    private readonly ActionSlot[] _slots = new ActionSlot[SlotsPerBar];
    public readonly int BarIndex;
    public UserObject Actor => BarIndex == 2 ? GameScene.Hero : GameScene.User;

    public SkillBarDialog(int barIndex)
    {
        BarIndex = barIndex;
        AutoSize = false;
        Size = new Size(360, 40);
        Movable = true;
        Sort = true;
        KeyboardNavigation = false;
        Hint = "Drag the edge of the bar to move it.";
        PanelTitle = "";
        OnMoving += (_, _) =>
        {
            Settings.SkillbarLocation[BarIndex, 0] = Location.X;
            Settings.SkillbarLocation[BarIndex, 1] = Location.Y;
        };

        for (int i = 0; i < _slots.Length; i++)
            _slots[i] = new ActionSlot(this, barIndex * SlotsPerBar + i + 1)
            {
                Parent = this, Location = new Point(10 + i * Pitch, 2)
            };
    }

    public void Arrange()
    {
        int x = Settings.SkillbarLocation[BarIndex, 0], y = Settings.SkillbarLocation[BarIndex, 1];
        if (x < 0 || y < 0)
        {
            x = BarIndex == 2 ? Settings.ScreenWidth - Size.Width - 12 : (Settings.ScreenWidth - Size.Width) / 2;
            y = Settings.ScreenHeight - (BarIndex == 2 ? 198 : 134 - BarIndex * 41);
        }
        Location = new Point(Math.Clamp(x, 0, Math.Max(0, Settings.ScreenWidth - Size.Width)),
            Math.Clamp(y, 0, Math.Max(0, Settings.ScreenHeight - Size.Height)));
    }

    public void Update()
    {
        UserObject actor = Actor;
        foreach (ActionSlot slot in _slots)
            slot.Update(actor?.Magics.Find(magic =>
                magic.Key == slot.Number && actor.CanBindAction(magic.Spell)));
    }

    public void Process()
    {
        if (!Visible) return;
        foreach (ActionSlot slot in _slots)
            slot.UpdateCooldown();
    }
}

internal sealed class ActionSlot : MirControl
{
    private readonly MirLabel _key, _cooldown, _comboBadge;
    private long _lastSeconds;
    private int _lastComboStacks;
    private ClientMagic _shownSpell;
    public readonly int Number;
    public readonly SkillBarDialog Bar;
    public ClientMagic Magic { get; private set; }

    public ActionSlot(SkillBarDialog bar, int number)
    {
        Bar = bar;
        Number = number;
        Size = new Size(38, 36);
        _key = MirPanel.AddCaption(this, "", new Rectangle(2, 20, 33, 14), false);
        _key.DrawFormat |= TextFormatFlags.Right;
        _key.OutLine = true;
        _cooldown = MirPanel.AddCaption(this, "", new Rectangle(0, 3, 38, 19), false);
        _cooldown.DrawFormat |= TextFormatFlags.HorizontalCenter;
        _cooldown.OutLine = true;
        _comboBadge = MirPanel.AddCaption(this, "", new Rectangle(3, 1, 33, 16), false);
        _comboBadge.ForeColour = Color.Gold;
        _comboBadge.OutLine = true;
    }

    public void Update(ClientMagic magic)
    {
        Magic = magic;
        string key = CMain.InputKeys.GetKey(GameScene.ActionKey(Number));
        _key.Text = GameScene.CompactActionKey(key);
        string slot = $"Bar {Bar.BarIndex + 1}, slot {(Number - 1) % SkillBarDialog.SlotsPerBar + 1}";
        Hint = (magic == null ? slot + " · Drag a spell here." :
            $"{magic.Name} · {slot}\n{magic.BaseCost + magic.LevelCost * magic.Level} MP · {Functions.PrintTimeSpanFromMilliSeconds(magic.Delay)} cooldown") +
            (key.Length == 0 ? "\nUnbound" : $"\n{key}");
        if (magic != null && SpellCombos.For(magic.Spell) is { } combo)
            Hint += $"\n{combo.Rotation}";
        if (magic != null && Bar.Actor?.Rotation is { } recipe && Bar.Actor.FinisherAction(recipe.Finisher) == magic &&
            SpecialisationAbilities.TryGet(recipe.Finisher, out var finisher))
            Hint += $"\n{finisher.Name} uses this slot when ready.\n" + string.Join(" → ", recipe.Steps.Select(step => step.Hint));
        UpdateCooldown();
    }

    public void UpdateCooldown()
    {
        ClientMagic followup = Bar.Actor?.ReadyFollowup(Magic);
        ClientMagic shown = followup ?? Magic;
        int stacks = Magic == null ? 0 : Bar.Actor?.ActiveComboCount(Magic.Spell) ?? 0;
        if (stacks != _lastComboStacks || shown != _shownSpell)
        {
            _lastComboStacks = stacks;
            _shownSpell = shown;
            _comboBadge.Text = followup != null ? "PROC" : stacks == 0 ? "" : stacks.ToString();
            Redraw();
        }
        long seconds = shown == null || shown.CastTime <= 0 ? 0 :
            Math.Max(0, (shown.CastTime + shown.Delay - CMain.Time + 999) / 1000);
        if (seconds == _lastSeconds) return;
        _lastSeconds = seconds;
        _cooldown.Text = seconds > 0 ? seconds.ToString() : "";
    }

    protected internal override void DrawControl()
    {
        Rectangle bounds = DisplayRectangle;
        var scene = GameScene.Scene;
        var actor = Bar.Actor;
        bool comboReady = Magic != null && _lastSeconds == 0 && actor?.ActiveComboCount(Magic.Spell) > 0 &&
            (actor.ComboTarget == 0 || actor.ComboTarget == MapObject.TargetObjectID ||
                actor.ComboTarget == MapObject.MouseObjectID);
        ClientMagic followup = actor?.ReadyFollowup(Magic);
        bool procReady = followup != null;
        UiVisualState state = comboReady || procReady || scene.IsActionDropTarget(this) || (scene.BindingActionKeys && MouseControl == this)
            ? UiVisualState.Selected : SkinState;
        DXManager.Device.Ui.Draw(UiSurface.Slot, bounds, state);
        bounds.Inflate(-3, -3);
        if (Magic == null) return;
        ClientMagic shown = followup ?? Magic;
        Libraries.MagIcon.Draw(shown.Icon * 2, bounds.Location, bounds.Size, Color.White);
        if (_lastSeconds > 0 && shown.Delay > 0)
        {
            double remaining = (shown.CastTime + shown.Delay - CMain.Time) / (double)shown.Delay;
            bounds.Height = (int)Math.Round(bounds.Height * Math.Clamp(remaining, 0, 1));
            DXManager.Device.Ui.Fill(bounds, Color.FromArgb(175, 5, 6, 9));
        }
    }
}
