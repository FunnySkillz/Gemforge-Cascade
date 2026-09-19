# UI and UX Audit

## Scope and current state

Source-reviewed files: `BoardManager.cs`, `Piece.cs`, `UIManager.cs`, `GameManager.cs` and `BoardConfig.cs`. The game has mouse click/drag swaps, input locking during resolution, invalid-move rollback, remaining moves, score, target, automatic dead-board regeneration and a result/restart screen. These systems are implemented, but Unity rendering and real-device input remain unverified.

## Prioritized gaps

| Priority | Gap | Why it matters | Proposed change |
| --- | --- | --- | --- |
| P0 | No Editor/device smoke test | Actual playability is not established by model tests | Import, compile, play, build, then test mouse and touch |
| P1 | All gem shapes are identical | Color is the only type cue | Distinct silhouettes and secondary marks |
| P1 | Target is only a number | Distance to success requires mental subtraction | Objective meter with current/target values |
| P1 | Cascade multiplier is invisible | Players cannot easily connect chains to rewards | Small chain indicator and local point feedback |
| P1 | No pause or settings | No in-game way to pause or control presentation | Pause sheet with resume, restart and settings |
| P1 | Result screen hides the board and only offers restart | Outcome lacks context and a next goal | Brief outcome summary, retry/next and a visible board behind it |
| P1 | No designed first-play experience | A new player may not discover legal swaps or the goal | Guided first puzzle using highlighted cells and immediate feedback |
| P1 | Mouse emulation is the touch strategy | Finger tracking and canceled gestures are untested | Explicit mobile testing, then dedicated pointer handling if needed |
| P2 | Dead boards regenerate instantly | The board can change without an understandable transition | Short board-wide reforge transition, no move penalty |
| P2 | Fixed top-20% HUD, no safe-area handling | Notches and short landscape screens may crowd content | Safe-area layout and aspect-specific HUD arrangements |
| P2 | Only end-of-game restart | Players cannot retry a difficult run mid-level | Restart in pause, with confirmation when progress would be lost |
| P2 | No save/resume or best-score record | Returning players lose continuity | Versioned local save for settings, progress and stable board state |

P0 is a release blocker. P1 is the next playable milestone. P2 follows once the core presentation works.

## In-level layout

The board remains the primary surface. Use a compact header with the level identifier, objective progress and remaining moves. Moves should be prominent but not constantly pulsing. Make low-move feedback a single restrained state change with an additional non-color cue.

Show the forge charge beside the objective only after that mechanic exists. Keep pause accessible outside the board. Avoid currencies, locked slots and buttons for features that have not been built. Use recognizable icons with desktop hover labels and accessible names.

For portrait, place the HUD above the board; for wide landscape, evaluate a narrow side HUD to recover board height. Do not stretch the grid. At large configured board sizes, enforce a minimum usable cell size or limit those sizes on phones rather than shrinking indefinitely.

## Interaction contract

- One pointer owns a gesture; additional touches cannot initiate concurrent swaps.
- Tapping a piece selects it; tapping it again deselects it. A non-neighbor becomes the selection.
- A directional drag starts at a piece and attempts exactly one neighboring swap.
- Ambiguous diagonal and out-of-board drags do not consume moves.
- Test gesture thresholds relative to cell size, not just screen height, across board sizes and devices.
- Clearing or replacing selection should have immediate visual feedback. Decide explicitly whether tapping empty space clears selection; the current implementation leaves it selected.
- Pause, focus loss, pointer cancellation and restart must discard unfinished gestures.
- Input remains blocked through the complete cascade, then becomes available without an additional cosmetic delay.
- Hints, if introduced, wait for inactivity and are optional. A hint identifies a legal move, not necessarily the best one.

## Outcome and return flow

Win: let the final cascade settle, show completed objective and earned score, then offer Next Level as the main action once multiple levels exist. Replay and level selection are secondary. Any star thresholds must be authored and visible, not generated after the outcome.

Loss: show the actual achieved objective and target, with Retry as the main action. Offer a clear route back to level selection once available. Avoid implying that a randomly generated retry is the identical puzzle; introduce an explicit same-seed retry if practicing the same board is a goal.

Return: restore the last stable board or return to a clear level selection state. Saving midway through a cascade requires a defined snapshot policy; initially save at stable turn boundaries. Settings should persist immediately.

## Accessibility and acceptance

Use shape plus color, readable contrast, scalable text with stable layout, reduced motion, separate sound controls and optional haptics. Keyboard selection and swapping should have a visible focus state when added. Full screen-reader support is not provided by the current runtime-created UI and needs its own implementation and testing scope.

Before release, verify:

1. First-time players can identify the goal and make a valid swap without verbal coaching.
2. Invalid attempts leave moves unchanged and do not feel like input failures.
3. A player can explain why a cascade awarded more points.
4. Score, target and moves remain readable at phone portrait, phone landscape, tablet and desktop sizes, including notched safe areas.
5. End results wait for the final cascade, and restart resets score, moves, selection, input state and board.
6. Background/resume, rapid taps, two-finger touches and dragging off-screen do not trigger unintended moves.
7. A forced dead board recovers with readable feedback and no penalty.

These are planned checks, not completed test results.
