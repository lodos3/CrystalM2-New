using Client.MirControls;
using Client.MirGraphics;
using Client.MirSounds;

namespace Client.MirScenes.Dialogs;

internal sealed class GraphicsOptionsPage : MirControl
{
    private const int Right = 253;
    private static readonly string[] BlendModes = ["Authored", "Alpha", "Additive", "Screen", "Multiply", "Subtract", "Max channel", "Min channel"];
    private enum Preview { World, Floor, Ambient, Baked, Textures, WeatherSource }

    private MirControl _content;
    private MirControl _selectedPage;
    private MirButton _selectedTab;
    private MirTextBox _libraries;
    private MirLabel _libraryStatus;

    internal GraphicsOptionsPage()
    {
        Size = new Size(488, 391);
        Visible = false;

        AddPage("Offsets", 0, BuildOffsets);
        AddPage("Lighting", 1, BuildLighting);
        AddPage("Post", 2, BuildPost);
        AddPage("Blending", 3, BuildBlending);
        AddPage("Shadows", 4, BuildShadows);
        AddPage("Textures", 5, BuildTextures);
        AddPage("Weather", 6, BuildWeather);

        MirPanel.AddCaption(this, "Drag, scroll, or use +/−. Click a value to reset it.", new Rectangle(0, 366, 488, 20));
    }

    private void AddPage(string title, int index, Action build)
    {
        var page = new MirControl { Parent = this, Location = new Point(0, 32), Size = new Size(488, 328), Visible = false };
        var tab = AddButton(this, title, new Rectangle(index * 70, 0, 66, 24), () => { });
        tab.Click += (_, _) => SelectPage();
        _content = page;
        build();
        if (index == 0) SelectPage();

        void SelectPage()
        {
            if (_selectedPage != null) _selectedPage.Visible = false;
            if (_selectedTab != null) _selectedTab.Selected = false;
            _selectedPage = page;
            _selectedTab = tab;
            page.Visible = true;
            tab.Selected = true;
        }
    }

    private void BuildOffsets()
    {
        MirControl page = _content;
        var individual = new MirControl { Parent = page, Size = page.Size };
        var links = new MirControl { Parent = page, Size = page.Size, Visible = false };
        _content = individual;
        Caption("Horizontal offset", 0, 0);
        Caption("Vertical offset", Right, 0, 109);
        AddButton(individual, "Link offsets", new Rectangle(374, 0, 114, 20), () => Select(true));
        for (int i = 0; i < GraphicsTuning.Layers.Length; i++)
        {
            GraphicsLayer layer = GraphicsTuning.Layers[i];
            var offset = GraphicsTuning.Offset(layer);
            Preview preview = layer is GraphicsLayer.Ground or GraphicsLayer.Middle or GraphicsLayer.Front ? Preview.Floor : Preview.World;
            AddValue(LayerName(layer), 0, 20 + i * 23, offset.X, "px", preview);
            AddValue(LayerName(layer), Right, 20 + i * 23, offset.Y, "px", preview);
        }
        _content = links;
        Caption("Select offsets to move together", 0, 0);
        AddButton(links, "Individual offsets", new Rectangle(374, 0, 114, 20), () => Select(false));
        AddValue("Group X", 0, 25, GraphicsTuning.GroupOffset.X, "px", Preview.Floor);
        AddValue("Group Y", Right, 25, GraphicsTuning.GroupOffset.Y, "px", Preview.Floor);

        var members = GraphicsTuning.Layers.Select(layer => (LayerName(layer), GraphicsTuning.Offset(layer)))
            .Concat(GraphicsTuning.LightSources.Select(source => (LightName(source) + " light", GraphicsTuning.Light(source).Offset)))
            .Append(("All lights", GraphicsTuning.AllLights))
            .Append(("Projected shadows", GraphicsTuning.ProjectedOffset))
            .ToArray();
        for (int i = 0; i < members.Length; i++)
        {
            var (name, position) = members[i];
            AddToggle(name, i < 11 ? 0 : Right, 54 + (i % 11) * 21,
                () => position.Linked, linked => position.Link.Current = linked ? 1 : 0, Preview.Floor);
        }
        AddToggle("Show offset guides", 0, 292, () => GraphicsTuning.OffsetGuides.Current != 0,
            visible => GraphicsTuning.OffsetGuides.Current = visible ? 1 : 0);
        Caption("Cyan sprites · Gold lights · Violet map", Right, 292);
        _content = page;

        void Select(bool group)
        {
            individual.Visible = !group;
            links.Visible = group;
        }
    }

