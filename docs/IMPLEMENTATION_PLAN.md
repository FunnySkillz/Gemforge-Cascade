# Gemforge Cascade Implementation Plan

Last updated: 2026-09-19

This is the canonical source of truth for delivery. Update it in the same commit as implementation work. Product direction lives in the supporting design documents, but status, order, accepted decisions and completion evidence live here. If documents disagree, this plan wins until the conflict is resolved here.

## Product target

Build a polished tactical match-3 game in which players deliberately forge and combine powerful gems across handcrafted levels, then use the rewards to restore a magical workshop.

Current release target: a limited external vertical slice containing ten authored levels. The slice must establish game feel, special-piece strategy, two objective families, progression, saved state, useful analytics and stable mobile play. It does not include monetization or live operations.

## Status rules

| Status | Meaning |
| --- | --- |
| `NOT STARTED` | No implementation work accepted |
| `IN PROGRESS` | Active work exists but exit criteria are not met |
| `BLOCKED` | A named dependency prevents further progress |
| `DONE` | Exit criteria are met and evidence is recorded |
| `DEFERRED` | Deliberately outside the current release target |

Only mark work `DONE` with evidence. Passing pure .NET rules tests does not prove Unity compilation, rendering, input or device performance.

## Current snapshot

| Item | State |
| --- | --- |
| Active phase | Phase 2 - Competitive core (engine-independent work) |
| Current task | `P2.3` special-to-special combinations |
| Last completed task | `P2.2` blast and color-clear specials |
| Primary blocker | Unity 2022.3 LTS/Hub is not installed or available on PATH |
| Next review gate | P2.3 combination matrix is defined and all interactions pass tests |

## Phase overview

| Phase | Outcome | Status |
| --- | --- | --- |
| 0. Establish reality | Existing prototype compiles, runs and is captured on target hardware | `BLOCKED` |
| 1. Board architecture | Model supports structured matches, specials, deterministic levels and validation | `BLOCKED` |
| 2. Competitive core | Specials, combinations, objectives and ten authored levels are playable | `IN PROGRESS` |
| 3. Feel and identity | Production board art, animation, audio, VFX and accessible feedback | `NOT STARTED` |
| 4. Player journey | Tutorial, progression, workshop, results, settings and persistence | `NOT STARTED` |
| 5. Measurement and quality | Analytics, automated Unity tests, builds and device quality gates | `NOT STARTED` |
| 6. Forge experiment | Signature Forge mechanic validated against the core version | `NOT STARTED` |
| 7. Limited release | Store-ready external build and evidence-based product review | `NOT STARTED` |
| 8. Expansion decision | Premium expansion or live-service path selected from evidence | `DEFERRED` |

## Phase 0 - Establish reality

Outcome: the inherited prototype is known to compile and behave correctly in the engine and on representative hardware.

| ID | Deliverable | Status | Evidence / blocker |
| --- | --- | --- | --- |
| P0.1 | Open and import with the pinned Unity version | `BLOCKED` | Unity 2022.3 LTS is unavailable on this machine |
| P0.2 | Resolve all Unity compile/import errors and warnings | `BLOCKED` | Depends on P0.1 |
| P0.3 | Create Android and desktop development builds | `BLOCKED` | Depends on P0.1-P0.2 |
| P0.4 | Smoke-test click, touch, pause/resume, restart, safe areas and aspect ratios | `BLOCKED` | Depends on P0.3 and devices/emulators |
| P0.5 | Exercise pure board and game-state rules outside Unity | `DONE` | `dotnet run --project Tests/BoardRules/BoardRules.csproj`; 200 seeded boards and 2,000 turns pass |
| P0.6 | Capture baseline gameplay and first-use observations | `BLOCKED` | Depends on P0.2-P0.4 |

Exit criteria:

- Main opens and plays without errors.
- Android and desktop development builds complete.
- Board, HUD, input and restart work at agreed target aspect ratios.
- Baseline capture and known Unity/device defects are attached to this plan.

Phase 0 may remain blocked while engine-independent Phase 1 work continues. No later presentation or release phase can be marked done before Phase 0 closes.

## Phase 1 - Board architecture

Outcome: the rule model can represent the planned game without encoding presentation or relying on Unity scene objects.

| ID | Deliverable | Status | Acceptance evidence |
| --- | --- | --- | --- |
| P1.1 | Separate piece color from special-piece kind | `DONE` | Special state survives swap and gravity tests |
| P1.2 | Return structured horizontal/vertical match groups with deduplicated cells | `DONE` | Run, T and L tests verify direction, length and overlap |
| P1.3 | Introduce deterministic RNG with serializable state | `DONE` | Same seed/state reproduces boards, pieces and refill state |
| P1.4 | Define versioned level data and stable-board snapshot formats | `DONE` | JSON round-trip, replay and legacy/future-version tests pass outside Unity |
| P1.5 | Add objectives and cell layers as explicit model concepts | `DONE` | Score, collection, multi-hit layer and deduplicated-clear tests pass |
| P1.6 | Build level validation and simulation APIs | `DONE` | Invalid, pre-matched, dead and one-move configurations receive coded results; deterministic simulation is tested |
| P1.7 | Build a minimal Unity level-authoring tool | `BLOCKED` | Tool can be coded, but acceptance requires Unity import and designer use; depends on P0.1 |

