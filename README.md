# Gemforge Cascade

An original match-3 prototype with generated colored gems, animated swaps, gravity, cascades, limited moves and a score objective.

## Open

Open this folder in Unity 2022.3 LTS, open `Assets/Scenes/Main.unity`, and press Play. No imported art or TMP resource setup is required.

Click a piece and a neighbor, or drag horizontally/vertically. Invalid swaps cost no moves. Reach 1,000 points within 25 valid moves. Cascades award 10 points per piece times the cascade multiplier (1, 2, 3, ...). Restart appears after winning or losing.

## Design Documentation

See [the design docs](docs/README.md) for the graphics/audio brief, UI/UX gaps, replayability proposals and prioritized roadmap. These describe planned improvements, not implemented features.

## Level Configuration

Select BoardManager in Main. Config controls width/height (3-16), piece types (3-6), cell size, moves and target score. Animation Duration controls swap/fall/clear timing.

## Scripts and Scene

- `BoardManager.cs`: scene startup, click/drag input, animation, cascades and dead-board recovery.
- `BoardModel.cs`: generation, legal moves, matches and gravity, independent of Unity.
- `Piece.cs`: coordinates, type, selection and movement state, visuals.
- `GameManager.cs`: score, remaining moves, win/loss and restart.
- `UIManager.cs`: HUD, result overlay and restart button.
- `BoardConfig.cs`: serialized level settings.

Main stores BoardManager and its configuration and is included in Build Settings. On Play it creates Main Camera, GameManager, UIManager/HUD Canvas, EventSystem and pieces. The Canvas contains score, moves, target, result text and restart. Use Input Manager or Both under Active Input Handling.

## Verification and Limitations

Run `dotnet run --project Tests/BoardRules/BoardRules.csproj` with .NET 8 or later. Tests exercise the actual board rules and GameManager (with minimal host types): 200 seeded boards, 2,000 turns, invalid rollback, gravity order, match intersections, cascade settling and final-move outcomes.

Unity is not installed on the implementation machine. Editor compilation, scene import, input and rendering still need an Editor smoke test. Standalone tests do not validate Unity APIs or visuals.

Dead boards regenerate with no matches and a legal move; the previous color inventory is not preserved. Special pieces, blockers, audio and additional levels are deferred. Touch relies on Unity mouse emulation; mouse is the primary input.
