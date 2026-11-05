using Client.MirGraphics;
using Client.MirObjects;

namespace Client.MirScenes;

public sealed partial class MapControl
{
    private const int MaxAnimatedMapLights = 64;
    private const int MaxShapedSpellLights = 64;
    private static int ShapedLightPixelBudget => (int)Math.Clamp((long)Settings.ScreenWidth * Settings.ScreenHeight / 2, 120_000, 900_000);
    private readonly List<AnimatedMapLight> _animatedMapLights = new(MaxAnimatedMapLights);
    private readonly HashSet<Point> _shapedMapLightCells = new();
    private long _lightPreviewUntil, _projectionPreviewUntil;
    private LightSource _lightPreviewSource;
    private bool LightPreviewActive => CMain.Time < _lightPreviewUntil;
    internal bool ProjectionPreviewActive => CMain.Time < _projectionPreviewUntil;

    private readonly record struct AnimatedMapLight(MLibrary Library, int Frame, Point Position,
        Point Cell, Color Tint, float Strength, int RadialIndex);

    internal void PreviewLight(LightSource source)
    {
        _lightPreviewSource = source;
        _lightPreviewUntil = CMain.Time + 4500;
        TextureValid = false;
    }

    internal void PreviewProjection()
    {
        _projectionPreviewUntil = CMain.Time + 4500;
        TextureValid = false;
    }