    private void BuildLighting()
    {
        Caption("Ambient and global lighting", 0, 0);
        AddSlider("Ambient shade", 0, 22, 0, 100, "%", () => Settings.AmbientDarknessPercent,
            value => Settings.AmbientDarknessPercent = value, 100, Preview.Ambient);
        AddValue("Day brightness", 0, 46, GraphicsTuning.DayBrightness, "%", Preview.Ambient);
        AddValue("Dawn shade", 0, 70, GraphicsTuning.Dawn, "%", Preview.Ambient);
        AddValue("Evening shade", 0, 94, GraphicsTuning.Evening, "%", Preview.Ambient);
        AddValue("Night shade", 0, 118, GraphicsTuning.Night, "%", Preview.Ambient);
        AddValue("Warmth", 0, 142, GraphicsTuning.LightWarmth, "%", Preview.Ambient);
        AddValue("Daytime glow", 0, 166, GraphicsTuning.DayGlow);
        AddValue("All lights X", 0, 190, GraphicsTuning.AllLights.X, "px");
        AddValue("All lights Y", 0, 214, GraphicsTuning.AllLights.Y, "px");
        AddSlider("All spell light", 0, 238, 0, 200, "%", () => Settings.SpellLightIntensityPercent,
            value => Settings.SpellLightIntensityPercent = value, 100);
        AddSlider("All map light", 0, 262, 0, 200, "%", () => Settings.MapLightIntensityPercent,
            value => Settings.MapLightIntensityPercent = value, 100);

        Caption("Light sources", Right, 0);
        MirControl page = _content, selected = null;
        MirButton selectedButton = null;
        for (int i = 0; i < GraphicsTuning.LightSources.Length; i++)
        {
            LightSource source = GraphicsTuning.LightSources[i];
            string name = LightName(source);
            var emitterPanel = new MirControl { Parent = page, Location = new Point(Right, 77), Size = new Size(235, 242), Visible = false };
            var button = AddButton(page, name, new Rectangle(Right + (i % 4) * 59, 22 + (i / 4) * 25, 55, 22), () => { });
            button.Click += (_, _) => SelectEmitter();
            var placement = new MirControl { Parent = emitterPanel, Size = emitterPanel.Size };
            var optics = new MirControl { Parent = emitterPanel, Size = emitterPanel.Size, Visible = false };
            bool shaped = GraphicsTuning.IsEffectLight(source);
            _content = placement;
            Caption(name + " light", 0, 0);
            var light = GraphicsTuning.Light(source);
            AddValue(shaped ? "Intensity" : "Strength", 0, 22, light.Strength);
            AddValue(shaped ? "Size" : "Radius", 0, 46, light.Radius);
            if (shaped)
            {
                AddValue("Offset X", 0, 70, light.Offset.X, "px");
                AddValue("Offset Y", 0, 94, light.Offset.Y, "px");
                AddValue("Gaussian blur", 0, 118, light.Blur, "px", hint: "Screen-space blur on the authored shape.");
                AddValue("Glow radius", 0, 142, light.GlowRadius, "px", hint: "Radius of the soft floor light around the source.");
                AddValue("Glow intensity", 0, 166, light.GlowStrength);
                AddChoice("Projection", 0, 190, light.Shape, ["Authored", "Radial", "Cone"]);
            }
            else
            {
                AddValue("Warmth", 0, 70, light.Warmth);
                AddValue("Offset X", 0, 94, light.Offset.X, "px");
                AddValue("Offset Y", 0, 118, light.Offset.Y, "px");
                AddChoice("Projection", 0, 142, light.Shape, ["Authored", "Radial", "Cone"]);
                AddValue("Direction", 0, 166, light.Direction, "°", hint: "Cone direction. Zero points right.");
                AddValue("Beam width", 0, 190, light.Beam, "°");
            }
            AddButton(placement, "Test light", new Rectangle(0, 216, 96, 21),
                () => GameScene.Scene?.MapControl?.PreviewLight(source));
            AddButton(placement, shaped ? "Colour and beam" : "Colour", new Rectangle(107, 216, 122, 21), () =>
            {
                placement.Visible = false;
                optics.Visible = true;
            });

            _content = optics;
            Caption(name + " colour" + (shaped ? " and beam" : ""), 0, 0);
            if (shaped)
            {
                AddValue("Warmth", 0, 24, light.Warmth);
                AddValue("Direction", 0, 47, light.Direction, "°", hint: "Cone direction. Zero points right.");
                AddValue("Beam width", 0, 70, light.Beam, "°");
            }
            int colourRow = shaped ? 94 : 54;
            AddToggle("Override emitter colour", 0, colourRow, () => light.CustomColour.Current != 0,
                enabled => light.CustomColour.Current = enabled ? 1 : 0);
            AddValue("Red", 0, colourRow + 24, light.Red, "");
            AddValue("Green", 0, colourRow + 47, light.Green, "");
            AddValue("Blue", 0, colourRow + 70, light.Blue, "");
            var swatch = new MirControl { Parent = optics, Location = new Point(0, colourRow + 96), Size = new Size(229, 18), NotControl = true };
            swatch.BeforeDraw += (_, _) => DXManager.Device.Ui.Fill(swatch.DisplayRectangle, light.Colour);
            AddButton(optics, "Test light", new Rectangle(0, 216, 96, 21),
                () => GameScene.Scene?.MapControl?.PreviewLight(source));
            AddButton(optics, "Back to light", new Rectangle(107, 216, 122, 21), () =>
            {
                optics.Visible = false;
                placement.Visible = true;
            });
            if (i == 0) SelectEmitter();

            void SelectEmitter()
            {
                if (selected != null) selected.Visible = false;
                if (selectedButton != null) selectedButton.Selected = false;
                selected = emitterPanel;
                selectedButton = button;
                selected.Visible = true;
                button.Selected = true;
            }
        }
        _content = page;
        AddValue("Map tint", 0, 285, GraphicsTuning.MapColour, "%", Preview.Ambient,
            "Blend dark ambient light towards the average colour of the visible map tiles.");
        AddValue("Night floor", 0, 307, GraphicsTuning.NightFloor, "%", Preview.Ambient,
            "Minimum brightness for the sampled night tint. Set Map tint to 0 for authored ambient lighting.");
    }

