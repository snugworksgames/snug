#!/usr/bin/env node
// Automated playtest: a bot plays the real built game in headless Chromium and checks the main flows.
//
//   node tools/build.mjs && node tools/bot-playtest.mjs [screenshot-dir]
//
// Checks: no page errors, a full bot run with every mechanic, buying and wearing a hat, results screen,
// challenge link round trip (a second player gets the identical gate sequence), and the daily challenge
// (same gates for everyone, one try, no revive, result saved and shareable).
import { createRequire } from "node:module";
import { execFileSync } from "node:child_process";
import { createServer } from "node:http";
import { readFileSync, existsSync, mkdirSync } from "node:fs";
import { dirname, join, extname } from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const { chromium, devices } = require("playwright");
const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const shots = process.argv[2] || null;
if (shots) mkdirSync(shots, { recursive: true });

// Tiny static server for docs/. The page gets one extra line that exposes game state to the bot.
const TYPES = { ".html": "text/html", ".js": "text/javascript", ".png": "image/png", ".woff2": "font/woff2", ".webmanifest": "application/manifest+json" };
const server = createServer((req, res) => {
  let path = decodeURIComponent(req.url.split("?")[0]);
  if (path.endsWith("/")) path += "index.html";
  const file = join(root, "docs", path);
  if (!file.startsWith(join(root, "docs")) || !existsSync(file)) { res.writeHead(404); res.end(); return; }
  let data = readFileSync(file);
  if (path.endsWith("index.html")) {
    data = data.toString().replace("function boot(){", "window.__g=()=>({R,blobR,state,BY,CH,S,squish,flash});function boot(){");
  }
  res.writeHead(200, { "content-type": TYPES[extname(file)] || "application/octet-stream" });
  res.end(data);
});
await new Promise((r) => server.listen(0, r));
const base = `http://localhost:${server.address().port}/`;

const today = (() => { const d = new Date(); return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`; })();
const errors = [];
const failures0 = [];
{
  const src = readFileSync(join(root, "web", "snug.html"), "utf8");
  const js = src.slice(src.indexOf("<script>"), src.lastIndexOf("</script>"));
  const hidden = [];
  js.split("\n").forEach((line, i) => {
    const m = line.match(/(^|[^:"'`\\])\/\/(.*)$/); if (!m) return;
    const c = m[2];
    if (/[A-Za-z_$][\w$.\[\]]*\s*=(?!=)[^;]*;/.test(c) || /\b[A-Za-z_$][\w$]*\([^)]*\)\s*;/.test(c)) hidden.push(`line ${i + 1}: ${c.trim().slice(0, 60)}`);
  });
  if (hidden.length) failures0.push(`code hidden after a // comment (${hidden.join(" | ")})`);
}
const failures = [];
const check = (ok, what) => { console.log(`${ok ? "PASS" : "FAIL"}  ${what}`); if (!ok) failures.push(what); };
check(failures0.length === 0, failures0[0] || "no code hidden after a // comment");
{ let ok = true, out = "";
  try { out = execFileSync("node", [join(root, "tools", "check-secrets.mjs")], { cwd: root, encoding: "utf8" }).trim(); } catch (e) { ok = false; out = String(e.stderr || e.message).trim(); }
  check(ok, `no API keys or signing keys committed (${out.split("\n")[0]})`); }
const shot = async (page, name) => { if (shots) await page.screenshot({ path: join(shots, name + ".png") }); };

async function open(browser, url, save) {
  const ctx = await browser.newContext({ ...devices["iPhone 13"], deviceScaleFactor: 1 });
  if (save) await ctx.addInitScript((s) => { if (!localStorage.getItem("snug.v1")) localStorage.setItem("snug.v1", JSON.stringify(s)); }, save);
  const page = await ctx.newPage();
  page.on("pageerror", (e) => errors.push(e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push(m.text()); });
  await page.goto(url);
  await page.waitForTimeout(700);
  return page;
}

