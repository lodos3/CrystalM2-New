using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Client.MirControls;
using Client.MirScenes;

namespace Client.MirGraphics
{
    class DXManager
    {
        public static List<MImage> TextureList = new List<MImage>();
        // 16 MiB logical BGRA cap for lazily generated, checkerboard-repaired
        // shadow masks; ordinary sprite textures retain their existing policy.
        internal static readonly ShadowMaskBudget ShadowMasks =
            new ShadowMaskBudget(16L * 1024 * 1024);
        public static List<MirControl> ControlList = new List<MirControl>();

        public static RendererDevice Device;
        public static SpriteBatch Sprite;

        public static Surface CurrentSurface;
        public static Surface MainSurface;
        public static bool DeviceLost;
        public static float Opacity = 1F;
        public static bool Blending;
        public static float BlendingRate;
        public static BlendMode BlendingMode;


        public static Texture RadarTexture;
        public static List<Texture> Lights = new List<Texture>();
        public static Texture PoisonDotBackground;

        public static Texture FloorTexture, LightTexture;
        public static Surface FloorSurface, LightSurface;

        public const ShaderEffect GrayScalePixelShader = ShaderEffect.Grayscale;
        public const ShaderEffect NormalPixelShader = ShaderEffect.Normal;
        public const ShaderEffect MagicPixelShader = ShaderEffect.Magic;
        public const ShaderEffect PostProcessingPixelShader = ShaderEffect.Atmosphere;
        private static readonly Vector4[] PostProcessConstants =
        {
            new Vector4(1.035f, 0.22f, 1.0f, 0.80f),
            new Vector4(0.5f, 0.5f, 0.0f, 0.0f),
            new Vector4(1f, 1f, 1f, 0f),
            new Vector4(0f, 0.78f, 0.84f, 0.94f),
            new Vector4(0f, 0.78f, 1.38f, 0.095f),
            new Vector4(0f, 0.80f, 0.90f, 1.0f)
        };

        public static bool GrayScale;
        public static int ProjectedShadowsDrawn;
        public const int MaxProjectedShadowsPerMapDraw = 48;

        public static Point[] LightSizes =
        {
            new Point(125,95),
            new Point(205,156),
            new Point(285,217),
            new Point(365,277),
            new Point(445,338),
            new Point(525,399),
            new Point(605,460),
            new Point(685,521),
            new Point(765,581),
            new Point(845,642),
            new Point(925,703)
        };

        public static void Create()
        {
            Device = new RendererDevice(Program.Form.Handle, Settings.ScreenWidth, Settings.ScreenHeight);
            Sprite = Device.Sprite;
            LoadTextures();
            DeviceLost = false;
        }

