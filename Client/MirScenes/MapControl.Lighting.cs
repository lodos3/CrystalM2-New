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

    private readonly record struct AnimatedMapLight(MLibrary Library, int Frame, Point Position,
        Point Cell, Color Tint, float Strength);

    private void CaptureAnimatedMapLight(MLibrary library, int frame, Point position, bool offset,
                                        int x, int y, bool additive)
    {
        if (Settings.MapLightIntensityPercent == 0 || _animatedMapLights.Count >= MaxAnimatedMapLights) return;
        bool authored = MapLightEncoding.TryDecode(M2CellInfo[x, y].Light, DXManager.LightSizes.Length - 1,
            out _, out Color tint);
        if (!authored && !additive) return;

        Size size = library.GetSize(frame);
        if (offset) position.Offset(library.GetOffSet(frame));
        var bounds = new Rectangle(position, size);
        bounds.Inflate(6, 6);
        if (size.IsEmpty || !bounds.IntersectsWith(new Rectangle(0, 0, Settings.ScreenWidth, Settings.ScreenHeight))) return;

        _animatedMapLights.Add(new AnimatedMapLight(library, frame, position, new Point(x, y),
            tint, authored ? 1f : 0.55f));
    }

    private void DrawAnimatedMapLights(float intensity, bool blind)
    {
        _shapedMapLightCells.Clear();
        int pixelBudget = ShapedLightPixelBudget;
        foreach (var light in _animatedMapLights)
        {
            Color tint = blind ? GetBlindLight(light.Tint) : light.Tint;
            if (light.Library.DrawLightShape(light.Frame, light.Position, tint, false, intensity * light.Strength, ref pixelBudget))
                _shapedMapLightCells.Add(light.Cell);
        }
    }

    private bool HasSpellLights() => Settings.Effect && Settings.SpellLightIntensityPercent > 0 &&
        (!_spellParticles.Active.IsEmpty || Effects.Count != 0 || Objects.Values.Any(ob =>
            ob is SpellObject { Light: > 0 } || ob.Effects.Count != 0));

    private static Color ApplyAmbientDarkness(Color colour)
    {
        float intensity = Settings.AmbientDarknessPercent / 100f;
        return Color.FromArgb(255, 255 - (int)MathF.Round((255 - colour.R) * intensity),
            255 - (int)MathF.Round((255 - colour.G) * intensity), 255 - (int)MathF.Round((255 - colour.B) * intensity));
    }

    private void DrawLights(Color ambient)
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
        try
        {
            DrawObjectLights(Settings.Effect ? Settings.SpellLightIntensityPercent / 100f : 0, blind);
            if (Settings.MapLightIntensityPercent > 0)
                DrawMapLights(Settings.MapLightIntensityPercent / 100f, blind);
        }
        finally
        {
            DXManager.SetBlend(false);
            DXManager.SetSurface(world);
        }

        ShaderEffect previousShader = DXManager.Device.PixelShader;
        if (ambient.ToArgb() != Color.White.ToArgb())
        {
            DXManager.Device.Blend = SpriteBlendMode.Multiply;
            DXManager.Device.PixelShader = ShaderEffect.LightComposite;
            DXManager.Device.SetPixelShaderConstant(0, [new Vector4(ambient.R / 255f, ambient.G / 255f, ambient.B / 255f, 1f)]);
            DXManager.Draw(DXManager.LightTexture, null, Vector3.Zero, Color.White);
        }
        // Multiplication cannot illuminate white ambient. Add a restrained contribution in daylight.
        int daylight = blind ? 0 : Math.Min(ambient.R, Math.Min(ambient.G, ambient.B)) * 36 / 255;
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
                        Color tint = blind ? GetBlindLight(actor.LightColour) : actor.LightColour;
                        if (shaped < MaxShapedSpellLights && spell.DrawLightMask(tint, spellIntensity, ref pixelBudget)) shaped++;
                        else DrawRadialLight(actor.Light % 15, actor.DrawLocation, ScaleLight(tint, spellIntensity));
                    }
                }
                else if (!actor.Dead || actor == User)
                {
                    Color tint = actor.LightColour;
                    if (actor.Race is ObjectType.Player or ObjectType.Merchant)
                    {
                        int brightness = actor.Race == ObjectType.Merchant ? 120 : Math.Min(255, 60 * (actor.Light / 15 + 1));
                        tint = Color.FromArgb(brightness, brightness, brightness);
                    }
                    DrawRadialLight(actor.Light % 15, actor.DrawLocation, blind ? GetBlindLight(tint) : tint);
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
            Color tint = blind ? GetBlindLight(effect.LightColour) : effect.LightColour;
            if (shaped < MaxShapedSpellLights && effect.DrawLightMask(tint, spellIntensity, ref pixelBudget)) shaped++;
            else DrawRadialLight(effect.Light, effect.DrawLocation, ScaleLight(tint, spellIntensity));
        }
    }

    private void DrawMapLights(float intensity, bool blind)
    {
        DrawAnimatedMapLights(intensity, blind);
        UpdateStaticLights();
        Point centre = User.Movement;
        foreach (CachedMapLight emitter in _staticLightCache.Items)
        {
            if (_shapedMapLightCells.Contains(new Point(emitter.X, emitter.Y)) ||
                !_staticLightCache.InViewport(emitter, centre.X, centre.Y, ViewRangeX, ViewRangeY)) continue;

            Point origin = new((emitter.X + OffSetX - centre.X) * CellWidth + User.OffSetMove.X + 10,
                (emitter.Y + OffSetY - centre.Y) * CellHeight + User.OffSetMove.Y + 32);
            CellInfo cell = M2CellInfo[emitter.X, emitter.Y];
            int frame = (cell.FrontImage & 0x7FFF) - 1;
            if (cell.FrontAnimationFrame > 0 && frame >= 0 && (uint)cell.FrontIndex < (uint)Libraries.MapLibs.Length &&
                Libraries.MapLibs[cell.FrontIndex] is { } library)
                origin.Offset(library.GetOffSet(frame));
            Color tint = blind ? GetBlindLight(emitter.Colour) : emitter.Colour;
            DrawRadialLight(emitter.Index, origin, ScaleLight(tint, intensity));
        }
    }

    private static Color ScaleLight(Color tint, float intensity) => Color.FromArgb(tint.A,
        Math.Min(255, (int)MathF.Round(tint.R * intensity)), Math.Min(255, (int)MathF.Round(tint.G * intensity)),
        Math.Min(255, (int)MathF.Round(tint.B * intensity)));

    private static void DrawRadialLight(int index, Point point, Color tint)
    {
        index = Math.Min(index, DXManager.Lights.Count - 1);
        Texture texture = DXManager.Lights[index];
        if (texture == null || texture.Disposed) return;
        point.Offset(-(DXManager.LightSizes[index].X / 2) - CellWidth / 2,
            -(DXManager.LightSizes[index].Y / 2) - CellHeight / 2 - 5);
        DXManager.Draw(texture, null, new Vector3(point.X, point.Y, 0), tint);
    }
}
