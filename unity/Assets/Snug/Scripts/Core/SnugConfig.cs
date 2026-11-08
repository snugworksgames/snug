// Snug: all gameplay tuning in one place. Units are "logical pixels" on a 360-wide portrait screen.
// Pure C# (no UnityEngine) so the rules can be unit-tested and simulated outside the engine.
namespace Snug.Core
{
    [System.Serializable]
    public class SnugConfig
    {
        // Blob
        public float logicalWidth = 360f;
        public float growRate = 140f;      // top radius speed (px/s) while holding
        public float shrinkRate = 160f;    // top radius speed (px/s) while released
        public float minRateFraction = 0.3f; // speed right after pressing/releasing, as a fraction of top speed
        public float rampSeconds = 0.3f;     // time to reach top speed: short taps nudge, long holds sweep
        public float minRadius = 12f;
        public float maxRadius = 98f;
        public float startRadius = 26f;
        public float blobHeightFraction = 0.72f; // blob y as a fraction of screen height, from the top
        public float barHeight = 22f;

        // Pace
        public float firstGateDistance = 310f;
        public float firstRunSlowSpeed = 150f; // first 6 gates of the very first run
        public int firstRunSlowGates = 6;
        public float baseSpeed = 150f;
        public float speedPerGate = 4.5f;
        public float maxSpeed = 360f;
        public float baseSpacing = 300f;
        public float spacingPerGate = 4f;
        public float minSpacing = 190f;

        // Gaps. The first five gates are scripted to teach by play:
        // wide (holding from the start squeezes through) -> narrow -> wide -> narrow -> medium.
        public float[] scriptedGaps = { 190f, 88f, 136f, 70f, 120f };
        public float earlyMinGap = 70f, lateMinGap = 44f;
        public float earlyMaxGap = 132f, lateMaxGap = 176f;
        public float rampGates = 40f;           // gates until gap ranges reach their late values
        public float reachSlack = 0.72f;        // only ask for 72% of the physically possible size change
        public float baseMaxDelta = 28f, maxDeltaPerGate = 4f, maxDeltaCap = 135f;

        // Judging (fit = blob diameter / gap)
        public int earlyGates = 5;
        public float perfectFitEarly = 0.80f;
        public float perfectFit = 0.84f;
        public float goodFit = 0.6f;
        public float crashToleranceEarly = 10f;  // px of overlap forgiven
        public float crashTolerance = 6f;
        public float squeezeMargin = 1.5f;      // within this of the gap = SQUEEZE

        // Scoring
        public int maxGlow = 3;
        public int squeezePoints = 5, perfectPoints = 3, goodPoints = 1;
        public int chainPerMultiplier = 3, maxMultiplier = 5;
        public int goldEvery = 10, goldCoins = 5;

        // Late-game variety
        public int breathingFromGate = 30;
        public float breathingBaseAmp = 6f, breathingAmpPerGate = 0.6f, breathingMaxAmp = 14f;

        // Beginner assist: growth slows near the perfect size for the first gates of the first runs
        public int assistRuns = 3, assistGates = 5;
        public float assistBand = 7f, assistRateFactor = 0.3f;
    }
}