        private static unsafe void LoadTextures()
        {
            MainSurface = Device.GetBackBuffer();
            CurrentSurface = MainSurface;
            Device.SetRenderTarget(MainSurface);

            if (RadarTexture == null || RadarTexture.Disposed)
            {
                RadarTexture = new Texture(Device, 2, 2);

                DataRectangle stream = RadarTexture.BeginUpload();
                using (Bitmap image = new Bitmap(2, 2, 8, PixelFormat.Format32bppArgb, stream.Data.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.Clear(Color.White);
                RadarTexture.EndUpload();
            }
            if (PoisonDotBackground == null || PoisonDotBackground.Disposed)
            {
                PoisonDotBackground = new Texture(Device, 5, 5);

                DataRectangle stream = PoisonDotBackground.BeginUpload();
                using (Bitmap image = new Bitmap(5, 5, 20, PixelFormat.Format32bppArgb, stream.Data.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.Clear(Color.White);
                PoisonDotBackground.EndUpload();
            }
            CreateLights();
        }

        private unsafe static void CreateLights()
        {

            for (int i = Lights.Count - 1; i >= 0; i--)
                Lights[i].Dispose();

            Lights.Clear();

            for (int i = 1; i < LightSizes.Length; i++)
            {
                // int width = 125 + (57 *i);
                //int height = 110 + (57 * i);
                int width = LightSizes[i].X;
                int height = LightSizes[i].Y;

                Texture light = new Texture(Device, width, height);

                DataRectangle stream = light.BeginUpload();
                using (Bitmap image = new Bitmap(width, height, width * 4, PixelFormat.Format32bppArgb, stream.Data.DataPointer))
                {
                    using (Graphics graphics = Graphics.FromImage(image))
                    {
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            //path.AddEllipse(new Rectangle(0, 0, width, height));
                            //using (PathGradientBrush brush = new PathGradientBrush(path))
                            //{
                            //    graphics.Clear(Color.FromArgb(0, 0, 0, 0));
                            //    brush.SurroundColors = new[] { Color.FromArgb(0, 255, 255, 255) };
                            //    brush.CenterColor = Color.FromArgb(255, 255, 255, 255);
                            //    graphics.FillPath(brush, path);
                            //    graphics.Save();
                            //}

                            path.AddEllipse(new Rectangle(0, 0, width, height));
                            using (PathGradientBrush brush = new PathGradientBrush(path))
                            {
                                Color[] blendColours = { Color.White,
                                                     Color.FromArgb(255,210,210,210),
                                                     Color.FromArgb(255,160,160,160),
                                                     Color.FromArgb(255,70,70,70),
                                                     Color.FromArgb(255,40,40,40),
                                                     Color.FromArgb(0,0,0,0)};

                                float[] radiusPositions = { 0f, .20f, .40f, .60f, .80f, 1.0f };

                                ColorBlend colourBlend = new ColorBlend();
                                colourBlend.Colors = blendColours;
                                colourBlend.Positions = radiusPositions;

                                graphics.Clear(Color.FromArgb(0, 0, 0, 0));
                                brush.InterpolationColors = colourBlend;
                                brush.SurroundColors = blendColours;
                                brush.CenterColor = Color.White;
                                graphics.FillPath(brush, path);
                                graphics.Save();
                            }
                        }
                    }
                }

                light.EndUpload();
                //light.Disposing += (o, e) => Lights.Remove(light);
                Lights.Add(light);
            }
        }

        public static void SetSurface(Surface surface)
        {
            if (CurrentSurface == surface)
                return;

            Sprite.Flush();
            CurrentSurface = surface;
            Device.SetRenderTarget(surface);
        }
        public static void SetGrayscale(bool value)
        {
            GrayScale = value;

            if (value == true)
            {
                if (Device.PixelShader == GrayScalePixelShader) return;
                Sprite.Flush();
                Device.PixelShader = GrayScalePixelShader;
            }
            else
            {
                if (Device.PixelShader == ShaderEffect.Artwork) return;
                Sprite.Flush();
                Device.PixelShader = ShaderEffect.Artwork;
            }
        }

        // One-sample map colour grading/vignette on the existing map texture.
        // It does not shade UI or create another full-screen render target.
        public static bool BeginPostProcessing(Color atmosphere, float gradeStrength, float fogStrength,
                                               AtmosphericWeatherFrame weatherFrame, Color hazeTint)
        {
            float intensity = AtmosphereIntensity.Scale(Settings.AtmosphereIntensityPercent);
            PostProcessConstants[0] = new Vector4(
                AtmosphereExposure.Brightness(weatherFrame.CloudCover, intensity),
                0.22f, 1f, 0.80f);
            PostProcessConstants[2] = new Vector4(atmosphere.R / 255f, atmosphere.G / 255f,
                                                   atmosphere.B / 255f,
                                                   Math.Clamp(gradeStrength * intensity, 0f, 0.4f));
            PostProcessConstants[3] = new Vector4(Math.Clamp(fogStrength * intensity, 0f, 0.2f),
                                                   hazeTint.R / 255f, hazeTint.G / 255f,
                                                   hazeTint.B / 255f);
            PostProcessConstants[4] = new Vector4(
                Settings.AtmosphereWetSurfaces ? Math.Clamp(weatherFrame.Wetness * intensity, 0f, 1f) : 0f,
                0.78f, 1.38f, 0.095f);
            PostProcessConstants[5] = new Vector4(
                Settings.AtmosphereLightning ? Math.Clamp(weatherFrame.LightningFlash * intensity, 0f, 0.12f) : 0f,
                0.80f, 0.90f, 1f);
            Sprite.Flush();
            Device.PixelShader = PostProcessingPixelShader;
            Device.SetPixelShaderConstant(0, PostProcessConstants);
            return true;
        }

        public static void EndPostProcessing()
        {
            Sprite.Flush();
            Device.PixelShader = ShaderEffect.Artwork;
        }

        // Project a frame silhouette from its ground contact point every redraw.
        // Dither repair uses a 2x-downsampled alpha texture and never changes art.
        public static void DrawProjectedShadow(Texture texture, Rectangle sourceRect,
                                                Point topLeft, int sourceHeight, bool halfResolution,
                                                float horizontal, float vertical)
        {
            if (texture == null || texture.Disposed || sourceHeight <= 0 ||
                ProjectedShadowsDrawn >= MaxProjectedShadowsPerMapDraw)
                return;

            Matrix original = Sprite.Transform;
            Sprite.Flush();
            try
            {
                float scale = halfResolution ? 2f : 1f;
                Matrix projection = Matrix.Identity;
                projection.M11 = scale;
                projection.M21 = -horizontal * scale;
                projection.M22 = -vertical * scale;
                projection.M41 = topLeft.X + horizontal * sourceHeight;
                projection.M42 = topLeft.Y + (1f + vertical) * sourceHeight;
                Sprite.Transform = projection;
                Draw(texture, sourceRect, Vector3.Zero, Color.FromArgb(92, 0, 0, 0));
                ProjectedShadowsDrawn++;
                Sprite.Flush();
            }
            finally
            {
                Sprite.Transform = original;
            }
        }

        public static void DrawOpaque(Texture texture, Rectangle? sourceRect, Vector3? position, Color color, float opacity)
        {
            color = Color.FromArgb((int)(Math.Clamp(opacity, 0f, 1f) * 255), color.R, color.G, color.B);
            Draw(texture, sourceRect, position, color);
        }

        public static void Draw(Texture texture, Rectangle? sourceRect, Vector3? position, Color color)
        {
            Sprite.Draw(texture, sourceRect, position, RendererDevice.ToVector(color));
            CMain.DPSCounter++;
        }

        public static void AttemptReset()
        {
            try { ResetDevice(); }
            catch (Exception ex) { CMain.SaveError("D3D11 recovery failed: " + ex); }
        }

        public static void ResetDevice()
        {
            Size size = Program.Form.ClientSize;
            if (size.Width <= 0 || size.Height <= 0) return;
            Settings.ScreenWidth = size.Width;
            Settings.ScreenHeight = size.Height;
            DeviceLost = true;
            bool removed = Device == null || Device.IsRemoved;
            CleanUp();
            if (removed)
            {
                Device?.Dispose();
                Device = null;
                Create();
            }
            else
            {
                Device.Resize(size.Width, size.Height);
                LoadTextures();
                Opacity = 1f;
                Device.Opacity = 1f;
                Blending = false;
                Device.Blend = SpriteBlendMode.Alpha;
                Device.PixelShader = ShaderEffect.Artwork;
            }
            DeviceLost = false;
        }

        public static void AttemptRecovery()
        {
            Device?.DiscardPending();
            if (Device != null && !Device.IsRemoved && MainSurface != null)
            {
                Device.PixelShader = ShaderEffect.Artwork;
                Device.Blend = SpriteBlendMode.Alpha;
                SetSurface(MainSurface);
            }
            else DeviceLost = true;
        }

        public static void SetOpacity(float opacity)
        {
            Opacity = opacity >= 1 || opacity < 0 ? 1f : opacity;
            Device.Opacity = Opacity;
        }

        public static void SetBlend(bool value, float rate = 1F, BlendMode mode = BlendMode.NORMAL)
        {
            Blending = value;
            BlendingRate = rate;
            BlendingMode = mode;
            Device.BlendRate = rate;
            Device.Blend = !value ? SpriteBlendMode.Alpha :
                mode == BlendMode.INVLIGHT ? SpriteBlendMode.InverseLight : SpriteBlendMode.Additive;
        }

        public static void SetNormal(float blend, Color tintcolor)
        {
            Sprite.Flush();
            Device.PixelShader = NormalPixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255f, tintcolor.G / 255f, tintcolor.B / 255f, 1.0F) });
            Sprite.Flush();
        }

