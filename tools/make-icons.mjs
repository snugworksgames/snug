#!/usr/bin/env node
// Draws the Snug app icon (blob squeezing between two candy gates) and writes every size the web app
// and the stores need. Needs Playwright with Chromium.
//
//   node tools/make-icons.mjs
//
//   docs/icons/icon-192.png, icon-512.png      web app / Android home screen
//   docs/icons/maskable-512.png                Android adaptive icon (art kept inside the safe zone)
//   docs/icons/apple-touch-icon.png (180)      iPhone home screen
//   store/icon-1024.png, store/icon-512.png    App Store and Google Play listings
import { createRequire } from "node:module";
import { mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const require = createRequire(import.meta.url);
const { chromium } = require("playwright");
const root = join(dirname(fileURLToPath(import.meta.url)), "..");

// Runs in the page: paints the icon on a square canvas. `art` scales the artwork (maskable icons use less).
function paint(size, art) {
  const INK = "#2b2040";
  const cv = document.createElement("canvas");
  cv.width = cv.height = size;
  document.body.replaceChildren(cv);
  const c = cv.getContext("2d");
  const sky = c.createLinearGradient(0, 0, 0, size);
  sky.addColorStop(0, "#4fbdf0");
  sky.addColorStop(1, "#c9f1ff");
  c.fillStyle = sky;
  c.fillRect(0, 0, size, size);

  c.translate(size / 2, size / 2);
  c.scale((size / 1024) * art, (size / 1024) * art);   // artwork is designed on a 1024 grid, centred

  // soft cloud puffs
  c.fillStyle = "rgba(255,255,255,.9)";
  for (const [x, y, s] of [[-330, -300, 1.5], [300, 330, 1.3], [340, -350, 1]]) {
    c.beginPath();
    c.arc(x, y, 44 * s, 0, 7); c.arc(x + 50 * s, y - 20 * s, 54 * s, 0, 7); c.arc(x + 108 * s, y, 42 * s, 0, 7); c.arc(x + 54 * s, y + 16 * s, 46 * s, 0, 7);
    c.fill();
  }

  // gates
  const gap = 250, barH = 150, lw = 26;
  for (const side of [-1, 1]) {
    const x = side < 0 ? -900 : gap, w = 900 - gap;
    const bar = () => { c.beginPath(); c.roundRect(x, -barH / 2, w, barH, 75); };
    c.fillStyle = "rgba(43,32,64,.22)"; c.beginPath(); c.roundRect(x, -barH / 2 + 40, w, barH, 75); c.fill();
    c.save(); bar(); c.clip();
    c.fillStyle = "#ff8fb1"; c.fillRect(x, -barH / 2, w, barH);
    c.fillStyle = "#d65b86"; c.fillRect(x, barH / 2 - 42, w, 42);
    c.fillStyle = "rgba(255,255,255,.3)";
    for (let sx = x - 200; sx < x + w + 200; sx += 150) { c.beginPath(); c.moveTo(sx, barH / 2); c.lineTo(sx + 60, barH / 2); c.lineTo(sx + 130, -barH / 2); c.lineTo(sx + 70, -barH / 2); c.fill(); }
    c.fillStyle = "rgba(255,255,255,.55)"; c.fillRect(x, -barH / 2 + 18, w, 16);
    c.restore();
    c.lineWidth = lw; c.strokeStyle = INK; bar(); c.stroke();
    const kx = side * (gap + 6);
    c.fillStyle = "#fffaf0"; c.beginPath(); c.arc(kx, 0, 56, 0, 7); c.fill(); c.stroke();
    c.fillStyle = "#ff8fb1"; c.beginPath(); c.arc(kx, 0, 22, 0, 7); c.fill();
  }

  // blob, squeezed a little by the gates
  const r = 262;
  c.save();
  c.scale(0.93, 1.09);
  c.beginPath(); c.arc(0, 0, r, 0, 7); c.fillStyle = "#ffa979"; c.fill();
  c.save(); c.beginPath(); c.arc(0, 0, r, 0, 7); c.clip();
  c.fillStyle = "#e2734a"; c.globalAlpha = 0.9;
  c.beginPath(); c.arc(r * 0.18, r * 0.32, r * 1.02, 0, 7); c.arc(0, -r * 0.12, r * 1.04, 0, 7, true); c.fill("evenodd");
  c.globalAlpha = 1; c.fillStyle = "rgba(255,255,255,.78)";
  c.beginPath(); c.ellipse(-r * 0.42, -r * 0.5, r * 0.22, r * 0.13, -0.6, 0, 7); c.fill();
  c.beginPath(); c.arc(-r * 0.16, -r * 0.66, r * 0.06, 0, 7); c.fill();
  c.restore();
  c.lineWidth = lw; c.strokeStyle = INK; c.beginPath(); c.arc(0, 0, r, 0, 7); c.stroke();
  const ex = r * 0.3, ey = -r * 0.06, er = r * 0.12;
  c.fillStyle = INK; c.lineCap = "round";
  for (const g of [-1, 1]) {
    c.beginPath(); c.ellipse(g * ex, ey, er * 0.85, er * 1.15, 0, 0, 7); c.fill();
    c.fillStyle = "#fff"; c.beginPath(); c.arc(g * ex - er * 0.3, ey - er * 0.45, er * 0.32, 0, 7); c.fill(); c.fillStyle = INK;
  }
  c.lineWidth = r * 0.075; c.beginPath(); c.arc(0, r * 0.2, r * 0.15, 0.12 * Math.PI, 0.88 * Math.PI); c.stroke();
  c.fillStyle = "rgba(255,90,120,.38)";
  c.beginPath(); c.ellipse(-ex * 1.55, ey + r * 0.24, r * 0.12, r * 0.07, 0, 0, 7); c.ellipse(ex * 1.55, ey + r * 0.24, r * 0.12, r * 0.07, 0, 0, 7); c.fill();
  c.restore();
}

const targets = [
  ["docs/icons/icon-192.png", 192, 1],
  ["docs/icons/icon-512.png", 512, 1],
  ["docs/icons/maskable-512.png", 512, 0.74],
  ["docs/icons/apple-touch-icon.png", 180, 1],
  ["store/icon-1024.png", 1024, 1],
  ["store/icon-512.png", 512, 1],
];

const browser = await chromium.launch();
const page = await browser.newPage({ deviceScaleFactor: 1 });
await page.setContent("<body style='margin:0'></body>");
for (const [file, size, art] of targets) {
  mkdirSync(dirname(join(root, file)), { recursive: true });
  await page.setViewportSize({ width: size, height: size });
  await page.evaluate(([fn, s, a]) => new Function(`return (${fn})`)()(s, a), [paint.toString(), size, art]);
  await page.locator("canvas").screenshot({ path: join(root, file) });
  console.log("wrote", file);
}
await browser.close();
