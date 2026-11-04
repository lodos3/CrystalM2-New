using Client.MirControls;
using Client.MirGraphics;
using Client.MirNetwork;
using Client.MirObjects;
using Client.MirScenes.Dialogs;
using C = ClientPackets;

namespace Client.MirScenes;

public sealed partial class GameScene
{
    private ClientMagic _draggedSpell;
    private UserObject _draggedActor;
    private ActionSlot _pressedActionSlot;
    private Point _actionPressPoint;
    private MouseButtons _actionPressButton;
    private bool _draggingSpell, _consumeHudClick;
    private (int Slot, Keys KeyData, bool Tilde) _bindingConflict;
    private bool _bindingKeyReleased;

    internal bool BindingActionKeys { get; private set; }

    internal static KeybindOptions ActionKey(int slot) => slot <= 16
        ? KeybindOptions.Bar1Skill1 + (slot - 1)
        : KeybindOptions.HeroSkill1 + (slot - 17);

    internal static string CompactActionKey(string name)
    {
        name = name.Replace(" + ", "-").Replace("Ctrl-", "C-").Replace("Shift-", "S-").Replace("Alt-", "A-");
        int keyStart = name.LastIndexOf('-') + 1;
        if (name.Length == keyStart + 2 && name[keyStart] == 'D' && char.IsAsciiDigit(name[keyStart + 1]))
            return name.Remove(keyStart, 1);
        return name.Replace("NumPad", "NP");
    }

    internal void RefreshActionBars()
    {
        foreach (SkillBarDialog bar in SkillBarDialogs)
            bar.Update();
    }

    private void UpdateActionBarVisibility()
    {
        foreach (SkillBarDialog bar in SkillBarDialogs)
            bar.Visible = MainDialog.Visible && Settings.SkillBar &&
                (bar.BarIndex != 2 || (Hero != null && HeroSpawnState >= HeroSpawnState.Summoned));
    }

    internal void PressSpellbookSpell(UserObject actor, ClientMagic magic)
    {
        if (Observing || BindingActionKeys || actor == null || magic == null) return;
        _draggedActor = actor;
        _draggedSpell = magic;
        _actionPressPoint = CMain.MPoint;
    }