        public static void SetGrayscale(float blend, Color tintcolor)
        {
            Sprite.Flush();
            Device.PixelShader = GrayScalePixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255f, tintcolor.G / 255f, tintcolor.B / 255f, 1.0F) });
            Sprite.Flush();
        }

        public static void SetBlendMagic(float blend, Color tintcolor)
        {
            Sprite.Flush();
            Device.PixelShader = MagicPixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255f, tintcolor.G / 255f, tintcolor.B / 255f, 1.0F) });
            Sprite.Flush();
        }

        public static void Clean()
        {
            for (int i = TextureList.Count - 1; i >= 0; i--)
            {
                MImage m = TextureList[i];

                if (m == null)
                {
                    TextureList.RemoveAt(i);
                    continue;
                }

                m.CleanRepairedShadow(CMain.Time);
                if (CMain.Time <= m.CleanTime) continue;

                m.DisposeTexture(false);
                TextureList.RemoveAt(i);
            }

            for (int i = ControlList.Count - 1; i >= 0; i--)
            {
                MirControl c = ControlList[i];

                if (c == null)
                {
                    ControlList.RemoveAt(i);
                    continue;
                }

                if (CMain.Time <= c.CleanTime) continue;

                c.DisposeTexture(false);
                ControlList.RemoveAt(i);
            }
        }


        private static void CleanUp()
        {
            Device?.DiscardPending();
            LightBloomPass.DisposeAll();
            AtmosphereFogOverlay.DisposeAll();
            WetSurfacePass.DisposeAll();
            MainSurface?.Dispose();
            MainSurface = null;

            if (CurrentSurface != null)
            {
                if (!CurrentSurface.Disposed)
                {
                    CurrentSurface.Dispose();
                }

                CurrentSurface = null;
            }

            if (PoisonDotBackground != null)
            {
                if (!PoisonDotBackground.Disposed)
                {
                    PoisonDotBackground.Dispose();
                }

                PoisonDotBackground = null;
            }

            if (RadarTexture != null)
            {
                if (!RadarTexture.Disposed)
                {
                    RadarTexture.Dispose();
                }

                RadarTexture = null;
            }

            if (FloorTexture != null)
            {
                if (!FloorTexture.Disposed)
                {
                    FloorTexture.Dispose();
                }

                DXManager.FloorTexture = null;

                if (DXManager.FloorSurface != null && !DXManager.FloorSurface.Disposed)
                {
                    DXManager.FloorSurface.Dispose();
                }

                DXManager.FloorSurface = null;
            }

            if (LightTexture != null)
            {
                if (!LightTexture.Disposed)
                    LightTexture.Dispose();

                DXManager.LightTexture = null;

                if (DXManager.LightSurface != null && !DXManager.LightSurface.Disposed)
                {
                    DXManager.LightSurface.Dispose();
                }

                DXManager.LightSurface = null;
            }

            if (Lights != null)
            {
                for (int i = 0; i < Lights.Count; i++)
                {
                    if (!Lights[i].Disposed)
                        Lights[i].Dispose();
                }
                Lights.Clear();
            }

            for (int i = TextureList.Count - 1; i >= 0; i--)
            {
                MImage m = TextureList[i];

                if (m == null) continue;

                m.DisposeTexture(false);
            }
            TextureList.Clear();


            for (int i = ControlList.Count - 1; i >= 0; i--)
            {
                MirControl c = ControlList[i];

                if (c == null) continue;

                c.DisposeTexture(false);
            }
            ControlList.Clear();
        }

        public static void Dispose()
        {
            CleanUp();

            Device?.Dispose();
            Device = null;
            Sprite = null;
        }
    }
}