// Plays until the run ends or `maxMs` passes. Aims a little under the gap (or over it for glass).
async function play(page, maxMs, { takeRevive = true, label = "run" } = {}) {
  const box = await page.locator("#cv").boundingBox();
  await page.mouse.move(box.x + box.width / 2, box.y + box.height * 0.45);
  await page.mouse.down();
  let down = true, peak = { passed: 0, chain: 0, layout: new Map() }, sawFire = false;
  const t0 = Date.now();
  while (Date.now() - t0 < maxMs) {
    const s = await page.evaluate(() => {
      const g = window.__g(); if (!g.R) return { state: g.state };
      const ng = g.R.gates.find((x) => !x.done), gem = g.R.gems.find((m) => !m.done && m.y < g.BY && m.y > (ng ? ng.y : -999));
      return { state: g.state, r: g.blobR, passed: g.R.passed, chain: g.R.chain, type: ng && ng.type, gap: ng && ng.cur, gy: ng && ng.y, BY: g.BY,
        gemdx: gem ? Math.abs(gem.x - 180) : 0, gemy: gem ? gem.y : 0,
        specs: g.R.gates.map((x) => [x.n, [Math.round(x.base * 10) / 10, x.type, !!x.box, !!x.twin, !!x.gold].join(":")]) };
    });
    for (const [n, spec] of s.specs || []) peak.layout.set(n, spec);
    if (s.state === "revive") {
      peak.sawRevive = true;
      if (down) { await page.mouse.up(); down = false; }
      await page.click(takeRevive ? "#rvYes" : "#rvNo"); await page.waitForTimeout(200); continue;
    }
    if (s.state === "over") break;
    if (s.state !== "play") { await page.waitForTimeout(40); continue; }
    peak.passed = Math.max(peak.passed, s.passed); peak.chain = Math.max(peak.chain, s.chain);
    if (s.chain >= 6 && !sawFire) { sawFire = true; await shot(page, `${label}-on-fire`); }
    let target = s.type === "glass" ? s.gap / 2 + 6 : (s.gap * 0.92) / 2;
    if (s.gemdx && s.gemy > s.gy && s.BY - s.gemy < 120) target = s.gemdx - 6;
    const want = s.r < target;
    if (want && !down) { await page.mouse.down(); down = true; } else if (!want && down) { await page.mouse.up(); down = false; }
    await page.waitForTimeout(12);
  }
  if (down) await page.mouse.up();
  return peak;
}
// Lets the current run end (declining the revive) and waits for the results screen.
async function finish(page) {
  await page.mouse.up();
  await page.waitForFunction(() => ["over", "revive"].includes(window.__g().state), null, { timeout: 60000 });
  if (await page.locator("#rvNo").isVisible()) await page.click("#rvNo");
  await page.waitForFunction(() => window.__g().state === "over", null, { timeout: 10000 });
  await page.waitForTimeout(1500);
}
const startByTap = async (page) => {
  const box = await page.locator("#cv").boundingBox();
  await page.mouse.click(box.x + box.width / 2, box.y + box.height * 0.45);
  await page.waitForTimeout(200);
};

const browser = await chromium.launch();

// 1. Brand-new player
let page = await open(browser, base);
check(await page.locator("#bClaim").isVisible(), "new player sees the daily reward");
await shot(page, "new-daily");
await page.click("#bClaim"); await page.waitForTimeout(400);
await shot(page, "new-home");
await page.context().close();

// 2. Returning player with coins: collection, hats, a long run, results
const rich = { best: 40, coins: 2000, xp: 10, level: 3, skins: ["peach", "mint"], skin: "peach", streak: 2, lastDay: today, runs: 9, sessions: 4,
  seen: { gem: true, glass: true, box: true, twin: true }, zone: 1 };
page = await open(browser, base, rich);
await page.click("#bSkins"); await page.waitForTimeout(400);
await shot(page, "collection");
check(/Available in the Android app/.test(await page.locator(".plus").textContent()) && !(await page.locator(".plus [data-buy]").count()),
  "the web version shows Snug Plus without a buy button (it's in the Android app)");
await page.click('[data-hat="crown"]'); await page.waitForTimeout(300);
let st = await page.evaluate(() => window.__g().S);
check(st.hat === "crown" && st.hats.includes("crown") && st.coins === 1400, "buying the crown costs 600 coins and equips it");
await page.locator(".scroll").evaluate((el) => (el.scrollTop = 200)); await shot(page, "collection-hats");
await page.click("[data-x]"); await page.waitForTimeout(200);
await page.click("#bSet"); await page.waitForTimeout(300); await shot(page, "settings");
await page.click("[data-x]"); await page.waitForTimeout(200);
const peak = await play(page, 70000, { label: "crown" });
check(peak.passed >= 30, `bot run got through ${peak.passed} gates (needs 30+)`);
check(await page.evaluate(() => window.__g().R.beatBest === true), "passing your best mid-run is celebrated (NEW BEST during the run)");
check(peak.chain >= 6, `bot reached an on-fire chain (best chain ${peak.chain})`);
await finish(page);   // if the bot is still alive at the time limit, let go and let the run end
await shot(page, "results");
const res = await page.evaluate(() => ({ score: +document.getElementById("oScore").textContent, real: window.__g().R.score, seed: window.__g().R.seed,
  medal: document.getElementById("oMedal").textContent, share: !document.getElementById("bShare").hidden }));
