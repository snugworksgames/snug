using System;

namespace Snug.Core
{
    /// <summary>
    /// The one control: hold = grow, release = shrink. Speed ramps up from a slow start after every
    /// press or release, so quick taps make fine adjustments and long holds make big ones.
    /// </summary>
    public sealed class BlobMotion
    {
        readonly SnugConfig c;
        bool lastHold;
        float sinceToggle;

        public float Radius;

        public BlobMotion(SnugConfig config) { c = config; Reset(c.startRadius); }

        public void Reset(float radius)
        {
            Radius = radius;
            sinceToggle = 0f;
        }

        public float Rate(bool holding)
        {
            float top = holding ? c.growRate : c.shrinkRate;
            float k = c.rampSeconds <= 0f ? 1f : Math.Min(1f, sinceToggle / c.rampSeconds);
            return top * (c.minRateFraction + (1f - c.minRateFraction) * k);
        }

        /// <param name="assist">multiplier on speed, e.g. the beginner "sticky" zone near the perfect size</param>
        public void Step(bool holding, float dt, float assist = 1f)
        {
            if (holding != lastHold) { lastHold = holding; sinceToggle = 0f; }
            sinceToggle += dt;
            float d = Rate(holding) * assist * dt;
            Radius = holding ? Math.Min(c.maxRadius, Radius + d) : Math.Max(c.minRadius, Radius - d);
        }
    }
}
