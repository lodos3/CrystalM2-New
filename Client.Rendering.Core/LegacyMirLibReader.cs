using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace CrystalM2.Rendering.Core
{
    public sealed class LegacySpriteFrame
    {
        public int Width { get; }
        public int Height { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public int ShadowOffsetX { get; }
        public int ShadowOffsetY { get; }
        public byte ShadowFlags { get; }
        public byte[] Bgra { get; }
        public LegacySpriteFrame? Mask { get; }

        internal LegacySpriteFrame(int width, int height, int offsetX, int offsetY,
                                   int shadowX, int shadowY, byte shadow,
                                   byte[] bgra, LegacySpriteFrame? mask)
        {
            Width = width; Height = height;
            OffsetX = offsetX; OffsetY = offsetY;
            ShadowOffsetX = shadowX; ShadowOffsetY = shadowY;
            ShadowFlags = shadow;
            Bgra = bgra;
            Mask = mask;
        }
    }

    // Original CrystalM2 .Lib v2/v3 index and GZip BGRA frame layout.
    // Pure file decoder: no SlimDX, Vortice or Windows dependency.
    // Does not try to improve pixels or infer missing normals/materials.
    public sealed class LegacyMirLibReader : IDisposable
    {
        public const int MaxFrameBytes = 64 * 1024 * 1024;
        public const int MaxCompressedBytes = 32 * 1024 * 1024;
        public const int MaxFrames = 500_000;
        private const int HeaderBytes = 17;

        private readonly BinaryReader _reader;
        private readonly Stream _stream;
        private readonly int[] _offsets;
        private readonly object _gate = new object();

        public int Count => _offsets.Length;

        public LegacyMirLibReader(Stream stream, bool leaveOpen = false)
        {
            if (stream == null || !stream.CanRead || !stream.CanSeek)
                throw new ArgumentException("A readable seekable LIB stream is required.");

            _stream = stream;
            _reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen);
            try
            {
                int version = _reader.ReadInt32();
                if (version != 2 && version != 3)
                    throw new InvalidDataException("Only CrystalM2 LIB versions 2/3 are supported.");
                int count = _reader.ReadInt32();
                if (count < 0 || count > MaxFrames ||
                    (long)count * sizeof(int) > stream.Length - (version == 3 ? 12 : 8))
                    throw new InvalidDataException("Invalid LIB image index count.");
                if (version == 3) _reader.ReadInt32(); // FrameSet metadata offset.
                _offsets = new int[count];
                for (int i = 0; i < count; i++)
                {
                    int offset = _reader.ReadInt32();
                    // A zero entry is a missing sprite, not a valid image.
                    if (offset != 0 &&
                        (offset < (version == 3 ? 12 : 8) + count * 4 ||
                         offset > stream.Length - HeaderBytes))
                        throw new InvalidDataException("LIB frame offset outside the archive.");
                    _offsets[i] = offset;
                }
            }
            catch
            {
                _reader.Dispose();
                throw;
            }
        }

        public bool TryReadFrame(int index, out LegacySpriteFrame? frame,
                                 bool includeMask = false)
        {
            frame = null;
            if (index < 0 || index >= _offsets.Length || _offsets[index] == 0)
                return false;
            frame = ReadFrame(index, includeMask);
            return true;
        }

        public LegacySpriteFrame ReadFrame(int index, bool includeMask = false)
        {
            if (index < 0 || index >= _offsets.Length || _offsets[index] == 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            lock (_gate)
            {
                _stream.Seek(_offsets[index], SeekOrigin.Begin);
                short width = _reader.ReadInt16();
                short height = _reader.ReadInt16();
                short x = _reader.ReadInt16();
                short y = _reader.ReadInt16();
                short shadowX = _reader.ReadInt16();
                short shadowY = _reader.ReadInt16();
                byte shadow = _reader.ReadByte();
                int length = _reader.ReadInt32();

                byte[] pixels = InflateExact(width, height, length);
                LegacySpriteFrame? mask = null;

                // Mir's second layer is an optional *authored* effect mask.
                // Never merge or replace baked RGB shadow details with it.
                if (includeMask && (shadow & 0x80) != 0)
                {
                    if (_stream.Length - _stream.Position < 12)
                        throw new InvalidDataException("Truncated LIB mask header.");
                    short maskWidth = _reader.ReadInt16();
                    short maskHeight = _reader.ReadInt16();
                    short maskX = _reader.ReadInt16();
                    short maskY = _reader.ReadInt16();
                    int maskLength = _reader.ReadInt32();
                    byte[] maskPixels = InflateExact(maskWidth, maskHeight, maskLength);
                    mask = new LegacySpriteFrame(maskWidth, maskHeight,
                                                maskX, maskY, 0, 0, 0, maskPixels, null);
                }

                return new LegacySpriteFrame(width, height, x, y, shadowX, shadowY,
                                             shadow, pixels, mask);
            }
        }

        private byte[] InflateExact(int width, int height, int compressedLength)
        {
            long expected = (long)width * height * 4;
            if (width <= 0 || height <= 0 || expected > MaxFrameBytes ||
                compressedLength <= 0 || compressedLength > MaxCompressedBytes ||
                compressedLength > _stream.Length - _stream.Position)
                throw new InvalidDataException("Invalid dimensions or compressed LIB frame size.");

            byte[] compressed = _reader.ReadBytes(compressedLength);
            if (compressed.Length != compressedLength)
                throw new InvalidDataException("Truncated LIB compressed data.");

            var pixels = new byte[(int)expected];
            using var data = new MemoryStream(compressed, writable: false);
            using var gzip = new GZipStream(data, CompressionMode.Decompress);
            try
            {
                gzip.ReadExactly(pixels);
                if (gzip.ReadByte() != -1)
                    throw new InvalidDataException("Frame decompresses beyond its declared dimensions.");
            }
            catch (EndOfStreamException ex)
            {
                throw new InvalidDataException("Truncated decompressed LIB image.", ex);
            }
            return pixels;
        }

        public void Dispose() => _reader.Dispose();
    }
}
