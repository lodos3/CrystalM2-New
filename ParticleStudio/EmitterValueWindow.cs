using Panel = System.Windows.Controls.Panel;
using Brushes = System.Windows.Media.Brushes;
using Rectangle = System.Windows.Shapes.Rectangle;
using ColorConverter = System.Windows.Media.ColorConverter;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace CrystalM2.ParticleStudio;

internal sealed class EmitterValueWindow : Window
{
    private readonly StackPanel _body = new() { Margin = new Thickness(16) };
    private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly Func<JsonNode?> _read;
    public JsonNode? Value { get; private set; }

    public EmitterValueWindow(string label, string key, JsonNode? configured, string inherited)
    {
        Title = label;
        Width = 460;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Content = _body;
        _body.Children.Add(new TextBlock { Text = inherited, Foreground = Brushes.DimGray, Margin = new Thickness(0, 0, 0, 8) });

        _read = key.StartsWith("Start", StringComparison.Ordinal) || key == "SpeedRange"
            ? Range(key, configured, inherited)
            : Curve(key, configured as JsonArray);

        _body.Children.Add(_error);
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(12, 5, 12, 5) };
        var apply = new Button { Content = "Apply", IsDefault = true, Margin = new Thickness(6, 0, 0, 0), Padding = cancel.Padding };
        apply.Click += (_, _) =>
        {
            try { Value = _read(); DialogResult = true; }
            catch (FormatException error) { _error.Text = error.Message; }
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        _body.Children.Add(buttons);
    }