    private void CaptureAnimatedMapLight(MLibrary library, int frame, Point position, bool offset,
                                        int x, int y, bool additive, bool originLinked)
    {
        var emitter = GraphicsTuning.Light(LightSource.MapAnimation);
        if (Settings.MapLightIntensityPercent == 0 || emitter.Intensity == 0 ||
            _animatedMapLights.Count >= MaxAnimatedMapLights) return;
        bool authored = MapLightEncoding.TryDecode(M2CellInfo[x, y].Light, DXManager.LightSizes.Length - 1,
            out int lightIndex, out Color tint);
        if (!authored && !additive) return;

        Size size = library.GetSize(frame);
        if (offset) position.Offset(library.GetOffSet(frame));
        position = GraphicsTuning.LightPosition(LightSource.MapAnimation, position, originLinked);
        var bounds = ShapedLightPass.ScaleFrame(position, size.Width, size.Height, emitter.Radius.Current);
        bounds.Inflate(emitter.Blur.Current, emitter.Blur.Current);
        if (size.IsEmpty || !bounds.IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight))) return;

        _animatedMapLights.Add(new AnimatedMapLight(library, frame, position, new Point(x, y),
            tint, authored ? 1f : 0.55f, authored ? lightIndex : 3));
    }

    private void DrawAnimatedMapLights(float intensity, bool blind)
    {
        _shapedMapLightCells.Clear();
        int pixelBudget = ShapedLightPixelBudget;
        foreach (var light in _animatedMapLights)
        {
            Color tint = TintLight(light.Tint, LightSource.MapAnimation, blind);
            var emitter = GraphicsTuning.Light(LightSource.MapAnimation);
            if (emitter.Shape.Current != 0 || !light.Library.DrawLightShape(light.Frame, light.Position, tint, false,
                intensity * light.Strength, ref pixelBudget, LightSource.MapAnimation))
            {
                Size size = light.Library.GetSize(light.Frame);
                Point center = new(light.Position.X + size.Width / 2, light.Position.Y + size.Height / 2);
                DrawRadialLight(light.RadialIndex, center, ScaleLight(tint, intensity * light.Strength), LightSource.MapAnimation);
            }
            _shapedMapLightCells.Add(light.Cell);
        }
    }

    private bool HasSpellLights() => Settings.Effect && Settings.SpellLightIntensityPercent > 0 &&
        (!_spellParticles.Active.IsEmpty || Effects.Count != 0 || Objects.Values.Any(ob =>
            ob is SpellObject { Light: > 0 } || ob.Effects.Count != 0));

    private bool HasDaylightEmitters()
    {
        if (GraphicsTuning.DayGlow.Current == 0) return false;
        if (Objects.Values.Any(actor => actor.Light > 0 && actor is not SpellObject &&
            (!actor.Dead || actor == User) && GraphicsTuning.Light(ActorLightSource(actor)).Intensity > 0)) return true;
        if (Settings.MapLightIntensityPercent == 0 || GraphicsTuning.Light(LightSource.Map).Intensity == 0) return false;
        UpdateStaticLights();
        return _staticLightCache.Items.Count != 0;
    }

    internal void PreviewAmbient() => _ambientLighting.Reset();

    private static Color ApplyAmbientDarkness(Color colour, LightSetting setting)
    {
        int shade = setting switch
        {
            LightSetting.Dawn => GraphicsTuning.Dawn.Current,
            LightSetting.Evening => GraphicsTuning.Evening.Current,
            LightSetting.Night => GraphicsTuning.Night.Current,
            _ => 100
        };
        float intensity = shade * Settings.AmbientDarknessPercent / 10000f;
        float daylight = setting is LightSetting.Day or LightSetting.Normal ? GraphicsTuning.DayBrightness.Current / 100f : 1;
        return WarmLight(Color.FromArgb(255, Channel(colour.R), Channel(colour.G), Channel(colour.B)),
            GraphicsTuning.LightWarmth.Current);

        int Channel(int value) => (int)MathF.Round((255 - (255 - value) * intensity) * daylight);
    }

    private void DrawLights(Color ambient, Texture mapColour)
    {
        if (DXManager.Lights.Count == 0) return;
        if (DXManager.LightTexture == null || DXManager.LightTexture.Disposed)
        {
            DXManager.LightTexture = new Texture(DXManager.Device, Settings.ScreenWidth, Settings.ScreenHeight, true);
            DXManager.LightSurface = DXManager.LightTexture.GetSurface();
        }

        bool blind = User.Poison.HasFlag(PoisonType.Blindness);
        if (blind) ambient = GetBlindLight(ambient);
        Surface world = DXManager.CurrentSurface;
        DXManager.SetSurface(DXManager.LightSurface);
        DXManager.Device.Clear(Color.Black); // Bloom sees emission without ambient.
        DXManager.SetBlend(true);
        ShapedLightPass.Reset();
        try
        {
            DrawObjectLights(Settings.Effect ? Settings.SpellLightIntensityPercent / 100f *
                GraphicsTuning.Light(LightSource.Spell).Intensity : 0, blind);
            if (Settings.MapLightIntensityPercent > 0)
                DrawMapLights(Settings.MapLightIntensityPercent / 100f, blind);
            if (LightPreviewActive)
            {
                var preview = GraphicsTuning.Light(_lightPreviewSource);
                Color tint = TintLight(Color.White, _lightPreviewSource, blind);
                Point point = ActorLightPosition(User, _lightPreviewSource);
                int budget = ShapedLightPixelBudget;
                bool shown = (_lightPreviewSource is LightSource.MapAnimation or LightSource.Spell) &&
                    preview.Shape.Current == 0 && User.BodyLibrary != null &&
                    User.BodyLibrary.DrawLightShape(User.DrawFrame + User.ArmourOffSet, point, tint, true,
                        preview.Intensity, ref budget, _lightPreviewSource);
                if (!shown) DrawRadialLight(3, point, ScaleLight(tint, preview.Intensity), _lightPreviewSource);
            }
            ShapedLightPass.Composite();
        }
        finally
        {
            DXManager.SetBlend(false);
            DXManager.SetSurface(world);
        }

        ShaderEffect previousShader = DXManager.Device.PixelShader;
        if (ambient.ToArgb() != Color.White.ToArgb())
        {
            var device = DXManager.Device;
            Texture previous = device.AuxiliaryTexture;
            device.Blend = SpriteBlendMode.Multiply;
            device.PixelShader = ShaderEffect.LightComposite;
            try
            {
                device.AuxiliaryTexture = mapColour;
                device.SetPixelShaderConstant(0, [new(ambient.R / 255f, ambient.G / 255f, ambient.B / 255f,
                    mapColour == null ? 0 : GraphicsTuning.MapColour.Current / 100f),
                    new(GraphicsTuning.NightFloor.Current / 100f, GraphicsTuning.LightWarmth.Current * 0.003f, 0, 0)]);
                DXManager.Draw(DXManager.LightTexture, null, Vector3.Zero, Color.White);
            }
            finally { device.AuxiliaryTexture = previous; }
        }
        // Multiplication cannot illuminate white ambient. Add a restrained contribution in daylight.
        int daylight = blind ? 0 : (int)MathF.Round(Math.Min(ambient.R, Math.Min(ambient.G, ambient.B)) *
            Math.Max(GraphicsTuning.DayGlow.Current, LightPreviewActive ? 42 : 0) / 100f);
        if (daylight > 0)
        {
            DXManager.Device.PixelShader = ShaderEffect.Artwork;
            DXManager.Device.Blend = SpriteBlendMode.Additive;
            DXManager.Draw(DXManager.LightTexture, null, Vector3.Zero, Color.FromArgb(daylight, Color.White));
        }
        DXManager.Sprite.End();
        DXManager.Device.PixelShader = previousShader;
        DXManager.Sprite.Begin();
    }

    private void DrawObjectLights(float spellIntensity, bool blind)
    {
        int shaped = 0;
        int pixelBudget = ShapedLightPixelBudget;
        foreach (MapObject actor in Objects.Values)
        {
            if (actor.Light > 0)
            {
                if (actor is SpellObject spell)
                {
                    if (spellIntensity > 0)
                    {
                        Color tint = TintLight(actor.LightColour, LightSource.Spell, blind);
                        if (GraphicsTuning.Light(LightSource.Spell).Shape.Current == 0 &&
                            shaped < MaxShapedSpellLights && spell.DrawLightMask(tint, spellIntensity, ref pixelBudget)) shaped++;
                        else DrawRadialLight(actor.Light % 15, spell.LightPosition, ScaleLight(tint, spellIntensity), LightSource.Spell);
                    }
                }
                else if (!actor.Dead || actor == User)
                {
                    Color tint = actor.LightColour;
                    LightSource source = ActorLightSource(actor);
                    float intensity = GraphicsTuning.Light(source).Intensity;
                    if (actor.Race is ObjectType.Player or ObjectType.Hero or ObjectType.Merchant)
                    {
                        int brightness = actor.Race == ObjectType.Merchant ? 120 : Math.Min(255, 60 * (actor.Light / 15 + 1));
                        tint = Color.FromArgb(brightness, brightness, brightness);
                    }
                    if (intensity > 0)
                        DrawRadialLight(actor.Light % 15, ActorLightPosition(actor, source),
                            ScaleLight(TintLight(tint, source, blind), intensity), source);
                }
            }

            if (spellIntensity <= 0) continue;
            foreach (Effect effect in actor.Effects) DrawEffectLight(effect);
        }

        if (spellIntensity <= 0) return;
        foreach (Effect effect in Effects) DrawEffectLight(effect);
        DrawSpellParticleLights(spellIntensity * (blind ? 0.08f : 0.22f));

        void DrawEffectLight(Effect effect)
        {
            if (!effect.Blend || effect.Light <= 0 || CMain.Time < effect.Start ||
                effect.CurrentFrame < 0 || effect.CurrentFrame >= effect.Count) return;
            Color tint = TintLight(effect.LightColour, LightSource.Spell, blind);
            if (GraphicsTuning.Light(LightSource.Spell).Shape.Current == 0 &&
                shaped < MaxShapedSpellLights && effect.DrawLightMask(tint, spellIntensity, ref pixelBudget)) shaped++;
            else DrawRadialLight(effect.Light, effect.LightPosition,
                ScaleLight(tint, spellIntensity), LightSource.Spell);
        }
    }

    private void DrawMapLights(float intensity, bool blind)
    {
        DrawAnimatedMapLights(intensity * GraphicsTuning.Light(LightSource.MapAnimation).Intensity, blind);
        intensity *= GraphicsTuning.Light(LightSource.Map).Intensity;
        if (intensity == 0) return;
        UpdateStaticLights();
        Point centre = User.Movement;
        foreach (CachedMapLight emitter in _staticLightCache.Items)
        {
            if (_shapedMapLightCells.Contains(new Point(emitter.X, emitter.Y)) ||
                !_staticLightCache.InViewport(emitter, centre.X, centre.Y, ViewRangeX, ViewRangeY)) continue;

            Color tint = TintLight(emitter.Colour, LightSource.Map, blind);
            DrawRadialLight(emitter.Index, GraphicsTuning.LightPosition(LightSource.Map, MapLightOrigin(emitter)),
                ScaleLight(tint, intensity), LightSource.Map);
        }
    }

    private Point MapLightOrigin(CachedMapLight emitter)
    {
        Point origin = ToMouseLocation(new Point(emitter.X, emitter.Y));
        origin.Offset(10, 32);
        CellInfo cell = M2CellInfo[emitter.X, emitter.Y];
        int frame = (cell.FrontImage & 0x7FFF) - 1;
        if (cell.FrontAnimationFrame > 0 && frame >= 0 && (uint)cell.FrontIndex < (uint)Libraries.MapLibs.Length &&
            Libraries.MapLibs[cell.FrontIndex] is { } library)
            origin.Offset(library.GetOffSet(frame));
        return origin;
    }

    private static Color ScaleLight(Color tint, float intensity) => Color.FromArgb(tint.A,
        Math.Min(255, (int)MathF.Round(tint.R * intensity)), Math.Min(255, (int)MathF.Round(tint.G * intensity)),
        Math.Min(255, (int)MathF.Round(tint.B * intensity)));

    private static LightSource ActorLightSource(MapObject actor) => actor.Race switch
    {
        ObjectType.Merchant => LightSource.Npc,
        ObjectType.Player or ObjectType.Hero => HasTorch(actor) ? LightSource.Torch : LightSource.Player,
        _ => LightSource.Monster
    };

    private static Point ActorLightPosition(MapObject actor, LightSource source) =>
        GraphicsTuning.LightPosition(source, actor.DrawLocation, GraphicsTuning.Offset(GraphicsTuning.Layer(actor.Race)).Linked);

    private static bool HasTorch(MapObject actor) => actor is UserObject user
        ? user.Equipment[(int)EquipmentSlot.Torch]?.Info is { Light: > 0 } torch && torch.Light == actor.Light
        : actor.Light >= 15;

    private static Color WarmLight(Color tint, int warmth)
    {
        float bias = warmth / 100f;
        return Color.FromArgb(tint.A, (int)Math.Clamp(tint.R * (1 + 0.30f * bias), 0, 255),
            tint.G, (int)Math.Clamp(tint.B * (1 - 0.30f * bias), 0, 255));
    }

    private Color TintLight(Color tint, LightSource source, bool blind)
    {
        var light = GraphicsTuning.Light(source);
        if (light.CustomColour.Current != 0) tint = light.Colour;
        return blind ? GetBlindLight(tint) : WarmLight(tint, light.Warmth.Current);
    }

    private static void DrawRadialLight(int index, Point point, Color tint, LightSource source)
    {
        index = Math.Min(index, DXManager.Lights.Count - 1);
        Texture texture = DXManager.Lights[index];
        if (texture == null || texture.Disposed) return;
        var tuning = GraphicsTuning.Light(source);
        int radius = tuning.Radius.Current;
        int width = Math.Max(1, (int)MathF.Round(texture.Width * radius / 100f));
        int height = Math.Max(1, (int)MathF.Round(texture.Height * radius / 100f));
        Point authored = DXManager.LightSizes[index];
        var bounds = new Rectangle(
            point.X - authored.X / 2 - CellWidth / 2 + (texture.Width - width) / 2,
            point.Y - authored.Y / 2 - CellHeight / 2 - 5 + (texture.Height - height) / 2,
            width, height);
        var sourceRect = new Rectangle(0, 0, texture.Width, texture.Height);
        if (tuning.Shape.Current != 2)
        {
            DXManager.Sprite.Draw(texture, sourceRect, bounds, RendererDevice.ToVector(tint));
            return;
        }

        var device = DXManager.Device;
        ShaderEffect shader = device.PixelShader;
        Span<Vector4> constants = stackalloc Vector4[RendererDevice.EffectConstantCount];
        device.CopyPixelShaderConstants(constants);
        float direction = tuning.Direction.Current * MathF.PI / 180f;
        float beam = tuning.Beam.Current * MathF.PI / 360f;
        device.PixelShader = ShaderEffect.ConeLight;
        try
        {
            device.SetPixelShaderConstant(0, [new(MathF.Cos(direction), MathF.Sin(direction),
                MathF.Cos(beam), MathF.Cos(beam * 0.7f))]);
            DXManager.Sprite.Draw(texture, sourceRect, bounds, RendererDevice.ToVector(tint));
        }
        finally
        {
            device.PixelShader = shader;
            device.SetPixelShaderConstant(0, constants);
        }
    }
}
