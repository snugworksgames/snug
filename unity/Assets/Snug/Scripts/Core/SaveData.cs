using System;
using System.Collections.Generic;

namespace Snug.Core
{
    [Serializable]
    public class MissionState
    {
        public string key;
        public int target;
        public int reward;
        public int progress;
        public bool done;
    }

    /// <summary>Everything persisted between sessions. Public fields so Unity's JsonUtility can serialize it.</summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public int best;
        public int coins;
        public int xp;
        public int level = 1;
        public List<string> ownedSkins = new List<string> { "peach" };
        public string skin = "peach";
        public string theme = "pond";
        public int streak;
        public string lastDay = "";
        public string missionDay = "";
        public List<MissionState> missions = new List<MissionState>();
        public int runs;
        public int sessions;
        public bool sound = true;
        public bool haptics = true;
        public bool noAds;           // set by the optional "Remove ads" purchase
        public long firstSeenUnix;   // for D1/D7 analytics
    }

    public static class Days
    {
        public static string Key(DateTime d) => d.ToString("yyyy-MM-dd");

        public static int Between(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return int.MaxValue;
            DateTime da = DateTime.ParseExact(a, "yyyy-MM-dd", null);
            DateTime db = DateTime.ParseExact(b, "yyyy-MM-dd", null);
            return (int)Math.Round((db - da).TotalDays);
        }
    }
}