    private Func<JsonNode?> Range(string key, JsonNode? configured, string inherited)
    {
        var modes = new ComboBox { ItemsSource = new[] { "Authored", "Constant", "Random between two constants" } };
        var fields = new StackPanel();
        string hint = inherited.StartsWith("Authored · ", StringComparison.Ordinal) ? inherited["Authored · ".Length..] : "";
        string[] values = hint.TrimEnd('p', 'x', 's', '/').Split('–');
        string fallback = float.TryParse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _) ? values[0]
            : key == "StartLifetime" ? "1" : key == "StartSize" ? "8" : "0";
        var minimum = Field(fields, "Value / Minimum", configured?["Min"]?.ToJsonString() ?? fallback);
        var maximum = Field(fields, "Maximum", configured?["Max"]?.ToJsonString() ?? (values.Length == 2 ? values[1] : fallback));
        modes.SelectionChanged += (_, _) =>
        {
            fields.IsEnabled = modes.SelectedIndex != 0;
            maximum.IsEnabled = modes.SelectedIndex == 2;
        };
        modes.SelectedIndex = configured == null ? 0 : JsonNode.DeepEquals(configured["Min"], configured["Max"]) ? 1 : 2;
        _body.Children.Add(modes);
        _body.Children.Add(fields);
        return () =>
        {
            if (modes.SelectedIndex == 0) return null;
            float min = Parse(minimum.Text);
            float max = modes.SelectedIndex == 1 ? min : Parse(maximum.Text);
            if (min > max) throw new FormatException("Maximum must be at least minimum.");
            return new JsonObject { ["Min"] = min, ["Max"] = max };
        };
    }

    private Func<JsonNode?> Curve(string key, JsonArray? configured)
    {
        bool gradient = key.StartsWith("Colour", StringComparison.Ordinal);
        string valueName = gradient ? "Colour" : "Value";
        string first = gradient ? "#FFFFFF" : key.StartsWith("Rotation", StringComparison.Ordinal) ? "0" : "1";
        var keys = new ObservableCollection<KeyRow>(configured?.Select(item => new KeyRow
        {
            Time = item!["Time"]!.ToJsonString(),
            Value = gradient ? item[valueName]!.GetValue<string>() : item[valueName]!.ToJsonString()
        }) ?? [new KeyRow { Time = "0", Value = first }, new KeyRow { Time = "1", Value = first }]);
        var graph = new Canvas { Height = 78, Width = 390, Background = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 8), ClipToBounds = true };
        var grid = new DataGrid { ItemsSource = keys, AutoGenerateColumns = false, Height = 190, CanUserAddRows = false };
        grid.Columns.Add(new DataGridTextColumn { Header = "Time (0–1)", Binding = new Binding(nameof(KeyRow.Time)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = gradient ? "Colour (#RRGGBB)" : "Value", Binding = new Binding(nameof(KeyRow.Value)),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.CellEditEnding += (_, _) => Dispatcher.BeginInvoke(new Action(UpdateGraph));
        keys.CollectionChanged += (_, _) => UpdateGraph();
        _body.Children.Add(graph);
        _body.Children.Add(grid);

        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 0) };
        var add = new Button { Content = "Add key", Margin = new Thickness(0, 0, 6, 0) };
        add.Click += (_, _) => keys.Add(new KeyRow { Time = "0.5", Value = first });
        var remove = new Button { Content = "Remove key", Margin = new Thickness(0, 0, 6, 0) };
        remove.Click += (_, _) => { if (grid.SelectedItem is KeyRow row) keys.Remove(row); };
        var authored = new Button { Content = "Use authored" };
        authored.Click += (_, _) => { Value = null; DialogResult = true; };
        buttons.Children.Add(add); buttons.Children.Add(remove); buttons.Children.Add(authored);
        _body.Children.Add(buttons);
        UpdateGraph();
        return Read;

        JsonArray Read()
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            if (keys.Count is < 1 or > 32) throw new FormatException("Add between 1 and 32 keys.");
            var result = new JsonArray();
            float previous = -1;
            foreach (var (time, row) in keys.Select(row => (Time: Parse(row.Time), Row: row)).OrderBy(pair => pair.Time))
            {
                if (time <= previous || time is < 0 or > 1)
                    throw new FormatException("Each key needs a unique time between 0 and 1.");
                var value = gradient ? JsonValue.Create(Colour(row.Value)) : JsonValue.Create(Parse(row.Value));
                result.Add(new JsonObject { ["Time"] = time, [valueName] = value });
                previous = time;
            }
            return result;
        }

        void UpdateGraph()
        {
            graph.Children.Clear();
            try
            {
                var ordered = keys.Select(row => (Time: Parse(row.Time), row.Value)).OrderBy(pair => pair.Time).ToArray();
                if (ordered.Length == 0) return;
                if (gradient)
                {
                    var brush = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 0) };
                    foreach (var (time, colour) in ordered)
                        brush.GradientStops.Add(new GradientStop((System.Windows.Media.Color)ColorConverter.ConvertFromString(Colour(colour)), time));
                    graph.Children.Add(new Rectangle { Width = graph.Width, Height = graph.Height, Fill = brush });
                }
                else
                {
                    var data = ordered.Select(pair => (pair.Time, Value: Parse(pair.Value))).ToArray();
                    float min = Math.Min(0, data.Min(pair => pair.Value)), max = Math.Max(1, data.Max(pair => pair.Value));
                    var line = new Polyline { Stroke = Brushes.Gold, StrokeThickness = 2 };
                    foreach (var (time, value) in data)
                    {
                        var point = new System.Windows.Point(3 + time * (graph.Width - 6), 3 + (1 - (value - min) / (max - min)) * (graph.Height - 6));
                        line.Points.Add(point);
                        var dot = new Ellipse { Width = 6, Height = 6, Fill = Brushes.Gold };
                        Canvas.SetLeft(dot, point.X - 3); Canvas.SetTop(dot, point.Y - 3);
                        graph.Children.Add(dot);
                    }
                    graph.Children.Add(line);
                }
            }
            catch (FormatException) { }
        }
    }

    private static TextBox Field(Panel panel, string label, string value)
    {
        panel.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 4) });
        var box = new TextBox { Text = value, Padding = new Thickness(5) };
        panel.Children.Add(box);
        return box;
    }

    private static float Parse(string text) =>
        float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && float.IsFinite(value)
        ? value : throw new FormatException("Enter a finite number using a decimal point.");

    private static string Colour(string text)
    {
        string colour = text.Trim();
        if (colour.Length != 7 || colour[0] != '#' || !colour.Skip(1).All(Uri.IsHexDigit))
            throw new FormatException("Use colours in #RRGGBB format.");
        return colour;
    }

    public sealed class KeyRow
    {
        public string Time { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
