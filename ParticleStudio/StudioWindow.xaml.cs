using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Client.MirScenes;
using CrystalM2.Rendering.Core;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace CrystalM2.ParticleStudio;

public partial class StudioWindow : Window
{
    private readonly PreviewViewport _viewport = new();
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
    private readonly Stopwatch _clock = new();
    private readonly Dictionary<Spell, Draft> _drafts = new();
    private Dictionary<string, JsonObject> _defaults = new(StringComparer.Ordinal);
    private string _folder = FindAssetFolder();
    private SpellVfxEntry? _spell;
    private PreviewSequence? _sequence;
    private bool _ready, _updating, _settingTime;
    private int _playOrigin;

    public StudioWindow()
    {
        InitializeComponent();
        ViewportHost.Child = _viewport.Surface;
        _viewport.Surface.Paint += (_, _) => Draw();
        _viewport.Surface.Resize += (_, _) => Draw();
        _timer.Tick += (_, _) => Advance();
        Loaded += (_, _) => Initialize();
        Closed += (_, _) =>
        {
            _timer.Stop();
            _ready = false;
            ViewportHost.Child = null;
            _viewport.Dispose();
        };
    }

    private void Initialize()
    {
        if (_ready) return;
        ClassFilter.ItemsSource = new[] { "All classes", "Warrior", "Wizard", "Taoist" };
        ClassFilter.SelectedIndex = 0;
        DirectionPicker.ItemsSource = Enum.GetValues<MirDirection>().Take(8);
        DirectionPicker.SelectedItem = MirDirection.Right;
        AmbientPicker.ItemsSource = new[] { LightSetting.Day, LightSetting.Evening, LightSetting.Night };
        AmbientPicker.SelectedItem = LightSetting.Day;
        _ready = true;
        Try(() =>
        {
            LoadFolder(_folder);
            FilterChanged(this, new RoutedEventArgs());
            Spells.SelectedItem = SpellVfxCatalogue.All.First(entry => entry.Spell == Spell.TwinDrakeBlade);
            Spells.ScrollIntoView(Spells.SelectedItem);
        });
        if (ReferenceScene.FindClientDirectory() is { } client) Try(() => UseClient(client));
        else ReferenceText.Text = "Choose a client folder to load the player and map.";

    }

    private static string FindAssetFolder()
    {
        for (var parent = new DirectoryInfo(AppContext.BaseDirectory); parent != null; parent = parent.Parent)
        {
            string source = Path.Combine(parent.FullName, "Client", "Data", "SpellVfx");
            if (File.Exists(Path.Combine(source, "Warrior.json"))) return source;
        }
        return SpellVfxAssets.InstalledDirectory;
    }

