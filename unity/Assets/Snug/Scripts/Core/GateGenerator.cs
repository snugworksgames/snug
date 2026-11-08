using System;

namespace Snug.Core
{
    public sealed class GateSpec
    {
        public int Index;
        public float BaseGap;
        public float Amplitude;  // >0 = "breathing" gate whose gap pulses
        public float Phase;
        public bool Gold;

        public float GapAt(float time) => BaseGap + Amplitude * (float)Math.Sin(time * 3f + Phase);
    }

    /// <summary>
    /// Builds the gate sequence. Difficulty rises through speed, tighter spacing, wider gap swings and
    /// breathing gates, but every size change is checked against what the blob can physically do in
    /// the time available, so a loss is always the player's timing, never an impossible gate.
    /// </summary>
    public sealed class GateGenerator
    {
        readonly SnugConfig c;
        readonly Random rng;
        float prevGap = 60f;
        int index;

        public GateGenerator(SnugConfig config, int seed)
        {
            c = config;
            rng = new Random(seed);
        }

        public int Count => index;

        public float Speed(int gatesPassed, bool firstRun) =>
            firstRun && gatesPassed < c.firstRunSlowGates
                ? c.firstRunSlowSpeed
                : Math.Min(c.baseSpeed + c.speedPerGate * gatesPassed, c.maxSpeed);

        public float Spacing(int gateIndex) => Math.Max(c.minSpacing, c.baseSpacing - c.spacingPerGate * gateIndex);

        /// <summary>Largest diameter change the next gate may ask for, given the time until it arrives.</summary>
        public float MaxFairDelta(int gateIndex)
        {
            float secondsBetween = Spacing(gateIndex) / Speed(gateIndex, false);
            // distance covered in that time, including the slow start of the speed ramp
            float effective = Math.Max(0f, secondsBetween - c.rampSeconds * (1f - c.minRateFraction) / 2f);
            float reach = 2f * Math.Min(c.growRate, c.shrinkRate) * effective * c.reachSlack;
            return Math.Min(Math.Min(c.baseMaxDelta + gateIndex * c.maxDeltaPerGate, c.maxDeltaCap), reach);
        }

        public GateSpec Next()
        {
            int n = index++;
            float gap;
            if (n < c.scriptedGaps.Length)
            {
                gap = c.scriptedGaps[n];
            }
            else
            {
                float k = Clamp01(n / c.rampGates);
                float minG = Lerp(c.earlyMinGap, c.lateMinGap, k);
                float maxG = Lerp(c.earlyMaxGap, c.lateMaxGap, k);
                float maxDelta = MaxFairDelta(n);
                float minDelta = Math.Min(18f, maxDelta * 0.5f); // avoid dull repeats
                int dir = rng.NextDouble() < 0.5 ? -1 : 1;
                if (prevGap - maxDelta < minG) dir = 1;
                if (prevGap + maxDelta > maxG) dir = -1;
                float delta = minDelta + (float)rng.NextDouble() * (maxDelta - minDelta);
                gap = Clamp(prevGap + dir * delta, minG, maxG);
            }

            float amp = 0f;
            if (n >= c.breathingFromGate)
                amp = Math.Min(c.breathingBaseAmp + (n - c.breathingFromGate) * c.breathingAmpPerGate, c.breathingMaxAmp);

            prevGap = gap;
            return new GateSpec
            {
                Index = n,
                BaseGap = gap,
                Amplitude = amp,
                Phase = (float)(rng.NextDouble() * Math.PI * 2),
                Gold = n > 0 && n % c.goldEvery == c.goldEvery - 1,
            };
        }

        static float Lerp(float a, float b, float t) => a + (b - a) * t;
        static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v;
        static float Clamp01(float v) => Clamp(v, 0f, 1f);
    }
}
