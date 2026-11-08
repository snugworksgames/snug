using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Snug.Core;

namespace Snug
{
    /// <summary>Full-screen invisible layer behind all UI. Pressing anywhere that is not a button = hold.</summary>
    public class HoldSurface : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public SnugGame game;
        public void OnPointerDown(PointerEventData e) => game.OnPress();
        public void OnPointerUp(PointerEventData e) => game.OnRelease();
    }

    /// <summary>
    /// All screens, built in code with uGUI so the prototype has no prefabs.
    /// Reference resolution 1080 wide; sizes are roughly 3x the web prototype's CSS px.
    /// For production, rebuild these as prefabs with TextMeshPro and a rounded display font.
    /// </summary>
    public class SnugUI : MonoBehaviour
    {
        static readonly Color Cream = SpriteFactory.Hex("#fff4e8"), Muted = SpriteFactory.Hex("#9fb3c8"),
            Peach = SpriteFactory.Hex("#ffb38a"), PeachDark = SpriteFactory.Hex("#3a1d10"), Mint = SpriteFactory.Hex("#7fd1b9"),
            Gold = SpriteFactory.Hex("#ffd66b"), Danger = SpriteFactory.Hex("#ff8a8a"), AdBlue = SpriteFactory.Hex("#2a4d6b"),
            Panel = new Color(0.055f, 0.106f, 0.176f, 0.88f), Dim = new Color(0.024f, 0.047f, 0.086f, 0.7f);

        SnugGame game;
        Font font;
        RectTransform root, safe, home, hud, over, overlays, popups, toasts;
        Text coinsTxt, levelTxt, streakTxt, bestTxt, hintTxt, scoreTxt, multTxt, statusTxt, coachTxt;
        Image xpFill;
        Image[] pips;
        RectTransform missionList;
        Text oScore, oBest, oMsg, oGates, oPerf, oCoins;
        Button doubleBtn;
        Rect lastSafe;

        // revive countdown
        RectTransform revivePanel;
        Text reviveCount;
        float reviveLeft;

        public static SnugUI Create(SnugGame game)
        {
            var go = new GameObject("UI", typeof(RectTransform));
            var ui = go.AddComponent<SnugUI>();
            ui.game = game;
            ui.Build();
            return ui;
        }

        // ================================================================== build
        void Build()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0f;
            gameObject.AddComponent<GraphicRaycaster>();

#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() == null)
#else
            if (FindObjectOfType<EventSystem>() == null)
#endif
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            root = (RectTransform)transform;
            var hold = Img(root, "HoldSurface", Color.clear, true);
            Stretch(hold.rectTransform);
            hold.gameObject.AddComponent<HoldSurface>().game = game;

            safe = Node(root, "SafeArea");
            Stretch(safe);

            BuildHome();
            BuildHud();
            BuildOver();
            popups = Node(safe, "Popups"); Stretch(popups);
            coachTxt = Txt(safe, "", 50, Cream, TextAnchor.MiddleCenter);
            Place(coachTxt.rectTransform, new Vector2(0, 0.1f), new Vector2(1, 0.2f), Vector2.zero, Vector2.zero);
            statusTxt = Txt(safe, "", 100, Cream, TextAnchor.MiddleCenter);
            Place(statusTxt.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.72f), Vector2.zero, Vector2.zero);
            toasts = Node(safe, "Toasts");
            Place(toasts, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -560), new Vector2(-40, -180));
            var v = toasts.gameObject.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter; v.spacing = 16; v.childControlHeight = false; v.childControlWidth = false;
            overlays = Node(root, "Overlays"); Stretch(overlays);
        }

        void BuildHome()
        {
            home = Node(safe, "Home"); Stretch(home);

            // top bar
            var bar = Node(home, "TopBar");
            Place(bar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -150), new Vector2(-40, -40));
            var h = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20; h.childForceExpandWidth = false; h.childControlWidth = true; h.childControlHeight = true; h.childAlignment = TextAnchor.MiddleLeft;

            coinsTxt = Chip(bar, "0", 180).GetComponentInChildren<Text>();
            var lvl = Chip(bar, "", 0);
            lvl.GetComponent<LayoutElement>().flexibleWidth = 1;
            levelTxt = lvl.GetComponentInChildren<Text>();
            levelTxt.alignment = TextAnchor.UpperLeft;
            levelTxt.rectTransform.offsetMin = new Vector2(36, 0);
            var xpBack = Img(lvl.transform, "XpBack", new Color(1, 1, 1, 0.12f));
            Place(xpBack.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 22), new Vector2(-36, 40));
            xpFill = Img(xpBack.transform, "Xp", Mint);
            xpFill.rectTransform.anchorMin = Vector2.zero; xpFill.rectTransform.anchorMax = new Vector2(0, 1);
            xpFill.rectTransform.offsetMin = xpFill.rectTransform.offsetMax = Vector2.zero;
            streakTxt = Chip(bar, "Day 1", 200).GetComponentInChildren<Text>();
            streakTxt.color = SpriteFactory.Hex("#ffb070");
            var looks = Btn(bar, "Looks", Panel, Cream, OpenLooks, 40);
            Layout(looks.gameObject, 200, 110);
            var set = Btn(bar, "···", Panel, Cream, OpenSettings, 40);
            Layout(set.gameObject, 110, 110);

            // title
            var title = Txt(home, "Snug", 280, Peach, TextAnchor.MiddleCenter);
            Place(title.rectTransform, new Vector2(0, 0.6f), new Vector2(1, 0.78f), Vector2.zero, Vector2.zero);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -16);
            title.GetComponent<Shadow>().effectColor = SpriteFactory.Hex("#c4744f");
            var tag = Txt(home, "Hold to grow. Let go to shrink. Fit the gap.", 44, Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            Place(tag.rectTransform, new Vector2(0, 0.56f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero);
            bestTxt = Txt(home, "BEST 0", 44, Cream, TextAnchor.MiddleCenter);
            Place(bestTxt.rectTransform, new Vector2(0, 0.52f), new Vector2(1, 0.56f), Vector2.zero, Vector2.zero);
            hintTxt = Txt(home, "Hold anywhere to start", 54, Cream, TextAnchor.MiddleCenter);
            Place(hintTxt.rectTransform, new Vector2(0, 0.45f), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);

            // missions card
            var card = Img(home, "Missions", Panel, true);
            Rounded(card);
            Place(card.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 40), new Vector2(-40, 440));
            var head = Txt(card.transform, "TODAY'S MISSIONS", 34, Muted, TextAnchor.UpperLeft);
            Place(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -80), new Vector2(-40, -26));
            missionList = Node(card.transform, "List");
            Place(missionList, Vector2.zero, Vector2.one, new Vector2(40, 30), new Vector2(-40, -90));
            var vl = missionList.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 18; vl.childControlHeight = false; vl.childForceExpandHeight = false; vl.childControlWidth = true;
        }

        void BuildHud()
        {
            hud = Node(safe, "Hud"); Stretch(hud);
            hud.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            scoreTxt = Txt(hud, "0", 190, Cream, TextAnchor.UpperCenter);
            Place(scoreTxt.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -260), new Vector2(0, -30));
            scoreTxt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -10);
            pips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                pips[i] = Img(hud, "Pip", Peach);
                pips[i].sprite = SpriteFactory.Circle;
                Place(pips[i].rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(50 + i * 64, -110), new Vector2(92 + i * 64, -68));
            }
            multTxt = Txt(hud, "", 64, Gold, TextAnchor.UpperRight);
            Place(multTxt.rectTransform, new Vector2(0.6f, 1), new Vector2(1, 1), new Vector2(0, -140), new Vector2(-50, -60));
        }

        void BuildOver()
        {
            over = Node(safe, "Over"); Stretch(over);
            var label = Txt(over, "SCORE", 36, Muted, TextAnchor.MiddleCenter);
            Place(label.rectTransform, new Vector2(0, 0.8f), new Vector2(1, 0.84f), Vector2.zero, Vector2.zero);
            oScore = Txt(over, "0", 300, Cream, TextAnchor.MiddleCenter);
            Place(oScore.rectTransform, new Vector2(0, 0.66f), new Vector2(1, 0.8f), Vector2.zero, Vector2.zero);
            oBest = Txt(over, "", 46, Muted, TextAnchor.MiddleCenter);
            Place(oBest.rectTransform, new Vector2(0, 0.62f), new Vector2(1, 0.66f), Vector2.zero, Vector2.zero);
            oMsg = Txt(over, "", 54, Peach, TextAnchor.MiddleCenter);
            Place(oMsg.rectTransform, new Vector2(0, 0.565f), new Vector2(1, 0.62f), new Vector2(60, 0), new Vector2(-60, 0));

            var stats = Node(over, "Stats");
            Place(stats, new Vector2(0, 0.45f), new Vector2(1, 0.55f), new Vector2(40, 0), new Vector2(-40, 0));
            var hl = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 24; hl.childForceExpandWidth = true; hl.childControlWidth = true; hl.childControlHeight = true;
            oGates = Stat(stats, "GATES"); oPerf = Stat(stats, "PERFECTS"); oCoins = Stat(stats, "COINS");

            doubleBtn = Btn(over, "2x coins  (watch ad)", AdBlue, Cream, () => game.RequestDoubleCoins(), 48);
            Place((RectTransform)doubleBtn.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 470), new Vector2(-40, 610));
            var retry = Btn(over, "Play again", Peach, PeachDark, () => game.StartRun(), 58);
            Place((RectTransform)retry.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 290), new Vector2(-40, 450));
            var tip = Txt(over, "or hold anywhere", 36, Muted, TextAnchor.MiddleCenter, FontStyle.Normal);
            Place(tip.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 220), new Vector2(0, 280));
            var homeBtn = Btn(over, "Home", Panel, Cream, () => { game.PlayUiSound(); game.GoHome(); }, 46);
            Place((RectTransform)homeBtn.transform, new Vector2(0.25f, 0), new Vector2(0.75f, 0), new Vector2(0, 60), new Vector2(0, 190));
        }

        // ================================================================== screens
        public void ShowHome()
        {
            SetScreen(home);
            var s = game.Save;
            coinsTxt.text = "● " + s.coins;
            levelTxt.text = "Lv " + s.level;
            xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(s.xp / (float)Progression.XpNeeded(s.level)), 1);
            streakTxt.text = "Day " + Mathf.Max(1, s.streak);
            bestTxt.text = "BEST " + s.best;

            foreach (Transform c in missionList) Destroy(c.gameObject);
            foreach (var m in s.missions)
            {
                var row = Node(missionList, "Mission");
                Layout(row.gameObject, 0, 84);
                var t = Txt(row, Missions.Describe(m), 40, m.done ? Muted : Cream, TextAnchor.UpperLeft, FontStyle.Normal);
                Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 20), new Vector2(-180, 0));
                var rw = Txt(row, m.done ? "Done" : "● " + m.reward, 40, m.done ? Mint : Gold, TextAnchor.UpperRight);
                Place(rw.rectTransform, Vector2.zero, Vector2.one, new Vector2(0, 20), Vector2.zero);
                var back = Img(row, "Bar", new Color(1, 1, 1, 0.1f));
                Place(back.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(0, 12));
                var fill = Img(back.transform, "Fill", m.done ? Mint : Peach);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = new Vector2(m.progress / (float)m.target, 1);
                fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            }
        }

        public void ShowHud(RunState run) { SetScreen(hud); UpdateHud(run); }

        public void UpdateHud(RunState run)
        {
            scoreTxt.text = run.Score.ToString();
            for (int i = 0; i < pips.Length; i++) pips[i].color = i < run.Glow ? Peach : new Color(Peach.r, Peach.g, Peach.b, 0.25f);
            multTxt.text = run.Multiplier > 1 ? "x" + run.Multiplier : "";
        }

        public void ShowRevive(RunState run, float seconds)
        {
            reviveLeft = seconds;
            revivePanel = Modal(out var card, 640);
            Txt(card, "Keep going?", 84, Peach, TextAnchor.UpperCenter).rectTransform.Also(r => Place(r, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -140), new Vector2(0, -40)));
            reviveCount = Txt(card, Mathf.CeilToInt(seconds).ToString(), 160, Cream, TextAnchor.MiddleCenter);
            Place(reviveCount.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -340), new Vector2(0, -150));
            var yes = Btn(card, "Watch ad to continue", AdBlue, Cream, () => { CloseRevive(); game.AcceptRevive(); }, 46);
            Place((RectTransform)yes.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 160), new Vector2(-50, 290));
            var no = Btn(card, "No thanks", Color.clear, Muted, () => { CloseRevive(); game.DeclineRevive(); }, 40);
            Place((RectTransform)no.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 40), new Vector2(-50, 140));
        }

        void CloseRevive()
        {
            if (revivePanel) Destroy(revivePanel.gameObject);
            revivePanel = null;
        }

        public void ShowOver(RunState run, int prevBest, bool isBest, bool offerDouble, List<MissionState> missions, List<int> levels)
        {
            SetScreen(over);
            var s = game.Save;
            oScore.text = run.Score.ToString();
            oBest.text = isBest && run.Score > 0 ? "NEW BEST" : "BEST " + s.best;
            oBest.color = isBest ? Gold : Muted;
            string msg;
            if (run.EndedByCrash) msg = s.runs <= 3 ? "Too big! Let go to shrink." : "Squished!";
            else msg = s.runs <= 3 ? "Too loose! Hold to grow." : "Faded out. Fit tighter!";
            int gapToBest = prevBest - run.Score;
            if (!isBest && run.Score > 0 && gapToBest <= Mathf.Max(3, Mathf.RoundToInt(prevBest * 0.15f)))
                msg = "Only " + (gapToBest + 1) + " more to beat your best!";
            if (isBest && prevBest > 0) msg = "+" + (run.Score - prevBest) + " over your old best";
            oMsg.text = msg;
            oGates.text = run.Passed.ToString();
            oPerf.text = run.Perfects.ToString();
            oCoins.text = run.Coins.ToString();
            doubleBtn.gameObject.SetActive(offerDouble);

            float delay = 0.3f;
            foreach (var m in missions) { StartCoroutine(Later(delay, () => { Toast("Mission done  +" + m.reward); game.PlayRewardSound(); })); delay += 0.5f; }
            foreach (var l in levels)
            {
                int lv = l;
                StartCoroutine(Later(delay, () =>
                {
                    Toast("Level " + lv + "!  +" + Progression.CoinsPerLevel);
                    var th = Array.Find(Progression.Themes, t => t.Level == lv);
                    if (th != null) StartCoroutine(Later(0.6f, () => Toast("New theme: " + th.Name)));
                }));
                delay += 0.5f;
            }
        }

        public void OnCoinsDoubled(RunState run)
        {
            oCoins.text = (run.Coins * 2).ToString();
            doubleBtn.gameObject.SetActive(false);
            Toast("+" + run.Coins + " bonus coins");
        }

        public void ShowStreak(StreakReward r)
        {
            var panel = Modal(out var card, 900);
            Txt(card, "DAILY STREAK", 36, Muted, TextAnchor.UpperCenter).rectTransform.Also(t => Place(t, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -110), new Vector2(0, -50)));
            Txt(card, "Day " + r.Day, 120, Peach, TextAnchor.UpperCenter).rectTransform.Also(t => Place(t, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -270), new Vector2(0, -110)));
            var days = Node(card, "Days");
            Place(days, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -470), new Vector2(-40, -310));
            var hl = days.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12; hl.childControlWidth = true; hl.childControlHeight = true; hl.childForceExpandWidth = true;
            for (int i = 0; i < DailyStreak.Rewards.Length; i++)
            {
                Color bg = i < r.DayInCycle ? new Color(Mint.r, Mint.g, Mint.b, 0.22f) : i == r.DayInCycle ? Peach : new Color(1, 1, 1, 0.06f);
                var cell = Img(days, "Day", bg);
                Rounded(cell);
                var t = Txt(cell.transform, "D" + (i + 1) + "\n" + (i == 6 ? "Gift" : DailyStreak.Rewards[i].ToString()), 32,
                    i == r.DayInCycle ? PeachDark : Cream, TextAnchor.MiddleCenter);
                Stretch(t.rectTransform);
            }
            string body = "+" + r.Coins + " coins" + (r.GiftSkin != null ? " and the Aurora skin" : "") + ".\nCome back tomorrow for more.";
            Txt(card, body, 42, Cream, TextAnchor.MiddleCenter, FontStyle.Normal).rectTransform.Also(t => Place(t, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 210), new Vector2(-40, 390)));
            var claim = Btn(card, "Claim", Peach, PeachDark, () =>
            {
                game.PlayRewardSound();
                Destroy(panel.gameObject);
                ShowHome();
                Toast("+" + r.Coins + " coins");
            }, 54);
            Place((RectTransform)claim.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 50), new Vector2(-50, 190));
        }

        public void ShowFakeAd(bool rewarded, int seconds, Action done)
        {
            var bg = Img(overlays, "FakeAd", Color.black, true);
            Stretch(bg.rectTransform);
            Txt(bg.transform, (rewarded ? "REWARDED VIDEO" : "INTERSTITIAL") + " · SIMULATED", 34, Muted, TextAnchor.MiddleCenter)
                .rectTransform.Also(t => Place(t, new Vector2(0, 0.55f), new Vector2(1, 0.6f), Vector2.zero, Vector2.zero));
            Txt(bg.transform, "Your ad here", 90, Cream, TextAnchor.MiddleCenter)
                .rectTransform.Also(t => Place(t, new Vector2(0, 0.47f), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero));
            var count = Txt(bg.transform, seconds + "s", 44, Muted, TextAnchor.MiddleCenter);
            Place(count.rectTransform, new Vector2(0.2f, 0.36f), new Vector2(0.8f, 0.44f), Vector2.zero, Vector2.zero);
            StartCoroutine(AdCountdown(bg, count, rewarded, seconds, done));
        }

        IEnumerator AdCountdown(Image bg, Text count, bool rewarded, int seconds, Action done)
        {
            for (int s = seconds; s > 0; s--) { count.text = s + "s"; yield return new WaitForSecondsRealtime(1f); }
            count.gameObject.SetActive(false);
            var b = Btn(bg.transform, rewarded ? "Claim reward" : "Close", Peach, PeachDark, () => { Destroy(bg.gameObject); done(); }, 50);
            Place((RectTransform)b.transform, new Vector2(0.25f, 0.36f), new Vector2(0.75f, 0.44f), Vector2.zero, Vector2.zero);
        }

        // ================================================================== sheets
        void OpenLooks()
        {
            game.PlayUiSound();
            var sheet = Sheet("Looks", out var content);
            var s = game.Save;
            Section(content, "BLOBS  ·  YOU HAVE ● " + s.coins);
            var grid = Grid(content);
            foreach (var skin in Progression.Skins)
            {
                bool own = s.ownedSkins.Contains(skin.Id);
                string sub = own ? (s.skin == skin.Id ? "Wearing" : "Owned") : skin.Cost < 0 ? "7-day streak" : "● " + skin.Cost;
                var b = Btn(grid, skin.Name + "\n" + sub, s.skin == skin.Id ? new Color(Gold.r, Gold.g, Gold.b, 0.25f) : new Color(1, 1, 1, 0.06f), Cream, () =>
                {
                    if (game.BuyOrWearSkin(skin.Id)) { Destroy(sheet.gameObject); OpenLooks(); ShowHomeIfIdle(); }
                    else Toast(skin.Cost < 0 ? "Reach a 7-day streak" : "Need " + (skin.Cost - game.Save.coins) + " more coins");
                }, 34);
                var dot = Img(b.transform, "Ball", SpriteFactory.Hex(skin.Color));
                dot.sprite = SpriteFactory.Circle;
                Place(dot.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-40, -100), new Vector2(40, -20));
                if (!own) dot.color *= 0.55f;
                b.GetComponentInChildren<Text>().rectTransform.offsetMax = new Vector2(0, -90);
            }
            Section(content, "PONDS  ·  UNLOCK BY LEVEL");
            var grid2 = Grid(content);
            foreach (var th in Progression.Themes)
            {
                bool open = s.level >= th.Level;
                string sub = open ? (s.theme == th.Id ? "In use" : "Unlocked") : "Level " + th.Level;
                var b = Btn(grid2, th.Name + "\n" + sub, s.theme == th.Id ? new Color(Gold.r, Gold.g, Gold.b, 0.25f) : new Color(1, 1, 1, 0.06f), Cream, () =>
                {
                    if (game.SetTheme(th.Id)) { Destroy(sheet.gameObject); OpenLooks(); }
                    else Toast("Reach level " + th.Level);
                }, 34);
                var sw = Img(b.transform, "Swatch", SpriteFactory.Hex(th.Bottom));
                Place(sw.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(-40, -100), new Vector2(40, -20));
                var lip = Img(sw.transform, "Gate", SpriteFactory.Hex(th.Gate));
                Place(lip.rectTransform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 22));
                b.GetComponentInChildren<Text>().rectTransform.offsetMax = new Vector2(0, -90);
            }
        }

        void ShowHomeIfIdle() { if (game.Current == SnugGame.State.Home) ShowHome(); }

        void OpenSettings()
        {
            game.PlayUiSound();
            var sheet = Sheet("Settings", out var content);
            var s = game.Save;
            Button sound = null, vib = null, reset = null;
            sound = Row(content, "Sound: " + (s.sound ? "On" : "Off"), () => { game.SetSound(!game.Save.sound); sound.GetComponentInChildren<Text>().text = "Sound: " + (game.Save.sound ? "On" : "Off"); });
            vib = Row(content, "Vibration: " + (s.haptics ? "On" : "Off"), () => { game.SetHaptics(!game.Save.haptics); vib.GetComponentInChildren<Text>().text = "Vibration: " + (game.Save.haptics ? "On" : "Off"); });
            var note = Txt(content, "Ads here are simulated. Rewarded ads only play when you choose them. A short interstitial can appear between runs from your second session on, at most every 4th run, and never within 90 seconds of a rewarded ad.", 36, Muted, TextAnchor.UpperLeft, FontStyle.Normal);
            Layout(note.gameObject, 0, 210);
            bool armed = false;
            reset = Row(content, "Reset all progress", () =>
            {
                if (!armed) { armed = true; reset.GetComponentInChildren<Text>().text = "Tap again to erase everything"; return; }
                Destroy(sheet.gameObject);
                game.ResetProgress();
            });
            reset.GetComponentInChildren<Text>().color = Danger;
        }

        // ================================================================== per-frame
        public void Tick(float time, bool paused, bool ready, string hint)
        {
            if (Screen.safeArea != lastSafe) ApplySafeArea();
            if (home.gameObject.activeSelf) hintTxt.color = new Color(Cream.r, Cream.g, Cream.b, 0.55f + 0.45f * Mathf.Sin(time * 4.5f));
            statusTxt.text = paused ? "Paused\n<size=44>Hold to resume</size>" : ready ? "Ready" : "";
            coachTxt.text = hint ?? "";
            if (hint != null) coachTxt.color = new Color(Cream.r, Cream.g, Cream.b, 0.75f + 0.25f * Mathf.Sin(time * 8f));

            if (revivePanel != null && game.Current == SnugGame.State.Revive)
            {
                reviveLeft -= Time.deltaTime;
                reviveCount.text = Mathf.Max(0, Mathf.CeilToInt(reviveLeft)).ToString();
                if (reviveLeft <= 0f) { CloseRevive(); game.DeclineRevive(); }
            }
        }

        public void Popup(string text, Vector3 screenPos, Color c, int size)
        {
            var t = Txt(popups, text, size, c, TextAnchor.MiddleCenter);
            t.rectTransform.sizeDelta = new Vector2(1000, size * 1.4f);
            t.rectTransform.position = screenPos;
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            StartCoroutine(Float(t));
        }

        public void Toast(string text)
        {
            var bg = Img(toasts, "Toast", Mint);
            Rounded(bg);
            var t = Txt(bg.transform, text, 42, SpriteFactory.Hex("#0d2a22"), TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            bg.rectTransform.sizeDelta = new Vector2(Mathf.Min(980, 80 + t.preferredWidth), 96);
            StartCoroutine(FadeAndDie(bg.gameObject, 2.6f));
        }

        // ================================================================== helpers
        void SetScreen(RectTransform s)
        {
            home.gameObject.SetActive(s == home);
            hud.gameObject.SetActive(s == hud);
            over.gameObject.SetActive(s == over);
        }

        void ApplySafeArea()
        {
            lastSafe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            safe.anchorMin = new Vector2(lastSafe.xMin / Screen.width, lastSafe.yMin / Screen.height);
            safe.anchorMax = new Vector2(lastSafe.xMax / Screen.width, lastSafe.yMax / Screen.height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        IEnumerator Float(Text t)
        {
            float life = 1f;
            var rt = t.rectTransform;
            Color c = t.color;
            while (life > 0)
            {
                life -= Time.deltaTime * 0.9f;
                rt.anchoredPosition += new Vector2(0, 120f * Time.deltaTime);
                c.a = Mathf.Clamp01(life * 1.6f);
                t.color = c;
                yield return null;
            }
            Destroy(t.gameObject);
        }

        IEnumerator FadeAndDie(GameObject go, float seconds)
        {
            var g = go.AddComponent<CanvasGroup>();
            float t = 0;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = t < 0.25f ? t / 0.25f : t > seconds - 0.5f ? (seconds - t) / 0.5f : 1f;
                yield return null;
            }
            Destroy(go);
        }

        static IEnumerator Later(float s, Action a) { yield return new WaitForSecondsRealtime(s); a(); }

        RectTransform Modal(out RectTransform card, float height)
        {
            var dim = Img(overlays, "Modal", Dim, true);
            Stretch(dim.rectTransform);
            var c = Img(dim.transform, "Card", SpriteFactory.Hex("#12233a"), true);
            Rounded(c);
            Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-460, -height / 2), new Vector2(460, height / 2));
            card = c.rectTransform;
            return dim.rectTransform;
        }

        RectTransform Sheet(string title, out RectTransform content)
        {
            var dim = Img(overlays, "Sheet", new Color(Dim.r, Dim.g, Dim.b, 0.5f), true);
            Stretch(dim.rectTransform);
            var closeAll = dim.gameObject.AddComponent<Button>();
            closeAll.transition = Selectable.Transition.None;
            closeAll.onClick.AddListener(() => Destroy(dim.gameObject));
            var panel = Img(dim.transform, "Panel", SpriteFactory.Hex("#12233a"), true);
            Rounded(panel);
            Place(panel.rectTransform, new Vector2(0, 0), new Vector2(1, 0.8f), new Vector2(0, -40), Vector2.zero);
            panel.gameObject.AddComponent<Button>().transition = Selectable.Transition.None; // swallow taps
            var head = Txt(panel.transform, title, 84, Peach, TextAnchor.UpperLeft);
            Place(head.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -150), new Vector2(-50, -40));
            var close = Btn(panel.transform, "Close", new Color(1, 1, 1, 0.08f), Cream, () => Destroy(dim.gameObject), 38);
            Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-250, -140), new Vector2(-50, -50));
            content = Node(panel.transform, "Content");
            Place(content, Vector2.zero, Vector2.one, new Vector2(50, 80), new Vector2(-50, -170));
            var vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 24; vl.childControlHeight = true; vl.childControlWidth = true; vl.childForceExpandHeight = false;
            return dim.rectTransform;
        }

        void Section(Transform parent, string text) => Layout(Txt(parent, text, 34, Muted, TextAnchor.LowerLeft).gameObject, 0, 60);

        Transform Grid(Transform parent)
        {
            var g = Node(parent, "Grid");
            var grid = g.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(300, 230);
            grid.spacing = new Vector2(24, 24);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            g.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return g;
        }

        Button Row(Transform parent, string label, Action onClick)
        {
            var b = Btn(parent, label, new Color(1, 1, 1, 0.06f), Cream, onClick, 44);
            Layout(b.gameObject, 0, 130);
            return b;
        }

        Text Stat(Transform parent, string label)
        {
            var bg = Img(parent, label, new Color(1, 1, 1, 0.05f));
            Rounded(bg);
            var v = Txt(bg.transform, "0", 64, Cream, TextAnchor.MiddleCenter);
            Place(v.rectTransform, new Vector2(0, 0.4f), Vector2.one, Vector2.zero, Vector2.zero);
            var l = Txt(bg.transform, label, 30, Muted, TextAnchor.MiddleCenter);
            Place(l.rectTransform, Vector2.zero, new Vector2(1, 0.42f), Vector2.zero, Vector2.zero);
            return v;
        }

        GameObject Chip(Transform parent, string text, float width)
        {
            var bg = Img(parent, "Chip", Panel);
            Rounded(bg);
            Layout(bg.gameObject, width, 110);
            var t = Txt(bg.transform, text, 40, Cream, TextAnchor.MiddleCenter);
            Place(t.rectTransform, Vector2.zero, Vector2.one, new Vector2(16, 0), new Vector2(-16, -14));
            return bg.gameObject;
        }

        Button Btn(Transform parent, string label, Color bg, Color fg, Action onClick, int size)
        {
            var img = Img(parent, "Button", bg, true);
            Rounded(img);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors; colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = colors;
            b.onClick.AddListener(() => onClick());
            var t = Txt(img.transform, label, size, fg, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return b;
        }

        static void Layout(GameObject go, float w, float h)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            if (w > 0) le.preferredWidth = w;
            if (h > 0) { le.preferredHeight = h; le.minHeight = h; }
        }

        static RectTransform Node(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image Img(Transform parent, string name, Color c, bool raycast = false)
        {
            var rt = Node(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = raycast;
            return img;
        }

        static void Rounded(Image img)
        {
            img.sprite = SpriteFactory.Bar;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.35f;
        }

        Text Txt(Transform parent, string text, int size, Color c, TextAnchor align, FontStyle style = FontStyle.Bold)
        {
            var rt = Node(parent, "Text");
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = c;
            t.alignment = align;
            t.raycastTarget = false;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        static void Stretch(RectTransform rt) => Place(rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        static void Place(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = offMin; rt.offsetMax = offMax;
        }
    }

    static class FluentExtensions
    {
        public static T Also<T>(this T self, Action<T> a) { a(self); return self; }
    }
}
