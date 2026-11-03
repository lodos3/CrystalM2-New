using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Map cells encode colour in the tens digit and radius in the units digit.
    // Keep this independent of Direct3D so map data can be tested without a GPU.
    internal static class MapLightEncoding
    {
        internal static bool TryDecode(byte encoded, int maximumLightIndex, out int lightIndex, out Color colour)
        {
            lightIndex = 0;
            colour = Color.White;

            // Valid map light colours are 0..4; zero-radius entries are not emitters.
            if (encoded == 0 || encoded >= 50 || encoded % 10 == 0 || maximumLightIndex < 1)
                return false;

            lightIndex = Math.Min((encoded % 10) * 3, maximumLightIndex);
            switch (encoded / 10)
            {
                case 2:
                    colour = Color.FromArgb(255, 120, 180, 255);
                    break;
                case 3:
                    colour = Color.FromArgb(255, 255, 180, 120);
                    break;
                case 4:
                    colour = Color.FromArgb(255, 22, 160, 5);
                    break;
            }

            return true;
        }
    }
}
