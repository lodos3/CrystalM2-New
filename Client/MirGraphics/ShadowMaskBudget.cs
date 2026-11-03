using System;

namespace Client.MirGraphics
{
    // Single-render-thread accounting for lazy GPU shadow mask textures.
    // Counts nominal BGRA bytes; driver pitch/allocation overhead is external.
    internal sealed class ShadowMaskBudget
    {
        private readonly long _limitBytes;
        private long _usedBytes;

        internal long UsedBytes => _usedBytes;
        internal long LimitBytes => _limitBytes;

        internal ShadowMaskBudget(long limitBytes)
        {
            if (limitBytes < 0) throw new ArgumentOutOfRangeException(nameof(limitBytes));
            _limitBytes = limitBytes;
        }

        internal bool TryReserve(int bytes)
        {
            if (bytes < 0) throw new ArgumentOutOfRangeException(nameof(bytes));
            if (bytes > _limitBytes - _usedBytes) return false;
            _usedBytes += bytes;
            return true;
        }

        internal void Release(int bytes)
        {
            if (bytes < 0 || bytes > _usedBytes)
                throw new ArgumentOutOfRangeException(nameof(bytes));
            _usedBytes -= bytes;
        }
    }
}