Exit criteria:

- Color, special kind, cell layer and objective state are separate concepts.
- Matches include groups and a deduplicated clear set.
- Level attempts and refills are reproducible from persisted state.
- Versioned level files and snapshots round-trip through tests.
- A Unity authoring tool can create and validate a level when Unity becomes available.

## Phase 2 - Competitive core

Outcome: ten authored levels offer real planning choices and introduce mechanics in a deliberate sequence.

| ID | Deliverable | Status |
| --- | --- | --- |
| P2.1 | Four-in-line creates horizontal/vertical line-clear specials | `DONE` |
| P2.2 | T/L creates blast special; five-in-line creates color clear | `DONE` |
| P2.3 | Define and implement every special-to-special combination | `NOT STARTED` |
| P2.4 | Implement collect-color objective | `NOT STARTED` |
| P2.5 | Implement clear-layer objective | `NOT STARTED` |
| P2.6 | Add blockers needed by the first ten levels | `NOT STARTED` |
| P2.7 | Author and validate ten tutorial-to-mastery levels | `NOT STARTED` |
| P2.8 | Add hints based on legal moves, without selecting the best move | `NOT STARTED` |

Exit criteria:

- Specials have deterministic creation, survival, placement, activation and combination rules.
- A full turn resolves chained effects once per cell and finishes before outcome evaluation.
- Ten levels can be completed, replayed and validated without code changes.
- Each level introduces or combines a named decision, not merely a larger score target.

## Phase 3 - Feel and identity

Outcome: the game is readable and satisfying at production target resolution.

| ID | Deliverable | Status |
| --- | --- | --- |
| P3.1 | Six original gem silhouettes with non-color identifiers | `NOT STARTED` |
| P3.2 | Board sockets, frame and magical-workshop background | `NOT STARTED` |
| P3.3 | Separate swap, rejection, clear, fall and landing curves | `NOT STARTED` |
| P3.4 | Selection, clear, cascade and special VFX | `NOT STARTED` |
| P3.5 | Layered SFX, music, mixer controls and optional haptics | `NOT STARTED` |
| P3.6 | Reduced-motion and color-vision-readable presentation | `NOT STARTED` |

Exit criteria:

- Every gem and special is identifiable without hue at minimum cell size.
- Accepted, rejected and locked input states are understandable without text.
- Effects remain readable during long cascades and never obscure the objective HUD.
- Audio and motion can be independently reduced or disabled.

## Phase 4 - Player journey

Outcome: players understand why they are playing, see progress and can leave and return safely.

| ID | Deliverable | Status |
| --- | --- | --- |
| P4.1 | Objective preview and guided first level | `NOT STARTED` |
| P4.2 | Responsive HUD with objective progress and chain feedback | `NOT STARTED` |
| P4.3 | Pause, settings, retry, result and next-level flows | `NOT STARTED` |
| P4.4 | Ten-level chapter map | `NOT STARTED` |
| P4.5 | Compact workshop restoration rewards | `NOT STARTED` |
| P4.6 | Versioned local progress, settings and stable-turn resume | `NOT STARTED` |
| P4.7 | Personal best and authored mastery thresholds | `NOT STARTED` |

Exit criteria:

- An uncoached player can explain the current goal, remaining moves and result.
- Progress and settings survive restart and supported upgrades.
- Retry behavior clearly distinguishes same-seed practice from a new attempt.
- Workshop rewards visibly correspond to completed levels.

## Phase 5 - Measurement and quality

Outcome: the team can identify rule regressions, broken scenes, poor levels and technical failures before broad release.

| ID | Deliverable | Status |
| --- | --- | --- |
| P5.1 | Versioned analytics event schema | `NOT STARTED` |
| P5.2 | Privacy-conscious analytics implementation with offline tolerance | `NOT STARTED` |
| P5.3 | Unity EditMode tests for data, validation and migrations | `NOT STARTED` |
| P5.4 | Unity PlayMode tests for scene wiring, input lock and restart | `NOT STARTED` |
| P5.5 | Automated Android and desktop development builds | `NOT STARTED` |
| P5.6 | Performance, memory, crash and low-end device budgets | `NOT STARTED` |
| P5.7 | First-use and level-balancing playtest rounds | `NOT STARTED` |

Exit criteria:

- Every attempt records level/version/seed and a terminal outcome.
- Analytics failure cannot block play.
- CI catches rule, serialization, scene and build failures.
- Device tests meet the agreed frame-time, load-time and stability budgets.
- Level changes cite observed data or a documented design hypothesis.

## Phase 6 - Forge experiment

Outcome: determine whether Forge is a useful strategic identity rather than an automatic bailout.

