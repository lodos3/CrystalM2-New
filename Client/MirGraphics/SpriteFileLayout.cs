using System;
using System.IO;

namespace Client.MirGraphics
{
    internal static class SpriteFileLayout
    {
        // Sprite metadata can include a compressed image immediately before the
        // optional mask header. Seek past it: ReadBytes(length) creates a large,
        // otherwise unused allocation every time the header is inspected.
        // Defend against malformed LIB metadata before allocating GPU textures.
        // A single auxiliary mask must not exceed 64 MiB of uncompressed BGRA.
        internal static bool HasSafeMask(int width, int height, int compressedBytes)
        {
            return width > 0 && height > 0 && compressedBytes > 0 &&
                   (long)width * height * 4 <= 64L * 1024 * 1024;
        }

        internal static void SkipCompressedLayer(BinaryReader reader, int compressedBytes)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (compressedBytes < 0) throw new InvalidDataException("Negative LIB layer length.");

            Stream stream = reader.BaseStream;
            if (!stream.CanSeek) throw new NotSupportedException("LIB metadata requires a seekable stream.");
            if (compressedBytes > stream.Length - stream.Position)
                throw new EndOfStreamException("Truncated LIB image layer.");

            stream.Seek(compressedBytes, SeekOrigin.Current);
        }
    }
}
