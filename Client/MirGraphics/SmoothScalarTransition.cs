using System;

namespace Client.MirGraphics
{
    // Single float with the same 10-second easing as ambient daylight.
    // Uses no frame allocations and supports interruption without snapping.
    internal sealed class SmoothScalarTransition
    {
        private readonly long _duration;
        private bool _initialized;
        private float _from, _to;
        private long _start;

        internal SmoothScalarTransition(long durationMilliseconds)
        {
            if (durationMilliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            _duration = durationMilliseconds;
        }

        internal void Reset()
        {
            _initialized = false;
        }

        internal float Sample(float target, long now)
        {
            if (!_initialized)
            {
                _initialized = true;
                _from = _to = target;
                _start = now;
                return target;
            }

            float current = Evaluate(now);
            if (Math.Abs(target - _to) > 0.000001f)
            {
                _from = current;
                _to = target;
                _start = now;
            }
            return Evaluate(now);
        }

        private float Evaluate(long now)
        {
            long elapsed = now - _start;
            if (elapsed <= 0) return _from;
            if (elapsed >= _duration) return _to;

            double t = elapsed / (double)_duration;
            t = t * t * (3.0 - 2.0 * t);
            return (float)(_from + (_to - _from) * t);
        }
    }
}