check(res.score === res.real && res.real > 0, `results show the final score after the count-up (${res.real})`);
check(await page.evaluate(() => ![...document.querySelectorAll(".toast")].some((t) => /Mission|Level/.test(t.textContent))),
  `mission and level rewards sit inside the results panel, not over the banner (${await page.evaluate(() => document.querySelectorAll("#oRew .rew").length)} shown)`);
check(/MEDAL/.test(res.medal), `a medal is shown (${res.medal.trim()})`);
// Sharing sits behind a parental gate: a wrong answer shares nothing, the right one goes through.
await page.click("#bShare"); await page.waitForTimeout(400);
const gate = async () => page.evaluate(() => { const q = document.querySelector(".gateq"); if (!q) return null;
  const [a, b] = q.textContent.match(/\d+/g).map(Number); return { ans: a * b, opts: [...document.querySelectorAll(".gate [data-a]")].map((x) => +x.dataset.a) }; });
const g1 = await gate();
check(!!g1 && g1.opts.length === 4 && g1.opts.includes(g1.ans), "share button asks a grown-up first (parental gate)");
if (g1) await page.click(`.gate [data-a="${g1.opts.find((o) => o !== g1.ans)}"]`);
await page.waitForTimeout(400);
const leaked = await page.evaluate(() => !!document.querySelector(".linkbox") || /copied/i.test(document.querySelector(".toast")?.textContent || ""));
check(!leaked && !(await gate()), "a wrong answer closes the gate without sharing");
await page.click("#bShare"); await page.waitForTimeout(400);
const g2 = await gate();
if (g2) await page.click(`.gate [data-a="${g2.ans}"]`);
await page.waitForTimeout(600);
await shot(page, "share-after-gate");
const shared = await page.evaluate(() => (document.querySelector(".linkbox") || document.querySelector(".toast") || {}).textContent || "");
check(/\?c=\d+-\d+-g\d+/.test(shared) || /copied/i.test(shared), `challenge button produces a link with the gate version (${shared.slice(-40)})`);
if (await page.locator(".linkbox").isVisible()) { await shot(page, "share-link"); await page.click("[data-x]"); await page.waitForTimeout(200); }

// 2b. Crash squash and squeeze flash wear off (a mid-line comment once froze them, flattening the blob for the session)
{
  const cr = await open(browser, base, rich);
  await startByTap(cr);
  const cb = await cr.locator("#cv").boundingBox();
  await cr.mouse.move(cb.x + cb.width / 2, cb.y + cb.height * 0.45); await cr.mouse.down();   // hold: grow until a narrow gate squashes the blob
  await cr.waitForFunction(() => window.__g().state !== "play", null, { timeout: 30000 }); await cr.mouse.up();
  const hit = await cr.evaluate(() => ({ end: window.__g().R.end, squish: window.__g().squish }));
  await cr.waitForTimeout(1500);
  const fx = await cr.evaluate(() => ({ squish: window.__g().squish, flash: window.__g().flash }));
  check(hit.end === "crash" && hit.squish > 0 && fx.squish === 0 && fx.flash === 0,
    `crash squash and squeeze flash wear off (${hit.end}: squish ${hit.squish.toFixed(2)} -> ${fx.squish}, flash ${fx.flash})`);
  if (await cr.locator("#rvNo").isVisible()) { await cr.click("#rvNo"); await cr.waitForTimeout(400); }
  await cr.waitForFunction(() => window.__g().state === "over"); await cr.waitForTimeout(800);
  await cr.click("#bHome", { force: true }); await cr.waitForTimeout(300); await cr.click("#bSet"); await cr.waitForTimeout(300);
  for (let i = 0; i < 5; i++) { await cr.click(".note"); await cr.waitForTimeout(60); }
  await cr.waitForTimeout(300);
  const stats = (await cr.locator(".stattext").textContent().catch(() => "")) || "";
  check(/runs 1\b/.test(stats) && /crash 1/.test(stats), `the hidden tester screen counts the run (${stats.match(/runs \d+/)?.[0] || "not found"})`);
  await cr.context().close();
}