    private void BuildPost()
    {
        Caption("World colour and tone", 0, 0);
        AddToggle("Post processing", 0, 23, () => Settings.PostProcessing, value => Settings.PostProcessing = value);
        AddValue("Exposure", 0, 47, GraphicsTuning.Exposure);
        AddValue("Contrast", 0, 71, GraphicsTuning.Contrast);
        AddValue("Saturation", 0, 95, GraphicsTuning.Saturation);
        AddValue("Gamma", 0, 119, GraphicsTuning.Gamma);
        AddChoice("Colour grade", 0, 143, GraphicsTuning.Grade, ["Neutral", "Warm", "Cool", "Sepia", "Jade"]);
        AddChoice("Tone curve", 0, 167, GraphicsTuning.Tone, ["Linear", "Soft highlight", "S-curve"]);
        AddSlider("Vignette", 0, 191, 0, 100, "%", () => Settings.VignetteIntensityPercent,
            value => Settings.VignetteIntensityPercent = value, 100);
        AddChoice("World shader", 0, 215, GraphicsTuning.PostShader,
            ["Standard", "Monochrome", "Posterize", "Sharpen", "Soft focus", "Chromatic", "Film grain", "Invert", "Scanlines"]);
        AddChoice("Death effect", 0, 239, GraphicsTuning.DeathLook, ["Grayscale", "Post settings", "Original"]);
        AddValue("Shader effect", 0, 263, GraphicsTuning.PostShaderStrength);

        Caption("Atmosphere and effects", Right, 0);
        AddToggle("Environment atmosphere", Right, 23, () => Settings.EnvironmentAtmosphere, value => Settings.EnvironmentAtmosphere = value);
        AddSlider("Atmosphere", Right, 47, 0, 100, "%", () => Settings.AtmosphereIntensityPercent,
            value => Settings.AtmosphereIntensityPercent = value, 100);
        AddValue("Black lift", Right, 71, GraphicsTuning.BlackLift);
        AddValue("Highlights", Right, 95, GraphicsTuning.Highlights);
        AddValue("Vibrance", Right, 119, GraphicsTuning.Vibrance);
        AddToggle("Light bloom", Right, 167, () => Settings.LightingBloom, value => Settings.LightingBloom = value);
        AddSlider("Bloom strength", Right, 191, 0, 200, "%", () => Settings.BloomIntensityPercent,
            value => Settings.BloomIntensityPercent = value, 100);
        AddValue("Bloom cutoff", Right, 215, GraphicsTuning.BloomThreshold);
        AddSlider("Bloom divisor", Right, 239, 2, 8, "", () => Settings.BloomDivisor,
            value => Settings.BloomDivisor = value, 4, hint: "Lower values use a larger blur buffer.");
    }

