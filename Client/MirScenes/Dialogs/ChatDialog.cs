using Client.MirControls;
using Client.MirNetwork;
using C = ClientPackets;

namespace Client.MirScenes.Dialogs;

public sealed class ChatDialog : MirPanel
{
    private const int HistoryLimit = 512, HeaderHeight = 24, ComposerHeight = 28;
    private static readonly (string Name, string Prefix)[] SendChannels =
    [
        ("Say", ""), ("Yell", "!"), ("Whisper", "/"), ("Party", "!!"),
        ("Guild", "!~"), ("Partner", ":)"), ("Mentor", "!#")
    ];

    private sealed class Tab(ChatDialog owner, ChatChannel channel)
    {
        public readonly ChatChannel Channel = channel;
        public readonly ChatLogView View = new(channel, owner.Whisper) { Visible = false };
        public MirButton Button;
        public MirPanel Window;
        public string Name => Channel.ToString();
    }

    private enum Gesture { None, Tab, Move, Resize }
    private readonly GameScene _scene;
    private readonly Queue<ChatMessage> _history = new(HistoryLimit);
    private readonly Tab[] _tabs;
    private readonly MirButton _channel, _settings, _trade, _report, _resize, _prompt;
    private readonly MirPanel _channelMenu;
    private Tab _active, _pointerTab;
    private MirPanel _pointerWindow;
    private Gesture _gesture;
    private MouseButtons _pointerButton;
    private Point _pointerOrigin;
    private Rectangle _initialBounds;
    private bool _moved;
    private long _sequence;
    private string _lastWhisper = "";
    private int _sendChannel;

    public readonly MirTextBox ChatTextBox;
    public readonly List<ChatItem> LinkedItems = new();
    internal bool HasPointer => _gesture != Gesture.None;

