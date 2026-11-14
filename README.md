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
| `unity/` | Early Unity prototype of the core rules. Not kept in sync with the web game; see the launch plan |

Fonts in `docs/fonts` are Lilita One and Nunito, both under the SIL Open Font License (license files alongside).
