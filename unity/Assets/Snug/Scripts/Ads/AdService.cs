using System;
using UnityEngine;

namespace Snug
{
    public interface IAdService
    {
        void Init();
        bool RewardedReady { get; }
        /// <summary>done(true) only when the player earned the reward.</summary>
        void ShowRewarded(string placement, Action<bool> done);
        void ShowInterstitial(string placement, Action done);
    }

    /// <summary>Editor / prototype ads: a full-screen card with a countdown, drawn by SnugUI.</summary>
    public class MockAdService : IAdService
    {
        readonly SnugUI ui;
        public MockAdService(SnugUI ui) { this.ui = ui; }
        public void Init() { }
        public bool RewardedReady => true;
        public void ShowRewarded(string placement, Action<bool> done) => ui.ShowFakeAd(true, 3, () => done(true));
        public void ShowInterstitial(string placement, Action done) => ui.ShowFakeAd(false, 2, done);
    }

#if SNUG_LEVELPLAY
    // Unity LevelPlay (ironSource) mediation. Install the LevelPlay package, add SNUG_LEVELPLAY to
    // Player > Scripting Define Symbols, and set the app key and ad unit ids on SnugGame.
    // Written against the LevelPlay 8.x ad-unit API; event signatures shift between SDK majors,
    // so check them against the version you install.
    public class LevelPlayAdService : IAdService
    {
        readonly string appKey, rewardedId, interstitialId;
        Unity.Services.LevelPlay.LevelPlayRewardedAd rewarded;
        Unity.Services.LevelPlay.LevelPlayInterstitialAd interstitial;
        Action<bool> rewardedDone;
        Action interstitialDone;
        bool earned;

        public LevelPlayAdService(string appKey, string rewardedId, string interstitialId)
        {
            this.appKey = appKey; this.rewardedId = rewardedId; this.interstitialId = interstitialId;
        }

        public void Init()
        {
            Unity.Services.LevelPlay.LevelPlay.OnInitSuccess += _ =>
            {
                rewarded = new Unity.Services.LevelPlay.LevelPlayRewardedAd(rewardedId);
                rewarded.OnAdRewarded += (info, reward) => earned = true;
                rewarded.OnAdClosed += info => { rewardedDone?.Invoke(earned); rewardedDone = null; rewarded.LoadAd(); };
                rewarded.OnAdDisplayFailed += err => { rewardedDone?.Invoke(false); rewardedDone = null; rewarded.LoadAd(); };
                rewarded.LoadAd();

                interstitial = new Unity.Services.LevelPlay.LevelPlayInterstitialAd(interstitialId);
                interstitial.OnAdClosed += info => { interstitialDone?.Invoke(); interstitialDone = null; interstitial.LoadAd(); };
                interstitial.OnAdDisplayFailed += err => { interstitialDone?.Invoke(); interstitialDone = null; interstitial.LoadAd(); };
                interstitial.LoadAd();
            };
            Unity.Services.LevelPlay.LevelPlay.Init(appKey);
        }

        public bool RewardedReady => rewarded != null && rewarded.IsAdReady();

        public void ShowRewarded(string placement, Action<bool> done)
        {
            if (!RewardedReady) { done(false); return; }
            earned = false;
            rewardedDone = done;
            rewarded.ShowAd(placement);
        }

        public void ShowInterstitial(string placement, Action done)
        {
            if (interstitial == null || !interstitial.IsAdReady()) { done(); return; } // never block play on a missing ad
            interstitialDone = done;
            interstitial.ShowAd(placement);
        }
    }
#endif

    // ====================================================================== analytics
    public interface IAnalytics
    {
        void Event(string name, params (string key, object value)[] data);
    }

    /// <summary>Logs to the console. Swap for Unity Analytics, Firebase or GameAnalytics in production.</summary>
    public class DebugAnalytics : IAnalytics
    {
        public void Event(string name, params (string key, object value)[] data)
        {
            var sb = new System.Text.StringBuilder("[analytics] ").Append(name);
            foreach (var d in data) sb.Append(' ').Append(d.key).Append('=').Append(d.value);
            Debug.Log(sb.ToString());
        }
    }

    // ====================================================================== saving
    public static class SaveSystem
    {
        const string Key = "snug.v1";

        public static Core.SaveData Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var data = JsonUtility.FromJson<Core.SaveData>(json);
                    if (data != null) return data;
                }
            }
            catch (Exception e) { Debug.LogWarning("Snug save unreadable, starting fresh: " + e.Message); }
            return new Core.SaveData { firstSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
        }

        public static void Save(Core.SaveData data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static void Erase() { PlayerPrefs.DeleteKey(Key); PlayerPrefs.Save(); }
    }
}