// 2c. Store: Snug Plus through the parental gate (simulated Google Play), Golden, and a refund taking it back
{
  const st = await open(browser, `${base}?iap=sim`, { ...rich, sessions: 4 });
  await st.click("#bSkins"); await st.waitForTimeout(500);
  check(await st.locator(".plus [data-buy='plus']").isVisible(), "the app's store shows Snug Plus");
  const price = (await st.locator(".plus [data-price='snug_plus']").textContent()) || "";
  await st.click(".plus [data-buy='plus']"); await st.waitForTimeout(300);
  check(await st.locator(".gateq").isVisible(), "buying asks a grown-up first");
  const ans = await st.evaluate(() => { const [a, b] = document.querySelector(".gateq").textContent.match(/\d+/g).map(Number); return a * b; });
  await st.click(`.gate [data-a="${ans}"]`); await st.waitForTimeout(900);
  const s1 = await st.evaluate(() => ({ plus: window.__g().S.plus, noAds: window.__g().S.noAds }));
  check(s1.plus && s1.noAds && /\d/.test(price), `buying Snug Plus unlocks it and removes ads (${price})`);
  await st.click("#bSkins"); await st.waitForTimeout(400);
  await st.click('[data-skin="golden"]'); await st.waitForTimeout(300);
  check(await st.evaluate(() => window.__g().S.skin === "golden"), "Plus owners can wear the Golden blob");
  await st.evaluate(() => localStorage.removeItem("snug.iapsim"));   // refunded: Google Play no longer lists it
  await st.reload(); await st.waitForTimeout(900);
  const s2 = await st.evaluate(() => ({ plus: window.__g().S.plus, skin: window.__g().S.skin }));
  check(!s2.plus && s2.skin === "peach", "a refund takes Plus and Golden away again");
  await st.context().close();
}

// 3. Pause
await page.click("#bRetry", { force: true }); await page.waitForTimeout(600);
await page.click("#bPause"); await page.waitForTimeout(300);
const y1 = await page.evaluate(() => window.__g().R.gates[0].y); await page.waitForTimeout(500);
const y2 = await page.evaluate(() => window.__g().R.gates[0].y);
check(y1 === y2, "pause button freezes the run");
await shot(page, "paused");
check(await page.locator(".pausep [data-r]").isVisible(), "pause opens a menu with RESUME");
check(await page.evaluate(() => +document.getElementById("score").textContent === window.__g().R.score), "the HUD score rolls to the real score");
await page.click(".pausep [data-r]"); await page.waitForTimeout(250);
const r1 = await page.evaluate(() => window.__g().R.gates[0].y); await page.waitForTimeout(350);
const r2 = await page.evaluate(() => window.__g().R.gates[0].y); await page.waitForTimeout(700);
const r3 = await page.evaluate(() => window.__g().R.gates[0].y);
check(r1 === r2 && r3 > r2, "resume shows READY? before the gates move again");

// 3b. A Good costs one multiplier step, not the whole chain
const mg = await open(browser, base, rich);
await startByTap(mg); await mg.mouse.up();
{
  const mb = await mg.locator("#cv").boundingBox();
  await mg.mouse.move(mb.x + mb.width / 2, mb.y + mb.height * 0.45);
  let held = false, primed = false;
  for (let i = 0; i < 2000; i++) {
    const s = await mg.evaluate(() => { const g = window.__g(); const ng = g.R && g.R.gates.find((x) => !x.done);
      return { state: g.state, r: g.blobR, passed: g.R ? g.R.passed : 0, gap: ng && ng.cur, gy: ng && ng.y, BY: g.BY }; });
    if (s.state !== "play" || s.passed >= 1) break;
    if (!primed && s.gy > s.BY - 120) { primed = true; await mg.evaluate(() => { window.__g().R.chain = 8; }); }   // ×3, just before the gate
    const want = s.r < (s.gap * 0.70) / 2;                                                                   // aim for a Good
    if (want && !held) { await mg.mouse.down(); held = true; } else if (!want && held) { await mg.mouse.up(); held = false; }
    await mg.waitForTimeout(10);
  }
  if (held) await mg.mouse.up();
  const after = await mg.evaluate(() => ({ chain: window.__g().R.chain, last: window.__g().R.log[window.__g().R.log.length - 1] }));
  check(after.last === "G" && after.chain === 5, `a Good costs one multiplier step (chain 8 -> ${after.chain}, result ${after.last})`);
}
await mg.context().close();

