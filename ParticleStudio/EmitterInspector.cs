using Panel = System.Windows.Controls.Panel;
using Brush = System.Windows.Media.Brush;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CrystalM2.Rendering.Core;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using TextBox = System.Windows.Controls.TextBox;

namespace CrystalM2.ParticleStudio;

internal static class EmitterInspector
{
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(215, 222, 230));
    private static readonly Brush Subtle = new SolidColorBrush(Color.FromRgb(154, 168, 182));
    private static readonly Brush Header = new SolidColorBrush(Color.FromRgb(47, 57, 68));
    private static readonly string[][] Modules =
    [
        ["Main", "StartLifetime", "StartSpeed", "StartSize", "StartRotation", "StartColour", "StartOpacity", "StartDelay", "GravityModifier", "MaxParticles"],
        ["Emission", "Enabled", "EmissionProbability"],
        ["Shape", "SpawnShape", "PositionX", "PositionY", "ShapeSpread", "ShapeHeight", "ShapeRotation"],
        ["Velocity over Lifetime", "VelocityMultiplier", "VelocityOverLifetime"],
        ["Limit Velocity over Lifetime", "VelocityLimit"],
        ["Force over Lifetime", "ForceX", "ForceY"],
        ["Colour over Lifetime", "ColourOverLifetime", "OpacityOverLifetime"],
        ["Colour by Speed", "SpeedRange", "ColourBySpeed"],
        ["Size over Lifetime", "SizeOverLifetime"],
        ["Size by Speed", "SizeBySpeed"],
        ["Rotation over Lifetime", "RotationOverLifetime"],
        ["Rotation by Speed", "RotationBySpeed"],
        ["Collision", "Collision"],
        ["Sub Emitters", "SubEmitterCount", "SubEmitterSprite"],
        ["Texture Sheet Animation", "TextureFrame", "TextureCycles"],
        ["Renderer", "RenderMode", "Sprite", "SortingLayer", "CastsLight", "LightEnergy", "AlwaysOnTop"]
    ];

    private static readonly Dictionary<string, PropertyInfo> Settings =
        typeof(SpellEmitterSettings).GetProperties().ToDictionary(property => property.Name);

    public static void Show(Panel host, Client.MirScenes.SpellVfxAssets.EmitterSample sample, JsonObject values,
        HashSet<string> expanded, Func<string, JsonNode?, bool> edit, Window owner)
    {
        host.Children.Clear();
        foreach (string[] module in Modules)
        {
            string title = module[0];
            var rows = new StackPanel { Margin = new Thickness(4, 3, 4, 6) };
            foreach (string key in module.Skip(1))
                rows.Children.Add(Row(key, sample, values[key], edit, owner));
            var section = new Expander
            {
                Header = title, Content = rows, IsExpanded = expanded.Contains(title), Foreground = LabelBrush,
                Background = Header, Margin = new Thickness(0, 0, 0, 3), Padding = new Thickness(5, 3, 5, 3)
            };
            section.Expanded += (_, _) => expanded.Add(title);
            section.Collapsed += (_, _) => expanded.Remove(title);
            host.Children.Add(section);
        }
    }

    private static FrameworkElement Row(string key, Client.MirScenes.SpellVfxAssets.EmitterSample sample, JsonNode? value,
        Func<string, JsonNode?, bool> edit, Window owner)
    {
        var row = new Grid { Margin = new Thickness(2, 3, 2, 3) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var label = new TextBlock { Text = Label(key), Foreground = LabelBrush, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
        row.Children.Add(label);

        Type type = Nullable.GetUnderlyingType(Settings[key].PropertyType) ?? Settings[key].PropertyType;
        bool unavailable = (!sample.Persistent && key is "StartLifetime" or "StartDelay" or "StartSpeed" or
            "GravityModifier" or "VelocityMultiplier" or "VelocityOverLifetime" or "VelocityLimit" or "ForceX" or "ForceY") ||
            (!sample.HasCollision && key is "Collision" or "SubEmitterCount" or "SubEmitterSprite");
        string inherited = Inherited(key, sample);
        FrameworkElement control;
        Action reset;
        if (type.IsEnum || type == typeof(bool))
        {
            string[] options = type.IsEnum ? Enum.GetNames(type) : ["On", "Off"];
            var picker = new ComboBox { ItemsSource = new[] { inherited }.Concat(options).ToArray() };
            string? configured = type == typeof(bool) ? value == null ? null : value.GetValue<bool>() ? "On" : "Off" : value?.GetValue<string>();
            picker.SelectedItem = configured ?? inherited;
            bool restoring = false;
            picker.SelectionChanged += (_, _) =>
            {
                if (restoring || picker.SelectedIndex < 0) return;
                var next = picker.SelectedIndex == 0 ? null : type == typeof(bool)
                    ? JsonValue.Create(picker.SelectedIndex == 1) : JsonValue.Create((string)picker.SelectedItem);
                if (edit(key, next)) configured = picker.SelectedIndex == 0 ? null : (string)picker.SelectedItem;
                else
                {
                    restoring = true;
                    picker.SelectedItem = configured ?? inherited;
                    restoring = false;
                }
            };
            reset = () => picker.SelectedIndex = 0;
            control = picker;
        }
        else if (type == typeof(ParticleRange) || type.IsGenericType)
        {
            var button = new Button { Content = Summary(value, inherited), Margin = new Thickness(0) };
            button.Click += (_, _) =>
            {
                var dialog = new EmitterValueWindow(Label(key), key, value, inherited) { Owner = owner };
                if (dialog.ShowDialog() == true && edit(key, dialog.Value))
                {
                    value = dialog.Value;
                    button.Content = Summary(value, inherited);
                }
            };
            reset = () => { value = null; button.Content = inherited; };
            control = button;
        }
        else
        {
            string DefaultText() => key switch
            {
                "StartColour" => inherited,
                "StartOpacity" => (sample.Particle.Colour.A / 255f).ToString("g4", CultureInfo.InvariantCulture),
                "LightEnergy" => sample.Particle.LightEnergy.ToString("g4", CultureInfo.InvariantCulture),
                "StartDelay" or "ShapeRotation" or "PositionX" or "PositionY" or "ShapeSpread" or "ForceX" or "ForceY" => "0",
                "EmissionProbability" or "VelocityMultiplier" => "1",
                _ => ""
            };
            string Display() => value == null ? DefaultText() : Text(value);
            var box = new TextBox { Text = Display(), ToolTip = inherited, Padding = new Thickness(5, 3, 5, 3) };
            var panel = new DockPanel();
            if (type == typeof(string))
            {
                var colour = new Button { Content = "…", Margin = new Thickness(4, 0, 0, 0), ToolTip = "Pick a colour" };
                DockPanel.SetDock(colour, Dock.Right);
                panel.Children.Add(colour);
                colour.Click += (_, _) =>
                {
                    using var picker = new System.Windows.Forms.ColorDialog();
                    try { picker.Color = System.Drawing.ColorTranslator.FromHtml(box.Text.Length == 0 ? inherited : box.Text); }
                    catch (ArgumentException) { }
                    if (picker.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                    box.Text = $"#{picker.Color.R:X2}{picker.Color.G:X2}{picker.Color.B:X2}";
                    Commit();
                };
            }
            panel.Children.Add(box);
            if (value == null) box.SetValue(System.Windows.Automation.AutomationProperties.HelpTextProperty, inherited);
            box.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Commit(); e.Handled = true; } };
            box.LostKeyboardFocus += (_, _) => Commit();
            void Commit()
            {
                string current = box.Text.Trim();
                if (current == Display()) return;
                try
                {
                    JsonNode? next = current.Length == 0 ? null : type == typeof(string) ? JsonValue.Create(current) : JsonNode.Parse(current);
                    if (next == null && current.Length > 0) throw new JsonException("Clear the field to inherit its authored value.");
                    if (edit(key, next)) value = next;
                    box.Text = Display();
                }
                catch (JsonException)
                {
                    box.Text = Display();
                    box.ToolTip = "Enter a number using a decimal point.";
                }
            }
            reset = () => { value = null; box.Text = Display(); };
            control = panel;
        }
        control.IsEnabled = !unavailable;
        if (!sample.HasAnimatedSprite && key is "TextureFrame" or "TextureCycles")
            row.ToolTip = "Choose an animated sprite to see this setting in the preview.";
        if (unavailable) row.ToolTip = sample.Persistent
            ? "This emitter has no authored landing collision."
            : "This layer is regenerated by the spell each frame. Its motion and timing come from the spell stage.";
        Grid.SetColumn(control, 1);
        row.Children.Add(control);
        var restore = new Button { Content = "↺", ToolTip = "Restore authored value", Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(4, 0, 0, 0), IsEnabled = !unavailable };
        restore.Click += (_, _) => { if (edit(key, null)) reset(); };
        Grid.SetColumn(restore, 2);
        row.Children.Add(restore);
        return row;
    }

    private static string Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text)
        ? text : node?.ToJsonString() ?? "";

    private static string Summary(JsonNode? value, string inherited) => value switch
    {
        JsonObject range => JsonNode.DeepEquals(range["Min"], range["Max"])
            ? Text(range["Min"]) : $"{Text(range["Min"])} – {Text(range["Max"])}",
        JsonArray curve => $"{curve.Count} keys · Edit",
        _ => inherited
    };

    private static string Inherited(string key, Client.MirScenes.SpellVfxAssets.EmitterSample sample)
    {
        var p = sample.Particle;
        return key switch
        {
            "StartSize" => sample.MinimumSize == sample.MaximumSize ? $"Authored · {sample.MinimumSize}px" : $"Authored · {sample.MinimumSize}–{sample.MaximumSize}px",
            "StartLifetime" when sample.Persistent => $"Authored · {p.Duration / 1000f:g3}s",
            "StartColour" => $"#{p.Colour.R:X2}{p.Colour.G:X2}{p.Colour.B:X2}",
            "StartOpacity" => $"Authored · {p.Colour.A / 255f:g3}",
            "SpeedRange" => "Authored · 0–200px/s",
            "RenderMode" => sample.HasRibbon ? "Authored ribbon" : "Authored billboard",
            "Sprite" => $"Authored · {p.Shape}",
            "SortingLayer" => $"Authored · {p.Layer}",
            "Enabled" => "On (authored)",
            _ => "Authored"
        };
    }

    private static string Label(string key) => key switch
    {
        "StartLifetime" => "Start Lifetime (s)", "StartDelay" => "Start Delay (ms)",
        "StartSize" => "Start Size (px)", "StartRotation" => "Start Rotation (°)",
        "StartSpeed" => "Start Speed (px/s)", "VelocityLimit" => "Limit (px/s)",
        "ShapeSpread" => "Radius / Half Width (px)", "ShapeHeight" => "Vertical Radius (px)",
        "ShapeRotation" => "Shape Rotation (°)", "TextureFrame" => "Frame Override", "SubEmitterCount" => "Burst Count",
        "SubEmitterSprite" => "Particle Sprite",
        _ => System.Text.RegularExpressions.Regex.Replace(key, "(?<=[a-z])(?=[A-Z])", " ")
    };
}
