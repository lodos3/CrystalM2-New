using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using Binding = System.Windows.Data.Binding;
using CrystalM2.Rendering.Core;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;

namespace CrystalM2.ParticleStudio;

internal sealed class ParticleValueEditor : Window
{
    private readonly StackPanel _body = new() { Margin = new Thickness(16) };
    private readonly TextBlock _error = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8) };
    private readonly Func<Dictionary<string, JsonNode>> _read;

    public IReadOnlyDictionary<string, JsonNode> Values { get; private set; } = new Dictionary<string, JsonNode>();

    public ParticleValueEditor(string label, string key, JsonNode value, (string Key, bool Fade, JsonNode Value)? pair)
    {
        Title = label;
        Width = 400;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Content = _body;

        if (pair is { } linked)
        {
            double start = value.GetValue<double>(), second = linked.Value.GetValue<double>();
            var first = Field(linked.Fade ? "Start opacity at 0%" : "Minimum", Number(start));
            var last = Field(linked.Fade ? "End opacity at 100%" : "Maximum",
                Number(linked.Fade ? start * (1 - second) : start + second));
            _body.Children.Add(new TextBlock
            {
                Text = linked.Fade ? "Linear fade over the effect stage." : "Particles sample between these two values.",
                Margin = new Thickness(0, 8, 0, 0)
            });
            _read = () =>
            {
                double a = Parse(first.Text), b = Parse(last.Text);
                if (linked.Fade ? a < 0 || a > 1 || b < 0 || b > a : b < a)
                    throw new FormatException(linked.Fade ? "Use 0 to 1 and an end opacity at or below the start." : "Maximum must be at least minimum.");
                return new() { [key] = JsonValue.Create(a)!, [linked.Key] = JsonValue.Create(linked.Fade ? a == 0 ? 0 : 1 - b / a : b - a)! };
            };
        }
        else if (value is JsonArray curve)
            _read = Curve(key, curve);
        else if (value.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            var choice = new ComboBox { ItemsSource = new[] { false, true }, SelectedItem = value.GetValue<bool>() };
            _body.Children.Add(choice);
            _read = () => new() { [key] = JsonValue.Create((bool)choice.SelectedItem)! };
        }
        else if (value.GetValueKind() == JsonValueKind.String && key.Contains("Shape") &&
            Enum.TryParse<SpellParticleShape>(value.GetValue<string>(), out _))
            _read = Select(key, value, Enum.GetNames<SpellParticleShape>());
        else if (value.GetValueKind() == JsonValueKind.String && key.Contains("Layer") &&
            Enum.TryParse<SpellParticleLayer>(value.GetValue<string>(), out _))
            _read = Select(key, value, Enum.GetNames<SpellParticleLayer>());
        else
        {
            bool text = value.GetValueKind() == JsonValueKind.String;
            var field = Field(label, text ? value.GetValue<string>() : value.ToJsonString());
            _read = () => new() { [key] = text ? JsonValue.Create(field.Text.Trim())! : JsonNode.Parse(field.Text)! };
        }

        _body.Children.Add(_error);
        var buttons = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(12, 5, 12, 5) };
        var apply = new Button { Content = "Apply", IsDefault = true, Margin = new Thickness(6, 0, 0, 0), Padding = cancel.Padding };
        apply.Click += (_, _) =>
        {
            try { Values = _read(); DialogResult = true; }
            catch (Exception error) when (error is FormatException or JsonException)
            {
                _error.Text = error.Message;
            }
        };
        buttons.Children.Add(cancel);
        buttons.Children.Add(apply);
        _body.Children.Add(buttons);
    }

    private Func<Dictionary<string, JsonNode>> Select(string key, JsonNode value, string[] choices)
    {
        var picker = new ComboBox { ItemsSource = choices, SelectedItem = value.GetValue<string>() };
        _body.Children.Add(picker);
        return () => new() { [key] = JsonValue.Create((string)picker.SelectedItem)! };
    }

    private Func<Dictionary<string, JsonNode>> Curve(string key, JsonArray source)
    {
        var stops = new System.Collections.ObjectModel.ObservableCollection<ColourStop>(source.Select(stop =>
            new ColourStop { Position = stop!["position"]!.ToJsonString(), Colour = stop["colour"]!.GetValue<string>() }));
        var grid = new DataGrid { ItemsSource = stops, AutoGenerateColumns = false, Height = 190,
            CanUserAddRows = true, CanUserDeleteRows = true };
        grid.Columns.Add(new DataGridTextColumn { Header = "Position (0–1)", Binding = new Binding(nameof(ColourStop.Position)) });
        grid.Columns.Add(new DataGridTextColumn { Header = "Colour (#RRGGBB)", Binding = new Binding(nameof(ColourStop.Colour)) });
        _body.Children.Add(grid);
        return () =>
        {
            grid.CommitEdit(DataGridEditingUnit.Cell, true);
            grid.CommitEdit(DataGridEditingUnit.Row, true);
            var curve = new JsonArray();
            double previous = -1;
            foreach (var stop in stops)
            {
                double position = Parse(stop.Position);
                if (position <= previous || position < 0 || position > 1)
                    throw new FormatException("Use increasing stop positions between 0 and 1.");
                string colour = stop.Colour.Trim();
                if (colour.Length != 7 || colour[0] != '#' || !colour.Skip(1).All(Uri.IsHexDigit))
                    throw new FormatException("Use colours in #RRGGBB format.");
                curve.Add(new JsonObject { ["position"] = position, ["colour"] = colour });
                previous = position;
            }
            if (curve.Count == 0) throw new FormatException("Add at least one colour stop.");
            return new() { [key] = curve };
        };
    }

    private TextBox Field(string label, string value)
    {
        _body.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 6, 0, 4) });
        var field = new TextBox { Text = value, Padding = new Thickness(5) };
        _body.Children.Add(field);
        return field;
    }

    private static string Number(double value) => value.ToString("G", CultureInfo.InvariantCulture);
    private static double Parse(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number) && double.IsFinite(number)
        ? number : throw new FormatException("Enter a finite number using a decimal point.");

    public sealed class ColourStop
    {
        public string Position { get; set; } = "";
        public string Colour { get; set; } = "";
    }
}
