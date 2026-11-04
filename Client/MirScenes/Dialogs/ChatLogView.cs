using System.Text;
using System.Text.RegularExpressions;
using Client.MirControls;
using Client.MirGraphics;

namespace Client.MirScenes.Dialogs;

internal enum ChatChannel { General, System, Party, Guild, Whispers, Shouts }

internal sealed class ChatMessage
{
    public readonly long Sequence;
    public readonly ChatType Type;
    public readonly string Text, Sender;
    public readonly ChatLink[] Links;
    public readonly Color Colour;

    public ChatMessage(long sequence, string raw, ChatType type)
    {
        Sequence = sequence;
        Type = type;
        raw = raw.Replace("\r", "");
        List<ChatLink> links = new();
        var text = new StringBuilder();
        int cursor = 0;
        foreach (Match match in RegexFunctions.ChatItemLinks.Matches(raw))
        {
            string value = match.Groups[1].Value;
            int separator = value.LastIndexOf('/');
            if (separator <= 0 || !ulong.TryParse(value.AsSpan(separator + 1), out ulong id)) continue;
            text.Append(raw, cursor, match.Index - cursor);
            links.Add(new ChatLink(text.Length, separator, id));
            text.Append(value, 0, separator);
            cursor = match.Index + match.Length;
        }
        Text = text.Append(raw, cursor, raw.Length - cursor).ToString();
        Links = links.ToArray();
        ChatChannel channel = Classify(type);
        if (channel != ChatChannel.System)
        {
            int end = Text.IndexOfAny([':', '=', ' ']);
            if (end > 0)
            {
                string candidate = Text[..end].TrimStart('/', '[', '!', '~').TrimEnd(']');
                if (candidate.Length > 0 && candidate.All(c => char.IsLetterOrDigit(c) || c == '_'))
                    Sender = candidate;
            }
        }
        Colour = channel switch
        {
            ChatChannel.Party => Color.FromArgb(133, 189, 240),
            ChatChannel.Guild => Color.FromArgb(145, 211, 146),
            ChatChannel.Whispers => Color.FromArgb(227, 157, 230),
            ChatChannel.Shouts => Color.FromArgb(238, 182, 113),
            ChatChannel.System => Color.FromArgb(225, 204, 144),
            _ => DarkFantasyPalette.Text
        };
        if (type == ChatType.Relationship) Colour = Color.LightPink;
        if (type == ChatType.Mentor) Colour = Color.Plum;
    }

    public static ChatChannel Classify(ChatType type) => type switch
    {
        ChatType.Group => ChatChannel.Party,
        ChatType.Guild => ChatChannel.Guild,
        ChatType.WhisperIn or ChatType.WhisperOut => ChatChannel.Whispers,
        ChatType.Shout or ChatType.Shout2 or ChatType.Shout3 => ChatChannel.Shouts,
        ChatType.System or ChatType.System2 or ChatType.Hint or ChatType.Announcement or
            ChatType.LevelUp or ChatType.Trainer => ChatChannel.System,
        _ => ChatChannel.General
    };

    public bool Matches(ChatChannel channel)
    {
        if (channel != ChatChannel.General && channel != Classify(Type)) return false;
        return Type switch
        {
            ChatType.Normal or ChatType.LineMessage => !Settings.FilterNormalChat,
            ChatType.WhisperIn or ChatType.WhisperOut => !Settings.FilterWhisperChat,
            ChatType.Shout or ChatType.Shout2 or ChatType.Shout3 => !Settings.FilterShoutChat,
            ChatType.System or ChatType.System2 => !Settings.FilterSystemChat,
            ChatType.Group => !Settings.FilterGroupChat,
            ChatType.Guild => !Settings.FilterGuildChat,
            ChatType.Relationship => !Settings.FilterLoverChat,
            ChatType.Mentor => !Settings.FilterMentorChat,
            _ => true
        };
    }
}

internal readonly record struct ChatLink(int Start, int Length, ulong Item);
internal readonly record struct ChatRow(ChatMessage Message, string Text, int Offset);