    public ChatDialog(GameScene scene)
    {
        _scene = scene;
        AutoSize = false;
        Sort = true;
        PanelTitle = "";
        KeyboardNavigation = false;
        Opacity = Settings.TransparentChat ? .67f : .94f;
        Parent = scene;

        ChatTextBox = new MirTextBox
        {
            BackColour = DarkFantasyPalette.Input, ForeColour = DarkFantasyPalette.Text, Parent = this,
            Size = new Size(180, 19), MaxLength = Globals.MaxChatLength, CanLoseFocus = true, Visible = false
        };
        ChatTextBox.TextBox.KeyDown += InputKeyDown;
        ChatTextBox.TextBox.KeyUp += InputKeyUp;
        ChatTextBox.TextBox.KeyPress += (sender, e) =>
        {
            if (e.KeyChar is not ((char)Keys.Enter or (char)Keys.Escape)) return;
            e.Handled = true;
            InputKeyDown(sender, new KeyEventArgs((Keys)e.KeyChar));
        };
        KeyPress += (_, e) => ChatKeyPress(e);
        KeyDown += (_, e) => ChatScrollKey(e);

        _tabs = Enum.GetValues<ChatChannel>().Select(channel => new Tab(this, channel)).ToArray();
        foreach (Tab tab in _tabs)
        {
            tab.View.Parent = this;
            tab.Button = Button(this, tab.Name, UiGlyph.None,
                tab.Channel == ChatChannel.General ? "General chat. Drag the tab to move the chat dock." :
                "Drag this tab out or right-click to detach it.");
            tab.Button.MouseDown += (_, e) => BeginPointer(e, Gesture.Tab, this, tab);
        }

        _channel = Button(this, "Say", UiGlyph.None, "Choose the outgoing chat channel.");
        _settings = Button(this, "", UiGlyph.Gear, "Chat settings");
        _settings.Click += (_, _) => _scene.ChatOptionDialog.Visible = !_scene.ChatOptionDialog.Visible;
        _trade = Button(this, "", UiGlyph.Coin, "Request trade");
        _trade.Click += (_, _) => Network.Enqueue(new C.TradeRequest());
        _report = Button(this, "", UiGlyph.Help, "Report a player");
        _report.Click += (_, _) => _scene.ReportDialog.Visible = !_scene.ReportDialog.Visible;
        _resize = ResizeButton(this);
        _prompt = Button(this, "Enter to chat", UiGlyph.None, "Click or press Enter to start typing.");
        _prompt.Click += (_, _) => StartInput(CurrentPrefix());
        ChatTextBox.VisibleChanged += (_, _) => _prompt.Visible = !ChatTextBox.Visible;

        _channelMenu = new MirPanel
        {
            Parent = scene, PanelTitle = "", AutoSize = false, Size = new Size(112, SendChannels.Length * 23 + 8),
            Visible = false, Sort = true, KeyboardNavigation = false
        };
        for (int i = 0; i < SendChannels.Length; i++)
        {
            int index = i;
            MirButton channel = Button(_channelMenu, SendChannels[i].Name, UiGlyph.None, "");
            channel.Location = new Point(5, 4 + i * 23);
            channel.Size = new Size(102, 22);
            channel.Click += (_, _) =>
            {
                _sendChannel = index;
                _channel.Text = SendChannels[index].Name;
                _channelMenu.Hide();
                StartInput(CurrentPrefix());
            };
        }
        _channel.Click += (_, _) =>
        {
            _channelMenu.Location = ClampPosition(new Point(DisplayLocation.X, DisplayLocation.Y - _channelMenu.Size.Height - 3), _channelMenu.Size);
            _channelMenu.Visible = !_channelMenu.Visible;
        };

        Rectangle saved = Settings.ChatWindows[(int)ChatChannel.General];
        Size size = saved.IsEmpty ? new Size(396, 139) : saved.Size;
        Size = ClampSize(size, this);
        Location = saved.IsEmpty ? new Point(10, Math.Max(0, Settings.ScreenHeight - Size.Height - 166)) :
            ClampPosition(saved.Location, Size);
        foreach (Tab tab in _tabs.Skip(1))
        {
            Rectangle layout = Settings.ChatWindows[(int)tab.Channel];
            if (!layout.IsEmpty) Detach(tab, layout.Location, layout.Size);
        }
        _active = _tabs[0];
        ArrangeDock();
        Select(_tabs[Math.Clamp(Settings.ActiveChatTab, 0, _tabs.Length - 1)]);
    }

    private static MirButton Button(MirControl parent, string text, UiGlyph glyph, string hint) => new()
    {
        Parent = parent, AutoSize = false, Size = new Size(22, 22), Skin = UiSurface.Button,
        Text = text, Glyph = glyph, Hint = hint
    };

    private MirButton ResizeButton(MirPanel window)
    {
        var button = Button(window, "", UiGlyph.Grip, "Drag to resize");
        button.Size = new Size(17, 18);
        button.MouseDown += (_, e) => BeginPointer(e, Gesture.Resize, window, null);
        return button;
    }

    private Rectangle TabsBounds => new(DisplayLocation.X, DisplayLocation.Y, Size.Width, HeaderHeight);

    private void ArrangeDock()
    {
        if (_active == null) return;
        int left = 5;
        int width = Math.Min(64, (Size.Width - 10) / _tabs.Count(tab => tab.Window == null));
        foreach (Tab tab in _tabs)
        {
            tab.Button.Visible = tab.Window == null;
            if (tab.Window != null) continue;
            tab.Button.Location = new Point(left, 4);
            tab.Button.Size = new Size(width - 1, HeaderHeight - 4);
            left += width;
        }

        _active.View.Arrange(new Rectangle(7, HeaderHeight + 3, Size.Width - 13, Size.Height - HeaderHeight - ComposerHeight - 5));
        int y = Size.Height - ComposerHeight;
        _channel.Location = new Point(5, y + 2);
        _channel.Size = new Size(63, 22);
        ChatTextBox.Location = new Point(70, y + 4);
        ChatTextBox.Size = new Size(Size.Width - 165, 18);
        _prompt.Location = new Point(70, y + 2);
        _prompt.Size = new Size(ChatTextBox.Size.Width, 22);
        _settings.Location = new Point(Size.Width - 92, y + 2);
        _trade.Location = new Point(Size.Width - 69, y + 2);
        _report.Location = new Point(Size.Width - 46, y + 2);
        _resize.Location = new Point(Size.Width - 21, Size.Height - 22);
    }