    private void BuildWeather()
    {
        Caption("Weather source and client preview", 0, 0);
        AddChoice("Weather source", 0, 22, GraphicsTuning.WeatherSource, ["Client", "Server legacy"], Preview.WeatherSource);
        AddWeatherToggle("Rain", 50, GraphicsTuning.WeatherRain);
        AddWeatherToggle("Snow", 74, GraphicsTuning.WeatherSnow);
        AddWeatherToggle("Ground fog", 98, GraphicsTuning.WeatherFog);
        AddWeatherToggle("Thunderstorm", 122, GraphicsTuning.WeatherStorm);
        Caption("Rain and snow build up, then fade.", 0, 158);
        Caption("Client weather can combine effects.", 0, 180);
        Caption("Server legacy uses the map's settings.", 0, 202);

        Caption("Weather intensity", Right, 0);
        AddValue("Precipitation", Right, 24, GraphicsTuning.Precipitation);
        AddValue("Ground water", Right, 48, GraphicsTuning.SurfaceWater);
        AddValue("Snow cover", Right, 72, GraphicsTuning.SurfaceSnow);
        AddValue("Fog density", Right, 96, GraphicsTuning.FogDensity);
        AddValue("Wind", Right, 120, GraphicsTuning.WeatherWind);
        Caption("Server legacy effects", Right, 163);
        AddToggle("Legacy fog layers", Right, 186, () => Settings.EnvironmentFogLayers, value => Settings.EnvironmentFogLayers = value);
        AddToggle("Legacy wet surfaces", Right, 210, () => Settings.AtmosphereWetSurfaces, value => Settings.AtmosphereWetSurfaces = value);
        AddToggle("Legacy lightning", Right, 234, () => Settings.AtmosphereLightning, value => Settings.AtmosphereLightning = value);

        void AddWeatherToggle(string label, int y, GraphicsTuning.Value value) =>
            AddToggle(label, 0, y, () => value.Current != 0, enabled => value.Current = enabled ? 1 : 0);
    }

    private void BuildBlending()
    {
        Caption("Actors and effects", 0, 0);
        Caption("Terrain and render states", Right, 0);
        GraphicsLayer[] left = [GraphicsLayer.Player, GraphicsLayer.Monster, GraphicsLayer.Npc,
            GraphicsLayer.Item, GraphicsLayer.Decoration, GraphicsLayer.Spell, GraphicsLayer.Particles, GraphicsLayer.Weather];
        GraphicsLayer[] right = [GraphicsLayer.Ground, GraphicsLayer.Middle, GraphicsLayer.Front,
            GraphicsLayer.MapAnimation, GraphicsLayer.Background];
        for (int i = 0; i < left.Length; i++)
            AddChoice(LayerName(left[i]), 0, 22 + i * 24, GraphicsTuning.Blend(left[i]), BlendModes);
        for (int i = 0; i < right.Length; i++)
            AddChoice(LayerName(right[i]), Right, 22 + i * 24, GraphicsTuning.Blend(right[i]), BlendModes, Preview.Floor);

        AddChoice("Corpses", Right, 142, GraphicsTuning.CorpseBlend, BlendModes);
        AddChoice("Target highlight", Right, 166, GraphicsTuning.HighlightBlend, BlendModes);
        AddChoice("Ground particles", Right, 190, GraphicsTuning.GroundParticleBlend, BlendModes);
        Caption("Spell compositing", 0, 213);
        AddChoice("Glow blend", Right, 213, GraphicsTuning.SpellGlowBlend, ["Additive", "Screen"]);
        AddChoice("Spell passes", 0, 235, GraphicsTuning.SpellComposite, ["Authored", "Black key", "Black key + glow", "Glow only"]);
        AddSlider("Spell opacity", 0, 257, 0, 200, "%", () => Settings.SpellBlendIntensityPercent,
            value => Settings.SpellBlendIntensityPercent = value, 100);
        AddValue("Black cutoff", 0, 279, GraphicsTuning.SpellBlackCutoff);
        AddValue("Black feather", 0, 301, GraphicsTuning.SpellBlackFeather);
        AddValue("Spell exposure", Right, 235, GraphicsTuning.SpellExposure);
        AddValue("Spell colours", Right, 257, GraphicsTuning.SpellSaturation);
        AddValue("Glow cutoff", Right, 279, GraphicsTuning.SpellGlowCutoff);
        AddValue("Glow power", Right, 301, GraphicsTuning.SpellGlowStrength);
    }

