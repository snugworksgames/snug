# Snug

A one-thumb mobile game: **hold to grow a jelly blob, let go to shrink it, fit it snugly through each gap.**
Too big and you get squished. Too loose and your glow fades.

- **Play in a browser:** `docs/index.html` (published with GitHub Pages; works on phones, and you can add it to your home screen)
- **Design plan:** [GAME_DESIGN.md](GAME_DESIGN.md): concepts, core loop, first 30 seconds, difficulty, retention, ads, analytics, roadmap
- **Unity prototype:** [unity/](unity/README.md): add one component to an empty scene and press Play
- **Bot simulator:** [tools/SnugSim.cs](tools/SnugSim.cs): estimate run length and coin earnings before playtesting

| Folder | What's in it |
|---|---|
| `docs/` | Standalone web build for GitHub Pages |
| `web/` | Web prototype source (Claude artifact version) |
| `unity/Assets/Snug/` | C# game: pure rules (`Scripts/Core`, unit-tested) + Unity layer (rendering, UI, audio, haptics, ads) |
| `tools/` | Difficulty and economy simulator |