    private void PressActionSlot(ActionSlot slot, MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right)) return;
        _pressedActionSlot = slot;
        _actionPressButton = e.Button;
        if (e.Button == MouseButtons.Left && (!Settings.ActionBarsLocked || CMain.Shift))
            PressSpellbookSpell(slot.Bar.Actor, slot.Magic);
    }

    private void MoveSpellDrag(MouseEventArgs e)
    {
        if (_draggedSpell == null || _draggingSpell || (e.Button & MouseButtons.Left) == 0) return;
        Point point = CMain.MPoint;
        if (Math.Abs(point.X - _actionPressPoint.X) < 5 && Math.Abs(point.Y - _actionPressPoint.Y) < 5) return;
        _draggingSpell = true;
        Settings.SkillBar = true;
        UpdateActionBarVisibility();
        Redraw();
    }

    private void ReleaseActionPointer(MouseEventArgs e)
    {
        if (e.Button is not (MouseButtons.Left or MouseButtons.Right)) return;
        var target = MouseControl as ActionSlot;
        if (target?.IsMouseOver(CMain.MPoint) != true) target = null;

        if (_draggingSpell && e.Button == MouseButtons.Left)
        {
            _consumeHudClick = true;
            if (target != null && IsActionDropTarget(target))
                AssignActionSpell(_draggedActor, _draggedSpell, target.Number, _pressedActionSlot != null);
        }
        else if (_pressedActionSlot != null && e.Button == _actionPressButton)
        {
            _consumeHudClick = true;
            if (target == _pressedActionSlot && !BindingActionKeys)
            {
                if (e.Button == MouseButtons.Left && target.Magic != null)
                    UseSpell(target.Number);
                else if (e.Button == MouseButtons.Right && (!Settings.ActionBarsLocked || CMain.Shift))
                    AssignActionSpell(target.Bar.Actor, target.Magic, 0, false);
            }
        }
        CancelActionPointer();
    }

    internal void CancelActionPointer()
    {
        if (_draggingSpell) Redraw();
        _draggingSpell = false;
        _draggedSpell = null;
        _draggedActor = null;
        _pressedActionSlot = null;
    }

    internal bool IsActionDropTarget(ActionSlot slot) =>
        _draggingSpell && MouseControl == slot && slot.Bar.Actor == _draggedActor && slot.Enabled;

    private void DrawSpellDrag()
    {
        if (!_draggingSpell || _draggedSpell == null) return;
        int x = Math.Clamp(CMain.MPoint.X + 9, 0, Math.Max(0, Settings.ScreenWidth - 35));
        int y = Math.Clamp(CMain.MPoint.Y + 9, 0, Math.Max(0, Settings.ScreenHeight - 35));
        DXManager.Device.Ui.Draw(UiSurface.Slot, new Rectangle(x, y, 35, 35), UiVisualState.Selected);
        Libraries.MagIcon.Draw(_draggedSpell.Icon * 2, new Point(x + 3, y + 3), new Size(29, 29), Color.White);
    }

    internal void AssignActionSpell(UserObject actor, ClientMagic magic, int slot, bool swap)
    {
        if (Observing || actor == null || magic == null || !actor.Magics.Contains(magic) || magic.Key == slot)
            return;
        bool hero = actor == Hero;
        if (hero && (HeroSpawnState != HeroSpawnState.Summoned || actor.Dead)) return;
        if (slot != 0 && (hero ? slot is < 17 or > 24 : slot is < 1 or > 16)) return;

        byte previous = magic.Key;
        ClientMagic displaced = slot == 0 ? null : actor.Magics.Find(other => other != magic && other.Key == slot);
        if (displaced != null) displaced.Key = 0;
        SendAssignment(magic, (byte)slot, previous);
        // The first packet clears the occupant on the server. The second puts it in
        // the source slot. The old slot still identifies hero spells when clearing.
        if (swap && displaced != null && previous != 0)
            SendAssignment(displaced, previous, (byte)slot);
        RefreshActionBars();
    }

    private static void SendAssignment(ClientMagic magic, byte slot, byte previous)
    {
        Network.Enqueue(new C.MagicKey { Spell = magic.Spell, Key = slot, OldKey = previous });
        magic.Key = slot;
    }

    internal void ToggleActionBinding()
    {
        BindingActionKeys = !BindingActionKeys;
        _bindingConflict = default;
        CancelActionPointer();
        if (BindingActionKeys)
        {
            Settings.SkillBar = true;
            UpdateActionBarVisibility();
            ShowBindingMessage("Hover a slot and press a key. Delete clears it. Esc finishes.");
        }
        MainDialog.UpdateActionButtons();
        Redraw();
    }

    private bool CaptureActionBinding(KeyEventArgs e)
    {
        if (!BindingActionKeys) return false;
        e.Handled = true;
        e.SuppressKeyPress = true;
        if (e.KeyCode == Keys.Escape)
        {
            ToggleActionBinding();
            return true;
        }
        if (e.KeyCode is Keys.None or Keys.ShiftKey or Keys.ControlKey or Keys.Menu or Keys.Oem8)
            return true;
        if (MouseControl is not ActionSlot slot || !slot.IsMouseOver(CMain.MPoint))
        {
            ShowBindingMessage("Hover an action slot first.");
            return true;
        }
        if (e.KeyCode is Keys.Enter or Keys.Tab or Keys.PrintScreen)
        {
            ShowBindingMessage("That key is reserved. Choose another shortcut.");
            return true;
        }

        var binds = CMain.InputKeys;
        KeyBind bind = binds.Keylist.Find(item => item.function == ActionKey(slot.Number));
        bool clearing = e.KeyCode == Keys.Delete;
        byte alt = (byte)(e.Alt ? 1 : 0), ctrl = (byte)(e.Control ? 1 : 0),
            shift = (byte)(e.Shift ? 1 : 0), tilde = (byte)(CMain.Tilde ? 1 : 0);
        var conflicts = clearing ? new List<KeyBind>() : binds.Keylist.FindAll(other => other != bind && other.Key == e.KeyCode &&
            MatchesModifier(other.RequireAlt, alt) && MatchesModifier(other.RequireCtrl, ctrl) &&
            MatchesModifier(other.RequireShift, shift) && MatchesModifier(other.RequireTilde, tilde));
        if (conflicts.Count != 0)
        {
            var attempt = (slot.Number, e.KeyData, CMain.Tilde);
            if (_bindingConflict != attempt || !_bindingKeyReleased)
            {
                if (_bindingConflict != attempt)
                    ShowBindingMessage($"Used by {conflicts[0].Description}. Release, then press again to replace.");
                _bindingConflict = attempt;
                _bindingKeyReleased = false;
                return true;
            }
            foreach (KeyBind other in conflicts)
                other.Key = Keys.None;
        }
        bind.Key = clearing ? Keys.None : e.KeyCode;
        bind.RequireAlt = alt;
        bind.RequireCtrl = ctrl;
        bind.RequireShift = shift;
        bind.RequireTilde = tilde;
        _bindingConflict = default;
        conflicts.Add(bind);
        binds.SaveBindings(conflicts);
        RefreshActionBars();
        ShowBindingMessage(clearing ? "Slot shortcut cleared." : $"{binds.GetKey(bind.function)} assigned.");
        return true;
    }

    private static bool MatchesModifier(byte requirement, byte actual) => requirement == 2 || requirement == actual;

    private void ShowBindingMessage(string text) => OutputMessage(text, OutputMessageType.Normal);
}
