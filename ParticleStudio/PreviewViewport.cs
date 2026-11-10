using System.Drawing;
using System.Numerics;
using Client.Rendering;
using CrystalM2.Rendering.Core;
using Vortice.Direct3D;
using WinForms = System.Windows.Forms;

namespace CrystalM2.ParticleStudio;

internal sealed class PreviewViewport : IDisposable
{
    private static readonly Color Canvas = Color.FromArgb(17, 22, 28);
    private static readonly Color Grid = Color.FromArgb(35, 45, 57);
    private static readonly Color Guide = Color.FromArgb(107, 126, 145);

    private readonly WinForms.Panel _panel = new() { BackColor = Canvas, TabStop = false, Dock = WinForms.DockStyle.Fill };
    private readonly SpellParticlePool _pool = new(4096);
    private readonly ReferenceLighting _lighting = new();
    private RendererDevice? _device;
    private ReferenceScene? _reference;
    private int _width, _height;

    public WinForms.Control Surface => _panel;

    public ReferenceScene? Reference => _reference;

    public void UseReference(ReferenceScene scene)
    {
        _reference?.Dispose();
        _reference = scene;
    }

    public void Render(PreviewStage? stage, int time, MirDirection direction, bool glow, bool guide,
        bool referenceVisible, LightSetting ambient)
    {
        if (!_panel.IsHandleCreated || _panel.Width <= 1 || _panel.Height <= 1) return;
        var device = Device();
        Vector2 centre = new(_width * .5f, _height * .62f);
        float angle = ((int)direction - 2) * MathF.PI / 4;
        Vector2 facing = Vector2.Normalize(new Vector2(MathF.Cos(angle), MathF.Sin(angle) * .68f));
        var rig = new PreviewRig(centre, facing, direction);
        var reference = referenceVisible ? _reference : null;
        if (reference != null)
        {
            reference.Gather(device, rig, stage, time);
            rig = reference.WithWeapon(device, rig, stage?.ActorAction ?? MirAction.Standing);
        }

        _pool.Clear();
        stage?.Sample(_pool, rig, time);

        device.BeginFrame();
        device.Clear(Canvas);
        if (reference?.HasFloor == true) reference.DrawFloor(device);
        else DrawGrid(device, centre);
        var policy = new SpellParticleGlowPolicy(glow ? 2 : 0, .55f, .68f, true);
        device.SpellParticles.Draw(_pool.Active, Vector2.Zero, time, ground: true, glowPolicy: policy);
        reference?.DrawObjects(device, depthMask: false);
        var mask = reference != null && !_pool.Active.IsEmpty ? _lighting.Mask(device, reference) : null;
        device.SpellParticles.Draw(_pool.Active, Vector2.Zero, time, ground: false, glowPolicy: policy,
            occlusion: mask, depthOrigin: -1);
        if (reference != null) _lighting.Composite(device, reference, _pool.Active, time, ambient);
        if (guide && reference?.HasActor != true) DrawGuide(device, centre, facing);
        device.SpellParticles.Draw(_pool.Active, Vector2.Zero, time, ground: false, emissiveOverlay: true, glowPolicy: policy,
            occlusion: mask, depthOrigin: -1);
        device.Present(vsync: false);
    }

    private RendererDevice Device()
    {
        if (_device == null)
        {
            (_width, _height) = (_panel.Width, _panel.Height);
            try { _device = new RendererDevice(_panel.Handle, _width, _height); }
            catch (Exception error) when (error is not OutOfMemoryException)
            {
                _device = new RendererDevice(_panel.Handle, _width, _height, DriverType.Warp);
            }
        }
        else if (_width != _panel.Width || _height != _panel.Height)
        {
            (_width, _height) = (_panel.Width, _panel.Height);
            _device.Resize(_width, _height);
        }
        return _device;
    }

    private static void DrawGrid(RendererDevice device, Vector2 centre)
    {
        Vector2 right = new(24, 16), left = new(-24, 16);
        for (int i = -7; i <= 7; i++)
        {
            device.DrawLines([centre + right * i - left * 7, centre + right * i + left * 7], Grid);
            device.DrawLines([centre + left * i - right * 7, centre + left * i + right * 7], Grid);
        }
    }

    private static void DrawGuide(RendererDevice device, Vector2 feet, Vector2 facing)
    {
        Vector2 body = feet - Vector2.UnitY * 37;
        device.DrawLines([feet + new Vector2(-10, 0), feet + new Vector2(0, 5), feet + new Vector2(10, 0),
            feet - new Vector2(0, 5), feet + new Vector2(-10, 0)], Guide);
        device.DrawLines([feet, body, body + facing * 14], Guide);
    }

    public void Dispose()
    {
        _reference?.Dispose();
        _lighting.Dispose();
        _device?.Dispose();
        _panel.Dispose();
    }
}
