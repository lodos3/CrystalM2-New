using System;
using System.Drawing;
using Client.MirGraphics;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }

Check(AtmosphereColourGrade.Target(LightSetting.Day, WeatherSetting.None, 0) == Color.White, "Day must remain neutral.");
Color dawn = AtmosphereColourGrade.Target(LightSetting.Dawn, WeatherSetting.None, 0);
Color evening = AtmosphereColourGrade.Target(LightSetting.Evening, WeatherSetting.None, 0);
Check(dawn.R > dawn.B && evening.R > evening.B, "Dawn and evening must warm the world.");
Color night = AtmosphereColourGrade.Target(LightSetting.Night, WeatherSetting.None, 0);
Check(night.B > night.R, "Night must grade towards cool blue.");
Check(AtmosphereColourGrade.Target(LightSetting.Night, WeatherSetting.None, 4).R > night.R,
      "Warm-map lighting must preserve its map-specific atmosphere.");
Color rain = AtmosphereColourGrade.Target(LightSetting.Day, WeatherSetting.Rain, 0);
Check(rain.B > rain.R, "Rain should grade cooler than a clear day.");
Check(AtmosphereColourGrade.GradeStrength(Color.White) == 0 &&
      AtmosphereColourGrade.GradeStrength(Color.FromArgb(255, 255, 240, 226)) <
      AtmosphereColourGrade.GradeStrength(evening),
      "Atmosphere grading intensity must ease towards neutral daylight.");
Check(AtmosphereColourGrade.FogStrength(WeatherSetting.Fog) > 0 &&
      AtmosphereColourGrade.FogStrength(WeatherSetting.None) == 0,
      "Fog must be opt-in according to the weather flag.");
Check(AtmosphereColourGrade.GradeStrength(LightSetting.Day, WeatherSetting.None) == 0,
      "Normal daylight must not add colour tint.");

var blocked = new bool[15, 15];
blocked[5, 5] = true;
bool IsBlocked(int x, int y) => blocked[x, y];
Check(!LightVisibility.Clear(2, 5, 8, 5, IsBlocked), "Static wall should occlude a crossing ray.");
Check(LightVisibility.Clear(2, 4, 8, 4, IsBlocked), "Parallel light ray must remain visible.");
Check(LightVisibility.Clear(2, 5, 5, 5, IsBlocked), "End point is excluded from occluder tests.");
Check(LightVisibility.Clear(5, 5, 8, 5, IsBlocked), "Source cell must not occlude itself.");
Check(!LightVisibility.Clear(8, 5, 2, 5, IsBlocked), "Occlusion must be symmetric.");
Check(!LightVisibility.Clear(0, 0, 30, 0, (x, y) => false), "Long rays must be rejected by work budget.");

// Atmospheric haze must ease in/out, and interrupted weather changes must
// continue from the current value without snapping.
var fogFade = new SmoothScalarTransition(10000);
Check(fogFade.Sample(0f, 0) == 0, "Initial weather state must not fade.");
Check(fogFade.Sample(0.13f, 1000) == 0, "New fog target starts from previous value.");
float halfway = fogFade.Sample(0.13f, 6000);
Check(halfway > 0.05f && halfway < 0.09f, "Fog must fade smoothly.");
Check(fogFade.Sample(0.13f, 11000) == 0.13f, "Fog must converge on its target.");
Check(fogFade.Sample(0f, 11001) == 0.13f, "Weather fade-out must not jump.");
Check(fogFade.Sample(0f, 21001) == 0f, "Weather fade-out must finish.");
fogFade.Reset();
Check(fogFade.Sample(0.045f, 50000) == 0.045f, "Map transitions must reset the haze history.");

Check(!LightVisibility.BlocksByMapFlags(0, 0), "Unblocked tiles must transmit light.");
Check(LightVisibility.BlocksByMapFlags(0x20000000, 0), "Pathfinding back-tile barrier must occlude.");
Check(LightVisibility.BlocksByMapFlags(0, 0x8000), "Pathfinding front-tile barrier must occlude.");

Size size = BloomBufferLayout.Resolve(1024, 768, 4);
Check(size == new Size(256, 192), "Quarter-resolution bloom dimensions wrong.");
Check(BloomBufferLayout.Resolve(1023, 767, 4) == new Size(256, 192), "Odd sizes must round up.");
Check(BloomBufferLayout.Resolve(1, 1, 8) == new Size(1, 1), "Tiny surfaces must stay valid.");
Check(BloomBufferLayout.ClampDivisor(1) == 2 && BloomBufferLayout.ClampDivisor(20) == 8,
      "Bloom quality must clamp to safe bounds.");
Check(BloomBufferLayout.Resolve(12000, 12000, 2) == Size.Empty,
      "Oversized bloom allocations must disable the pass.");
Console.WriteLine("Atmosphere, occlusion and bounded bloom geometry checks passed.");