    private void LoadFolder(string directory)
    {
        if (!SpellVfxAssets.Groups.All(group => File.Exists(Path.Combine(directory, group + ".json"))))
            throw new InvalidDataException("Choose a SpellVfx folder containing the seven group files.");
        var defaults = SpellVfxAssets.Groups.ToDictionary(group => group,
            group => SpellVfxAssets.ReadGroup(directory, group), StringComparer.Ordinal);
        SpellVfxAssets.Reload(directory);
        _defaults = defaults;
        _folder = directory;
        _drafts.Clear();
        FolderText.Text = directory;
        FolderText.ToolTip = directory;
        if (_spell != null) LoadSpell(_spell);
    }

    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        var prior = _spell;
        string query = SpellSearch.Text.Trim();
        var items = SpellVfxCatalogue.All.Where(entry =>
            (ClassFilter.SelectedIndex <= 0 || entry.Class.ToString() == (string)ClassFilter.SelectedItem) &&
            (entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             entry.Spell.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(entry => entry.Name).ToArray();
        _updating = true;
        try
        {
            Spells.ItemsSource = items;
            Spells.SelectedItem = items.Contains(prior) ? prior : items.FirstOrDefault();
        }
        finally { _updating = false; }
        LibraryCount.Text = $"{items.Length} of {SpellVfxCatalogue.All.Count} spells";
        if (Spells.SelectedItem is SpellVfxEntry entry && entry != prior)
            Try(() => LoadSpell(entry));
    }

    private void SpellChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready && !_updating && Spells.SelectedItem is SpellVfxEntry entry)
            Try(() => LoadSpell(entry));
    }

    private void LoadSpell(SpellVfxEntry entry)
    {
        Pause();
        if (!_drafts.TryGetValue(entry.Spell, out var draft))
        {
            draft = new Draft(SpellVfxAssets.ReadSpell(_folder, entry.Spell));
            _drafts.Add(entry.Spell, draft);
        }
        SpellVfxAssets.PreviewSpell(_folder, entry.Spell, draft.Working);
        _spell = entry;
        PreviewTitle.Text = entry.Name;
        RebuildStages();
        Status.Text = _sequence?.Stages.Count > 0
            ? $"{entry.Class} · {entry.Source} · Press F1 to play"
            : $"{entry.Name} has no standalone cast. Shared settings remain available.";
        UpdateDirty();
    }

    private void RebuildStages(bool preserveSelection = false)
    {
        if (_spell == null) return;
        string? selected = preserveSelection ? StagePicker.SelectedItem as string : null;
        int time = preserveSelection ? (int)Timeline.Value : 0;
        _sequence = new PreviewSequence(_spell);
        _updating = true;
        try
        {
            string[] names = ["Full sequence", .. _sequence.Stages.Select(stage => stage.Name)];
            StagePicker.ItemsSource = names;
            StagePicker.SelectedItem = names.Contains(selected) ? selected : names[0];
        }
        finally { _updating = false; }
        PlayButton.IsEnabled = _sequence.Stages.Count > 0;
        Timeline.IsEnabled = PlayButton.IsEnabled;
        SetRange(time);
        RefreshNodes();
        Draw();
    }

    private void StageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || _updating || _sequence == null) return;
        Pause();
        SetRange(0);
        Try(RefreshNodes);
        Draw();
    }

    private IEnumerable<PreviewStage> SelectedStages()
    {
        if (_sequence == null) return [];
        int index = StagePicker.SelectedIndex - 1;
        return index >= 0 && index < _sequence.Stages.Count ? [_sequence.Stages[index]] : _sequence.Stages;
    }

    private void SetRange(int value)
    {
        _settingTime = true;
        try
        {
            Timeline.Maximum = Math.Max(1, SelectedStages().Sum(stage => (double)stage.Duration + stage.Delay));
            Timeline.Value = Math.Min(value, Timeline.Maximum);
        }
        finally { _settingTime = false; }
        UpdateClock();
    }

    private void RefreshNodes()
    {
        if (_spell == null || _defaults.Count == 0) return;
        var prior = NodePicker.SelectedItem as NodeChoice;
        var nodes = SpellVfxAssets.CollectNodes(() =>
        {
            _ = new PreviewSequence(_spell);
            var pool = new SpellParticlePool(4096);
            var rig = new PreviewRig(new Vector2(300, 280), Vector2.UnitX, MirDirection.Right);
            foreach (var stage in SelectedStages())
            {
                foreach (float fraction in new[] { .08f, .33f, .67f, .92f })
                {
                    pool.Clear();
                    stage.Sample(pool, rig, (int)(stage.Duration * fraction));
                }
            }
        });
        var items = (AllNodes.IsChecked == true || nodes.Count == 0
            ? _defaults.SelectMany(group => group.Value.Select(node => (Group: group.Key, Node: node.Key)))
            : nodes.Where(node => _defaults.TryGetValue(node.Group, out var group) && group.ContainsKey(node.Node)))
            .OrderBy(node => node.Group).ThenBy(node => node.Node)
            .Select(node => new NodeChoice(node.Group, node.Node)).ToArray();
        NodePicker.ItemsSource = items;
        NodePicker.SelectedItem = items.Contains(prior) ? prior : items.FirstOrDefault();
        ShowParameters();
    }

    private void NodesChanged(object sender, RoutedEventArgs e)
    {
        if (_ready) Try(RefreshNodes);
    }

    private void NodeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready) ShowParameters();
    }

    private void ShowParameters()
    {
        if (_spell == null || NodePicker.SelectedItem is not NodeChoice choice)
        {
            Parameters.ItemsSource = null;
            return;
        }
        var shared = _defaults[choice.Group][choice.Node]!.AsObject();
        var overrides = _drafts[_spell.Spell].Working[choice.Group]?[choice.Node] as JsonObject;
        Parameters.ItemsSource = shared.Select(item =>
        {
            JsonNode? edit = overrides?[item.Key];
            JsonNode value = edit ?? item.Value!;
            return new ParameterRow(item.Key, Format(value), edit == null ? "Shared" : "Spell");
        }).ToArray();
    }

    private static string Format(JsonNode value) => value.GetValueKind() == JsonValueKind.String
        ? value.GetValue<string>() : value.ToJsonString();

    private void ParameterEdited(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || e.EditingElement is not TextBox input ||
            e.Row.Item is not ParameterRow row || _spell == null ||
            NodePicker.SelectedItem is not NodeChoice choice || row.Text == input.Text) return;
        try
        {
            JsonNode shared = _defaults[choice.Group][choice.Node]![row.Name]!;
            JsonNode value = shared.GetValueKind() == JsonValueKind.String
                ? JsonValue.Create(input.Text.Trim())! : JsonNode.Parse(input.Text)!;
            if (value == null) throw new InvalidDataException("Enter a number, true or false, or a colour curve as shown.");
            ChangeValue(choice, row.Name, JsonNode.DeepEquals(shared, value) ? null : value);
        }
        catch (Exception error) when (IsEditorError(error))
        {
            e.Cancel = true;
            Status.Text = error.Message;
            Status.ToolTip = error.Message;
        }
    }

    private void ResetValue(object sender, RoutedEventArgs e)
    {
        if (NodePicker.SelectedItem is NodeChoice choice && Parameters.SelectedItem is ParameterRow row)
            Try(() => ChangeValue(choice, row.Name, null));
    }

    private void ChangeValue(NodeChoice choice, string name, JsonNode? value)
    {
        if (_spell == null) return;
        Spell spell = _spell.Spell;
        var draft = _drafts[spell];
        var updated = (JsonObject)draft.Working.DeepClone();
        var group = updated[choice.Group] as JsonObject;
        var node = group?[choice.Node] as JsonObject;
        if (value != null)
        {
            if (group == null) updated[choice.Group] = group = new JsonObject();
            if (node == null) group[choice.Node] = node = new JsonObject();
            node[name] = value;
        }
        else if (node != null)
        {
            node.Remove(name);
            if (node.Count == 0) group!.Remove(choice.Node);
            if (group!.Count == 0) updated.Remove(choice.Group);
        }

        SpellVfxAssets.PreviewSpell(_folder, spell, updated);
        draft.Working = updated;
        Pause();
        Status.Text = $"{choice.Node} · {name} applied to preview";
        UpdateDirty();
        // The grid must finish committing its cell before the inspector is rebound.
        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            if (_spell?.Spell == spell) Try(() => RebuildStages(preserveSelection: true));
        }));
    }

    private void OpenFolder(object sender, RoutedEventArgs e)
    {
        if (!CanDiscard()) return;
        var dialog = new Microsoft.Win32.OpenFolderDialog { Title = "Open a SpellVfx asset folder", InitialDirectory = _folder };
        if (dialog.ShowDialog(this) == true) Try(() => LoadFolder(dialog.FolderName));
    }

    private void Reload(object sender, RoutedEventArgs e)
    {
        if (CanDiscard()) Try(() => LoadFolder(_folder));
    }

    private void Save(object sender, RoutedEventArgs e)
    {
        if (_spell == null || !Parameters.CommitEdit(DataGridEditingUnit.Cell, true)) return;
        Try(() =>
        {
            Spell spell = _spell.Spell;
            var draft = _drafts[spell];
            if (!JsonNode.DeepEquals(draft.Saved, SpellVfxAssets.ReadSpell(_folder, spell)))
                throw new InvalidDataException("This profile changed on disk. Reload it before saving.");
            SpellVfxAssets.SaveSpell(_folder, spell, draft.Working);
            draft.Saved = (JsonObject)draft.Working.DeepClone();
            Status.Text = $"Saved Spells\\{spell}.json";
            UpdateDirty();
        });
    }

    private void UseClient(string directory)
    {
        var scene = new ReferenceScene(directory);
        _viewport.UseReference(scene);
        try { scene.OpenDefaultMap(); }
        finally
        {
            UpdateReference();
            UpdateDirty();
            Draw();
        }
    }

    private void UpdateReference()
    {
        if (_viewport.Reference is not { } scene) return;
        ReferenceText.Text = scene.MapFile is { } map
            ? $"Reference map · {Path.GetFileName(map)}"
            : "Player artwork loaded · Choose a map";
        ReferenceText.ToolTip = scene.ClientDirectory;
    }

    private void OpenClientFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Choose the built client folder",
            InitialDirectory = _viewport.Reference?.ClientDirectory ?? AppContext.BaseDirectory
        };
        if (dialog.ShowDialog(this) == true) Try(() => UseClient(dialog.FolderName));
    }

    private void OpenMap(object sender, RoutedEventArgs e)
    {
        if (_viewport.Reference is not { } scene)
        {
            Status.Text = "Choose the client folder first.";
            return;
        }
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose a reference map",
            Filter = "CrystalM2 maps (*.map)|*.map",
            InitialDirectory = scene.MapDirectory,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true) return;
        Try(() =>
        {
            scene.OpenMap(dialog.FileName);
            UpdateReference();
            Draw();
        });
    }

    private void ApplyToClient(object sender, RoutedEventArgs e)
    {
        if (_spell == null || _viewport.Reference is not { } scene || !Parameters.CommitEdit(DataGridEditingUnit.Cell, true))
            return;
        Try(() =>
        {
            var draft = _drafts[_spell.Spell];
            if (draft.Dirty) throw new InvalidOperationException("Save the spell before applying it to the client.");
            string destination = ClientSpellProfiles.Apply(_folder, scene.ClientDirectory, _spell.Spell);
            Status.Text = $"Applied {_spell.Name} · Restart the client to load it";
            Status.ToolTip = destination;
        });
    }

    private void UpdateDirty()
    {
        bool dirty = _spell != null && _drafts[_spell.Spell].Dirty;
        SaveButton.IsEnabled = dirty;
        ApplyButton.IsEnabled = _spell != null && !dirty && _viewport.Reference != null;
        Title = _drafts.Values.Any(draft => draft.Dirty)
            ? "CrystalM2 Particle Studio • Unsaved changes" : "CrystalM2 Particle Studio";
    }

    private bool CanDiscard() => !_drafts.Values.Any(draft => draft.Dirty) ||
        MessageBox.Show(this, "Discard unsaved spell edits?", "Particle Studio",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!CanDiscard()) e.Cancel = true;
        base.OnClosing(e);
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.F1) { Play(this, e); e.Handled = true; }
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) { Save(this, e); e.Handled = true; }
        base.OnPreviewKeyDown(e);
    }

    private void Play(object sender, RoutedEventArgs e)
    {
        if (_sequence == null || _sequence.Stages.Count == 0) return;
        if (_timer.IsEnabled) { Pause(); return; }
        if (Timeline.Value >= Timeline.Maximum - 1) Timeline.Value = 0;
        _playOrigin = (int)Timeline.Value;
        _clock.Restart();
        _timer.Start();
        PlayButton.Content = "Pause  F1";
    }

    private void Pause()
    {
        _timer.Stop();
        _clock.Stop();
        PlayButton.Content = "Play  F1";
    }

    private void Advance()
    {
        double value = _playOrigin + _clock.ElapsedMilliseconds;
        if (value >= Timeline.Maximum)
        {
            if (Loop.IsChecked == true)
            {
                _playOrigin = 0;
                _clock.Restart();
                value %= Timeline.Maximum;
            }
            else
            {
                Pause();
                value = Timeline.Maximum;
            }
        }
        _settingTime = true;
        try { Timeline.Value = value; }
        finally { _settingTime = false; }
        UpdateClock();
        Draw();
    }

    private void Seek(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_ready || _settingTime) return;
        Pause();
        UpdateClock();
        Draw();
    }

    private void StepBack(object sender, RoutedEventArgs e) => Step(-16);
    private void StepForward(object sender, RoutedEventArgs e) => Step(16);

    private void Step(int milliseconds)
    {
        Pause();
        Timeline.Value = Math.Clamp(Timeline.Value + milliseconds, 0, Timeline.Maximum);
    }

    private void UpdateClock() => ClockText.Text = $"{(int)Timeline.Value:n0} / {(int)Timeline.Maximum:n0} ms";

    private void ViewChanged(object sender, RoutedEventArgs e)
    {
        if (_ready) Draw();
    }

    private void Draw()
    {
        if (!_ready || !IsVisible) return;
        var stage = (PreviewStage?)null;
        int time = Math.Min((int)Timeline.Value, (int)Timeline.Maximum - 1);
        foreach (var candidate in SelectedStages())
        {
            if (time < candidate.Delay) break;
            time -= candidate.Delay;
            if (time < candidate.Duration) { stage = candidate; break; }
            time -= candidate.Duration;
        }

        try
        {
            _viewport.Render(stage, time, DirectionPicker.SelectedItem is MirDirection direction ? direction : MirDirection.Right,
                Glow.IsChecked == true, Guide.IsChecked == true, ReferenceVisible.IsChecked == true,
                AmbientPicker.SelectedItem is LightSetting ambient ? ambient : LightSetting.Day);
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Pause();
            Status.Text = $"Preview unavailable · {error.Message}";
            Status.ToolTip = error.Message;
        }
    }

    private static bool IsEditorError(Exception error) => error is IOException or InvalidDataException or JsonException
        or ArgumentException or InvalidOperationException or UnauthorizedAccessException or FormatException;

    private void Try(Action action)
    {
        try { action(); }
        catch (Exception error) when (IsEditorError(error))
        {
            Status.Text = error.Message;
            Status.ToolTip = error.Message;
        }
    }

    private sealed record NodeChoice(string Group, string Node)
    {
        public override string ToString() => $"{Group} · {Node}";
    }

    private sealed class ParameterRow(string name, string text, string source)
    {
        public string Name { get; } = name;
        public string Text { get; set; } = text;
        public string Source { get; } = source;
    }

    private sealed class Draft(JsonObject saved)
    {
        public JsonObject Saved { get; set; } = saved;
        public JsonObject Working { get; set; } = (JsonObject)saved.DeepClone();
        public bool Dirty => !JsonNode.DeepEquals(Saved, Working);
    }
}
