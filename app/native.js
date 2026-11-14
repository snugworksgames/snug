// Snug Android app bridge. Loaded before the game in the store app only (tools/build-app.mjs adds it).
// It connects the game's hooks to native features through Capacitor plugins:
//   window.SNUG_ADS          AdMob rewarded videos and the capped between-runs ad
//   window.SNUG_HAPTIC       vibration
//   window.SNUG_SHARE_BASE   public web address, so challenge links work outside the app
//   navigator.share          Android's share sheet
// Ad settings come from app/config.json, written by tools/build-app.mjs (Google's test ads by default).
(function () {
  var cap = window.Capacitor, P = (cap && cap.Plugins) || {};
  if (!cap || !cap.isNativePlatform || !cap.isNativePlatform()) return;   // plain browser: leave the web game alone
  var CFG = window.SNUG_APP_CONFIG || {};

  window.SNUG_SHARE_BASE = CFG.shareBase;

  // ---- share sheet ----
  if (P.Share && !navigator.share) {
    navigator.share = function (d) {
      return P.Share.share({ title: d.title, text: d.text, url: d.url, dialogTitle: "Challenge a friend" })
        .catch(function (e) { var err = new Error("share cancelled"); err.name = "AbortError"; throw err; });
    };
  }

  // ---- haptics ----
  if (P.Haptics) {
    window.SNUG_HAPTIC = function (ms) {
      var total = Array.isArray(ms) ? ms.reduce(function (a, b) { return a + b; }, 0) : ms;
      var style = total < 15 ? "LIGHT" : total < 28 ? "MEDIUM" : "HEAVY";
      P.Haptics.impact({ style: style }).catch(function () {});
    };
  }

  // ---- ads (AdMob), set up for a young audience ----
  var AdMob = P.AdMob;
  if (!AdMob || !CFG.ads) return;
  var A = CFG.ads;
  var adOpts = function (id) { return { adId: id, isTesting: !!A.testing, npa: true }; };
  var ready = { rewarded: false, interstitial: false };
  var loading = { rewarded: false, interstitial: false };

  function load(kind) {
    if (ready[kind] || loading[kind]) return;
    loading[kind] = true;
    var p = kind === "rewarded" ? AdMob.prepareRewardVideoAd(adOpts(A.rewarded)) : AdMob.prepareInterstitial(adOpts(A.interstitial));
    p.then(function () { ready[kind] = true; }, function () {}).then(function () { loading[kind] = false; });
  }

  AdMob.initialize({
    initializeForTesting: !!A.testing,
    tagForChildDirectedTreatment: true,   // Google Play Families policy and COPPA: child-directed treatment
    tagForUnderAgeOfConsent: true,        // EU/UK: treat players as under the age of consent
    maxAdContentRating: "General"         // only ads suitable for all ages
  }).then(function () { load("rewarded"); load("interstitial"); }, function () {});

  // The game calls show(kind, done). For a rewarded video, done(true) only when the reward was earned.
  window.SNUG_ADS = {
    show: function (kind, done) {
      var isReward = kind !== "interstitial", key = isReward ? "rewarded" : "interstitial";
      var finished = false, handles = [];
      var finish = function (ok) {
        if (finished) return; finished = true;
        handles.forEach(function (h) { Promise.resolve(h).then(function (x) { x && x.remove && x.remove(); }); });
        ready[key] = false; load(key);
        done(ok);
      };
      if (!ready[key]) { load(key); finish(true); return; }   // nothing loaded (offline, no fill): be kind and give it anyway
      if (isReward) {
        var earned = false;
        handles.push(AdMob.addListener("onRewardedVideoAdReward", function () { earned = true; }));
        handles.push(AdMob.addListener("onRewardedVideoAdDismissed", function () { finish(earned); }));
        handles.push(AdMob.addListener("onRewardedVideoAdFailedToShow", function () { finish(false); }));
        AdMob.showRewardVideoAd().then(function () { earned = true; }, function () { finish(false); });
      } else {
        handles.push(AdMob.addListener("interstitialAdDismissed", function () { finish(true); }));
        handles.push(AdMob.addListener("interstitialAdFailedToShow", function () { finish(true); }));
        AdMob.showInterstitial().catch(function () { finish(true); });
      }
    }
  };
})();
