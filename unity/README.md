# Snug: Unity prototype

The whole game is built from code. There are no prefabs, art, audio files or scene setup beyond one component.

## Run it (2 minutes)

1. Create a new project in **Unity 6 LTS** (2021.3+ also works) with the **2D** template.
2. Copy this folder's `Assets/Snug` into your project's `Assets/`.
3. Make a new empty scene. Create an empty GameObject and add the **SnugGame** component.
4. Press **Play**. In the Game view, pick a portrait resolution (e.g. 1080×1920). Click and hold to play.

The build includes the daily streak pop-up, missions, skins and themes ("Looks"), settings, the revive offer and 2× coins. Ads are simulated with a 3-second card.

## Build for phones

- **Player Settings → Resolution:** Default Orientation = Portrait.
- **Android:** IL2CPP, ARM64, minimum API 24. The VIBRATE permission is added automatically.
- **iOS:** `Plugins/iOS/SnugHaptics.mm` provides short haptic taps (no setup needed).
- If the project uses the new Input System only, the UI event module is created automatically (`ENABLE_INPUT_SYSTEM`).

## Real ads (Unity LevelPlay)

1. Install the LevelPlay package (Ads Mediation) and set up your app in the LevelPlay dashboard.
2. Add `SNUG_LEVELPLAY` to **Player → Scripting Define Symbols**.
3. On SnugGame: untick **Use Mock Ads** and fill in the app key and the rewarded and interstitial ad unit ids.
4. Add Google UMP consent (EEA/UK) and the iOS ATT prompt before ads initialize.

`LevelPlayAdService` targets the LevelPlay 8.x ad-unit API. Event signatures change between SDK major versions, so check them against the version you install. That file is excluded from compilation until you add the define.

## Tests

`Window → General → Test Runner → EditMode → Run All`. The 10 tests cover fairness (every size change is reachable, every gap is passable), the first-gate win, judging, glow and multiplier, streak, missions and ad rules.

## Layout

```
Assets/Snug/
  Scripts/Core/      Pure C# rules (own assembly, no UnityEngine): tune everything in SnugConfig
  Scripts/Game/      SnugGame (loop and state machine), views, generated sprites
  Scripts/Fx/        Pooled particles, synthesized audio, haptics
  Scripts/Ads/       IAdService (mock and LevelPlay), analytics, save system
  Scripts/UI/        uGUI screens built in code
  Plugins/iOS/       Haptics bridge
  Tests/             EditMode NUnit tests
```

## Tuning

Every gameplay number lives in `SnugConfig` and is editable in the Inspector on SnugGame. To check a change before playtesting, run the bot simulator in `../tools/SnugSim.cs` (instructions at the top of that file).
