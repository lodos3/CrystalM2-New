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
Console.WriteLine("Shadow GPU mask budget and allocation-free LIB metadata skipping checks passed.");