    private void Select(Tab tab)
    {
        if (tab.Window != null) tab = _tabs[0];
        if (_active != null && _active.Window == null)
            _active.View.Visible = false;
        _active = tab;
        Settings.ActiveChatTab = (int)tab.Channel;
        foreach (Tab entry in _tabs) entry.Button.Selected = entry == tab;
        tab.View.Visible = true;
        ArrangeDock();
        tab.View.Rebuild(_history);
        Redraw();
    }

    public void ReceiveChat(string text, ChatType type)
    {
        if (string.IsNullOrEmpty(text)) return;
        var message = new ChatMessage(++_sequence, text, type);
        if (_history.Count == HistoryLimit) _history.Dequeue();
        _history.Enqueue(message);
        long oldest = _history.Peek().Sequence;
        _active?.View.Append(message, oldest);
        foreach (Tab tab in _tabs)
            if (tab.Window != null) tab.View.Append(message, oldest);
    }

    public void Update()
    {
        _active.View.Rebuild(_history);
        foreach (Tab tab in _tabs)
            if (tab.Window != null) tab.View.Rebuild(_history);
    }

    private void Detach(Tab tab, Point point, Size size)
    {
        if (tab.Channel == ChatChannel.General || tab.Window != null) return;
        var window = new MirPanel
        {
            Parent = _scene, PanelTitle = "", KeyboardNavigation = false, AutoSize = false,
            Sort = true, Opacity = Opacity, Visible = Visible
        };
        tab.Window = window;
        window.Size = ClampSize(size, window);
        window.Location = ClampPosition(point, window.Size);
        tab.View.Parent = window;
        tab.View.Visible = true;
        var header = Button(window, tab.Name, UiGlyph.None, "Drag to move. Drop on the main chat tabs to dock.");
        header.MouseDown += (_, e) => BeginPointer(e, Gesture.Move, window, tab);
        var dock = Button(window, "", UiGlyph.Close, "Dock this tab");
        dock.Click += (_, _) => Dock(tab);
        MirButton grip = ResizeButton(window);
        window.SizeChanged += (_, _) => ArrangeDetached();
        ArrangeDetached();
        tab.View.Rebuild(_history);
        tab.Button.Visible = false;
        if (_active == tab) Select(_tabs[0]);
        ArrangeDock();
        SaveWindow(window);
        window.TrySort();

        void ArrangeDetached()
        {
            header.Location = new Point(5, 4);
            header.Size = new Size(window.Size.Width - 31, HeaderHeight - 4);
            dock.Location = new Point(window.Size.Width - 25, 4);
            grip.Location = new Point(window.Size.Width - 21, window.Size.Height - 22);
            tab.View.Arrange(new Rectangle(7, HeaderHeight + 3, window.Size.Width - 13, window.Size.Height - HeaderHeight - 28));
        }
    }

    private void Dock(Tab tab)
    {
        MirPanel window = tab.Window;
        if (window == null) return;
        tab.View.Parent = this;
        tab.Window = null;
        window.Dispose();
        Settings.ChatWindows[(int)tab.Channel] = Rectangle.Empty;
        Select(tab);
    }

    private void BeginPointer(MouseEventArgs e, Gesture gesture, MirPanel window, Tab tab)
    {
        if (e.Button != MouseButtons.Left && !(gesture == Gesture.Tab && e.Button == MouseButtons.Right)) return;
        _gesture = gesture;
        _pointerWindow = window;
        _pointerTab = tab;
        _pointerButton = e.Button;
        _pointerOrigin = CMain.MPoint;
        _initialBounds = new Rectangle(window.Location, window.Size);
        _moved = false;
    }

