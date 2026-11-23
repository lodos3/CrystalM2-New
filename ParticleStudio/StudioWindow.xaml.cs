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
        SpellVfxAssets.Reload(directory);
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
        bool blade = SpellVfxBladeFrames.Supports(entry.Spell);
        BladeExtras.Visibility = blade ? Visibility.Visible : Visibility.Collapsed;
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
        return index >= 0 && index < _sequence.Stages.Count
            ? [_sequence.Stages[index]]
            : _sequence.Stages.Where(stage => stage.InFullSequence);
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
        bool spriteFrames = SelectedStages().Any(stage => stage.BladePose.HasValue);
        PreviousFrame.Content = spriteFrames ? "Previous frame" : "−16 ms";
        NextFrame.Content = spriteFrames ? "Next frame" : "+16 ms";
        UpdateClock();
    }

    private Dictionary<string, SpellVfxAssets.EmitterSample> _emitters = new();
    private readonly HashSet<string> _expandedModules = ["Main", "Emission", "Renderer"];

    private void RefreshNodes()
    {
        if (_spell == null) return;
        string? prior = (NodePicker.SelectedItem as LayerChoice)?.Id;
        _emitters = SpellVfxAssets.CollectEmitters(() =>
        {
            var pool = new SpellParticlePool(4096);
            var direction = DirectionPicker.SelectedItem is MirDirection selected ? selected : MirDirection.Right;
            float angle = ((int)direction - 2) * MathF.PI / 4;
            var facing = Vector2.Normalize(new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .68f));
            var samplePose = new PreviewRig(new Vector2(300, 280), facing, direction);
            var rig = samplePose with { WeaponImbue = PreviewImbue, WeaponTrail = PreviewTrail, ReferenceWeapon = progress => samplePose.Weapon(progress) };
            foreach (var stage in SelectedStages())
            foreach (float fraction in new[] { 0f, .08f, .2f, .33f, .5f, .67f, .8f, .92f, .99f })
            {
                pool.Clear();
                stage.Sample(pool, rig, (int)(stage.Duration * fraction));
            }
        });

        var names = new Dictionary<string, int>();
        var items = _emitters.Select(item =>
        {
            var particle = item.Value.Particle;
            string name = item.Key switch
            {
                "Blade.Sweep-filaments" => "Sweep sheet",
                "Blade.Sweep-soft-edge" => "Sweep feather",
                _ when item.Key.StartsWith("Blade.", StringComparison.Ordinal) => item.Key["Blade.".Length..].Replace('-', ' '),
                _ => $"{particle.Shape} · {particle.Layer}"
            };
            int number = names.GetValueOrDefault(name) + 1;
            names[name] = number;
            return new LayerChoice(item.Key, number == 1 ? name : $"{name} {number}");
        }).ToArray();
        NodePicker.ItemsSource = items;
        NodePicker.SelectedItem = items.FirstOrDefault(item => item.Id == prior) ?? items.FirstOrDefault();
        ShowParameters();
    }

    private void NodeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready) return;
        ShowParameters();
        if (SoloLayer.IsChecked == true) Draw();
    }

    private void ShowParameters()
    {
        if (_spell == null || NodePicker.SelectedItem is not LayerChoice choice)
        {
            EmitterModules.Children.Clear();
            EmitterModules.Children.Add(new TextBlock
            {
                Text = SelectedStages().Any(stage => stage.Name == "Readied weapon (bonus)")
                    ? "Enable Weapon imbue to preview and edit this optional layer."
                    : "No particle layers were emitted in this stage.",
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }
        Spell spell = _spell.Spell;
        var current = _drafts[spell].Working[SpellVfxAssets.EmitterGroup]?[choice.Id] as JsonObject ?? new JsonObject();
        EmitterInspector.Show(EmitterModules, _emitters[choice.Id], current, _expandedModules,
            (key, value) => ChangeEmitter(spell, choice, key, value), this);
    }

    private bool ChangeEmitter(Spell spell, LayerChoice layer, string key, JsonNode? value)
    {
        if (_spell?.Spell != spell) return false;
        var draft = _drafts[spell];
        var updated = (JsonObject)draft.Working.DeepClone();
        var group = updated[SpellVfxAssets.EmitterGroup] as JsonObject;
        var settings = group?[layer.Id] as JsonObject;
        if (value != null)
        {
            if (group == null) updated[SpellVfxAssets.EmitterGroup] = group = new JsonObject();
            if (settings == null) group[layer.Id] = settings = new JsonObject();
            settings[key] = value.DeepClone();
        }
        else settings?.Remove(key);
        if (settings?.Count == 0) group!.Remove(layer.Id);
        if (group?.Count == 0) updated.Remove(SpellVfxAssets.EmitterGroup);
        if (JsonNode.DeepEquals(updated, draft.Working)) return true;

        try
        {
            SpellVfxAssets.PreviewSpell(_folder, spell, updated);
            draft.Working = updated;
            UpdateDirty();
            Status.Text = $"{layer.Name} updated in preview";
            Draw();
            return true;
        }
        catch (Exception error) when (IsEditorError(error))
        {
            Status.Text = error.Message;
            Status.ToolTip = error.Message;
            return false;
        }
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
        if (_spell == null) return;
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
        if (_spell == null || _viewport.Reference is not { } scene)
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

    private void StepBack(object sender, RoutedEventArgs e) => Step(-1);
    private void StepForward(object sender, RoutedEventArgs e) => Step(1);

    private void Step(int direction)
    {
        Pause();
        int current = (int)Timeline.Value;
        var boundaries = new List<int>();
        int offset = 0;
        foreach (var stage in SelectedStages())
        {
            offset += stage.Delay;
            if (stage.BladePose is { } sprite)
                for (int frame = 0; frame < sprite.FrameCount; frame++)
                    boundaries.Add(offset + frame * sprite.FrameInterval);
            offset += stage.Duration;
        }

        Timeline.Value = boundaries.Count == 0 ? Math.Clamp(current + direction * 16, 0, Timeline.Maximum)
            : direction < 0 ? boundaries.LastOrDefault(time => time < current, boundaries[0])
            : boundaries.FirstOrDefault(time => time > current, boundaries[^1]);
    }

    private (PreviewStage? Stage, int Time) CurrentSample()
    {
        int time = Math.Max(0, Math.Min((int)Timeline.Value, (int)Timeline.Maximum - 1));
        foreach (var stage in SelectedStages())
        {
            if (time < stage.Delay) break;
            time -= stage.Delay;
            if (time < stage.Duration) return (stage, time);
            time -= stage.Duration;
        }

        return (null, time);
    }

    private MirDirection PreviewDirection =>
        DirectionPicker.SelectedItem is MirDirection direction ? direction : MirDirection.Right;

    private bool PreviewImbue => BladeExtras.Visibility == Visibility.Visible && WeaponImbue.IsChecked == true;
    private bool PreviewTrail => BladeExtras.Visibility == Visibility.Visible && WeaponTrail.IsChecked == true;

    private void UpdateClock()
    {
        var (stage, time) = CurrentSample();
        string elapsed = $"{(int)Timeline.Value:n0} / {(int)Timeline.Maximum:n0} ms";
        if (stage?.BladePose is { } sprite)
        {
            int frame = sprite.FrameAt(time);
            ClockText.Text = $"{frame + 1}/{sprite.FrameCount} · Ref {sprite.Library} #{sprite.FrameIndex(PreviewDirection, frame)} · {elapsed}";
        }
        else ClockText.Text = elapsed;
    }

    private void ViewChanged(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        if (ReferenceEquals(sender, DirectionPicker) || ReferenceEquals(sender, WeaponImbue) || ReferenceEquals(sender, WeaponTrail))
            Try(RefreshNodes);
        UpdateClock();
        Draw();
    }

    private void Draw()
    {
        if (!_ready || !IsVisible) return;
        var (stage, time) = CurrentSample();

        try
        {
            string? soloId = SoloLayer.IsChecked == true && NodePicker.SelectedItem is LayerChoice layer ? layer.Id : null;
            using var solo = SpellVfxAssets.SoloEmitter(soloId);
            _viewport.Render(stage, time, PreviewDirection,
                Glow.IsChecked == true, Guide.IsChecked == true, ReferenceVisible.IsChecked == true,
                AmbientPicker.SelectedItem is LightSetting ambient ? ambient : LightSetting.Day,
                PreviewImbue, PreviewTrail);
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

    private sealed record LayerChoice(string Id, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class Draft(JsonObject saved)
    {
        public JsonObject Saved { get; set; } = saved;
        public JsonObject Working { get; set; } = (JsonObject)saved.DeepClone();
        public bool Dirty => !JsonNode.DeepEquals(Saved, Working);
    }
}
