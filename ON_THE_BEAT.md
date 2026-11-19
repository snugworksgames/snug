# On the beat: Snug's signature system (planned, build after the closed test)

**The idea in one line:** gates arrive on the music's beat, and every good fit plays a note of the melody.
Snug becomes "the precision game you can hear": a Perfect chain sounds like a tune, a miss leaves a gap you can hear.

**Why:** one-thumb precision games are a crowded genre. This turns Snug's existing pieces (generated music,
chain notes that rise, seeded gates) into something easy to describe, easy to show in a 10-second video, and hard to copy
by accident.

**When:** after the 14-day closed test, and only if testers come back on day 2 at least as often as before.
It changes how gates are generated, so it is a deliberate release of its own, not a patch.

**Estimated effort:** about one day of work, plus tuning.

---

## How it works today (build 17)

| Part | Code (`web/snug.html`) | Behaviour |
|---|---|---|
| Music tempo | `music()`, `const bpm=inRun?104+zone*6+Math.min(16,R.passed*.3):92` | Tempo follows the zone and gates passed, on the audio clock |
| Music timing | `while(musicT<AC.currentTime+.18)` | Steps scheduled 0.18 s ahead on wall-clock time |
| Gate spacing | `spacing(n)`, `speedGen(n)` with `wave(n)`, `spawnUntilTop()` | Gates placed by distance; arrivals are not related to the beat |
| Fit sounds | `SFX.perfect(s)` with `note(i)` over `PENTA` | Rising pentatonic notes over C, not the current chord |
| Freezes and slow-mo | `hitStop`, `R.pw.slowUntil` (×0.6), intro slowdown (×0.55), pause | The world stops or slows; the music keeps its own time |

## What changes

### 1. Tempo comes from the gate number (deterministic)
- `bpmAt(n) = 96 + min(36, n × 0.75)`: 96 BPM at the start, 132 by gate 48.
- Because it depends only on the gate number, two players with the same seed hear the same tempo and get the same gates.

### 2. Gates are placed on the beat grid
- In `spawnUntilTop()`, round each gap between gates to a whole number of half-beats:
  `halfBeatPx(n) = speedGen(n) × 30 / bpmAt(n)`, then `sp = max(2, round(sp / halfBeatPx(n))) × halfBeatPx(n)`.
- Twin gates become exactly 2 half-beats apart. The first gate's distance (`BY-310`) is snapped the same way.
- `makeGate()` already computes fairness from the actual spacing (`dt = sp / speedGen(n)`), so every size change
  stays physically reachable after snapping.

### 3. The world is the metronome
- During a run, schedule music steps from **distance climbed** (`climb`), not from the audio clock:
  each segment between gate *n−1* and gate *n* has *k* half-beat steps spread evenly over its distance.
- A step's audio time is `AC.currentTime + (stepClimb − climb) / v`, with a short look-ahead (about 0.08 s).
- Result: freeze-frames stutter the music, slow-mo slows it, pause stops it, and **every gate arrives on a step**.
- Home and results keep today's time-based music.

### 4. Fits play the melody
- `SFX.perfect` picks notes from the **current chord** (`PROG[zone][bar]`) instead of a fixed C pentatonic,
  climbing through chord tones as the chain grows, so a chain always sounds in key.
- A Squeeze adds a downbeat accent (kick plus a chord stab). ON FIRE keeps its extra layer.
- A Loose fit or a crash **skips that beat's melody note**: the silence is the feedback.

### 5. It must stay fully playable with the sound off
Many kids play muted. Add a **visual beat**: the gates' bumper knobs and the ghost ring pulse softly on each step.
Rhythm is a bonus that makes timing intuitive; it is never required to play well.

### 6. Headphone delay
Bluetooth headphones can add 150–250 ms of audio delay, which would put the beat visibly late.
- Shift scheduling earlier by `AC.outputLatency` where the browser reports it.
- Add one Settings switch, "Bluetooth headphones (sound delay)", that adds 200 ms when the browser doesn't.

## Compatibility
- Bump `GEN` from 2 to 3. Challenge links from older builds then show the existing "different version" warning.
- Ship the web and the Android app together, so both play the same Daily Challenge gates.
- Daily Challenge numbering is unchanged.

## How to check it before shipping
1. **Difficulty stays the same:** run the human-model simulator on the new build against build 17. Snapping moves gate
   timing by up to a quarter beat, so check that runs end in the same places (within about 10%).
2. **On the beat:** a playtest check that every gate is judged within one frame of a music step.
3. **Same gates for everyone:** the existing challenge and Daily "identical gates" checks must still pass.
4. **Real phones:** play with the phone speaker, with wired headphones and with Bluetooth headphones, and once muted.
5. **Testers:** ask five of them "does it feel more fun or more stressful?" before releasing.

## Work plan (about one day)
| Step | Time |
|---|---|
| `bpmAt`, half-beat snapping in `spawnUntilTop`, `GEN` 3 | 2 h |
| Distance-based music scheduler, look-ahead and latency offset | 3 h |
| Chord-aware fit notes, Squeeze accent, silent misses | 1 h |
| Visual beat pulse on gates | 1 h |
| Simulator comparison, playtest checks, real-phone tuning | 2 h |

## How to describe it in the store
"Snug: the one-thumb game you can hear. Fit through the gaps on the beat, and every perfect fit plays the melody."
