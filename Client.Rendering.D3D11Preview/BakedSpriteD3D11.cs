using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using CrystalM2.Rendering.Core;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace CrystalM2.Rendering.D3D11Preview
{
    // An isolated Windows DX11 migration SPIKE, not a replacement for the
    // production D3D9 client. Only the original baked BGRA sprite is drawn.
    // No automatic per-pixel relighting, recolouring or new silhouette shadow.
    internal sealed unsafe class BakedSpriteD3D11 : IDisposable
    {
        private static readonly FeatureLevel[] RequestedLevels =
        {
            FeatureLevel.Level_11_1, FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1, FeatureLevel.Level_10_0
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct Vertex
        {
            public Vector2 Position;
            public Vector2 Uv;
            public Vector4 Tint;

            public Vertex(float x, float y, float u, float v)
            {
                Position = new Vector2(x, y);
                Uv = new Vector2(u, v);
                Tint = Vector4.One;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FrameConstants
        {
            public Vector2 Viewport;
            public Vector2 Pad;
        }

        private readonly IDXGIFactory2 _factory;
        private readonly ID3D11Device _device;
        private readonly ID3D11DeviceContext _context;
        private readonly IDXGISwapChain1 _swapChain;
        private readonly ID3D11Buffer _vertices;
        private readonly ID3D11Buffer _frameConstants;
        private readonly ID3D11VertexShader _vertexShader;
        private readonly ID3D11PixelShader _pixelShader;
        private readonly ID3D11InputLayout _inputLayout;
        private readonly ID3D11SamplerState _pointClamp;
        private readonly ID3D11BlendState _straightAlpha;

        private ID3D11Texture2D? _backBuffer;
        private ID3D11RenderTargetView? _renderTarget;
        private ID3D11Texture2D? _spriteTexture;
        private ID3D11ShaderResourceView? _spriteView;
        private LegacySpriteFrame? _sprite;
        private int _width;
        private int _height;
        private bool _disposed;

        internal BakedSpriteD3D11(IntPtr windowHandle, int width, int height)
        {
            if (windowHandle == IntPtr.Zero || width <= 0 || height <= 0)
                throw new ArgumentException("An existing WinForms window and positive dimensions are required.");

            // Explicitly require hardware. Silently selecting the CPU WARP
            // renderer would invalidate FPS comparisons against the old client.
            D3D11CreateDevice(
                IntPtr.Zero, DriverType.Hardware, DeviceCreationFlags.BgraSupport,
                RequestedLevels, out _device, out FeatureLevel _,
                out _context).CheckError();

            _factory = CreateDXGIFactory1<IDXGIFactory2>();
            var swapDescription = new SwapChainDescription1
            {
                Width = (uint)width,
                Height = (uint)height,
                Format = Format.B8G8R8A8_UNorm,
                BufferCount = 2,
                BufferUsage = Usage.RenderTargetOutput,
                SampleDescription = SampleDescription.Default,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                AlphaMode = AlphaMode.Ignore
            };
            _swapChain = _factory.CreateSwapChainForHwnd(
                _device, windowHandle, swapDescription,
                new SwapChainFullscreenDescription { Windowed = true });
            _factory.MakeWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

            string shaderPath = Path.Combine(AppContext.BaseDirectory, "Shaders", "BakedSprite.hlsl");
            ReadOnlyMemory<byte> vertexBytecode = Compiler.CompileFromFile(shaderPath, "VSMain", "vs_4_0");
            ReadOnlyMemory<byte> pixelBytecode = Compiler.CompileFromFile(shaderPath, "PSMain", "ps_4_0");
            _vertexShader = _device.CreateVertexShader(vertexBytecode.Span);
            _pixelShader = _device.CreatePixelShader(pixelBytecode.Span);
            var layout = new[]
            {
                new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
                new InputElementDescription("COLOR", 0, Format.R32G32B32A32_Float, 16, 0)
            };
            _inputLayout = _device.CreateInputLayout(layout, vertexBytecode.Span);

            _vertices = _device.CreateBuffer(
                sizeof(Vertex) * 6, BindFlags.VertexBuffer,
                ResourceUsage.Dynamic, CpuAccessFlags.Write);
            _frameConstants = _device.CreateBuffer(
                sizeof(FrameConstants), BindFlags.ConstantBuffer,
                ResourceUsage.Dynamic, CpuAccessFlags.Write);
            _pointClamp = _device.CreateSamplerState(SamplerDescription.PointClamp);
            _straightAlpha = _device.CreateBlendState(BlendDescription.NonPremultiplied);
            Resize(width, height);
        }

        internal void SetSprite(LegacySpriteFrame? sprite)
        {
            _context.PSUnsetShaderResource(0); // prevent texture replacement hazards
            _spriteView?.Dispose();
            _spriteTexture?.Dispose();
            _spriteView = null;
            _spriteTexture = null;
            _sprite = null;
            if (sprite == null) return;

            // DirectX BGRA_UNORM does not perform sRGB conversion or premultiply.
            // Keep the legacy pixel array unchanged, including baked shadow RGB.
            byte[] source = BakedArtPolicy.CopyBgraExact(sprite.Bgra,
                                                         sprite.Width, sprite.Height);
            _spriteTexture = _device.CreateTexture2D(source,
                Format.B8G8R8A8_UNorm, (uint)sprite.Width, (uint)sprite.Height,
                mipLevels: 1);
            _spriteView = _device.CreateShaderResourceView(_spriteTexture);
            _sprite = sprite;
        }

        internal void Resize(int width, int height)
        {
            if (_disposed || width <= 0 || height <= 0) return;
            if (_renderTarget != null && _width == width && _height == height) return;

            _context.OMSetRenderTargets((ID3D11RenderTargetView?)null);
            _renderTarget?.Dispose();
            _backBuffer?.Dispose();
            _renderTarget = null;
            _backBuffer = null;

            _swapChain.ResizeBuffers(0, (uint)width, (uint)height,
                                     Format.Unknown, SwapChainFlags.None).CheckError();
            _backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _renderTarget = _device.CreateRenderTargetView(_backBuffer);
            _width = width;
            _height = height;
        }

        internal void Draw()
        {
            if (_disposed || _renderTarget == null || _width <= 0 || _height <= 0)
                return;

            _context.ClearRenderTargetView(_renderTarget,
                                           new Color4(0.09f, 0.10f, 0.12f, 1.0f));
            _context.OMSetRenderTargets(_renderTarget);
            _context.RSSetViewport(new Viewport(_width, _height));
            _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            _context.IASetInputLayout(_inputLayout);
            _context.IASetVertexBuffer(0, _vertices, (uint)sizeof(Vertex));
            _context.VSSetShader(_vertexShader);
            _context.PSSetShader(_pixelShader);
            _context.PSSetSampler(0, _pointClamp);
            _context.OMSetBlendState(_straightAlpha);

            var frame = new FrameConstants
            {
                Viewport = new Vector2(_width, _height),
                Pad = Vector2.Zero
            };
            var mappedConstants = _context.Map(_frameConstants, MapMode.WriteDiscard);
            mappedConstants.AsSpan<FrameConstants>(1)[0] = frame;
            _context.Unmap(_frameConstants, 0);
            _context.VSSetConstantBuffer(0, _frameConstants);

            if (_sprite is not null && _spriteView is not null)
            {
                // Original frame offset remains visible in this preview.
                float x = (_width - _sprite.Width) * 0.5f + _sprite.OffsetX;
                float y = (_height - _sprite.Height) * 0.5f + _sprite.OffsetY;
                BakedSpriteCommand command =
                    BakedArtPolicy.ForArtwork(0, x, y, _sprite.Width, _sprite.Height);
                if (command.IsVisible(_width, _height))
                {
                    float right = x + _sprite.Width, bottom = y + _sprite.Height;
                    Span<Vertex> quad = stackalloc Vertex[6]
                    {
                        new Vertex(x, y, 0, 0),
                        new Vertex(right, y, 1, 0),
                        new Vertex(x, bottom, 0, 1),
                        new Vertex(right, y, 1, 0),
                        new Vertex(right, bottom, 1, 1),
                        new Vertex(x, bottom, 0, 1)
                    };
                    var mappedVertices = _context.Map(_vertices, MapMode.WriteDiscard);
                    quad.CopyTo(mappedVertices.AsSpan<Vertex>(6));
                    _context.Unmap(_vertices, 0);
                    _context.PSSetShaderResource(0, _spriteView);
                    _context.Draw(6, 0);
                    _context.PSUnsetShaderResource(0);
                }
            }

            _swapChain.Present(1, PresentFlags.None).CheckError();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _context.ClearState();
            _context.Flush();
            _spriteView?.Dispose();
            _spriteTexture?.Dispose();
            _renderTarget?.Dispose();
            _backBuffer?.Dispose();
            _straightAlpha.Dispose();
            _pointClamp.Dispose();
            _frameConstants.Dispose();
            _vertices.Dispose();
            _inputLayout.Dispose();
            _pixelShader.Dispose();
            _vertexShader.Dispose();
            _swapChain.Dispose();
            _factory.Dispose();
            _context.Dispose();
            _device.Dispose();
        }
    }
}
