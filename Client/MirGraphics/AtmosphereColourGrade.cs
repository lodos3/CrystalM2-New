using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Conservative colour adjustments; the ambient-light mask remains authoritative.
    // All world colour grading happens on the map texture, never the UI.
    internal static class AtmosphereColourGrade
    {
        internal static Color Target(LightSetting setting, WeatherSetting weather, byte mapDarkLight)
        {
            Color tint;
            switch (setting)
            {
                case LightSetting.Dawn: tint = Color.FromArgb(255, 255, 228, 197); break;
                case LightSetting.Evening: tint = Color.FromArgb(255, 255, 209, 177); break;
                case LightSetting.Night:
                    tint = mapDarkLight == 4
                        ? Color.FromArgb(255, 255, 220, 172)
                        : Color.FromArgb(255, 194, 212, 255);
                    break;
                default: tint = Color.White; break;
            }

            // Weather colours are a subtle screen-space grade, not fake volumetric fog.
            if ((weather & WeatherSetting.Fog) != 0 ||
                (weather & WeatherSetting.Rain) != 0)
                tint = Mix(tint, Color.FromArgb(255, 224, 236, 255), 0.65);
            if ((weather & WeatherSetting.Snow) != 0)
                tint = Mix(tint, Color.FromArgb(255, 226, 239, 255), 0.65);
            if ((weather & (WeatherSetting.RedEmber | WeatherSetting.FireParticle)) != 0)
                tint = Mix(tint, Color.FromArgb(255, 255, 194, 157), 0.66);
            if ((weather & WeatherSetting.YellowEmber) != 0)
                tint = Mix(tint, Color.FromArgb(255, 255, 231, 176), 0.45);
            if ((weather & WeatherSetting.PurpleLeaves) != 0)
                tint = Mix(tint, Color.FromArgb(255, 224, 199, 243), 0.36);
            if ((weather & WeatherSetting.Leaves) != 0)
                tint = Mix(tint, Color.FromArgb(255, 252, 234, 196), 0.24);
            return tint;
        }

        internal static float GradeStrength(LightSetting setting, WeatherSetting weather)
        {
            float strength;
            switch (setting)
            {
                case LightSetting.Dawn: strength = 0.32f; break;
                case LightSetting.Evening: strength = 0.36f; break;
                case LightSetting.Night: strength = 0.28f; break;
                default: strength = 0; break;
            }
            if ((weather & (WeatherSetting.Fog | WeatherSetting.Rain | WeatherSetting.Snow)) != 0)
                strength = Math.Max(strength, 0.24f);
            return strength;
        }

        // Use the interpolated tint to derive strength so entering daylight does
        // not abruptly disable grading before the colour transition completes.
        internal static float GradeStrength(Color interpolatedTint)
        {
            int spread = Math.Max(Math.Abs(interpolatedTint.R - interpolatedTint.G),
                            Math.Max(Math.Abs(interpolatedTint.R - interpolatedTint.B),
                                     Math.Abs(interpolatedTint.G - interpolatedTint.B)));
            return Math.Min(0.36f, spread / 200f);
        }

        // Fog colour follows weather, rather than applying blue distance haze
        // to every environment, including fire/ash maps.
        internal static Color FogTint(WeatherSetting weather)
        {
            if ((weather & (WeatherSetting.RedEmber | WeatherSetting.FireParticle)) != 0)
                return Color.FromArgb(255, 224, 177, 144);
            if ((weather & WeatherSetting.YellowEmber) != 0)
                return Color.FromArgb(255, 246, 225, 174);
            if ((weather & WeatherSetting.PurpleLeaves) != 0)
                return Color.FromArgb(255, 210, 189, 237);
            if ((weather & WeatherSetting.Snow) != 0)
                return Color.FromArgb(255, 205, 222, 249);
            if ((weather & WeatherSetting.Rain) != 0)
                return Color.FromArgb(255, 182, 201, 226);
            return Color.FromArgb(255, 198, 214, 240);
        }

        internal static float FogStrength(WeatherSetting weather)
        {
            if ((weather & WeatherSetting.Fog) != 0) return 0.13f;
            if ((weather & (WeatherSetting.Rain | WeatherSetting.Snow)) != 0) return 0.045f;
            if ((weather & (WeatherSetting.RedEmber | WeatherSetting.FireParticle)) != 0)
                return 0.065f;
            if ((weather & (WeatherSetting.YellowEmber | WeatherSetting.PurpleLeaves)) != 0)
                return 0.025f;
            return 0;
        }

        private static Color Mix(Color a, Color b, double t)
        {
            return Color.FromArgb(255,
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }
}
