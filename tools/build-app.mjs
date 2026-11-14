#!/usr/bin/env node
// Builds the web files for the Android app (Capacitor webDir app/www) from the built site in docs/.
//
//   node tools/build.mjs && node tools/build-app.mjs
//
// Adds app/native.js (ads, haptics, share sheet) in front of the game and leaves out the web-only service worker.
// Ad settings come from environment variables; without them the app uses Google's test ads:
//   SNUG_ADMOB_REWARDED, SNUG_ADMOB_INTERSTITIAL   real ad unit IDs from your AdMob account
//   SNUG_SHARE_BASE                                public address of the web game, for challenge links
// The AdMob *app* ID is set separately, in android/app/build.gradle (ADMOB_APP_ID).
import { cpSync, existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const src = join(root, "docs"), out = join(root, "app", "www");
if (!existsSync(join(src, "index.html"))) throw new Error("docs/index.html missing: run node tools/build.mjs first");

const TEST = { rewarded: "ca-app-pub-3940256099942544/5224354917", interstitial: "ca-app-pub-3940256099942544/1033173712" };
const env = process.env;
const ads = {
  rewarded: env.SNUG_ADMOB_REWARDED || TEST.rewarded,
  interstitial: env.SNUG_ADMOB_INTERSTITIAL || TEST.interstitial,
};
ads.testing = ads.rewarded === TEST.rewarded || ads.interstitial === TEST.interstitial;
const config = { shareBase: env.SNUG_SHARE_BASE || "https://snugworksgames.github.io/snug/", ads };

rmSync(out, { recursive: true, force: true });
mkdirSync(out, { recursive: true });
cpSync(src, out, { recursive: true, filter: (p) => !/(^|[\\/])(sw\.js|\.nojekyll)$/.test(p) });
cpSync(join(root, "app", "native.js"), join(out, "native.js"));

const page = join(out, "index.html");
let html = readFileSync(page, "utf8");
const at = html.indexOf("<script>");
if (at < 0) throw new Error("game <script> not found in docs/index.html");
html = html.slice(0, at) + `<script>window.SNUG_APP_CONFIG=${JSON.stringify(config)};</script>\n<script src="native.js"></script>\n` + html.slice(at);
writeFileSync(page, html);
console.log(`Built app/www (${ads.testing ? "Google TEST ads" : "real ad units"}, links to ${config.shareBase})`);
