using System;
using System.Drawing;
using Client.MirGraphics;

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

Assert(AmbientLightTransition.Target(LightSetting.Day, 0) == Color.White, "Day should be unlit.");
Assert(AmbientLightTransition.Target(LightSetting.Dawn, 0) == Color.FromArgb(255, 50, 50, 50), "Legacy dawn intensity changed.");
Assert(AmbientLightTransition.Target(LightSetting.Evening, 0) == Color.FromArgb(255, 50, 50, 50), "Legacy evening intensity changed.");
Assert(AmbientLightTransition.Target(LightSetting.Night, 0) == Color.Black, "Night without map tint should be black.");
Assert(AmbientLightTransition.Target(LightSetting.Night, 3) == Color.SkyBlue, "Map night tint should remain intact.");
Assert(AmbientLightTransition.Target(LightSetting.Night, 4) == Color.Goldenrod, "Map night tint should remain intact.");

var fade = new AmbientLightTransition();
Assert(fade.Sample(Color.White, 0) == Color.White, "Initial state must not fade from black.");
Assert(fade.Sample(Color.Black, 1000) == Color.White, "Changing target should start from current colour.");
Color middle = fade.Sample(Color.Black, 6000);
Assert(middle.R > 0 && middle.R < 255 && middle.R == middle.G && middle.G == middle.B,
       "Halfway colour should interpolate all channels.");
Assert(fade.Sample(Color.Black, 11000) == Color.Black, "Transition should finish at exact target.");
Assert(fade.Sample(Color.White, 11001) == Color.Black, "Day transition should begin without a snap.");
Assert(fade.Sample(Color.White, 21001) == Color.White, "Day transition should finish.");

fade.Reset();
Assert(fade.Sample(Color.Red, 30000) == Color.Red, "Map reset must snap to the new map colour.");
var interrupted = new AmbientLightTransition();
interrupted.Sample(Color.Black, 0);
interrupted.Sample(Color.White, 1000);
Color before = interrupted.Sample(Color.White, 3000);
Color after = interrupted.Sample(Color.SkyBlue, 3000);
Assert(before == after, "Interrupting a transition must not jump the ambient colour.");
Assert(interrupted.Sample(Color.SkyBlue, 13000) == Color.SkyBlue, "Interrupted transition must finish.");
Console.WriteLine("Ambient light transition regression checks passed.");
