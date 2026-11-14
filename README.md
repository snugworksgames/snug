# Snug

A one-thumb mobile game by Snugworks: **hold to grow a jelly blob, let go to shrink it, fit it snugly through each gap.**
Too big and you get squished. Too loose and your glow fades.

- **Play:** the GitHub Pages site built into `docs/` (works on phones, offline after the first visit, and can be added to the home screen)
- **Launch plan:** [LAUNCH_PLAN.md](LAUNCH_PLAN.md): path to Google Play and the App Store, costs, rules for a young audience, revenue estimate
- **Design:** [GAME_DESIGN.md](GAME_DESIGN.md): core loop, difficulty, variety, retention, monetization

## Working on the game

The whole game is one source file, `web/snug.html`. After changing it:

```bash
node tools/build.mjs
```

```bash
node tools/bot-playtest.mjs
```

The first command rebuilds the hosted site (`docs/` plus a copy at the repo root). The second has a bot play the built game in a headless browser and checks the main flows: a long run with every mechanic, buying a hat, results, pause, and a challenge link giving two players identical gates. It needs Playwright with Chromium. Bump `BUILD` in `web/snug.html` for each release so players' phones pick up the new version.

| Path | What's in it |
|---|---|
| `web/snug.html` | Game source (also runs as a standalone preview) |
| `docs/` | Built site for GitHub Pages: page, bundled fonts, icons, offline support |
| `index.html`, `manifest.webmanifest`, `sw.js` | The same site for when Pages serves the repo root |
| `store/` | App icons for the store listings |
| `tools/build.mjs` | Builds the site from the source file |
| `tools/make-icons.mjs` | Draws the app icon in every needed size |
| `tools/bot-playtest.mjs` | Automated playtest |
| `tools/SnugSim.cs` | Difficulty and economy simulator for the core rules |
| `android/`, `app/`, `capacitor.config.json` | Android app (Capacitor) that wraps the built game, with ads, vibration and the share sheet |
| `tools/build-app.mjs` | Builds the app's web files from `docs/` and adds `app/native.js` |
| `.github/workflows/android.yml` | Builds the Android app on GitHub: a test APK every run, a signed bundle for Google Play once the upload key is saved |
| `docs/privacy.html` | Privacy policy for the store listing |
| `unity/` | Early Unity prototype of the core rules. Not kept in sync with the web game; see the launch plan |

## Android app

The app is the same game inside a native shell. GitHub Actions builds it (Actions tab → "Android app"); to build it yourself you need Node 22, Java 21 and the Android SDK:

```bash
npm ci && npm run android:sync
```

```bash
cd android && ./gradlew assembleDebug
```

Without AdMob settings the app shows Google's test ads. Real ad unit IDs, the AdMob app ID, the upload key and the public game address are read from environment variables (see `tools/build-app.mjs`, `android/app/build.gradle` and the workflow). Ads are requested as child-directed, non-personalised and rated for general audiences, and the app never reads the advertising ID.

Fonts in `docs/fonts` are Lilita One and Nunito, both under the SIL Open Font License (license files alongside).