    internal void MovePointer(MouseEventArgs e)
    {
        if (!HasPointer || (_pointerButton & e.Button) == 0 || _pointerButton != MouseButtons.Left) return;
        Point delta = CMain.MPoint.Subtract(_pointerOrigin);
        if (!_moved && Math.Abs(delta.X) < 5 && Math.Abs(delta.Y) < 5) return;
        _moved = true;
        if (_gesture == Gesture.Tab)
        {
            if (_pointerTab.Channel != ChatChannel.General)
            {
                if (TabsBounds.Contains(CMain.MPoint)) return;
                Detach(_pointerTab, CMain.MPoint.Add(-38, -10), new Size(340, 134));
                _pointerWindow = _pointerTab.Window;
                _initialBounds = new Rectangle(_pointerWindow.Location, _pointerWindow.Size);
                _pointerOrigin = CMain.MPoint;
                delta = Point.Empty;
            }
            _gesture = Gesture.Move;
        }
        if (_gesture == Gesture.Resize)
        {
            _pointerWindow.Size = ClampSize(new Size(_initialBounds.Width + delta.X, _initialBounds.Height + delta.Y), _pointerWindow);
            _pointerWindow.Location = ClampPosition(_pointerWindow.Location, _pointerWindow.Size);
            if (_pointerWindow == this) ArrangeDock();
        }
        else
            _pointerWindow.Location = ClampPosition(_initialBounds.Location.Add(delta), _pointerWindow.Size);
    }

    internal bool ReleasePointer(MouseEventArgs e)
    {
        if (!HasPointer || _pointerButton != e.Button) return false;
        Gesture gesture = _gesture;
        Tab tab = _pointerTab;
        MirPanel window = _pointerWindow;
        CancelPointer();
        if (gesture == Gesture.Tab && !_moved && tab.Button.IsMouseOver(CMain.MPoint))
        {
            if (e.Button == MouseButtons.Right)
                Detach(tab, new Point(Location.X + 28, Location.Y - 142), new Size(340, 134));
            else
                Select(tab);
        }
        else if (gesture == Gesture.Move && _moved && tab?.Window != null && TabsBounds.Contains(CMain.MPoint))
            Dock(tab);
        else if (_moved)
            SaveWindow(window);
        return true;
    }

    internal void CancelPointer()
    {
        if (_gesture == Gesture.Resize && _moved && _pointerWindow is { IsDisposed: false })
        {
            Tab resized = _pointerWindow == this ? _active : _tabs.First(tab => tab.Window == _pointerWindow);
            resized.View.Rebuild(_history);
        }
        _gesture = Gesture.None;
        _pointerTab = null;
        _pointerWindow = null;
    }

    internal void CloseChannelMenuOutside()
    {
        if (_channelMenu.Visible && !_channelMenu.IsMouseOver(CMain.MPoint) && !_channel.IsMouseOver(CMain.MPoint))
            _channelMenu.Hide();
    }

    private void SaveWindow(MirPanel window)
    {
        int index = window == this ? 0 : (int)_tabs.First(tab => tab.Window == window).Channel;
        Settings.ChatWindows[index] = new Rectangle(window.Location, window.Size);
    }

    private Size ClampSize(Size size, MirPanel window)
    {
        int maxWidth = Math.Max(1, Math.Min(820, Settings.ScreenWidth));
        int maxHeight = Math.Max(1, Math.Min(520, Settings.ScreenHeight));
        return new Size(Math.Clamp(size.Width, Math.Min(window == this ? 370 : 220, maxWidth), maxWidth),
            Math.Clamp(size.Height, Math.Min(92, maxHeight), maxHeight));
    }

    private static Point ClampPosition(Point point, Size size) => new(
        Math.Clamp(point.X, 0, Math.Max(0, Settings.ScreenWidth - size.Width)),
        Math.Clamp(point.Y, 0, Math.Max(0, Settings.ScreenHeight - size.Height)));

