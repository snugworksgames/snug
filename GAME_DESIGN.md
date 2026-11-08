# Snug: game design and production plan

**One-line pitch:** Hold to grow a jelly blob, let go to shrink it, and fit it snugly through each gap. Too big and you get squished. Too loose and your glow fades.

**Platform:** iOS and Android, portrait, one thumb, fully offline.
**Session:** 15–60 s runs (simulated medians: casual 17 s, regular 23 s, skilled 40 s).
**Monetization:** rewarded ads the player chooses, plus a rare, capped interstitial. Optional "Remove ads" purchase.

Playable web prototype: https://claude.ai/artifact/4amX5rMvFyEV8f9R4R3E6K
Unity prototype: `unity/` (see `unity/README.md`).

---

## 1. Three original concepts

| | Concept | Core action | Why it could work | Why it might not |
|---|---|---|---|---|
| A | **Snug** — a jelly blob passes through gaps in sliding walls. Hold to grow, release to shrink. Score depends on how tight the fit is. | Hold / release | You can read it in one glance (the blob is a circle, the gap is a gap). It rewards precision, not just survival. Near-misses are visible and physical (the blob squishes). | Holding to a size takes skill; it needs careful tuning so it doesn't feel twitchy. |
| B | **Lantern Drift** — a paper lantern rises through a night sky. Tap to release a puff of heat; lanterns cool and sink. Pass through rings of fireflies. | Tap | Calm, cozy visual identity. Easy to start. | The flight model is close to an established genre. It's hard to make look new, and failures can feel floaty and unclear. |
| C | **Tidepool Sort** — shells roll down a chute. Swipe left/right to send each into the matching pool before the tide comes in. | Swipe | Sorting is satisfying and easy to understand. | It has two decisions per item (which pool and when), so the 10-second test is weaker. The speed ramp turns into reading, not dexterity. |

### Chosen: A, Snug

- **10-second clarity.** One verb, one shape, one goal, and the ghost ring shows the target size. No text is needed.
- **Visible skill growth.** Fit quality (Squeeze > Perfect > Good > Loose) is graded, so players see themselves improve within a run, not only across runs. That improvement keeps people playing.
- **Fair failure.** Every death has an obvious, physical cause: "I was too big" or "I was too small". The generator only creates size changes the blob can physically reach in time (proven in unit tests), so failure never feels random.
- **"One more try" built in.** Runs are short and restarting is instant (hold anywhere). Results show "Only 3 more to beat your best". Chains and multipliers make the next run feel winnable.
- **Distinct look.** A cute squishy character with a face, a night pond with fireflies and soft clay gates. It doesn't resemble the usual neon-geometry look, and every theme and skin is a recolor, so cosmetics are cheap to make.
- **Short to build.** Everything is generated in code. There are no levels to design.

---

## 2. Core loop

1. **Start.** Hold anywhere on the home screen. The press that starts the run also starts growing the blob, so there's no Play button.
2. **Core action.** Hold to grow, release to shrink. Speed ramps up from 30% to 100% over 0.3 s after every press or release, so quick taps nudge and long holds sweep.
3. **Gate arrives.** When a gate's walls reach the blob, the fit is judged (fit = blob diameter ÷ gap):

| Result | Rule | Points | Effect | Feedback |
|---|---|---|---|---|
| **Squeeze** | diameter within 1.5 px of the gap (up to 6 px over is forgiven) | 5 × mult | +1 glow, +1 chain, +1 coin | Big burst, ring, screen flash, rising chime, medium haptic |
| **Perfect** | fit ≥ 0.84 (0.80 for the first 5 gates) | 3 × mult | +1 glow, +1 chain, +1 coin | Burst + ring, pentatonic note that rises with the chain, light haptic |
| **Good** | fit ≥ 0.60 | 1 × mult | chain resets | Small sparkle, soft pluck |
| **Loose** | fit < 0.60 | 0 | −1 glow (of 3), chain resets | Blob dims, low thud, red label |
| **Crash** | diameter > gap + 6 px | run ends | | Splat, screen shake, heavy haptic, dazed eyes |

   Multiplier = 1 + chain ÷ 3 (capped at ×5). Every 10th gate is **gold** and pays 5 coins for a Perfect.