internal sealed class ChatLogView : MirControl
{
    private const int RowHeight = 15, MaxRows = 1024;
    private const TextFormatFlags RowFlags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding;
    private readonly List<ChatRow> _rows = new();
    private readonly List<ChatRowLabel> _labels = new();
    private readonly ChatChannel _channel;
    private readonly Action<string> _whisper;
    private readonly MirButton _latest;
    private int _first, _lineCount;
    private int LastPage => Math.Max(0, _rows.Count - _lineCount);

    public ChatLogView(ChatChannel channel, Action<string> whisper)
    {
        _channel = channel;
        _whisper = whisper;
        _latest = new MirButton
        {
            Parent = this, Skin = UiSurface.Button, Glyph = UiGlyph.Down, AutoSize = false,
            Size = new Size(17, 19), Hint = "Jump to latest chat", Visible = false
        };
        _latest.Click += (_, _) => ScrollTo(LastPage);
        _latest.MouseWheel += (_, e) => Scroll(-Math.Sign(e.Delta) * 3);
        MouseWheel += (_, e) => Scroll(-Math.Sign(e.Delta) * 3);
    }

    public void Arrange(Rectangle bounds)
    {
        Location = bounds.Location;
        if (Size == bounds.Size) return;
        bool atEnd = _first >= LastPage;
        Size = bounds.Size;
        _latest.Location = new Point(Size.Width - _latest.Size.Width, Size.Height - _latest.Size.Height);
        _lineCount = Math.Max(1, Size.Height / RowHeight);
        while (_labels.Count < _lineCount)
        {
            var label = new ChatRowLabel(_whisper) { Parent = this, Visible = false };
            label.MouseWheel += (_, e) => Scroll(-Math.Sign(e.Delta) * 3);
            _labels.Add(label);
        }
        Refresh(atEnd ? LastPage : _first);
    }

    public void Rebuild(IEnumerable<ChatMessage> messages)
    {
        ChatRow anchor = _first < LastPage ? _rows[_first] : default;
        _rows.Clear();
        foreach (ChatMessage message in messages)
            if (message.Matches(_channel)) Wrap(message);
        TrimRows(0);
        int index = anchor.Message == null ? -1 : _rows.FindIndex(row =>
            row.Message.Sequence >= anchor.Message.Sequence &&
            (row.Message != anchor.Message || row.Offset >= anchor.Offset));
        Refresh(index < 0 ? LastPage : index);
    }

    public void Append(ChatMessage message, long oldestMessage)
    {
        bool atEnd = _first >= LastPage;
        int count = _rows.Count;
        if (message.Matches(_channel)) Wrap(message);
        int removed = TrimRows(oldestMessage);
        if (count == _rows.Count && removed == 0) return;
        if (atEnd || removed > _first)
            Refresh(atEnd ? LastPage : 0);
        else
        {
            _first -= removed;
            _latest.Visible = _first < LastPage;
        }
    }

    public void Scroll(int delta) => ScrollTo(_first + delta);
    public void ScrollTo(int first)
    {
        int target = Math.Clamp(first, 0, LastPage);
        if (target != _first) Refresh(target);
    }

    private int TrimRows(long oldestMessage)
    {
        int removed = Math.Max(0, _rows.Count - MaxRows);
        while (removed < _rows.Count && _rows[removed].Message.Sequence < oldestMessage) removed++;
        if (removed > 0) _rows.RemoveRange(0, removed);
        return removed;
    }

    private void Wrap(ChatMessage message)
    {
        string text = message.Text;
        int width = Math.Max(1, Size.Width - 20);
        int start = 0;
        while (start < text.Length)
        {
            int lineEnd = text.IndexOf('\n', start);
            if (lineEnd < 0) lineEnd = text.Length;
            if (lineEnd == start)
            {
                start++;
                continue;
            }
            int length = lineEnd - start;
            if (Measure(text.Substring(start, length)) > width)
            {
                int low = 1, high = length - 1;
                while (low < high)
                {
                    int mid = (low + high + 1) / 2;
                    if (Measure(text.Substring(start, mid)) <= width) low = mid;
                    else high = mid - 1;
                }
                length = low;
                int wordEnd = text.LastIndexOf(' ', start + length - 1, length);
                if (wordEnd > start) length = wordEnd - start;
                if (char.IsHighSurrogate(text[start + length - 1])) length = Math.Max(2, length - 1);
            }
            _rows.Add(new ChatRow(message, text.Substring(start, length), start));
            start += length;
            while (start < text.Length && (text[start] == ' ' || text[start] == '\n')) start++;
        }
    }

