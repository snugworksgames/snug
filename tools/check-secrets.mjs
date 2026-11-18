#!/usr/bin/env node
// Fails if anything that looks like a secret is committed: API keys, access tokens, private keys or signing-key files.
// Snug has no server and calls no paid service, so it can never run up a bill. This check keeps it that way:
// a key for a paid API (the usual way a hobby app gets an $80,000 bill) can't slip into the repository unnoticed.
//
//   node tools/check-secrets.mjs
import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";

const files = execFileSync("git", ["ls-files"], { encoding: "utf8" }).split("\n").filter(Boolean);
const BAD_FILE = /\.(jks|keystore|p12|pfx|pem|key)$|(^|\/)\.env(\..*)?$|google-services\.json$|service-account[^/]*\.json$/i;
const PATTERNS = [
  ["Google API key", /AIza[0-9A-Za-z_-]{35}/],
  ["OpenAI key", /\bsk-(?:proj-)?[A-Za-z0-9_-]{32,}/],
  ["Anthropic key", /\bsk-ant-[A-Za-z0-9_-]{20,}/],
  ["GitHub token", /\bgh[pousr]_[A-Za-z0-9]{36}\b|\bgithub_pat_[A-Za-z0-9_]{22,}/],
  ["AWS key", /\bAKIA[0-9A-Z]{16}\b/],
  ["Stripe live key", /\b[sr]k_live_[0-9a-zA-Z]{20,}/],
  ["Slack token", /\bxox[baprs]-[A-Za-z0-9-]{10,}/],
  ["Private key", /-----BEGIN [A-Z ]*PRIVATE KEY-----/],
];
const found = [];
for (const f of files) {
  if (BAD_FILE.test(f)) { found.push(`${f}: signing key or secrets file`); continue; }
  let buf; try { buf = readFileSync(f); } catch { continue; }
  if (buf.includes(0)) continue;                       // binary (images, fonts, jars)
  const text = buf.toString("utf8");
  for (const [name, re] of PATTERNS) if (re.test(text)) found.push(`${f}: looks like this kind of secret: ${name}`);
}
if (found.length) { console.error("Possible secrets committed:\n  " + found.join("\n  ")); process.exit(1); }
console.log(`No secrets found in ${files.length} tracked files.`);