4. **Difficulty rises** (section 4). The run ends on a crash, or when glow hits 0 after three loose fits.
5. **Revive offer** (once per run, only if score ≥ 6): "Keep going?" with a 4-second countdown and a rewarded ad. Saying no or letting it time out goes straight to results.
6. **Results:** score, best (or a NEW BEST badge), a near-miss line ("Only 3 more to beat your best!"), gates, perfects and coins. Mission and level-up toasts follow. **2× coins** via an optional rewarded ad.
7. **Restart:** hold anywhere (after a 0.7 s lock so a finger that is still down doesn't skip the results), or tap Play again.

---

## 3. The first 30 seconds

There is no tutorial screen. Everything is taught by the first five scripted gates, the ghost ring and two context hints that only appear in the first three runs.

| Time | What happens | What the player learns |
|---|---|---|
| 0 s | Home: big "Snug" title, the blob breathing and blinking, "Hold anywhere to start". | It's alive; touch it. |
| 0.2 s | They press. The blob swells, the hum rises in pitch, and the first gate slides in, **extra wide (190 px)**. | Holding = growing. |
| ~2 s | Most players are still holding, so the blob is huge, and gate 1 is sized so maximum size squeezes through. **"SQUEEZE!"**: burst, flash, chime. | Early win. Tight is good. |
| ~3.5 s | Gate 2 is narrow (88 px). If the blob is too big, a hint pulses below it: "Let go to shrink". | Releasing = shrinking. |
| 3–10 s | Gates 3–5 alternate wide/narrow at a slow 150 px/s. Near the perfect size the blob gets "sticky" (growth slows to 30% within ±7 px), so Perfects come easily. Chain notes climb a scale. | The rhythm of the game. |
| ~10 s | Gate 10 is gold (+5 coins) and "10 gates!" fires with confetti. The simulated first-run median is exactly 10 gates. | Progress and reward. |
| 15–30 s | Speed ramps up and gaps swing wider. The first death is clearly caused ("Too big! Let go to shrink."). Any score above 0 on run 1 shows NEW BEST. | "I can do better", then hold to retry. |

Beginner assists: the sticky zone and context hints switch off after 3 runs, and the slower pace only applies to the very first run. Every run starts gentle: wider crash tolerance (10 px) and an easier Perfect for its first 5 gates, plus an extra-visible ghost ring (55% opacity, fading to 16% by gate 20).

---

## 4. Difficulty curve (fair, not random)

All values are in logical px on a 360-wide screen. `n` = gates passed.

| Gates | Speed (px/s) | Spacing (px) | Gap range | Largest size change | Extras |
|---|---|---|---|---|---|
| 1–5 | 150 | ~300 | scripted 190 / 88 / 136 / 70 / 120 | scripted | beginner assists |
| 6–15 | 177–218 | 276–240 | 60–148 | 52–88 | first gold gate at 10 |
| 16–30 | 222–285 | 236–190 | 50–165 | 92–128 (peaks near gate 25) | |
| 31–40 | 290–330 | 190 | 44–176 | 111–95 (limited by reach time) | **breathing gates**: gap pulses ±6–14 px |
| 40+ | 330–360 cap | 190 | 44–176 | 95–85 (limited by reach time) | |

**Fairness rules (enforced in `GateGenerator`, verified by unit tests):**
- The size change between consecutive gates is at most 72% of what the blob can physically change in the time between them, accounting for the slow start of the speed ramp.
- Every gap fits the smallest blob, and a Perfect is always reachable at the largest blob.
- Consecutive gaps usually differ by at least 18 px, so there are no dull repeats, and the direction flips near the range limits.
- Perfect thresholds stay the same after the first 5 gates. Difficulty comes from pace, not from moving goalposts.

**Simulation results** (`tools/SnugSim.cs`): bots with human-like reaction lag (160–300 ms), imperfect lag compensation, a 90 ms tapping limit and 3.5–10% size misjudgement play the real rules, 2,000 runs per profile:

| Profile | Median run | Middle 50% | Median gates | Median score | Coins / run |
|---|---|---|---|---|---|
| New player (first run, with assists) | 18 s | 8–23 s | 10 | 20 | 4.5 |
| Casual | 17 s | 8–22 s | 10 | 18 | 4.5 |
| Regular | 23 s | 17–29 s | 14 | 31 | 7.7 |
| Skilled | 40 s | 37–43 s | 35 | 85 | 21.5 |

Bots are a tuning aid, not a replacement for playtests. Re-tune `SnugConfig` against real soft-launch data (median run length, deaths per gate index).

---

## 5. "One more try" design

- **Restart in under 1 second.** Hold anywhere on the results screen. Results get a 0.7 s read window first.
- **The near miss is spelled out.** "Only N more to beat your best!" appears when within 15% (or 3 points).
- **The cause is shown.** The face is dazed on a crash, the blob fades on loose fits, and the message names the fix.
- **Progress always moves.** XP, mission bars and coins advance every run, even a bad one.
- **Chains reset per run**, so every run has a fresh chance at a big multiplier.

---

## 6. Retention

### Daily streak
Claimed on the first open each calendar day, shown as a 7-day strip.

| Day | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Coins | 10 | 15 | 20 | 25 | 30 | 40 | 50 + **Aurora skin** (only from the streak) |

Missing a day resets to day 1. After day 7 the cycle repeats at those values. Suggested v1.1: a "streak freeze" bought with coins or a rewarded ad.

### Three daily missions
The same three for everyone on a given date (seeded by the date), refreshed at local midnight with a visible countdown.

| Mission | Target | Reward |
|---|---|---|
| Get N Perfects in one run | 6 | 25 |
| Chain N Perfects in a row | 4 | 30 |
| Pass N gates in one run | 25 | 30 |
| Score N in one run | 40 | 35 |
| Play N runs | 5 | 20 |
| Collect N coins | 15 | 25 |
| Pull off N Squeezes | 2 | 30 |
| Nail a gold gate | 1 | 30 |

Rewards are paid automatically when the run that finishes a mission ends, with a toast and a sound. There's no claim button to forget.

### Progression
- **XP** = score + gates passed per run. Level *L* needs 80 + 40·L XP. Each level pays 20 coins. A casual player reaches about level 3 on day 1.
- **Coins** come from Perfects (1), gold gates (5), missions, the streak and level-ups.
- **Blob skins** (coins): Peach (free), Mint 150, Lilac 300, Lemon 450, Sky 600, Ruby 800, Aurora (day-7 streak).
- **Pond themes** (by level): Night pond (1), Dusk (3), Moss (5), Ember (8).
- **Economy check:** a casual player earns roughly 170–190 coins on day 1 (≈12 runs × 4.5 + missions ≈ 75 + streak 10 + level-ups ≈ 40). The first purchase therefore comes on day 1, and the whole skin set takes about 2–3 weeks. Plan new cosmetics (trails, hats, gate styles) before that runs out.

---

## 7. Monetization: few, well-placed ads

| Placement | Type | When | Frequency |
|---|---|---|---|
| **Keep going?** | Rewarded | Right after a fail, score ≥ 6 | Max 1 per run, 4 s to decide, skipping has no cost |
| **2× coins** | Rewarded | Results screen, coins ≥ 3 | Once per run |
| **Between runs** | Interstitial | When starting the next run | Never in the first session, never before run 6, at most every 4th run, ≥150 s into the session, never within 90 s of a rewarded ad |
| (later) Free skin trial | Rewarded | Looks sheet | Optional, max 3/day |

**Never:** banner ads during gameplay, ads at app launch, ads mid-run, fake close buttons, or rewards that can only be got through ads.
**Remove ads** (IAP, about $2.99) removes interstitials only and keeps the optional rewarded ads. The flag (`SaveData.noAds`) and the ad rule already exist; the store purchase itself still needs to be added (Unity IAP).
**Revenue model:** ARPDAU ≈ (rewarded views per DAU × rewarded eCPM + interstitial views per DAU × interstitial eCPM) ÷ 1000 + IAP. Fill this in with the eCPMs your mediation dashboard reports in soft launch rather than outside benchmarks.

**Compliance:** Google UMP consent (EEA/UK) and Apple ATT prompt before ads initialize. Mark the app "not primarily for children" unless you want to go COPPA/Families-compliant (which limits ad networks).

---

## 8. Analytics and KPIs

Events already wired up in the Unity build (`IAnalytics`): `session_start`, `streak_claim`, `run_start`, `run_end` (score, gates, seconds, cause, best, continued), `ad_offer`, `ad_rewarded`, `ad_interstitial`, `mission_complete`, `level_up`, `skin_unlock`.

Watch in soft launch:
- **D1 / D7 / D30 retention.** Commonly cited hyper-casual targets are D1 ≥ 35–40% and D7 ≥ 10–15%.
- **Median run length** (target 20–30 s) and **death gate histogram** (spikes mean an unfair section).
- **Runs per session** (target ≥ 6) and **sessions per day**.
- **Rewarded opt-in rate** (offers ÷ views) and **interstitial impressions per DAU** (keep ≤ 3).
- **First-session funnel:** opened → first run → run 3 → first Perfect chain ≥ 3 → day-2 return.

A/B tests to run first: assist strength, revive minimum score, interstitial cadence (every 4th vs 5th run), mission rewards, and a ghost ring that always shows vs one that fades.

---

## 9. Technical plan (Unity)

- **Engine:** Unity 6 LTS (also compiles on 2021.3+), 2D, portrait-locked, 60 fps target, works in both the Built-in and URP 2D pipelines (sprites only, no custom shaders).
- **Architecture:**
  - `Snug.Core` (pure C#, its own assembly, unit-tested): config, gate generator, blob motion, judging and scoring, save data, streak, missions, progression, ad rules.
  - Unity layer: `SnugGame` (state machine and loop), views (blob, gates, backdrop), sprite particles, synthesized audio, haptics, uGUI built in code, ad and analytics adapters.
- **Offline:** all gameplay, save data (`PlayerPrefs` JSON) and progression are local. Ads simply aren't offered when not loaded.
- **Ads:** `IAdService` with a mock (prototype) and a Unity LevelPlay adapter behind the `SNUG_LEVELPLAY` define.
- **Haptics:** Android `VibrationEffect` one-shots, iOS `UIImpactFeedbackGenerator` via a tiny native plugin.
- **Performance:** pooled gates and particles, no per-frame allocations in the gameplay loop, about 120 sprites on screen at most.
- **Save safety:** versioned save, a corrupt save falls back to a fresh one, and it saves on pause and at every run end.
- **Production swaps:** TextMeshPro with a rounded display font (Bagel Fat One is OFL), designed audio, UI prefabs, cloud save (Game Center / Play Games) for v1.1.

---

## 10. Roadmap

| Phase | Length | Exit criteria |
|---|---|---|
| Prototype (done) | — | Web and Unity builds, tests pass, sim tuned |
| Playtest | 1–2 weeks | 15+ fresh players: understand it without help in 10 s, ≥ 70% get a Perfect in run 1, median run 15–30 s |
| Vertical slice | 2–3 weeks | Final art direction, audio, haptics tuned on 3+ devices, real ads + consent, analytics |
| Soft launch (e.g. PH, CA, AU) | 3–4 weeks | D1 ≥ 35%, D7 ≥ 10%, crash-free ≥ 99.5% |
| Global launch | — | Add LiveOps: weekly event pond, new skins every 2 weeks, streak freeze |

## 11. Risks

| Risk | Mitigation |
|---|---|
| Hold-to-size feels twitchy on some devices | Ramped speed, sticky zone in early runs, tune `growRate`/`minRateFraction` per playtest |
| Players hold through narrow gates and crash early | Gate 1 rewards holding, then the hint system teaches release on gate 2 |
| Players only aim small to stay safe | Loose fits drain glow (3 strikes) and pay no points |
| Content runs out (skins) in ~3 weeks | Cosmetic pipeline is recolor-based. Plan 2 drops a month |
| Ad fatigue | Hard caps in `AdPolicy`, rewarded-first design, Remove-ads IAP |
