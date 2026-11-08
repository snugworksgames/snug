using System;

namespace Snug.Core
{
    public enum Fit { Crash, Squeeze, Perfect, Good, Loose }

    public struct FitOutcome
    {
        public Fit Fit;
        public int Points;
        public int Coins;
        public int Multiplier;
        public bool Faded;      // glow ran out
        public bool Milestone;  // 10 / 25 / 50 gates
        public string Label;
    }

    /// <summary>Everything about one run: judging, score, glow (lives), chain and per-run stats.</summary>
    public sealed class RunState
    {
        readonly SnugConfig c;

        public bool FirstRun;
        public int Score, Glow, Chain, BestChain, Passed, Perfects, Squeezes, Coins, GoldHits;
        public bool ContinueUsed, Doubled;
        public float Seconds;
        public bool Ended;
        public bool EndedByCrash;

        public RunState(SnugConfig config, bool firstRun)
        {
            c = config;
            FirstRun = firstRun;
            Glow = c.maxGlow;
        }

        public int Multiplier => 1 + Math.Min(c.maxMultiplier - 1, Chain / c.chainPerMultiplier);

        public static Fit Judge(float diameter, float gap, bool early, SnugConfig c)
        {
            float tol = early ? c.crashToleranceEarly : c.crashTolerance;
            if (diameter > gap + tol) return Fit.Crash;
            if (diameter > gap - c.squeezeMargin) return Fit.Squeeze;
            float fit = diameter / gap;
            if (fit >= (early ? c.perfectFitEarly : c.perfectFit)) return Fit.Perfect;
            if (fit >= c.goodFit) return Fit.Good;
            return Fit.Loose;
        }

        public FitOutcome Apply(float diameter, float gap, bool gold)
        {
            var o = new FitOutcome { Fit = Judge(diameter, gap, Passed < c.earlyGates, c) };
            if (o.Fit == Fit.Crash)
            {
                Ended = true;
                EndedByCrash = true;
                o.Label = "Squished!";
                return o;
            }

            Passed++;
            switch (o.Fit)
            {
                case Fit.Squeeze:
                    Chain++; Perfects++; Squeezes++;
                    o.Points = c.squeezePoints; o.Label = "SQUEEZE!";
                    if (Glow < c.maxGlow) Glow++;
                    break;
                case Fit.Perfect:
                    Chain++; Perfects++;
                    o.Points = c.perfectPoints; o.Label = Chain >= 3 ? "PERFECT x" + Chain : "PERFECT!";
                    if (Glow < c.maxGlow) Glow++;
                    break;
                case Fit.Good:
                    Chain = 0; o.Points = c.goodPoints; o.Label = "Good";
                    break;
                default:
                    Chain = 0; Glow--; o.Label = FirstRun && Passed < 8 ? "Too loose! Hold to grow" : "Too loose";
                    break;
            }

            BestChain = Math.Max(BestChain, Chain);
            o.Multiplier = Multiplier;
            Score += o.Points * o.Multiplier;

            if (o.Points >= c.perfectPoints)
            {
                o.Coins = gold ? c.goldCoins : 1;
                Coins += o.Coins;
                if (gold) { GoldHits++; o.Label = "GOLD! +" + c.goldCoins; }
            }

            o.Milestone = Passed == 10 || Passed == 25 || Passed == 50 || Passed == 100;
            if (Glow <= 0) { o.Faded = true; Ended = true; }
            return o;
        }

        /// <summary>Rewarded-ad continue: full glow, chain reset.</summary>
        public void Revive()
        {
            ContinueUsed = true;
            Ended = false;
            EndedByCrash = false;
            Glow = c.maxGlow;
            Chain = 0;
        }
    }
}