    protected override void OnVisibleChanged()
    {
        base.OnVisibleChanged();
        if (_tabs == null) return;
        foreach (Tab tab in _tabs)
            if (tab.Window != null) tab.Window.Visible = Visible;
        if (!Visible)
        {
            CancelPointer();
            _channelMenu?.Hide();
        }
        else if (_active != null)
            Update();
    }

    private string CurrentPrefix() => _sendChannel == 2 && _lastWhisper.Length > 0
        ? "/" + _lastWhisper + " " : SendChannels[_sendChannel].Prefix;

    private void StartInput(string text, bool keepLinks = false)
    {
        if (!Visible) return;
        if (!keepLinks && text != ChatTextBox.Text) LinkedItems.Clear();
        ChatTextBox.Visible = true;
        ChatTextBox.Text = text;
        ChatTextBox.SetFocus();
        ChatTextBox.TextBox.SelectionStart = ChatTextBox.Text.Length;
    }

    private void Whisper(string name) => StartInput("/" + name + " ");

    public void SetChatText(string newText)
    {
        string text = ChatTextBox.Text + newText;
        if (text.Length <= Globals.MaxChatLength) StartInput(text, true);
    }

    private void ChatKeyPress(KeyPressEventArgs e)
    {
        if (!Visible || e.Handled || _scene.BindingActionKeys) return;
        switch (e.KeyChar)
        {
            case (char)Keys.Enter:
            case ' ':
                StartInput(ChatTextBox.Visible ? ChatTextBox.Text : CurrentPrefix());
                break;
            case '@':
            case '!':
                StartInput(e.KeyChar.ToString());
                break;
            case '/':
                StartInput(_lastWhisper.Length == 0 ? "/" : "/" + _lastWhisper + " ");
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private void ChatScrollKey(KeyEventArgs e)
    {
        if (!Visible || e.Handled || e.Alt || e.Control || e.Shift) return;
        ChatLogView view = _active.View;
        foreach (Tab tab in _tabs)
            if (tab.Window?.IsMouseOver(CMain.MPoint) == true) view = tab.View;
        if (view == _active.View && !DisplayRectangle.Contains(CMain.MPoint)) return;
        switch (e.KeyCode)
        {
            case Keys.Home: view.ScrollTo(0); break;
            case Keys.End: view.ScrollTo(int.MaxValue); break;
            case Keys.PageUp: view.Scroll(-6); break;
            case Keys.PageDown: view.Scroll(6); break;
            default: return;
        }
        e.Handled = true;
    }

    private void InputKeyDown(object sender, KeyEventArgs e)
    {
        CMain.Shift = e.Shift;
        CMain.Alt = e.Alt;
        CMain.Ctrl = e.Control;
        if (e.KeyCode is Keys.Enter or Keys.Escape)
        {
            e.Handled = e.SuppressKeyPress = true;
            string text = ChatTextBox.Text.Trim();
            if (e.KeyCode == Keys.Enter && text.Length > 0)
            {
                if (text.Equals("@LEVELEFFECT", StringComparison.OrdinalIgnoreCase)) Settings.LevelEffect = !Settings.LevelEffect;
                if (text.Equals("@TARGETDEAD", StringComparison.OrdinalIgnoreCase)) Settings.TargetDead = !Settings.TargetDead;
                Network.Enqueue(new C.Chat { Message = text, LinkedItems = new List<ChatItem>(LinkedItems) });
                int space = text.IndexOf(' ');
                if (text.StartsWith('/') && space > 1) _lastWhisper = text[1..space];
            }
            ChatTextBox.Visible = false;
            ChatTextBox.Text = "";
            LinkedItems.Clear();
        }
        else if (e.KeyCode is >= Keys.F1 and <= Keys.F12 or Keys.Tab)
            CMain.CMain_KeyDown(sender, e);
    }

    private static void InputKeyUp(object sender, KeyEventArgs e)
    {
        CMain.Shift = e.Shift;
        CMain.Alt = e.Alt;
        CMain.Ctrl = e.Control;
        if (e.KeyCode is >= Keys.F1 and <= Keys.F12 or Keys.Tab) CMain.CMain_KeyUp(sender, e);
    }
}