    private int Measure(string text) => text.Length == 0 ? 0 :
        TextRenderer.MeasureText(CMain.Graphics, text, _labels[0].Font, Size.Empty, RowFlags).Width;

    private void Refresh(int first)
    {
        _first = Math.Clamp(first, 0, LastPage);
        int visibleRows = Math.Min(_lineCount, _rows.Count - _first);
        int top = Math.Max(0, Size.Height - visibleRows * RowHeight);
        for (int i = 0; i < _labels.Count; i++)
        {
            ChatRowLabel label = _labels[i];
            label.Visible = i < visibleRows || (i == 0 && visibleRows == 0);
            if (!label.Visible) continue;
            label.Location = new Point(0, visibleRows == 0 ? 0 : top + i * RowHeight);
            label.Size = new Size(Math.Max(1, Size.Width - 20), RowHeight);
            label.SetRow(i < visibleRows ? _rows[_first + i] : default);
        }
        _latest.Visible = _first < LastPage;
    }

    private sealed class ChatRowLabel : MirLabel
    {
        private readonly List<(Rectangle Bounds, ulong Item)> _links = new();
        private ChatRow _row;
        private int _rowWidth;

        public ChatRowLabel(Action<string> whisper)
        {
            DrawFormat = RowFlags | TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter;
            OutLine = false;
            Click += (_, e) =>
            {
                if (e is not MouseEventArgs mouse) return;
                if (mouse.Button == MouseButtons.Left)
                {
                    Point local = CMain.MPoint.Subtract(DisplayLocation);
                    foreach (var link in _links)
                    {
                        if (!link.Bounds.Contains(local)) continue;
                        UserItem item = GameScene.ChatItemList.Find(value => value.UniqueID == link.Item);
                        if (item != null)
                        {
                            GameScene.Scene.DisposeItemLabel();
                            GameScene.Scene.CreateItemLabel(item);
                        }
                        return;
                    }
                }
                if (mouse.Button == MouseButtons.Right && _row.Message?.Sender is string sender)
                    whisper(sender);
            };
            MouseLeave += (_, _) =>
            {
                if (_links.Count != 0) GameScene.Scene?.DisposeItemLabel();
            };
            AfterDraw += (_, _) =>
            {
                foreach (var link in _links)
                    DXManager.Device.Ui.Fill(new Rectangle(DisplayLocation.X + link.Bounds.X,
                        DisplayLocation.Y + 13, link.Bounds.Width, 1), DarkFantasyPalette.Accent);
            };
        }

        public void SetRow(ChatRow row)
        {
            if (_row == row && _rowWidth == Size.Width) return;
            _row = row;
            _rowWidth = Size.Width;
            Text = row.Text ?? "No messages in this tab.";
            ForeColour = row.Message?.Colour ?? DarkFantasyPalette.MutedText;
            _links.Clear();
            if (row.Message == null) return;
            foreach (ChatLink link in row.Message.Links)
            {
                int start = Math.Max(link.Start, row.Offset) - row.Offset;
                int end = Math.Min(link.Start + link.Length - row.Offset, row.Text.Length);
                if (end <= start) continue;
                int x = start == 0 ? 1 : 1 + TextRenderer.MeasureText(CMain.Graphics, row.Text[..start],
                    Font, Size.Empty, RowFlags).Width;
                int width = TextRenderer.MeasureText(CMain.Graphics, row.Text[start..end],
                    Font, Size.Empty, RowFlags).Width;
                width = Math.Min(width, Size.Width - x);
                if (width > 0) _links.Add((new Rectangle(x, 0, width, RowHeight), link.Item));
            }
        }
    }
}