| ID | Deliverable | Status |
| --- | --- | --- |
| P6.1 | Implement charge rules and visible progress | `NOT STARTED` |
| P6.2 | Implement explicit target/orientation choice and cancel | `NOT STARTED` |
| P6.3 | Define save, objective, scoring and end-state interactions | `NOT STARTED` |
| P6.4 | Compare core and Forge variants in player sessions | `NOT STARTED` |
| P6.5 | Keep, simplify or remove Forge based on evidence | `NOT STARTED` |

Exit criteria:

- Players can explain Forge after encountering it.
- Players use it intentionally to support a plan.
- Its benefit is strategic control, not merely a guaranteed win.
- The keep/remove decision and evidence are recorded under Decisions.

## Phase 7 - Limited release

Outcome: release a stable, honest vertical slice to a controlled audience and decide whether product fit justifies expansion.

| ID | Deliverable | Status |
| --- | --- | --- |
| P7.1 | Store metadata, gameplay captures, privacy/support pages | `NOT STARTED` |
| P7.2 | Compatibility, offline, upgrade and recovery matrix | `NOT STARTED` |
| P7.3 | Signed test-track builds and staged rollout | `NOT STARTED` |
| P7.4 | Feedback triage and level-version review process | `NOT STARTED` |
| P7.5 | Product review against qualitative and telemetry baseline | `NOT STARTED` |

No numerical retention target is set before an internal baseline exists. Release success requires stable technical quality, successful onboarding, chapter completion by a meaningful subset of the recruited audience and unprompted recognition of the planning/forging identity.

## Phase 8 - Expansion decision

Status: `DEFERRED` until Phase 7 evidence exists.

Choose one path explicitly:

- Premium expansion: more authored chapters, workshop content and optional cosmetic/supporter purchases.
- Live service: backend identity, cloud saves, remote configuration, events, social systems, economy, support and a sustainable content team.
- Stop or reposition: preserve the prototype and document why further investment is not justified.

## Accepted decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-19 | Position Gemforge as tactical gem-forging match-3 | A smaller clone cannot match incumbent content/live-ops scale; deliberate control fits the name |
| 2026-09-19 | Build a ten-level vertical slice before a campaign | Maximizes learning while limiting unvalidated content production |
| 2026-09-19 | Keep monetization and live operations out of the first slice | Audience, feel and differentiation are not yet validated |
| 2026-09-19 | Treat this file as delivery source of truth | Plans and status need one updateable location |
| 2026-09-19 | Continue engine-independent Phase 1 while Phase 0 is blocked | Pure model work and tests are useful without claiming Unity validation |
| 2026-09-19 | Use special precedence Blast > Color Clear > Line Clear | Intersecting shapes produce one clear result; straight five-plus outranks straight four |
| 2026-09-19 | Blast uses a 3x3 area; matched Color Clear removes its own color | Bounded, readable prototype rules that can be tuned after playtesting |

## Open decisions

| ID | Decision needed | Needed by | Current assumption |
| --- | --- | --- | --- |
| D1 | First release platform | Before P3/P5 device budgets | Android first, desktop development build second |
| D2 | Portrait-only or responsive portrait/landscape | Before authored HUD/art | Portrait first, landscape supported where practical |
| D3 | Retry seed behavior | Before P1.4/P4.6 | Replay offers same-seed practice; retry defaults to same level seed policy |
| D4 | Workshop reward scope | Before P4.5 | Visual restoration with no stat bonuses |
| D5 | Business model | After Phase 7 | Demo plus one-time unlock is the current recommendation |

## Progress log

| Date | Change | Evidence / next step |
| --- | --- | --- |
| 2026-09-19 | Prototype created with basic match, gravity, score and moves | Commit `4bc87c7`; standalone tests passed |
| 2026-09-19 | Art, UX, replayability and competitive analyses added | Commits `9516cb2` and `a6e6fc1` |
| 2026-09-19 | Canonical phased implementation plan established | Begin P1.1 and P1.2; Unity validation remains blocked |
| 2026-09-19 | Completed P1.1-P1.5 model foundation | 5,935,658 assertions pass; next is P1.6 validation/simulation |
| 2026-09-19 | Completed P1.6 validation and deterministic simulation | 5,935,670 assertions pass; P1.7 blocked on Unity, begin P2.1 rules |
| 2026-09-19 | Completed P2.1 line-special rules and runtime flow | Preferred-cell creation, row/column activation, chain reaction and layer-hit tests pass; prototype marker added |
| 2026-09-19 | Completed P2.2 blast and color-clear rules | T/L and five-match creation, survival and activation tests pass; 5,935,697 total assertions |

## Update checklist

For every implementation commit:

1. Update task statuses and the current snapshot.
2. Add completion evidence or a precise blocker.
3. Record material product/technical decisions.
4. Append one concise progress-log entry.
5. Keep supporting docs aligned or note the conflict here.
6. Run relevant tests and record limitations honestly.