// 4. Challenge link round trip: two players, same seed, same gates
const seed = 424242, target = 55;
const a = await open(browser, `${base}?c=${seed}-${target}`, rich);
check(await a.locator(".chalscore").isVisible(), "challenge link opens the challenge card");
await shot(a, "challenge-card");
await a.click("[data-go]"); await a.waitForTimeout(300);
await startByTap(a);
check(await a.evaluate(() => window.__g().R.seed) === seed, "the run uses the seed from the link");
check(await a.evaluate(() => document.getElementById("score").classList.contains("target")), "a challenge run shows progress toward the target under the score");
const peakA = await play(a, 25000);
await finish(a);
// second player: a brand-new one (first-run pacing and intro banners must not change the layout)
const b = await open(browser, `${base}?c=${seed}-${target}`, { ...rich, runs: 0, seen: {} });
await b.click("[data-go]"); await b.waitForTimeout(300);
await startByTap(b);
const peakB = await play(b, 25000);
const common = [...peakA.layout.keys()].filter((n) => peakB.layout.has(n));
const same = common.every((n) => peakA.layout.get(n) === peakB.layout.get(n));
check(same && common.length >= 15, `both players get identical gates (compared ${common.length} gates, e.g. gate 12 = ${peakA.layout.get(12)})`);
const won = await a.evaluate(() => ({ won: window.__g().R.chWon, score: window.__g().R.score, title: document.getElementById("oTitle").textContent }));
check(won.score > target && won.won && won.title === "YOU WIN!", `beating the target shows YOU WIN (score ${won.score} vs ${target})`);
await shot(a, "challenge-result");

// 4b. A challenge link from another gate-generator version says the gates won't match
const old = await open(browser, `${base}?c=4242-30-g1`, rich);
check(/different version/i.test(await old.locator(".modal").first().textContent()), "a challenge from another version warns that gates won't match");
await old.context().close();

// 5. Daily challenge: same gates for two players, one scored try, no revive, result saved and shareable
const d1 = await open(browser, base, rich);
check(/DAILY CHALLENGE #\d+/.test(await d1.locator("#dcT").textContent()), "home shows the daily challenge button");
await d1.click("#bDC"); await d1.waitForTimeout(300);
await shot(d1, "daily-intro");
await d1.click("[data-go]"); await d1.waitForTimeout(300);
const dSeed = await d1.evaluate(() => window.__g().R.daily && window.__g().R.seed);
const dPeakA = await play(d1, 25000, { label: "daily" });
await finish(d1);
const dRes = await d1.evaluate(() => ({ title: document.getElementById("oTitle").textContent, dc: window.__g().S.dc, score: window.__g().R.score, cont: window.__g().R.cont }));
check(dSeed !== false && !dPeakA.sawRevive && !dRes.cont, "the daily run offers no revive");
check(/^DAILY #\d+$/.test(dRes.title) && dRes.dc && dRes.dc.score === dRes.score && dRes.dc.log.length >= dRes.dc.gates,
  `daily result is saved (score ${dRes.dc && dRes.dc.score}, ${dRes.dc && dRes.dc.gates} gates)`);
await shot(d1, "daily-result");
await d1.click("#bHome"); await d1.waitForTimeout(300);
check(await d1.locator("#bDC.done").isVisible(), "after playing, the daily button shows today's score");
await d1.click("#bDC"); await d1.waitForTimeout(300);
check(await d1.locator(".dcgrid").isVisible() && !(await d1.locator("[data-go]").count()), "a second try is not offered; the result card shows instead");
await d1.click("[data-share]"); await d1.waitForTimeout(300);
const gq = await d1.evaluate(() => { const [a, b] = document.querySelector(".gateq").textContent.match(/\d+/g).map(Number); return a * b; });
await d1.click(`.gate [data-a="${gq}"]`); await d1.waitForTimeout(500);
const dShared = await d1.evaluate(() => (document.querySelector(".linkbox") || document.querySelector(".toast") || {}).textContent || "");
check(/Snug Daily #\d+ · \d+ points/.test(dShared) || /copied/i.test(dShared), `sharing the daily result works (${dShared.slice(0, 36).replace(/\n/g, " ")})`);
await shot(d1, "daily-share");
// a friend opens the shared daily link: same seed, same gates
const d2 = await open(browser, `${base}?daily`, { ...rich, dc: null });
check(await d2.locator("[data-go]").isVisible(), "a shared daily link opens the daily challenge");
await d2.click("[data-go]"); await d2.waitForTimeout(300);
check(await d2.evaluate(() => window.__g().R.seed) === dSeed, "the friend gets today's daily seed");
const dPeakB = await play(d2, 20000);
const dCommon = [...dPeakA.layout.keys()].filter((n) => dPeakB.layout.has(n));
check(dCommon.length >= 12 && dCommon.every((n) => dPeakA.layout.get(n) === dPeakB.layout.get(n)), `both players get identical daily gates (compared ${dCommon.length})`);

check(errors.length === 0, `no page errors${errors.length ? ": " + errors.slice(0, 3).join(" | ") : ""}`);
await browser.close();
server.close();
console.log(failures.length ? `\n${failures.length} check(s) failed` : "\nAll checks passed");
process.exit(failures.length ? 1 : 0);
