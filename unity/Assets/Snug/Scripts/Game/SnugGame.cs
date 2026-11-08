using System.Collections.Generic;
using UnityEngine;
using Snug.Core;

namespace Snug
{
    /// <summary>
    /// The whole game. Add this one component to an empty GameObject in an empty scene and press Play:
    /// it builds the camera, world, UI, audio and ads in code.
    ///
    /// Coordinates: gameplay math runs in "logical px" on a 360-wide portrait screen, y pointing down
    /// (same numbers as the web prototype). ToWorld() converts to Unity units (1 unit = 100 px).
    /// </summary>
    public class SnugGame : MonoBehaviour
    {
        public SnugConfig config = new SnugConfig();

        [Header("Ads")]
        public bool useMockAds = true;
        public string levelPlayAppKey = "";
        public string rewardedAdUnitId = "";
        public string interstitialAdUnitId = "";

        public enum State { Home, Play, Dying, Revive, Over, Paused, Busy }
        public State Current { get; private set; } = State.Home;
        public SaveData Save => save;
        public RunState Run => run;

        public const float PX = 0.01f;
        static readonly float[] GlowBrightness = { 0f, 0.45f, 0.75f, 1f };
        float LW => config.logicalWidth;
        float LH = 640f, BY = 460f;

        Camera cam;
        SaveData save;
        RunState run;
        GateGenerator gen;
        BlobMotion motion;
        readonly AdPolicy policy = new AdPolicy();
        IAdService ads;
        IAnalytics analytics;

        BlobView blob;
        Backdrop backdrop;
        Particles particles;
        SnugAudio sfx;
        SnugUI ui;
        Transform world;

        class Gate { public GateSpec spec; public float y, gap, flashAt = -9f; public bool done; public GateView view; }
        readonly List<Gate> gates = new List<Gate>();
        readonly Stack<GateView> gatePool = new Stack<GateView>();

        float radius, time, freezeUntil, shake, dyingTimer, blobAlpha = 1f, squish, overLockUntil;
        bool holding, fading;
        int pointers, runsSinceInterstitial;
        float sessionStart, lastRewardedAt = -9999f;

        // ------------------------------------------------------------------ setup
        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            save = SaveSystem.Load();
            save.sessions++;
            if (save.firstSeenUnix == 0) save.firstSeenUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Haptics.Enabled = save.haptics;

            cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SpriteFactory.Hex("#0f1d30");
            cam.transform.position = new Vector3(0, 0, -10);

            world = new GameObject("World").transform;
            backdrop = Backdrop.Create(world);
            particles = Particles.Create(world);
            blob = BlobView.Create(world);
            blob.SetSkin(Progression.Skin(save.skin));
            sfx = SnugAudio.Create(transform);
            sfx.Enabled = save.sound;
            ui = SnugUI.Create(this);

            analytics = new DebugAnalytics();
#if SNUG_LEVELPLAY
            ads = useMockAds ? (IAdService)new MockAdService(ui) : new LevelPlayAdService(levelPlayAppKey, rewardedAdUnitId, interstitialAdUnitId);
#else
            ads = new MockAdService(ui);
#endif
            ads.Init();

            motion = new BlobMotion(config);
            radius = config.startRadius;
            sessionStart = Time.realtimeSinceStartup;
        }

        void Start()
        {
            Missions.EnsureForDay(save, System.DateTime.Now);
            var streak = DailyStreak.Check(save, System.DateTime.Now);
            SaveSystem.Save(save);
            ui.ShowHome();
            if (streak.HasValue)
            {
                ui.ShowStreak(streak.Value);
                analytics.Event("streak_claim", ("day", streak.Value.Day), ("coins", streak.Value.Coins));
            }
            analytics.Event("session_start", ("session", save.sessions), ("runs", save.runs));
        }

