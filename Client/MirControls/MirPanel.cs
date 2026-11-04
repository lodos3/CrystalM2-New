using System.Text.RegularExpressions;
using Client.MirGraphics;

namespace Client.MirControls;

// Generated window chrome with the existing gameplay controls and handlers.
// Installed LIB header dimensions preserve authored placement during migration;
// explicit Size + AutoSize=false makes new layouts entirely asset-independent.
public class MirPanel : MirImageControl
{
    private MirLabel _caption;
    private MirButton _focused;
    private static MirPanel _keyboardOwner;
    public string PanelTitle { get; set; }
    public bool KeyboardNavigation { get; set; } = true;
    public override Size Size
    {
        get
        {
            Size size = base.Size;
            return size.IsEmpty ? new Size(320, 240) : size;
        }
        set => base.Size = value;
    }

    public MirPanel()
    {
        Skin = UiSurface.Window;
        PanelTitle = Regex.Replace(GetType().Name.Replace("Dialog", "").Replace("Panel", ""),
            "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
        if (GetType().Name is "MainDialog" or "ChatDialog" or "SkillBarDialog" or
            "MiniMapDialog" or "BeltDialog" or "HeroBeltDialog" or "BuffDialog" or "PoisonBuffDialog")
        {
            PanelTitle = "";
            KeyboardNavigation = false;
        }
    }

    protected internal override void DrawControl()
    {
        UpdateCaption();
        base.DrawControl();
        if (_caption?.Visible == true && DrawImage)
            DXManager.Device.Ui.Draw(UiSurface.Header,
                new Rectangle(DisplayLocation.X + 4, DisplayLocation.Y + 4, Size.Width - 8, 28));
    }

    private void UpdateCaption()
    {
        if (string.IsNullOrEmpty(PanelTitle) && _caption == null) return;
        if (_caption == null)
            _caption = new MirLabel
            {
                Parent = this, NotControl = true, AutoSize = false, OutLine = false,
                Font = new Font(FontFamily.GenericSerif, 9f, FontStyle.Bold),
                ForeColour = DarkFantasyPalette.Text, DrawFormat = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
            };
        _caption.Text = PanelTitle;
        _caption.Location = new Point(18, 6);
        _caption.Size = new Size(Math.Max(0, Size.Width - 58), 22);
        Rectangle header = new(_caption.Location, _caption.Size);
        _caption.Visible = DrawImage && !string.IsNullOrEmpty(PanelTitle) && !Controls.Any(c => c != _caption && c.Visible &&
            !(c is MirButton button && button.Glyph == UiGlyph.Close) &&
            new Rectangle(c.Location, c.Size).IntersectsWith(header));
    }

    public static MirLabel AddCaption(MirControl parent, string text, Rectangle bounds, bool muted = true) => new()
    {
        Parent = parent, Text = text, Location = bounds.Location, Size = bounds.Size,
        AutoSize = false, NotControl = true, OutLine = false,
        ForeColour = muted ? DarkFantasyPalette.MutedText : DarkFantasyPalette.Text,
        DrawFormat = TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
    };

    public override void OnKeyDown(KeyEventArgs e)
    {
        if (!Visible || !Enabled) return;
        if (KeyboardNavigation && (Movable || Modal) && e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
        {
            List<MirButton> candidates = Focusable(this).ToList();
            if (candidates.Count > 0)
            {
                int current = candidates.IndexOf(_focused);
                int next = e.Shift ? (current <= 0 ? candidates.Count - 1 : current - 1) : (current + 1) % candidates.Count;
                ClearKeyboardFocus();
                _focused = candidates[next];
                _focused.KeyboardFocused = true;
                _keyboardOwner = this;
                Redraw();
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }
        }
        if (_keyboardOwner == this && _focused != null && !_focused.IsDisposed && _focused.Visible && _focused.Enabled &&
            e.KeyCode is Keys.Enter or Keys.Space)
        {
            _focused.InvokeMouseClick(EventArgs.Empty);
            e.Handled = true;
            e.SuppressKeyPress = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnVisibleChanged()
    {
        if (!Visible && _keyboardOwner == this) ClearKeyboardFocus();
        base.OnVisibleChanged();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _keyboardOwner == this) ClearKeyboardFocus();
        base.Dispose(disposing);
    }

    internal static void ClearKeyboardFocus()
    {
        if (_keyboardOwner == null) return;
        MirPanel owner = _keyboardOwner;
        _keyboardOwner = null;
        if (owner._focused != null) owner._focused.KeyboardFocused = false;
        owner._focused = null;
        owner.Redraw();
    }

    private static IEnumerable<MirButton> Focusable(MirControl parent)
    {
        foreach (MirControl control in parent.Controls)
        {
            if (!control.Visible || !control.Enabled || control.IsDisposed) continue;
            if (control is MirButton button && (button is MirCheckBox || button.Glyph != UiGlyph.None || !string.IsNullOrEmpty(button.Text)))
                yield return button;
            else
                foreach (MirButton child in Focusable(control)) yield return child;
        }
    }
}
