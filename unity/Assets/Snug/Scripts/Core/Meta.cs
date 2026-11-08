using System;
using System.Collections.Generic;

namespace Snug.Core
{
    // ---------------------------------------------------------------- daily streak
    public struct StreakReward
    {
        public int Day;        // 1..n
        public int DayInCycle; // 0..6
        public int Coins;
        public string GiftSkin; // null unless granted today
    }

    public static class DailyStreak
    {
        public static readonly int[] Rewards = { 10, 15, 20, 25, 30, 40, 50 };
        public const string Day7Gift = "aurora";

        /// <summary>Call on app open. Returns a reward the first time each calendar day, otherwise null.</summary>
        public static StreakReward? Check(SaveData s, DateTime localNow)
        {
            string today = Days.Key(localNow);
            if (s.lastDay == today) return null;
            s.streak = Days.Between(s.lastDay, today) == 1 ? s.streak + 1 : 1;
            s.lastDay = today;

            int idx = (s.streak - 1) % Rewards.Length;
            var r = new StreakReward { Day = s.streak, DayInCycle = idx, Coins = Rewards[idx] };
            s.coins += r.Coins;
            if (idx == Rewards.Length - 1 && !s.ownedSkins.Contains(Day7Gift))
            {
                s.ownedSkins.Add(Day7Gift);
                r.GiftSkin = Day7Gift;
            }
            return r;
        }
    }

    // ---------------------------------------------------------------- daily missions
    public sealed class MissionDef
    {
        public string Key; public int Target; public int Reward; public string Text;
        public MissionDef(string k, int t, int r, string text) { Key = k; Target = t; Reward = r; Text = text; }
    }

    public static class Missions
    {
        public static readonly MissionDef[] Pool =
        {
            new MissionDef("perfRun", 6, 25, "Get {0} Perfects in one run"),
            new MissionDef("chain", 4, 30, "Chain {0} Perfects in a row"),
            new MissionDef("gatesRun", 25, 30, "Pass {0} gates in one run"),
            new MissionDef("scoreRun", 40, 35, "Score {0} in one run"),
            new MissionDef("runs", 5, 20, "Play {0} runs"),
            new MissionDef("coins", 15, 25, "Collect {0} coins"),
            new MissionDef("squeeze", 2, 30, "Pull off {0} Squeezes"),
            new MissionDef("gold", 1, 30, "Nail a gold gate"),
        };

        public static MissionDef Def(string key) => Array.Find(Pool, d => d.Key == key);
        public static string Describe(MissionState m) => string.Format(Def(m.key).Text, m.target);

        /// <summary>Same three missions for everyone on a given date (seeded), refreshed at local midnight.</summary>
        public static void EnsureForDay(SaveData s, DateTime localNow)
        {
            string key = Days.Key(localNow);
            if (s.missionDay == key && s.missions.Count == 3) return;
            var rng = new Random(StableHash(key)); // string.GetHashCode differs between runtimes
            var pool = new List<MissionDef>(Pool);
            s.missions.Clear();
            while (s.missions.Count < 3)
            {
                int i = rng.Next(pool.Count);
                var d = pool[i];
                pool.RemoveAt(i);
                s.missions.Add(new MissionState { key = d.Key, target = d.Target, reward = d.Reward });
            }
            s.missionDay = key;
        }

        static int StableHash(string text)
        {
            unchecked
            {
                int h = 23;
                foreach (char ch in text) h = h * 31 + ch;
                return h & 0x7fffffff;
            }
        }

        /// <summary>Applies a finished run. Pays rewards and returns the missions completed by it.</summary>
        public static List<MissionState> Track(SaveData s, RunState r)
        {
            var completed = new List<MissionState>();
            foreach (var m in s.missions)
            {
                if (m.done) continue;
                switch (m.key)
                {
                    case "perfRun": m.progress = Math.Max(m.progress, r.Perfects); break;
                    case "chain": m.progress = Math.Max(m.progress, r.BestChain); break;
                    case "gatesRun": m.progress = Math.Max(m.progress, r.Passed); break;
                    case "scoreRun": m.progress = Math.Max(m.progress, r.Score); break;
                    case "runs": m.progress++; break;
                    case "coins": m.progress += r.Coins; break;
                    case "squeeze": m.progress += r.Squeezes; break;
                    case "gold": m.progress += r.GoldHits; break;
                }
                if (m.progress >= m.target)
                {
                    m.progress = m.target;
                    m.done = true;
                    s.coins += m.reward;
                    completed.Add(m);
                }
            }
            return completed;
        }
    }