        // ------------------------------------------------------------------ input (called by SnugUI's hold surface)
        public void OnPress()
        {
            pointers++;
            if (Current == State.Paused) Current = State.Play;
            else if (Current == State.Home) StartRun();
            else if (Current == State.Over && time >= overLockUntil) StartRun();
            holding = Current == State.Play;
        }

        public void OnRelease()
        {
            pointers = Mathf.Max(0, pointers - 1);
            if (pointers == 0) holding = false;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Current == State.Play) { Current = State.Paused; holding = false; pointers = 0; }
            if (paused) SaveSystem.Save(save);
        }

        // ------------------------------------------------------------------ run flow
        public void StartRun()
        {
            if (Current != State.Home && Current != State.Over) return;
            double sessionSec = Time.realtimeSinceStartup - sessionStart;
            double sinceReward = Time.realtimeSinceStartup - lastRewardedAt;
            if (policy.ShowInterstitial(save, runsSinceInterstitial, sessionSec, sinceReward))
            {
                runsSinceInterstitial = 0;
                Current = State.Busy;
                analytics.Event("ad_interstitial", ("runs", save.runs));
                ads.ShowInterstitial("between_runs", BeginRun);
                return;
            }
            BeginRun();
        }

        void BeginRun()
        {
            foreach (var g in gates) Recycle(g);
            gates.Clear();
            run = new RunState(config, save.runs == 0);
            gen = new GateGenerator(config, Random.Range(int.MinValue, int.MaxValue));
            radius = config.startRadius;
            blobAlpha = 1f;
            fading = false;
            freezeUntil = 0f;
            Current = State.Play;
            AddGate(BY - config.firstGateDistance);
            SpawnUntilTop();
            ui.ShowHud(run);
            sfx.Ui();
            analytics.Event("run_start", ("run", save.runs + 1));
        }

        void AddGate(float y)
        {
            var view = gatePool.Count > 0 ? gatePool.Pop() : GateView.Create(world);
            view.gameObject.SetActive(true);
            var spec = gen.Next();
            gates.Add(new Gate { spec = spec, y = y, gap = spec.BaseGap, view = view });
        }

        void SpawnUntilTop()
        {
            if (gates.Count == 0) AddGate(BY - 300f);
            var top = gates[gates.Count - 1];
            while (top.y > -80f)
            {
                AddGate(top.y - gen.Spacing(gen.Count));
                top = gates[gates.Count - 1];
            }
        }

        void Recycle(Gate g)
        {
            g.view.gameObject.SetActive(false);
            gatePool.Push(g.view);
        }

        Gate NextGate()
        {
            foreach (var g in gates) if (!g.done) return g;
            return null;
        }

        void Resolve(Gate g)
        {
            var o = run.Apply(2f * radius, g.gap, g.spec.Gold);
            Vector2 c = ToWorld(LW / 2f, BY);
            Color skin = SpriteFactory.Hex(Progression.Skin(save.skin).Color);
            Color cream = SpriteFactory.Hex("#fff4e8");
            Color gold = SpriteFactory.Hex("#ffd66b");

            switch (o.Fit)
            {
                case Fit.Crash:
                    BeginDying(crash: true);
                    return;
                case Fit.Squeeze:
                    sfx.Squeeze(); Haptics.Medium();
                    particles.Burst(c, radius * PX, 40, g.spec.Gold ? gold : skin, cream, 3.6f, 0.06f);
                    particles.Ring(c, radius * PX);
                    break;
                case Fit.Perfect:
                    sfx.Perfect(run.Chain - 1); Haptics.Light();
                    particles.Burst(c, radius * PX, 26, g.spec.Gold ? gold : skin, cream, 2.4f, 0.045f);
                    particles.Ring(c, radius * PX);
                    break;
                case Fit.Good:
                    sfx.Good();
                    particles.Burst(c, radius * PX, 8, skin, cream, 2f, 0.035f);
                    break;
                case Fit.Loose:
                    sfx.Loose(); Haptics.Light();
                    break;
            }

            g.flashAt = time;
            Color labelCol = o.Fit == Fit.Loose ? SpriteFactory.Hex("#ff8a8a") : o.Fit == Fit.Good ? SpriteFactory.Hex("#9fb3c8") : g.spec.Gold ? gold : cream;
            ui.Popup(o.Label, cam.WorldToScreenPoint(ToWorld(LW / 2f, BY - radius - 26f)), labelCol, o.Label.Length > 10 ? 54 : 72);

            if (o.Coins > 0 && (g.spec.Gold || run.Chain % 3 == 0)) sfx.Coin();
            if (o.Milestone)
            {
                ui.Popup(run.Passed + " gates!", cam.WorldToScreenPoint(ToWorld(LW / 2f, BY - 140f)), gold, 84);
                sfx.Reward();
                particles.Ring(c, 0.6f);
            }
            ui.UpdateHud(run);
            if (o.Faded) BeginDying(crash: false);
        }

        void BeginDying(bool crash)
        {
            Current = State.Dying;
            holding = false;
            fading = !crash;
            dyingTimer = 0.9f;
            if (crash)
            {
                shake = 14f; squish = 1f;
                sfx.Crash(); Haptics.Heavy();
                particles.Splat(ToWorld(LW / 2f, BY), radius * PX, SpriteFactory.Hex(Progression.Skin(save.skin).Color));
            }
            else sfx.Loose();
        }

        /// <summary>After the fail animation: offer one rewarded revive, otherwise score the run.</summary>
        void EndRun()
        {
            if (policy.OfferContinue(run) && ads.RewardedReady)
            {
                Current = State.Revive;
                ui.ShowRevive(run, 4f);
                analytics.Event("ad_offer", ("placement", "continue"), ("score", run.Score));
                return;
            }
            FinishRun();
        }

        public void DeclineRevive()
        {
            if (Current == State.Revive) FinishRun();
        }

        /// <summary>Commits the run once: best, coins, missions, XP. Then shows results.</summary>
        void FinishRun()
        {
            Current = State.Over;
            blobAlpha = 1f;
            save.runs++;
            runsSinceInterstitial++;
            int prevBest = save.best;
            bool isBest = run.Score > save.best;
            if (isBest) save.best = run.Score;
            save.coins += run.Coins;
            var missions = Missions.Track(save, run);
            var levels = Progression.AddRunXp(save, run);
            SaveSystem.Save(save);

            analytics.Event("run_end", ("score", run.Score), ("gates", run.Passed), ("seconds", Mathf.RoundToInt(run.Seconds)),
                ("cause", run.EndedByCrash ? "crash" : "fade"), ("best", isBest), ("continued", run.ContinueUsed));
            foreach (var m in missions) analytics.Event("mission_complete", ("key", m.key));
            foreach (var l in levels) analytics.Event("level_up", ("level", l));

            ui.ShowOver(run, prevBest, isBest, policy.OfferDoubleCoins(run) && ads.RewardedReady, missions, levels);
            overLockUntil = time + 0.7f; // stops a still-held finger from instantly restarting
        }

        public void AcceptRevive()
        {
            if (Current != State.Revive) return;
            Current = State.Busy;
            analytics.Event("ad_rewarded", ("placement", "continue"));
            ads.ShowRewarded("continue", ok =>
            {
                if (!ok) { FinishRun(); return; }
                lastRewardedAt = Time.realtimeSinceStartup;
                run.Revive();
                for (int i = gates.Count - 1; i >= 0; i--)
                    if (gates[i].y >= BY - 260f) { Recycle(gates[i]); gates.RemoveAt(i); }
                SpawnUntilTop();
                radius = 30f; blobAlpha = 1f; fading = false;
                freezeUntil = time + 1.1f;
                Current = State.Play;
                ui.ShowHud(run);
            });
        }

        public void RequestDoubleCoins()
        {
            if (Current != State.Over || !policy.OfferDoubleCoins(run)) return;
            Current = State.Busy;
            analytics.Event("ad_rewarded", ("placement", "double_coins"));
            ads.ShowRewarded("double_coins", ok =>
            {
                Current = State.Over;
                if (!ok) return;
                lastRewardedAt = Time.realtimeSinceStartup;
                save.coins += run.Coins;
                run.Doubled = true;
                SaveSystem.Save(save);
                sfx.Reward();
                ui.OnCoinsDoubled(run);
            });
        }

        public void GoHome()
        {
            foreach (var g in gates) Recycle(g);
            gates.Clear();
            run = null;
            Current = State.Home;
            ui.ShowHome();
        }

        // Settings & shop, called by the UI
        public void SetSound(bool on) { save.sound = on; sfx.Enabled = on; SaveSystem.Save(save); }
        public void SetHaptics(bool on) { save.haptics = on; Haptics.Enabled = on; if (on) Haptics.Light(); SaveSystem.Save(save); }
        public bool BuyOrWearSkin(string id)
        {
            bool owned = save.ownedSkins.Contains(id);
            if (!Progression.TryBuySkin(save, id)) { sfx.Loose(); return false; }
            if (!owned) { sfx.Reward(); analytics.Event("skin_unlock", ("id", id)); } else sfx.Ui();
            blob.SetSkin(Progression.Skin(save.skin));
            SaveSystem.Save(save);
            return true;
        }
        public bool SetTheme(string id)
        {
            if (!Progression.TrySetTheme(save, id)) { sfx.Loose(); return false; }
            sfx.Ui(); SaveSystem.Save(save); return true;
        }
        public void ResetProgress()
        {
            SaveSystem.Erase();
            save = SaveSystem.Load();
            save.sessions = 1;
            Missions.EnsureForDay(save, System.DateTime.Now);
            DailyStreak.Check(save, System.DateTime.Now);
            SaveSystem.Save(save);
            blob.SetSkin(Progression.Skin(save.skin));
            GoHome();
        }
        public void PlayUiSound() => sfx.Ui();
        public void PlayRewardSound() => sfx.Reward();

        // ------------------------------------------------------------------ frame
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
            time += dt;
            Layout();

            switch (Current)
            {
                case State.Play: TickPlay(dt); break;
                case State.Dying:
                    dyingTimer -= dt;
                    if (fading) blobAlpha = Mathf.Max(0f, blobAlpha - dt * 1.4f);
                    if (dyingTimer <= 0f) EndRun();
                    break;
                case State.Home:
                case State.Over:
                    radius = Mathf.Lerp(radius, config.startRadius + Mathf.Sin(time * 2f) * 2f, 0.1f);
                    break;
            }

            float speed = Current == State.Play && run != null ? gen.Speed(run.Passed, run.FirstRun) : 40f;
            var theme = Progression.Theme(save.theme);
            backdrop.Tick(dt, time, speed, LW, LH, PX, theme);
            particles.Tick(dt);
            RenderGates(theme);
            RenderBlob();
            sfx.SetHum(Current == State.Play, holding, radius);
            ui.Tick(time, Current == State.Paused, Current == State.Play && time < freezeUntil, HintText());

            shake = Mathf.Max(0f, shake - dt * 40f);
            squish = Mathf.Max(0f, squish - dt * 2f);
            cam.transform.position = new Vector3(Random.Range(-shake, shake) * 0.4f * PX, Random.Range(-shake, shake) * 0.4f * PX, -10f);
        }

        void Layout()
        {
            float aspect = Screen.width / (float)Mathf.Max(1, Screen.height);
            LH = LW / aspect;
            BY = LH * config.blobHeightFraction;
            cam.orthographicSize = LH * PX / 2f;
        }

        Vector3 ToWorld(float x, float y) => new Vector3((x - LW / 2f) * PX, (LH / 2f - y) * PX, 0f);

        void TickPlay(float dt)
        {
            run.Seconds += dt;
            float assist = 1f;
            var next = NextGate();
            if (save.runs < config.assistRuns && run.Passed < config.assistGates && next != null &&
                Mathf.Abs(radius - next.gap * 0.93f / 2f) < config.assistBand)
                assist = config.assistRateFactor;
            motion.Radius = radius;
            motion.Step(holding, dt, assist);
            radius = motion.Radius;

            if (time < freezeUntil) return;

            float v = gen.Speed(run.Passed, run.FirstRun);
            for (int i = 0; i < gates.Count; i++)
            {
                var g = gates[i];
                g.y += v * dt;
                g.gap = g.spec.GapAt(time);
                if (!g.done && g.y >= BY)
                {
                    g.done = true;
                    Resolve(g);
                    if (Current != State.Play) return;
                }
            }
            for (int i = gates.Count - 1; i >= 0; i--)
                if (gates[i].y > LH + 60f) { Recycle(gates[i]); gates.RemoveAt(i); }
            SpawnUntilTop();
        }

        string HintText()
        {
            if (Current != State.Play || save.runs >= 3 || time < freezeUntil) return null;
            var g = NextGate();
            if (g == null || g.y < BY - 250f) return null;
            float d = 2f * radius;
            if (d > g.gap + 2f) return "Let go to shrink";
            if (d < g.gap * 0.7f) return "Hold to grow";
            return null;
        }

        void RenderGates(ThemeDef theme)
        {
            bool visible = run != null && Current != State.Home;
            Color col = SpriteFactory.Hex(theme.Gate), dark = SpriteFactory.Hex(theme.GateDark);
            Color gold = SpriteFactory.Hex("#ffd66b"), goldDark = SpriteFactory.Hex("#b88a1d");
            float ghostA = run == null ? 0f : run.Passed < 6 ? 0.55f : run.Passed < 20 ? 0.28f : 0.16f;
            foreach (var g in gates)
            {
                g.view.gameObject.SetActive(visible);
                if (!visible) continue;
                float flash = Mathf.Clamp01(0.5f - (time - g.flashAt) * 2f);
                g.view.Render(ToWorld(0, g.y).y, g.gap * PX, config.barHeight * PX, LW / 2f * PX,
                    g.spec.Gold ? gold : col, g.spec.Gold ? goldDark : dark,
                    g.spec.Gold ? ghostA + 0.1f : ghostA, !g.done && g.y > -40f, flash);
            }
        }

        void RenderBlob()
        {
            // squash when a tight gate overlaps the blob, wobble while holding
            float sx = 1f, sy = 1f;
            int face = 0;
            if (Current == State.Play)
            {
                foreach (var g in gates)
                {
                    float dy = Mathf.Abs(g.y - BY), reach = radius + config.barHeight / 2f;
                    float over = 2f * radius / g.gap;
                    if (dy < reach && over > 0.93f)
                    {
                        float k = Mathf.Clamp((over - 0.93f) * 4f, 0f, 0.18f) * (1f - dy / reach);
                        sx -= k; sy += k * 0.8f;
                    }
                    if (!g.done && g.y > BY - 90f && 2f * radius > g.gap * 0.9f) face = 1;
                }
                if (holding && radius > 70f) face = 1;
            }
            if (Current == State.Dying && !fading) face = 2;
            float w = Mathf.Sin(time * (holding ? 14f : 5f)) * (holding ? 0.035f : 0.015f);
            sx += w + squish * 0.6f; sy -= w + squish * 0.5f;

            int glow = run != null && (Current == State.Play || Current == State.Dying || Current == State.Paused) ? run.Glow : config.maxGlow;
            float glow01 = GlowBrightness[Mathf.Clamp(glow, 0, 3)];
            blob.Render(ToWorld(LW / 2f, BY), radius * PX, new Vector2(sx, sy), glow01, blobAlpha, face, time);
        }
    }
}
