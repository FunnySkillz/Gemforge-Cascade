# Replayability and Delivery Roadmap

## What should make it compelling?

The intended loop is: notice a promising pattern, choose a swap, enjoy a tactile clear, build a special, use it deliberately, and finish a short challenge. The next attempt should offer a new decision or a chance to improve.

The current game mostly rewards spotting any legal match and waiting for random refills. More particles alone will not add lasting depth. The strongest proposed improvement is giving the player an achievable plan: "I can make a four-match, forge a line clear, and use it where the objective needs it."

Treat this as a design hypothesis. There is no player research or measured retention baseline yet.

## A signature mechanic: Forge Charge

Start with a line-clearing special earned from a straight match of four. Once that works, test a visible Forge Charge meter as Gemforge's distinctive addition.

Proposed first rules:

- A valid player move contributes one charge, plus one if it creates a four-or-more match. Cap the gain at two per turn so random cascades do not dominate this resource.
- At six charges, the player earns one Forge action. Capacity is one; progress remains ready until used.
- Forge lets the player convert an ordinary gem into a horizontal or vertical line-clear gem, with an explicit orientation choice and cancel action.
- Conversion spends the charge but not a board move. It does not immediately clear the board or bypass the level's end state.
- A forged gem activates when matched. The player must plan its use within remaining moves.
- No forge input is available during swaps, cascades or the end screen.

Six charges is a starting tuning value, not a balanced result. Test whether it arrives early enough to matter and whether a choice of orientation adds useful strategy or unnecessary friction. Ship the simpler four-match special first if the meter complicates the board.

## Special-piece sequence

| Stage | Rule | Purpose |
| --- | --- | --- |
| First | Four in a row creates a line clear | Reward deliberate pattern setup |
| Second | T/L intersection creates a local blast | Reward spatial planning |
| Third | Five in a line creates a color clear | Create a rare, legible high-value outcome |
| Later | Special-to-special swaps have defined combinations | Support advanced setup and discovery |

Before implementing, define special placement, activation, overlapping matches and scoring. Prefer a player's swapped destination for creation when it belongs to the match; otherwise use a deterministic rule. A new special must survive its creation clear. Deduplicate cleared cells, bound activation chains and resolve the whole turn before checking the outcome.

`BoardModel` now stores color and special kind separately and returns structured match groups. Special creation, survival and activation rules still need implementation before these states affect play.

## Progression with a reason to return

After the polished level is validated, build a small authored sequence rather than a large set of score targets:

| Levels | Focus | New decision |
| --- | --- | --- |
| 1-2 | Basic matching and objective feedback | Choose useful matches |
| 3-4 | Line-clear introduction | Make a larger match instead of the first available one |
| 5-6 | Collect a specific gem type | Trade score against objective progress |
| 7-8 | Forge action, if validated | Decide where and when to place a special |
| 9-10 | Combine previously learned rules | Plan several moves ahead |

Author move budgets, type counts, goals and any starting arrangements per level. Each level should introduce one meaningful variation. Add blockers only after these choices remain interesting without them.

Longer-term options: visible workshop upgrades earned by completing chapters, cosmetic gem cuts, optional mastery medals, and a seeded daily puzzle with a personal best. None are needed for the first polished slice. Rewards should show exactly what was earned and why; avoid creating multiple currencies before they serve a clear purpose.

Aim for a satisfying stopping point after each level. No energy waits, loss of earned progress for missed days, fabricated near-wins or pressure timers are proposed. Replay should come from curiosity and mastery.

## Delivery order

| Milestone | Work | Acceptance gate | Main code areas |
| --- | --- | --- | --- |
| 0: Validate | Unity import/build, input and aspect-ratio smoke tests | Main plays and restarts without errors on target devices | Scene, packages, BoardManager |
| 1: Feel | Six sprites, sockets, separate animation timings, sound, selection and clear effects | Board readable; accepted/rejected actions and cascades understandable | Piece, BoardManager, art/audio assets |
| 2: Clarity | Objective meter, chain feedback, pause/settings, accessible cues, result flow | First-time players can explain goal, moves and outcome | UIManager, GameManager |
| 3: Decisions | Four-match special, deterministic placement and activation | Intentional creation/use works, overlap and chain tests pass | BoardModel, match results, Piece |
| 4: Identity | Prototype Forge Charge and compare against specials alone | Players understand the action and use it strategically | GameManager, BoardManager, UIManager |
| 5: Progression | Authored level configs, collection objective, unlocks and saves | Ten coherent levels; progress survives restart/update | Level data, objective logic, persistence |
| 6: Replay | Personal bests, optional mastery goals and daily seed | Returning has a clear purpose without blocking regular play | Level selection, saves, seed handling |

Milestones describe scope and order, not time estimates. Engine validation precedes polish; persistence follows explicit level/state definitions. Keep the existing board tests and extend them around each new mechanic.

## First polished slice

Recommended scope for the next implementation batch:

1. Validate the existing Unity project and record a baseline gameplay capture.
2. Add six original gem silhouettes and a restrained board surface.
3. Add objective progress, visible cascade multiplier and local score feedback.
4. Split swap/fall/clear timings and add a small sound set with mute controls.
5. Add a line-clear special from four-matches, with rule tests.
6. Add pause/restart and a clearer outcome screen.

Defer the campaign, workshop, daily mode and Forge Charge until this slice is enjoyable. This makes the next review about actual play quality instead of feature count.

## Playtest plan

Run an initial qualitative round with 5-8 people unfamiliar with the project. This can expose usability problems; it cannot establish population-level retention. Let them play without coaching, then ask what they were trying to achieve, what felt rewarding and what felt unfair.

Record with permission:

- Time and attempts before the first valid swap.
- Invalid swaps, accidental drags and misunderstood input locks.
- Whether the player can explain the goal and cascade multiplier.
- Special creation and intentional use, once available.
- Completion, remaining moves, cascade duration and outcome by level and seed.
- Voluntary replay after a level, and the reason given for replaying or stopping.
- Enjoyment, frustration and perceived control, using the same simple rating questions each round.

Compare the baseline and polished slice with comparable levels/seeds and alternate presentation order when practical. Random boards otherwise confound difficulty with presentation changes. Automated random-move simulations can find extreme seeds and rule failures but do not estimate human difficulty reliably.

Do not treat longer sessions as success by themselves. A player who finishes a satisfying level quickly and wants to return is a better signal than one stuck repeating an unclear task. Set numerical product targets after gathering a baseline; no lift or win-rate claim is justified yet.

## Open decisions

Default assumptions are mouse-first development, portrait-friendly layout, offline solo play and no monetization system. Before production art or a campaign, decide target devices, intended session length, same-seed versus new-seed retry behavior, and whether workshop progression is visual-only or changes gameplay. These decisions need not delay the single-level polish pass.
