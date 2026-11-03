using System.Drawing;
using Client.MirGraphics;

static void Check(byte value, int maximumIndex, int expectedIndex, Color expectedColour)
{
    if (!MapLightEncoding.TryDecode(value, maximumIndex, out int index, out Color colour) ||
        index != expectedIndex || colour.ToArgb() != expectedColour.ToArgb())
        throw new Exception($"Map light {value} decoded incorrectly: index={index}, colour={colour}");
}

static void Reject(byte value)
{
    if (MapLightEncoding.TryDecode(value, 9, out _, out _))
        throw new Exception($"Map light {value} should be invalid.");
}

Check(1, 9, 3, Color.White);
Check(21, 9, 3, Color.FromArgb(255, 120, 180, 255));
Check(32, 9, 6, Color.FromArgb(255, 255, 180, 120));
Check(49, 9, 9, Color.FromArgb(255, 22, 160, 5));
Check(49, 4, 4, Color.FromArgb(255, 22, 160, 5));
Reject(0);
Reject(10);
Reject(20);
Reject(50);
if (MapLightEncoding.TryDecode(21, 0, out _, out _))
    throw new Exception("Empty light texture array must not be indexed.");
Console.WriteLine("Map light decoding tests passed.");