    private void BuildShadows()
    {
        Caption("Baked checker shadows", 0, 0);
        AddToggle("Repair checker shadows", 0, 22, () => GraphicsTuning.BakedEnabled.Current != 0,
            value => GraphicsTuning.BakedEnabled.Current = value ? 1 : 0, Preview.Baked);
        AddToggle("Preserve map tile joins", 0, 46, () => GraphicsTuning.BakedJoin.Current != 0,
            value => GraphicsTuning.BakedJoin.Current = value ? 1 : 0, Preview.Baked);
        AddValue("Dark threshold", 0, 70, GraphicsTuning.BakedDarkness, "", Preview.Baked, "Higher values detect lighter checker dots.");
        AddValue("Min dot alpha", 0, 94, GraphicsTuning.BakedMinAlpha, "", Preview.Baked);
        AddValue("Colour match", 0, 118, GraphicsTuning.BakedTolerance, "", Preview.Baked,
            "Higher values allow more colour variation between checker dots.");
        AddValue("Blur radius", 0, 142, GraphicsTuning.BakedRadius, "px", Preview.Baked);
        AddValue("Opacity", 0, 166, GraphicsTuning.BakedOpacity, "%", Preview.Baked);
        Caption("Baked settings apply when you release.", 0, 192);
        AddChoice("Cast from", 0, 213, GraphicsTuning.ProjectedPolicy, ["Approved libraries", "Auto detect"]);
        AddButton(_content, "Test shadow", new Rectangle(0, 235, 96, 18),
            () => GameScene.Scene?.MapControl?.PreviewProjection());

        Caption("Projected shadows", Right, 0);
        AddToggle("Projected shadows", Right, 22, () => Settings.DynamicShadows, value => Settings.DynamicShadows = value);
        AddToggle("Follow nearby lights", Right, 46, () => Settings.ShadowsFollowLights, value => Settings.ShadowsFollowLights = value);
        AddToggle("Walls block shadow lights", Right, 70, () => Settings.MapLightOcclusion, value => Settings.MapLightOcclusion = value);
        AddSlider("Opacity", Right, 94, 0, 100, "%", () => Settings.ShadowOpacityPercent, value => Settings.ShadowOpacityPercent = value, 36);
        AddSlider("Length", Right, 118, 50, 175, "%", () => Settings.ShadowLengthPercent, value => Settings.ShadowLengthPercent = value, 100);
        AddSlider("Blur radius", Right, 142, 0, 8, "px", () => Settings.ShadowSoftness, value => Settings.ShadowSoftness = value, 0);
        AddValue("Width", Right, 166, GraphicsTuning.ProjectedWidth);
        AddValue("Direction", Right, 190, GraphicsTuning.ProjectedSlope);
        AddValue("Offset X", Right, 214, GraphicsTuning.ProjectedOffset.X, "px");
        AddValue("Offset Y", Right, 238, GraphicsTuning.ProjectedOffset.Y, "px");

        Caption("Approved shadow-free sprite libraries", 0, 253, 248);
        _libraries = new MirTextBox
        {
            Parent = _content, Location = new Point(0, 277), Size = new Size(366, 21), Border = true,
            Text = Settings.ProjectedShadowLibraries, Hint = "Relative .Lib paths separated by semicolons."
        };
        _libraries.TextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            ApplyLibraries();
            e.Handled = e.SuppressKeyPress = true;
        };
        AddButton(_content, "Apply libraries", new Rectangle(374, 277, 114, 21), ApplyLibraries);
        _libraryStatus = Caption("", 0, 302, 488);
        UpdateLibraryStatus();
    }

    private void BuildTextures()
    {
        Caption("Live GPU texture scaling", 0, 0, 488);
        AddChoice("Texture filter", 0, 27, GraphicsTuning.TextureFilter,
            ["Pixel", "Smooth", "Edge-aware 2x", "HD", "Bicubic"], Preview.Textures);
        AddValue("Filter mix", 0, 53, GraphicsTuning.TextureSmoothing, "%", Preview.Textures,
            "Blend between the original pixels and reconstructed texture.");
        AddValue("Edge match", Right, 27, GraphicsTuning.TextureEdgeMatch, "%", Preview.Textures,
            "How similar colours must be to join an edge in Edge-aware or HD.");
        AddValue("Detail", Right, 53, GraphicsTuning.TextureDetail, "%", Preview.Textures,
            "Sharpen texture detail without sharpening transparent alpha.");
        Caption("Pixel reference", 0, 94);
        Caption("Selected GPU filter", Right, 94);
        var reference = new MirControl { Parent = _content, Location = new Point(0, 117), Size = new Size(235, 166), NotControl = true };
        var filtered = new MirControl { Parent = _content, Location = new Point(Right, 117), Size = new Size(235, 166), NotControl = true };
        reference.BeforeDraw += (_, _) => DrawTexturePreview(reference.DisplayRectangle, true);
        filtered.BeforeDraw += (_, _) => DrawTexturePreview(filtered.DisplayRectangle, false);
        Caption("Preview uses your current character sprite. Filtering also applies to UI and effects.", 0, 292, 488);
    }

    private static void DrawTexturePreview(Rectangle bounds, bool reference)
    {
        RendererDevice device = DXManager.Device;
        device.Ui.Fill(bounds, Color.FromArgb(255, 22, 25, 30));
        var actor = GameScene.User;
        if (actor?.BodyLibrary == null) return;
        TextureFilterMode filter = device.TextureFilter;
        Vector3 tuning = device.TextureAdjustment;
        try
        {
            if (reference)
            {
                device.TextureFilter = TextureFilterMode.Pixel;
                device.TextureAdjustment = new Vector3(0.08f, 0, 0);
            }
            bounds.Inflate(-5, -5);
            actor.BodyLibrary.DrawTexturePreview(actor.DrawFrame + actor.ArmourOffSet, bounds);
        }
        finally
        {
            device.TextureFilter = filter;
            device.TextureAdjustment = tuning;
        }
    }

    private static string LayerName(GraphicsLayer layer) => layer switch
    {
        GraphicsLayer.Npc => "NPC",
        GraphicsLayer.MapAnimation => "Map animation",
        _ => layer.ToString()
    };

    private static string LightName(LightSource source) => source switch
    {
        LightSource.Npc => "NPC",
        LightSource.MapAnimation => "Animated",
        _ => source.ToString()
    };

    private MirLabel Caption(string text, int x, int y, int width = 235) =>
        MirPanel.AddCaption(_content, text, new Rectangle(x, y, width, 20), muted: false);

    private void AddToggle(string label, int x, int y, Func<bool> read, Action<bool> write, Preview preview = Preview.World)
    {
        var box = new MirCheckBox { Parent = _content, Location = new Point(x, y + 2), LabelText = label, Checked = read() };
        box.Click += (_, _) =>
        {
            write(box.Checked);
            ApplyPreview(preview);
            Settings.SaveGraphicsEffects();
        };
    }

    private void AddChoice(string label, int x, int y, GraphicsTuning.Value value, string[] names, Preview preview = Preview.World)
    {
        Caption(label, x, y, 105);
        MirButton button = null;
        button = AddButton(_content, names[value.Current], new Rectangle(x + 111, y, 118, 20), () =>
        {
            value.Current = (value.Current + 1) % names.Length;
            button.Text = names[value.Current];
            ApplyPreview(preview);
            Settings.SaveGraphicsEffects();
        });
    }

    private void AddValue(string label, int x, int y, GraphicsTuning.Value value, string unit = "%",
                          Preview preview = Preview.World, string hint = null) =>
        AddSlider(label, x, y, value.Minimum, value.Maximum, unit, () => value.Current,
            next => value.Current = next, value.Default, preview, hint);

    private void AddSlider(string label, int x, int y, int minimum, int maximum, string unit,
                           Func<int> read, Action<int> write, int reset, Preview preview = Preview.World, string hint = null)
    {
        MirPanel.AddCaption(_content, label, new Rectangle(x, y, 85, 20));
        bool changed = false;
        var value = AddButton(_content, Format(), new Rectangle(x + 191, y, 44, 20), () => { });
        value.Hint = "Reset " + label;
        value.Click += (_, _) => SetAndSave(reset);
        string stepHint = maximum - minimum >= 1000 ? " Shift steps by 10. Ctrl steps by 50." : "";
        AddButton(_content, "−", new Rectangle(x + 87, y, 19, 20), () => SetAndSave(read() - Step())).Hint = "Decrease " + label + stepHint;
        AddButton(_content, "+", new Rectangle(x + 169, y, 19, 20), () => SetAndSave(read() + Step())).Hint = "Increase " + label + stepHint;

        var slider = new GraphicsSlider(fraction => SetValue(minimum + (int)Math.Round(fraction * (maximum - minimum))), Commit)
        {
            Parent = _content, Location = new Point(x + 108, y), Size = new Size(59, 20), Hint = hint ?? label
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
        slider.MouseWheel += (_, e) => SetAndSave(read() + Math.Sign(e.Delta) * Step());

        string Format() => $"{read()}{unit}";
        int Step()
        {
            if (maximum - minimum < 1000) return 1;
            Keys modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Control) != 0) return 50;
            return (modifiers & Keys.Shift) != 0 ? 10 : 1;
        }

        void SetValue(int next)
        {
            next = Math.Clamp(next, minimum, maximum);
            if (next == read()) return;
            write(next);
            changed = true;
            value.Text = Format();
            if (preview == Preview.Baked) Redraw();
            else ApplyPreview(preview);
        }

        void SetAndSave(int next)
        {
            SetValue(next);
            Commit();
        }

        void Commit()
        {
            if (!changed) return;
            changed = false;
            if (preview == Preview.Baked) ApplyPreview(preview);
            Settings.SaveGraphicsEffects();
        }
    }

    private static MirButton AddButton(MirControl parent, string text, Rectangle bounds, Action action)
    {
        var button = new MirButton
        {
            Parent = parent, Location = bounds.Location, Size = bounds.Size, AutoSize = false,
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
        ApplyPreview(Preview.World);
        Settings.SaveGraphicsEffects();
        UpdateLibraryStatus();
    }

    private void UpdateLibraryStatus()
    {
        int count = Settings.ProjectedShadowLibraries.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length;
        _libraryStatus.Text = count == 0 ? "No libraries approved for projected shadows." : $"{count} library paths configured.";
    }

    private void ApplyPreview(Preview preview)
    {
        if (preview == Preview.Baked) GraphicsTuning.ApplyBakedSettings();
        if (preview == Preview.Textures) DXManager.ApplyTextureSettings();
        if (GameScene.Scene?.MapControl is { } map)
        {
            if (preview is Preview.Floor or Preview.Baked or Preview.Textures) map.FloorValid = false;
            if (preview == Preview.Ambient) map.PreviewAmbient();
            if (preview == Preview.WeatherSource) map.RefreshWeatherSource();
            map.TextureValid = false;
        }
        Redraw();
    }

    private sealed class GraphicsSlider(Action<double> change, Action commit) : MirControl
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
            if (Moving) commit();
            base.OnMouseUp(e);
        }

        protected override void OnVisibleChanged()
        {
            if (!Visible && Moving) commit();
            base.OnVisibleChanged();
        }

        private void UpdateValue(int x) => change(Math.Clamp((x - DisplayLocation.X) / (double)(Size.Width - 1), 0, 1));
    }
}
