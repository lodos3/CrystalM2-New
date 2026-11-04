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
            slot.Update(actor?.Magics.Find(magic => magic.Key == slot.Number));
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
    private readonly MirLabel _key, _cooldown;
    private long _lastSeconds;
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
        UpdateCooldown();
    }

    public void UpdateCooldown()
    {
        long seconds = Magic == null || Magic.CastTime <= 0 ? 0 :
            Math.Max(0, (Magic.CastTime + Magic.Delay - CMain.Time + 999) / 1000);
        if (seconds == _lastSeconds) return;
        _lastSeconds = seconds;
        _cooldown.Text = seconds > 0 ? seconds.ToString() : "";
    }

    protected internal override void DrawControl()
    {
        Rectangle bounds = DisplayRectangle;
        var scene = GameScene.Scene;
        UiVisualState state = scene.IsActionDropTarget(this) || (scene.BindingActionKeys && MouseControl == this)
            ? UiVisualState.Selected : SkinState;
        DXManager.Device.Ui.Draw(UiSurface.Slot, bounds, state);
        bounds.Inflate(-3, -3);
        if (Magic == null) return;
        Libraries.MagIcon.Draw(Magic.Icon * 2, bounds.Location, bounds.Size, Color.White);
        if (_lastSeconds > 0 && Magic.Delay > 0)
        {
            double remaining = (Magic.CastTime + Magic.Delay - CMain.Time) / (double)Magic.Delay;
            bounds.Height = (int)Math.Round(bounds.Height * Math.Clamp(remaining, 0, 1));
            DXManager.Device.Ui.Fill(bounds, Color.FromArgb(175, 5, 6, 9));
        }
    }
}
