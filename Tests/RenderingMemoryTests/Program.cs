using System;
using System.IO;
using Client.MirGraphics;

static void Assert(bool ok, string message)
{
    if (!ok) throw new Exception(message);
}

var budget = new ShadowMaskBudget(1024);
Assert(budget.TryReserve(512) && budget.TryReserve(512), "Allocations up to budget must succeed.");
Assert(!budget.TryReserve(1) && budget.UsedBytes == 1024,
       "Budget overflow must fail without increasing tracked memory.");
budget.Release(512);
Assert(budget.TryReserve(400) && budget.UsedBytes == 912, "Reclaimed space must be reusable.");
budget.Release(912);
Assert(budget.UsedBytes == 0, "Disposal must return memory accounting to zero.");
try { budget.Release(1); throw new Exception("Expected over-release exception."); }
catch (ArgumentOutOfRangeException) { }
try { budget.TryReserve(-1); throw new Exception("Expected invalid allocation exception."); }
catch (ArgumentOutOfRangeException) { }

byte[] bytes = new byte[32];
using (var reader = new BinaryReader(new MemoryStream(bytes, writable: false)))
{
    reader.BaseStream.Position = 4;
    SpriteFileLayout.SkipCompressedLayer(reader, 12);
    Assert(reader.BaseStream.Position == 16, "Skip should seek without reading or allocating payload.");
    SpriteFileLayout.SkipCompressedLayer(reader, 16);
    Assert(reader.BaseStream.Position == 32, "Exact end-of-file skip should be permitted.");
    try { SpriteFileLayout.SkipCompressedLayer(reader, 1); throw new Exception("Expected truncation."); }
    catch (EndOfStreamException) { }
    try { SpriteFileLayout.SkipCompressedLayer(reader, -1); throw new Exception("Expected negative-length rejection."); }
    catch (InvalidDataException) { }
}

Assert(SpriteFileLayout.HasSafeMask(128, 64, 100), "Ordinary LIB masks must be accepted.");
Assert(!SpriteFileLayout.HasSafeMask(0, 64, 100), "Zero-width masks must not allocate textures.");
Assert(!SpriteFileLayout.HasSafeMask(128, 64, 0), "Empty compressed masks must not be uploaded.");
Assert(!SpriteFileLayout.HasSafeMask(8192, 8192, 10), "Absurd masks must be rejected before GPU allocation.");

// A Direct3D9 REF device is a software rasterizer: never select it merely
// because the GPU exposes an older vertex/pixel shader version.
Assert(D3D9DevicePolicy.DeviceIsHardware, "Renderer must use the hardware D3D9 device.");
Assert(D3D9DevicePolicy.VertexProcessing(false) == D3D9VertexProcessing.Software,
       "Older GPUs without hardware T&L must use software vertex processing.");
Assert(D3D9DevicePolicy.VertexProcessing(true) == D3D9VertexProcessing.Hardware,
       "Hardware T&L must remain the fast path.");
Assert(!D3D9DevicePolicy.EnablePureDevice,
       "PureDevice is incompatible with renderer state readbacks.");

Console.WriteLine("Shadow memory, LIB parsing and hardware-device regression checks passed.");
