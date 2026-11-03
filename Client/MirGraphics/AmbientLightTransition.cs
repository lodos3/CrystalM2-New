using System;
using System.Drawing;

namespace Client.MirGraphics
{
    // Provides a short, allocation-free visual transition when server time-of-day
    // lighting changes. Server packets and the original per-map light colours win.
    internal sealed class AmbientLightTransition
    {
        private const long TransitionMilliseconds = 10000;
        private bool _initialized;
        private long _startTime;
        private Color _from;
        private Color _target;

        internal void Reset()
        {
            _initialized = false;
        }

        internal static Color Target(LightSetting setting, byte mapDarkLight)
        {
            switch (setting)
            {
                case LightSetting.Night:
                    switch (mapDarkLight)
                    {
                        case 1: return Color.FromArgb(255, 20, 20, 20);
                        case 2: return Color.LightSlateGray;
                        case 3: return Color.SkyBlue;
                        case 4: return Color.Goldenrod;
                        default: return Color.Black;
                    }
                case LightSetting.Dawn:
                case LightSetting.Evening:
                    return Color.FromArgb(255, 50, 50, 50);
                default:
                    return Color.White;
            }
        }

        internal Color Sample(Color target, long now)
        {
            if (!_initialized)
            {
                _initialized = true;
                _from = target;
                _target = target;
                _startTime = now;
                return target;
            }

            Color current = Evaluate(now);
            if (target.ToArgb() != _target.ToArgb())
            {
                _from = current;
                _target = target;
                _startTime = now;
            }
            return Evaluate(now);
        }

        private Color Evaluate(long now)
        {
            long elapsed = now - _startTime;
            if (elapsed <= 0) return _from;
            if (elapsed >= TransitionMilliseconds) return _target;

            double t = elapsed / (double)TransitionMilliseconds;
            t = t * t * (3 - 2 * t);
            return Color.FromArgb(255,
                Mix(_from.R, _target.R, t),
                Mix(_from.G, _target.G, t),
                Mix(_from.B, _target.B, t));
        }

        private static int Mix(int start, int end, double t)
        {
            return (int)Math.Round(start + (end - start) * t,
                                    MidpointRounding.AwayFromZero);
        }
    }
}
