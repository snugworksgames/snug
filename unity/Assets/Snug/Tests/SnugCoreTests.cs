using System;
using NUnit.Framework;
using Snug.Core;

// EditMode tests: Window > General > Test Runner > EditMode > Run All.
public class SnugCoreTests
{
    readonly SnugConfig c = new SnugConfig();

    [Test]
    public void FirstGate_HoldingFromTheStart_IsAWin()
    {
        // Players start a run by pressing, so many keep holding. Max size must squeeze through gate 1.
        var fit = RunState.Judge(2f * c.maxRadius, c.scriptedGaps[0], early: true, c);
        Assert.That(fit, Is.EqualTo(Fit.Squeeze).Or.EqualTo(Fit.Perfect));
    }

    [Test]
    public void EveryGap_CanBeFilledAndPassed()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var gen = new GateGenerator(c, seed);
            for (int i = 0; i < 200; i++)
            {
                var g = gen.Next();
                float lo = g.BaseGap - g.Amplitude, hi = g.BaseGap + g.Amplitude;
                Assert.Greater(lo, 2f * c.minRadius, "smallest blob must fit");
                Assert.LessOrEqual(hi * c.perfectFit, 2f * c.maxRadius, "a Perfect must be reachable");
            }
        }
    }

    [Test]
    public void SizeChanges_AreAlwaysPhysicallyReachable()
    {
        for (int seed = 0; seed < 50; seed++)
        {
            var gen = new GateGenerator(c, seed);
            float prev = gen.Next().BaseGap;
            for (int i = 1; i < 200; i++)
            {
                var g = gen.Next();
                if (i < c.scriptedGaps.Length) { prev = g.BaseGap; continue; }
                float seconds = gen.Spacing(i) / gen.Speed(i, false);
                float effective = Math.Max(0f, seconds - c.rampSeconds * (1f - c.minRateFraction) / 2f);
                float possible = 2f * Math.Min(c.growRate, c.shrinkRate) * effective;
                Assert.LessOrEqual(Math.Abs(g.BaseGap - prev), possible * c.reachSlack + 0.01f, $"seed {seed} gate {i}");
                prev = g.BaseGap;
            }
        }
    }

    [Test]
    public void ShortTapsNudge_LongHoldsSweep()
    {
        var m = new BlobMotion(c);
        m.Reset(50f);
        for (int i = 0; i < 3; i++) m.Step(true, 1f / 60f);     // a 50 ms tap
        float tap = m.Radius - 50f;
        m.Reset(50f);
        for (int i = 0; i < 30; i++) m.Step(true, 1f / 60f);    // a 500 ms hold
        float hold = m.Radius - 50f;
        Assert.Less(tap, 3f, "a quick tap is a fine adjustment");
        Assert.Greater(hold, 45f, "a long hold covers ground fast");
    }

    [Test]
    public void ThreeLooseFits_EndTheRun_AndPerfectsRestoreGlow()
    {
        var r = new RunState(c, false);
        r.Apply(30f, 120f, false);
        r.Apply(30f, 120f, false);
        Assert.AreEqual(1, r.Glow);
        r.Apply(110f, 120f, false); // perfect
        Assert.AreEqual(2, r.Glow);
        r.Apply(30f, 120f, false);
        var last = r.Apply(30f, 120f, false);
        Assert.IsTrue(last.Faded);
        Assert.IsTrue(r.Ended);
    }

    [Test]
    public void ChainRaisesMultiplier()
    {
        var r = new RunState(c, false);
        for (int i = 0; i < 6; i++) r.Apply(110f, 120f, false);
        Assert.AreEqual(3, r.Multiplier);
    }

    [Test]
    public void Streak_CountsConsecutiveDays_AndResetsAfterAGap()
    {
        var s = new SaveData();
        var d = new DateTime(2026, 11, 1, 9, 0, 0);
        Assert.AreEqual(1, DailyStreak.Check(s, d).Value.Day);
        Assert.IsNull(DailyStreak.Check(s, d.AddHours(5)));
        Assert.AreEqual(2, DailyStreak.Check(s, d.AddDays(1)).Value.Day);
        Assert.AreEqual(1, DailyStreak.Check(s, d.AddDays(3)).Value.Day);
    }

    [Test]
    public void Day7_GivesTheGiftSkinOnce()
    {
        var s = new SaveData();
        var d = new DateTime(2026, 11, 1);
        StreakReward? r = null;
        for (int i = 0; i < 7; i++) r = DailyStreak.Check(s, d.AddDays(i));
        Assert.AreEqual(DailyStreak.Day7Gift, r.Value.GiftSkin);
        Assert.Contains(DailyStreak.Day7Gift, s.ownedSkins);
    }

    [Test]
    public void Missions_AreThreeDistinct_AndStableForTheDay()
    {
        var a = new SaveData(); var b = new SaveData();
        var d = new DateTime(2026, 11, 8);
        Missions.EnsureForDay(a, d); Missions.EnsureForDay(b, d.AddHours(10));
        Assert.AreEqual(3, a.missions.Count);
        Assert.AreNotEqual(a.missions[0].key, a.missions[1].key);
        for (int i = 0; i < 3; i++) Assert.AreEqual(a.missions[i].key, b.missions[i].key);
    }

    [Test]
    public void NoInterstitials_InTheFirstSession()
    {
        var p = new AdPolicy();
        var s = new SaveData { sessions = 1, runs = 50 };
        Assert.IsFalse(p.ShowInterstitial(s, 10, 9999, 9999));
        s.sessions = 2;
        Assert.IsTrue(p.ShowInterstitial(s, 10, 9999, 9999));
        Assert.IsFalse(p.ShowInterstitial(s, 10, 9999, 30), "cooldown after a rewarded ad");
        s.noAds = true;
        Assert.IsFalse(p.ShowInterstitial(s, 10, 9999, 9999));
    }
}
