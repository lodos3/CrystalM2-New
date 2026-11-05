using Client.MirControls;
using Client.MirGraphics;
using Client.MirSounds;

namespace Client.MirScenes.Dialogs;

internal sealed class GraphicsOptionsPage : MirControl
{
    private readonly MirTextBox _libraries;
    private readonly MirLabel _libraryStatus;

    internal GraphicsOptionsPage()
    {
        Size = new Size(488, 284);
        Visible = false;

        AddHeading("Post processing", 0);
        AddToggle("Post processing", 0, 25, Settings.PostProcessing, value => Settings.PostProcessing = value);
        AddToggle("Environment atmosphere", 0, 49, Settings.EnvironmentAtmosphere, value => Settings.EnvironmentAtmosphere = value);
        AddSlider("Atmosphere", 0, 73, 0, 100, "%", () => Settings.AtmosphereIntensityPercent,
            value => Settings.AtmosphereIntensityPercent = value);
        AddSlider("Vignette", 0, 97, 0, 100, "%", () => Settings.VignetteIntensityPercent,
            value => Settings.VignetteIntensityPercent = value);
        AddToggle("Fog layers", 0, 121, Settings.EnvironmentFogLayers, value => Settings.EnvironmentFogLayers = value);
        AddToggle("Wet surfaces", 0, 145, Settings.AtmosphereWetSurfaces, value => Settings.AtmosphereWetSurfaces = value);
        AddToggle("Lightning", 0, 169, Settings.AtmosphereLightning, value => Settings.AtmosphereLightning = value);
        AddToggle("Light bloom", 0, 193, Settings.LightingBloom, value => Settings.LightingBloom = value);
        AddSlider("Bloom strength", 0, 217, 0, 200, "%", () => Settings.BloomIntensityPercent,
            value => Settings.BloomIntensityPercent = value);
        AddSlider("Bloom divisor", 0, 241, 2, 8, "", () => Settings.BloomDivisor, value => Settings.BloomDivisor = value,
            "Lower values use a larger blur buffer.");

        const int right = 253;
        AddHeading("Projected shadows", right);
        AddToggle("Projected shadows", right, 25, Settings.DynamicShadows, value => Settings.DynamicShadows = value);
        AddToggle("Follow nearby lights", right, 49, Settings.ShadowsFollowLights, value => Settings.ShadowsFollowLights = value);
        AddSlider("Opacity", right, 73, 0, 100, "%", () => Settings.ShadowOpacityPercent,
            value => Settings.ShadowOpacityPercent = value);
        AddSlider("Length", right, 97, 50, 175, "%", () => Settings.ShadowLengthPercent,
            value => Settings.ShadowLengthPercent = value);
        AddSlider("Softness", right, 121, 0, 8, "", () => Settings.ShadowSoftness,
            value => Settings.ShadowSoftness = value, "Blur radius in source sprite pixels.");
        AddToggle("Block shadow lights through walls", right, 145, Settings.MapLightOcclusion,
            value => Settings.MapLightOcclusion = value);

        MirPanel.AddCaption(this, "Shadow-free sprite libraries", new Rectangle(right, 171, 230, 18), muted: false);
        _libraries = new MirTextBox
        {
            Parent = this, Location = new Point(right, 195), Size = new Size(228, 20), Border = true,
            Text = Settings.ProjectedShadowLibraries, Hint = "Exact relative .Lib paths separated by semicolons."
        };
        _libraries.TextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            ApplyLibraries();
            e.Handled = e.SuppressKeyPress = true;
        };
        AddButton("Apply libraries", new Rectangle(right, 223, 94, 22), ApplyLibraries);
        _libraryStatus = MirPanel.AddCaption(this, "", new Rectangle(right, 250, 230, 16));
        UpdateLibraryStatus();
        MirPanel.AddCaption(this, "Use relative paths separated by semicolons.", new Rectangle(right, 266, 235, 16));
    }

    private void AddHeading(string text, int x) =>
        MirPanel.AddCaption(this, text, new Rectangle(x, 0, 235, 22), muted: false);

    private void AddToggle(string label, int x, int y, bool initial, Action<bool> write)
    {
        var checkBox = new MirCheckBox
        {
            Parent = this, Location = new Point(x, y + 2), LabelText = label, Checked = initial
        };
        checkBox.Click += (_, _) =>
        {
            write(checkBox.Checked);
            ApplyPreview();
            Settings.SaveGraphicsEffects();
        };
    }

    private void AddSlider(string label, int x, int y, int minimum, int maximum, string unit,
                           Func<int> read, Action<int> write, string hint = null)
    {
        MirPanel.AddCaption(this, label, new Rectangle(x, y, 85, 20));
        var value = MirPanel.AddCaption(this, $"{read()}{unit}", new Rectangle(x + 191, y, 44, 20), muted: false);
        void SetValue(int next)
        {
            next = Math.Clamp(next, minimum, maximum);
            if (next == read()) return;
            write(next);
            value.Text = $"{next}{unit}";
            ApplyPreview();
        }
        void Step(int delta)
        {
            SetValue(read() + delta);
            Settings.SaveGraphicsEffects();
        }

        AddButton("−", new Rectangle(x + 87, y, 19, 20), () => Step(-1)).Hint = $"Decrease {label}";
        AddButton("+", new Rectangle(x + 169, y, 19, 20), () => Step(1)).Hint = $"Increase {label}";
        var slider = new GraphicsSlider(fraction => SetValue(minimum + (int)Math.Round(fraction * (maximum - minimum))))
        {
            Parent = this, Location = new Point(x + 108, y), Size = new Size(59, 20), Hint = hint ?? label
        };
        slider.BeforeDraw += (_, _) =>
        {
            double fraction = (double)(read() - minimum) / (maximum - minimum);
            Rectangle bounds = slider.DisplayRectangle;
            DXManager.Device.Ui.Progress(new Rectangle(bounds.X, bounds.Y + 6, bounds.Width, 8),
                fraction, DarkFantasyPalette.Experience);
            DXManager.Device.Ui.Draw(UiSurface.ScrollThumb,
                new Rectangle(bounds.X + (int)((bounds.Width - 8) * fraction), bounds.Y + 2, 8, 16));
        };
        slider.MouseWheel += (_, e) => Step(Math.Sign(e.Delta));
    }

    private MirButton AddButton(string text, Rectangle bounds, Action action)
    {
        var button = new MirButton
        {
            Parent = this, Location = bounds.Location, Size = bounds.Size, AutoSize = false,
            Skin = UiSurface.Button, Text = text, Sound = SoundList.ButtonA
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void ApplyLibraries()
    {
        string paths = _libraries.Text.Trim();
        if (paths.IndexOfAny(['=', '\r', '\n']) >= 0)
        {
            _libraryStatus.Text = "Use one line without '=' characters.";
            return;
        }
        Settings.SetProjectedShadowLibraries(paths);
        ApplyPreview();
        Settings.SaveGraphicsEffects();
        UpdateLibraryStatus();
    }

    private void UpdateLibraryStatus()
    {
        int count = Settings.ProjectedShadowLibraries.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
        _libraryStatus.Text = count == 0 ? "No libraries approved for shadows." : $"{count} library paths configured.";
    }

    private void ApplyPreview()
    {
        if (GameScene.Scene?.MapControl is { } map) map.TextureValid = false;
        Redraw();
    }

    private sealed class GraphicsSlider(Action<double> change) : MirControl
    {
        public override void OnMouseDown(MouseEventArgs e)
        {
            if (!Enabled || e.Button != MouseButtons.Left) return;
            base.OnMouseDown(e);
            Moving = true;
            UpdateValue(e.X);
        }

        public override void OnMouseMove(MouseEventArgs e)
        {
            if (Moving) UpdateValue(e.X);
            else base.OnMouseMove(e);
        }

        public override void OnMouseUp(MouseEventArgs e)
        {
            if (Moving) Settings.SaveGraphicsEffects();
            base.OnMouseUp(e);
        }

        protected override void OnVisibleChanged()
        {
            if (!Visible && Moving) Settings.SaveGraphicsEffects();
            base.OnVisibleChanged();
        }

        private void UpdateValue(int x) => change(Math.Clamp((x - DisplayLocation.X) / (double)(Size.Width - 1), 0, 1));
    }
}
