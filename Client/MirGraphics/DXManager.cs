using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Client.MirControls;
using Client.MirScenes;
using SlimDX;
using SlimDX.Direct3D9;
using Blend = SlimDX.Direct3D9.Blend;

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

        public static Device Device;
        public static Sprite Sprite;
        public static Line Line;

        public static Surface CurrentSurface;
        public static Surface MainSurface;
        public static PresentParameters Parameters;
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

        public static PixelShader GrayScalePixelShader;
        public static PixelShader NormalPixelShader;
        public static PixelShader MagicPixelShader;
        public static PixelShader PostProcessingPixelShader;
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
            Parameters = new PresentParameters
            {
                BackBufferFormat = Format.X8R8G8B8,
                PresentFlags = PresentFlags.LockableBackBuffer,
                BackBufferWidth = Settings.ScreenWidth,
                BackBufferHeight = Settings.ScreenHeight,
                SwapEffect = SwapEffect.Discard,
                PresentationInterval = Settings.FPSCap ? PresentInterval.One : PresentInterval.Immediate,
                Windowed = !Settings.FullScreen,
            };


            Direct3D d3d = new Direct3D();

            Capabilities devCaps = d3d.GetDeviceCaps(0, DeviceType.Hardware);
            // A missing shader-model 2.0 capability must not switch the entire
            // game to the CPU-bound Direct3D9 reference rasterizer.
            DeviceType devType = DeviceType.Hardware;
            bool hardwareTransform = (devCaps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0;
            CreateFlags devFlags = D3D9DevicePolicy.VertexProcessing(hardwareTransform) ==
                                   D3D9VertexProcessing.Hardware
                                       ? CreateFlags.HardwareVertexProcessing
                                       : CreateFlags.SoftwareVertexProcessing;
            // Never enable PureDevice: this renderer reads PixelShader state.


            Device = new Device(d3d, d3d.Adapters.DefaultAdapter.Adapter, devType, Program.Form.Handle, devFlags, Parameters);

            Device.SetDialogBoxMode(true);

            LoadTextures();
            LoadPixelsShaders();
        }

        private static unsafe void LoadPixelsShaders()
        {
            var shaderNormalPath = Settings.ShadersPath + "normal.ps";
            var shaderGrayScalePath = Settings.ShadersPath + "grayscale.ps";
            var shaderMagicPath = Settings.ShadersPath + "magic.ps";

            if (System.IO.File.Exists(shaderNormalPath))
            {
                using (var gs = ShaderBytecode.AssembleFromFile(shaderNormalPath, ShaderFlags.None))
                    NormalPixelShader = new PixelShader(Device, gs);
            }
            if (System.IO.File.Exists(shaderGrayScalePath))
            {
                using (var gs = ShaderBytecode.AssembleFromFile(shaderGrayScalePath, ShaderFlags.None))
                    GrayScalePixelShader = new PixelShader(Device, gs);
            }
            if (System.IO.File.Exists(shaderMagicPath))
            {
                using (var gs = ShaderBytecode.AssembleFromFile(shaderMagicPath, ShaderFlags.None))
                    MagicPixelShader = new PixelShader(Device, gs);
            }

            // This shader is optional: missing/unsupported source must not prevent
            // startup or force an additional render target on old hardware.
            string shaderPostPath = System.IO.Path.Combine(Settings.ShadersPath, "postprocess.ps");
            if (System.IO.File.Exists(shaderPostPath))
            {
                try
                {
                    using (var bytecode = ShaderBytecode.AssembleFromFile(shaderPostPath, ShaderFlags.None))
                        PostProcessingPixelShader = new PixelShader(Device, bytecode);
                }
                catch (Exception ex)
                {
                    PostProcessingPixelShader?.Dispose();
                    PostProcessingPixelShader = null;
                    CMain.SaveError("Optional post-processing shader disabled: " + ex.Message);
                }
            }
        }

        private static unsafe void LoadTextures()
        {
            Sprite = new Sprite(Device);
            Line = new Line(Device) { Width = 1F };

            MainSurface = Device.GetBackBuffer(0, 0);
            CurrentSurface = MainSurface;
            Device.SetRenderTarget(0, MainSurface);

            if (RadarTexture == null || RadarTexture.Disposed)
            {
                RadarTexture = new Texture(Device, 2, 2, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);

                DataRectangle stream = RadarTexture.LockRectangle(0, LockFlags.Discard);
                using (Bitmap image = new Bitmap(2, 2, 8, PixelFormat.Format32bppArgb, stream.Data.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.Clear(Color.White);
                RadarTexture.UnlockRectangle(0);
            }
            if (PoisonDotBackground == null || PoisonDotBackground.Disposed)
            {
                PoisonDotBackground = new Texture(Device, 5, 5, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);

                DataRectangle stream = PoisonDotBackground.LockRectangle(0, LockFlags.Discard);
                using (Bitmap image = new Bitmap(5, 5, 20, PixelFormat.Format32bppArgb, stream.Data.DataPointer))
                using (Graphics graphics = Graphics.FromImage(image))
                    graphics.Clear(Color.White);
                PoisonDotBackground.UnlockRectangle(0);
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

                Texture light = new Texture(Device, width, height, 1, Usage.None, Format.A8R8G8B8, Pool.Managed);

                DataRectangle stream = light.LockRectangle(0, LockFlags.Discard);
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

                light.UnlockRectangle(0);
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
            Device.SetRenderTarget(0, surface);
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
                if (Device.PixelShader == null) return;
                Sprite.Flush();
                Device.PixelShader = null;
            }
        }

        // One-sample map colour grading/vignette on the existing map texture.
        // It does not shade UI or create another full-screen render target.
        public static bool BeginPostProcessing(Color atmosphere, float gradeStrength, float fogStrength,
                                               AtmosphericWeatherFrame weatherFrame)
        {
            if (PostProcessingPixelShader == null || PostProcessingPixelShader.Disposed)
                return false;

            float intensity = AtmosphereIntensity.Scale(Settings.AtmosphereIntensityPercent);
            PostProcessConstants[0] = new Vector4(
                AtmosphereExposure.Brightness(weatherFrame.CloudCover, intensity),
                0.22f, 1f, 0.80f);
            PostProcessConstants[2] = new Vector4(atmosphere.R / 255f, atmosphere.G / 255f,
                                                   atmosphere.B / 255f,
                                                   Math.Clamp(gradeStrength * intensity, 0f, 0.4f));
            PostProcessConstants[3] = new Vector4(Math.Clamp(fogStrength * intensity, 0f, 0.2f),
                                                   0.78f, 0.84f, 0.94f);
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
            Device.PixelShader = null;
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

        public static void DrawOpaque(Texture texture, Rectangle? sourceRect, Vector3? position, Color4 color, float opacity)
        {
            color.Alpha = opacity;
            Draw(texture, sourceRect, position, color);
        }

        public static void Draw(Texture texture, Rectangle? sourceRect, Vector3? position, Color4 color)
        {
            Sprite.Draw(texture, sourceRect, Vector3.Zero, position, color);
            CMain.DPSCounter++;
        }

        public static void AttemptReset()
        {
            try
            {
                Result result = DXManager.Device.TestCooperativeLevel();

                if (result.Code == ResultCode.DeviceLost.Code) return;

                if (result.Code == ResultCode.DeviceNotReset.Code)
                {
                    DXManager.ResetDevice();
                    return;
                }

                if (result.Code != ResultCode.Success.Code) return;

                DXManager.DeviceLost = false;
            }
            catch
            {
            }
        }

        public static void ResetDevice()
        {
            DXManager.CleanUp();
            DXManager.DeviceLost = true;

            if (DXManager.Parameters == null) return;

            Size clientSize = Program.Form.ClientSize;

            if (clientSize.Width == 0 || clientSize.Height == 0) return;

            DXManager.Parameters.Windowed = !Settings.FullScreen;
            DXManager.Parameters.BackBufferWidth = clientSize.Width;
            DXManager.Parameters.BackBufferHeight = clientSize.Height;
            DXManager.Parameters.PresentationInterval = Settings.FPSCap ? PresentInterval.Default : PresentInterval.Immediate;
            DXManager.Device.Reset(DXManager.Parameters);

            DXManager.LoadTextures();
        }

        public static void AttemptRecovery()
        {
            try
            {
                Sprite.End();
            }
            catch
            {
            }

            try
            {
                Device.EndScene();
            }
            catch
            {
            }

            try
            {
                MainSurface = Device.GetBackBuffer(0, 0);
                CurrentSurface = MainSurface;
                Device.SetRenderTarget(0, MainSurface);
            }
            catch
            {
            }
        }
        public static void SetOpacity(float opacity)
        {
            if (Opacity == opacity)
                return;

            Sprite.Flush();
            Device.SetRenderState(RenderState.AlphaBlendEnable, true);
            if (opacity >= 1 || opacity < 0)
            {
                Device.SetRenderState(RenderState.SourceBlend, SlimDX.Direct3D9.Blend.SourceAlpha);
                Device.SetRenderState(RenderState.DestinationBlend, SlimDX.Direct3D9.Blend.InverseSourceAlpha);
                Device.SetRenderState(RenderState.SourceBlendAlpha, Blend.One);
                Device.SetRenderState(RenderState.BlendFactor, Color.FromArgb(255, 255, 255, 255).ToArgb());
            }
            else
            {
                Device.SetRenderState(RenderState.SourceBlend, Blend.BlendFactor);
                Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseBlendFactor);
                Device.SetRenderState(RenderState.SourceBlendAlpha, Blend.SourceAlpha);
                Device.SetRenderState(RenderState.BlendFactor, Color.FromArgb((byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity), (byte)(255 * opacity)).ToArgb());
            }
            Opacity = opacity;
            Sprite.Flush();
        }
        public static void SetBlend(bool value, float rate = 1F, BlendMode mode = BlendMode.NORMAL)
        {
            if (value == Blending && BlendingRate == rate && BlendingMode == mode) return;

            Blending = value;
            BlendingRate = rate;
            BlendingMode = mode;

            Sprite.Flush();

            Sprite.End();

            if (Blending)
            {
                Sprite.Begin(SpriteFlags.DoNotSaveState);
                Device.SetRenderState(RenderState.AlphaBlendEnable, true);

                switch (BlendingMode)
                {
                    case BlendMode.INVLIGHT:
                        Device.SetRenderState(RenderState.BlendOperation, BlendOperation.Add);
                        Device.SetRenderState(RenderState.SourceBlend, Blend.BlendFactor);
                        Device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceColor);
                        break;
                    default:
                        Device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
                        Device.SetRenderState(RenderState.DestinationBlend, Blend.One);
                        break;
                }

                Device.SetRenderState(RenderState.BlendFactor, Color.FromArgb((byte)(255 * BlendingRate), (byte)(255 * BlendingRate),
                                                                (byte)(255 * BlendingRate), (byte)(255 * BlendingRate)).ToArgb());
            }
            else
                Sprite.Begin(SpriteFlags.AlphaBlend);

            Device.SetRenderTarget(0, CurrentSurface);
        }

        public static void SetNormal(float blend, Color tintcolor)
        {
            if (Device.PixelShader == NormalPixelShader)
                return;

            Sprite.Flush();
            Device.PixelShader = NormalPixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
            Sprite.Flush();
        }

        public static void SetGrayscale(float blend, Color tintcolor)
        {
            if (Device.PixelShader == GrayScalePixelShader)
                return;

            Sprite.Flush();
            Device.PixelShader = GrayScalePixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
            Sprite.Flush();
        }

        public static void SetBlendMagic(float blend, Color tintcolor)
        {
            if (Device.PixelShader == MagicPixelShader || MagicPixelShader == null)
                return;

            Sprite.Flush();
            Device.PixelShader = MagicPixelShader;
            Device.SetPixelShaderConstant(0, new Vector4[] { new Vector4(1.0F, 1.0F, 1.0F, blend) });
            Device.SetPixelShaderConstant(1, new Vector4[] { new Vector4(tintcolor.R / 255, tintcolor.G / 255, tintcolor.B / 255, 1.0F) });
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
            // The bloom pass owns a DEFAULT-pool target and DX9 shader objects.
            // Release these before Device.Reset or disposing the old sprite.
            LightBloomPass.DisposeAll();
            AtmosphereFogOverlay.DisposeAll();
            if (Sprite != null)
            {
                if (!Sprite.Disposed)
                {
                    Sprite.Dispose();
                }

                Sprite = null;
            }

            if (Line != null)
            {
                if (!Line.Disposed)
                {
                    Line.Dispose();
                }

                Line = null;
            }

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

            Device?.Direct3D?.Dispose();
            Device?.Dispose();

            NormalPixelShader?.Dispose();
            GrayScalePixelShader?.Dispose();
            MagicPixelShader?.Dispose();
            PostProcessingPixelShader?.Dispose();
            PostProcessingPixelShader = null;
        }
    }
}
