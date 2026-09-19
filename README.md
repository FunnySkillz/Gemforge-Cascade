# Gemforge Cascade

An original 2D match-3 prototype with deterministic levels, combinable special gems, collection/crystal goals and a ten-level candidate chapter.

## Play

Open this folder in **Unity 2022.3.15f1**, open `Assets/Scenes/Main.unity`, and press Play. Use **Input Manager (Old)** or **Both** for Active Input Handling. No imported art or TMP setup is needed.

Click a gem and its neighbor, or drag along a row/column. Invalid swaps cost no moves. Complete every displayed goal before moves run out. Four-in-line creates a line special, T/L a blast, and five-in-line a color clear. Adjacent specials combine without a color match. See the [combination contract](docs/IMPLEMENTATION_PLAN.md#special-swap-contract).

Pause with Escape or the pause button. Restart replays the same opening/refill seed. Hints and reduced motion can be toggled in pause and persist locally. Winning allows the next level for the current session; chapter progress is not yet saved.

## Content

`Assets/Resources/Levels/level-*.json` contains ten validated candidate configurations with seeded openings. They are not yet a playtested, handcrafted tutorial chapter. The catalog validates candidates, skips invalid/duplicate levels with diagnostics, and orders files by name.

Use **Gemforge > Level Workshop** to load a JSON asset, paint gems/specials/crystals, edit goals, validate and save a copy. Grid rows display top to bottom; JSON cells are row-major from the bottom-left. Color indices are 0-5; layer durability 0 means empty. Baking an opening stores explicit pieces without changing the refill seed. Interactive designer acceptance is still pending.

An optional BoardManager Level Asset overrides the chapter; otherwise Resources levels take priority over BoardConfig fallback settings. Swap, fall and clear durations are separate Inspector settings.

## Verification

Run the rules suite with .NET 8 or later:

```powershell
dotnet run --project Tests/BoardRules/BoardRules.csproj -p:UseAppHost=false
```

Use **Gemforge > Validate Chapter and Build Windows**, or invoke Unity with `-batchmode -nographics -quit -projectPath <project> -executeMethod GemforgeCascade.Editor.BuildVerification.BuildWindows -logFile <log>`.

The development executable is generated at `Builds/Windows/GemforgeCascade.exe`. Its `-gemforgeSmoke` flag checks startup, a full turn, pause rejection, same-seed retry, chapter simulation and offscreen scene/UI captures. Run it from the project directory so captures go into `Logs`. Both folders are gitignored; build artifacts are not uploaded to GitHub.

Unity import and Windows builds have been verified locally. Automated game-state smoke checks pass in portrait and landscape. Real pointer/touch interaction, Android builds, mobile safe areas and device performance still require acceptance. Hidden-window captures use offscreen cameras and do not establish real display/input correctness.

## Delivery Plan

[IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) is the source of truth for phases, status and evidence. [Implementation review](docs/implementation-review.md) records the manual-change audit and remaining risks. Supporting [design documents](docs/README.md) describe the intended product, not completed features.

Remaining major work: handcrafted teaching layouts and balance, final art/audio/VFX, stable-turn saves, chapter map/workshop rewards, analytics and mobile QA. Dead-board recovery currently regenerates the board and loses special inventory; this needs a player-friendly treatment.