    // ---------------------------------------------------------------- levels, skins, themes
    public sealed class SkinDef
    {
        public string Id, Name, Color, Dark; public int Cost; // Cost < 0 = not for sale
        public SkinDef(string id, string name, string color, string dark, int cost) { Id = id; Name = name; Color = color; Dark = dark; Cost = cost; }
    }

    public sealed class ThemeDef
    {
        public string Id, Name, Top, Bottom, Gate, GateDark, Mote; public int Level;
        public ThemeDef(string id, string name, string top, string bottom, string gate, string gateDark, string mote, int level)
        { Id = id; Name = name; Top = top; Bottom = bottom; Gate = gate; GateDark = gateDark; Mote = mote; Level = level; }
    }

    public static class Progression
    {
        public static readonly SkinDef[] Skins =
        {
            new SkinDef("peach", "Peach", "#ffb38a", "#c4744f", 0),
            new SkinDef("mint", "Mint", "#8ff0c8", "#3fa985", 150),
            new SkinDef("lilac", "Lilac", "#c9a7ff", "#7f5bc4", 300),
            new SkinDef("lemon", "Lemon", "#ffe36e", "#c2a12a", 450),
            new SkinDef("sky", "Sky", "#8fd3ff", "#3f8ec4", 600),
            new SkinDef("ruby", "Ruby", "#ff7b9c", "#c4446a", 800),
            new SkinDef("aurora", "Aurora", "#9ff5e6", "#8a6cff", -1),
        };

        public static readonly ThemeDef[] Themes =
        {
            new ThemeDef("pond", "Night pond", "#0f1d30", "#1d3b55", "#7fd1b9", "#3f8f78", "#7fd1b9", 1),
            new ThemeDef("dusk", "Dusk", "#241a33", "#5a3550", "#ffc9a3", "#b97a5a", "#ffd0e0", 3),
            new ThemeDef("moss", "Moss", "#122219", "#2c4a33", "#e9d78f", "#a5914a", "#d8f0a0", 5),
            new ThemeDef("ember", "Ember", "#1f1012", "#4b2419", "#ffb070", "#b7652c", "#ffcf8a", 8),
        };

        public const int CoinsPerLevel = 20;
        public static int XpNeeded(int level) => 80 + level * 40;

        public static SkinDef Skin(string id) => Array.Find(Skins, s => s.Id == id) ?? Skins[0];
        public static ThemeDef Theme(string id) => Array.Find(Themes, t => t.Id == id) ?? Themes[0];

        /// <summary>XP = score + gates passed. Returns the new levels reached (each pays coins).</summary>
        public static List<int> AddRunXp(SaveData s, RunState r)
        {
            var levels = new List<int>();
            s.xp += r.Score + r.Passed;
            while (s.xp >= XpNeeded(s.level))
            {
                s.xp -= XpNeeded(s.level);
                s.level++;
                s.coins += CoinsPerLevel;
                levels.Add(s.level);
            }
            return levels;
        }

        public static bool TryBuySkin(SaveData s, string id)
        {
            var skin = Skin(id);
            if (s.ownedSkins.Contains(id)) { s.skin = id; return true; }
            if (skin.Cost < 0 || s.coins < skin.Cost) return false;
            s.coins -= skin.Cost;
            s.ownedSkins.Add(id);
            s.skin = id;
            return true;
        }

        public static bool TrySetTheme(SaveData s, string id)
        {
            var t = Theme(id);
            if (s.level < t.Level) return false;
            s.theme = id;
            return true;
        }
    }

    // ---------------------------------------------------------------- ad placement rules
    /// <summary>
    /// When ads may appear. Rewarded ads are always the player's choice. Interstitials are rare and
    /// only between runs, never in the first session, and never right after a rewarded ad.
    /// </summary>
    public sealed class AdPolicy
    {
        public int continueMinScore = 6;
        public int doubleMinCoins = 3;
        public int interstitialMinTotalRuns = 6;
        public int interstitialEveryRuns = 4;
        public double interstitialMinSessionSeconds = 150;
        public double cooldownAfterRewardedSeconds = 90;

        public bool OfferContinue(RunState r) => !r.ContinueUsed && r.Score >= continueMinScore;
        public bool OfferDoubleCoins(RunState r) => !r.Doubled && r.Coins >= doubleMinCoins;

        public bool ShowInterstitial(SaveData s, int runsSinceLast, double sessionSeconds, double secondsSinceRewarded) =>
            !s.noAds
            && s.sessions > 1
            && s.runs >= interstitialMinTotalRuns
            && runsSinceLast >= interstitialEveryRuns
            && sessionSeconds >= interstitialMinSessionSeconds
            && secondsSinceRewarded >= cooldownAfterRewardedSeconds;
    }
}
