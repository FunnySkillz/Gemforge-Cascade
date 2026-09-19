# Competitive Readiness Analysis

Analysis date: 2026-09-19. This combines a source review of the current repository with public information about leading match-3 games. Gemforge Cascade has not yet been run in the Unity Editor or tested with players, so scores for feel, usability and performance are provisional.

## Executive conclusion

Gemforge Cascade is a functional prototype, not yet a market-ready game. It proves board generation, swapping, matching, cascades, scoring, moves and win/loss rules. It does not yet have the authored content, strategic systems, production presentation, progression, telemetry or release infrastructure required to compete for players' time.

The realistic opportunity is not to reproduce Candy Crush Saga or Royal Match at smaller scale. Those products combine mature match-3 rules with huge level libraries, multiple objectives and blockers, social competition, events, rewards and continuous operation. Royal Match currently presents castle-area progression and a broad event portfolio, while its match system includes four distinct board-created power-ups. Candy Crush exposes several objective types and uses live level data such as pass and reshuffle rates to revise content. [Royal Match](https://www.royalmatch.com/), [Royal Match power-ups](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/6-creating-and-using-the-power-ups/), [Candy Crush modes](https://community.king.com/en/candy-crush-saga/discussion/312857/candy-crush-saga-game-mode-guide), [Associated Press on King's level process](https://apnews.com/article/547254aaa06bf026df5b41458ac62dcc).

Gemforge should occupy a narrower position: a polished, tactical match-3 about deliberately forging and combining gems. Its promise should be planning and satisfying craft, with a clean presentation and respect for the player's attention. The Forge mechanic, authored puzzle boards and deterministic special-piece rules can make that position credible. Graphics support the promise; they cannot substitute for it.

## Current readiness scorecard

Scores use a 0-5 scale: 0 absent, 1 placeholder, 2 prototype, 3 credible vertical slice, 4 soft-launch ready, 5 mature live product. The target column is the minimum recommended before a limited external release, not parity with a market leader.

| Capability | Current | Limited-release target | Evidence and missing work |
| --- | ---: | ---: | --- |
| Core match rules | 2.5 | 4 | Basic matches, gravity and legal moves exist; specials, combinations, blockers and edge-case Unity tests do not |
| Strategic identity | 0.5 | 3.5 | Forge exists only as a proposal; current play is generic score-target matching |
| Game feel | 1 | 4 | Generated circles, one timing value, shrink clears, no authored VFX, audio or haptics |
| Level design | 0.5 | 4 | One random 8x8 score level; no authored layouts, difficulty curve or level editor |
| Objectives and variety | 0.5 | 3.5 | Score target only; no collection, clear-layer, delivery or mixed goals |
| Progression | 0 | 3 | No level sequence, map/workshop, unlocks, stars, best scores or saved progress |
| UX and onboarding | 1 | 4 | Functional labels and restart; no tutorial, objective preview, pause/settings or accessible mobile flow |
| Visual identity | 0.5 | 4 | Name and palette direction exist; no original production assets or finished screen composition |
| Audio identity | 0 | 3.5 | No music, sound effects, mixer or mute controls |
| Accessibility | 0.5 | 3.5 | Colors are distinct, but identical silhouettes rely on hue; no reduced motion, remapping or assist options |
| Content pipeline | 0 | 4 | No level data format, editor, validator, solver/simulator or remote content path |
| Analytics and balancing | 0 | 4 | No events, funnels, attempt outcomes, seed capture, dashboards or experiment configuration |
| Persistence | 0 | 3.5 | No save versioning, settings persistence, stable-turn resume or migration tests |
| Technical release readiness | 0.5 | 4 | Pure rule tests exist; Unity compilation, device builds, performance, crash handling and store setup are unverified |
| Live operations/social | 0 | 2 | Not required for the first product proof; needed later only if pursuing service-game economics |
| Monetization | 0 | Decision required | No business model has been selected; design this after the audience and product promise are validated |

Overall, this is roughly a 1/5 product prototype. That is not a criticism of the implementation; it identifies the distance between proving a mechanic and shipping a competitive game.

## What leading games actually compete on

### 1. Reliable moment-to-moment pleasure

The player's swipe must feel immediate and every board response must be understandable. Competitive games differentiate accepted swaps, rejected swaps, ordinary clears, special creation, combinations, cascades, objective hits, near-completion and victory through coordinated motion, sound and visual hierarchy.

Gemforge currently moves pieces correctly but uses one duration for swap, return, fall and clear. There are no anticipation frames, landing response, particles, local score labels, cascade callouts, sound or haptics. This makes technically correct play feel unfinished.

Required response:

- Give swap, return, fall, landing and clear separate timing curves.
- Add original gem silhouettes, sockets, selection outlines and special states.
- Add layered sound whose pitch and intensity rise during readable cascades.
- Add restrained particles and local objective/score feedback.
- Test on real 60 Hz mobile hardware and under interrupted input.

### 2. Decisions deeper than finding any match

Royal Match's documented board-created power-ups include row/column clears, targeted clears, area blasts and color clears. Their value comes from creation patterns, positioning and combinations, not simply spectacle. [Royal Match power-ups](https://dreamgames.helpshift.com/hc/en/3-royal-match/faq/6-creating-and-using-the-power-ups/).

Gemforge currently offers no reason to prefer a match of four, five, T or L beyond clearing more pieces. The player has little medium-term intent.

Required response:

- Implement deterministic line, blast and color-clear specials.
- Preserve the created special instead of clearing it with the source match.
- Define every special-to-special combination.
- Make objectives reward position and planning, not just total score.
- Prototype Forge Charge only after normal special creation is readable and fun.

The signature Forge action should offer controlled agency inside a random board. That is the strongest current concept because it matches the title and changes decisions. It must be constrained enough that it does not trivialize levels.

### 3. Authored challenge and pacing

Current leaders vary goals, obstacles and difficulty. Candy Crush publicly documents score, jelly, ingredient, order, mixed and path-like objectives. King also monitors pass and reshuffle rates and revises existing levels; the important lesson is the feedback loop between authored design and player behavior, rather than the specific use of AI. [Candy Crush modes](https://community.king.com/en/candy-crush-saga/discussion/312857/candy-crush-saga-game-mode-guide), [Associated Press](https://apnews.com/article/547254aaa06bf026df5b41458ac62dcc).

Gemforge generates a random board and asks for 1,000 points in 25 moves. There is no authored opening, difficulty model or guarantee that two attempts present comparable challenge.

Required response:

- Store levels as versioned ScriptableObjects or JSON with dimensions, initial cells, goals, moves, available colors, blockers and seed rules.
- Build an Editor level tool with paint, validate, simulate and play buttons.
- Add a deterministic random seed to every attempt and analytics record.
- Create automated simulation for impossible, trivial and high-variance levels.
- Hand-author the first 20-30 levels around one learning goal at a time.
- Tune difficulty as a wave, with recovery levels after demanding ones.

Do not begin with hundreds of levels. Twenty excellent levels and a usable authoring pipeline are more valuable than a large spreadsheet of score targets.

### 4. A reason to continue after one board

Royal Match connects levels to rooms/areas and operates many competitive and cooperative events, including weekly contests and team tournaments. This illustrates how the puzzle feeds visible progress and recurring goals. [Royal Match areas and events](https://www.royalmatch.com/), [Royal Match events help](https://dreamgames.helpshift.com/hc/en/3-royal-match/section/23-events-and-tournaments/).

Gemforge currently restarts the same rule set. It has no level map, workshop, collection, unlock, personal best or daily challenge.

Required response for an initial release:

- Add a 20-30 level chapter with visible progression.
- Let completed levels restore and customize a compact magical workshop.
- Unlock new gem cuts, forge tools or cosmetic board details at clear milestones.
- Add stars or mastery goals only when their thresholds are authored and visible.
- Save progress locally with versioning and migration tests.
- Add personal bests and a seeded daily puzzle after the campaign loop works.

The workshop should express achievement, not become an unrelated idle economy. Each reward needs a visible connection to what the player did.

### 5. Continuous learning from real play

Without telemetry, difficulty and frustration are guesses. Automated rule tests answer whether the game is valid; they do not answer whether a board is enjoyable or fair.

Instrument at minimum:

| Event | Essential fields |
| --- | --- |
| Level start | level/version, attempt, seed, starting boosters, device class |
| Move | turn, swap cells/types, legal, resulting groups, specials created/used |
| Cascade | depth, cells cleared, objectives affected, duration |
| Level end | win/loss/quit, score, moves left, duration, objective progress |
| UX | tutorial step, hint shown/used, pause, restart, settings changed |
| Technical | load time, frame-rate bucket, exception, low-memory interruption |

Use event schemas with versions and validate them in development. Review level start-to-end funnels, first-attempt understanding, fail state, quit point, pass rate, attempts to pass, remaining moves on wins, reshuffle rate and booster use. Segment by level version and seed so balancing changes can be evaluated rather than mixed together.

Analytics must be privacy-conscious, documented and proportionate. The game should remain playable if analytics are unavailable.

### 6. Operational depth, when the core deserves it

The genre's largest products run many simultaneous events and competitions. That is a mature-stage capability, not the starting requirement for Gemforge. Building events before the core is distinctive would multiply interfaces and balancing work without solving the primary problem.

Later service-game requirements would include:

- Server time, player identity, cloud save and conflict handling.
- Remote configuration and content delivery with rollback.
- Event definitions, segmentation, reward mail and support tools.
- Teams, fair leaderboard cohorts, moderation and anti-cheat controls.
- Economy sources/sinks, offer configuration, purchase restoration and receipts.
- Customer support, privacy requests and incident response.
- A content calendar and the staff capacity to sustain it.

This is a separate business commitment. Decide explicitly whether Gemforge is a premium/low-pressure indie game or a free-to-play live service before implementing lives, currencies or offers.

## Recommended market position

### Product promise

**Forge powerful gems, plan compact chain reactions, and rebuild a magical workshop one handcrafted puzzle at a time.**

Three pillars:

1. **Plan:** deterministic special rules and authored boards reward foresight.
2. **Forge:** controlled gem transformation provides agency and a recognizable identity.
3. **Restore:** each solved puzzle visibly improves the player's workshop.

The tone should be calm, tactile and precise rather than loud, sugary or crowded. That contrast is a useful differentiator only if the board still delivers strong feedback.

### Audience

Primary candidate: puzzle players who understand match-3 immediately but want more control, less interface clutter and shorter handcrafted challenges. Secondary candidate: players drawn to gemcraft, workshop customization and mastery goals.

This audience hypothesis needs testing. It should guide recruitment for interviews and prototypes, not be treated as established fact.

### Business-model choices

| Model | Fit | Tradeoff |
| --- | --- | --- |
| Premium or demo + unlock | Strong fit for handcrafted, low-pressure positioning | Requires clear value and enough launch content; lower ceiling |
| Free with cosmetic/supporter purchases | Preserves fair puzzle design | Cosmetics need production value and a motivated audience |
| Free-to-play lives/boosters | Familiar category economics | Requires economy, backend, content scale, live ops and careful trust design |
| Ad-supported | Simple in theory | Interruptions conflict with a calm premium feel; privacy and SDK burden |

The recommended initial test is a free polished demo followed by a one-time chapter/full-game unlock. Do not add monetization pressure while basic enjoyment and audience fit remain unknown.

## Architecture gaps behind the product gaps

The current scene creates the camera, UI, event system, textures, pieces and managers at runtime. This was efficient for a prototype, but it makes art iteration, localization, prefabs, accessibility and scene validation harder.

Before scaling content:

- Replace runtime-built presentation with authored prefabs and scenes using serialized references.
- Continue the new separation of `PieceColor`, `SpecialKind` and cell layers into blockers and special activation rules.
- Build special creation and resolution on the new structured match groups and deduplicated clear set.
- Define a turn-resolution command/event stream so visuals, audio, analytics and tests observe the same outcome.
- Move level rules and objectives behind explicit interfaces/data definitions.
- Add deterministic RNG state, save snapshots at stable turn boundaries and save migrations.
- Add object pools for pieces/effects after profiling; avoid premature pooling everywhere.
- Add Unity EditMode tests for serialization and level validation, and PlayMode tests for scene wiring, restart and resolution locks.
- Add build automation for at least Android and desktop development builds.
- Add localization-ready string tables and avoid constructing all UI copy in code.

Google Play expects stable, responsive apps and recommends test tracks; Android vitals tracks crash, ANR and other quality signals that can affect discoverability. Technical readiness therefore includes device testing, performance budgets and crash reporting, not just correct match logic. [Google Play testing guidance](https://support.google.com/googleplay/android-developer/answer/15191715), [Android vitals](https://support.google.com/googleplay/android-developer/answer/9844486).

## Staged plan

### Phase 0: Establish reality

Goal: prove that the current project compiles and plays on target hardware.

- Install the matching Unity LTS version and import the project cleanly.
- Fix compile/import warnings and create Android plus desktop development builds.
- Test portrait and landscape, safe areas, mouse, touch, interruption and restart.
- Capture baseline video and frame timing.
- Run 5 uncoached first-use sessions and record confusion points.

Gate: no blocking errors; every tester can identify the goal and complete a valid swap.

### Phase 1: Competitive vertical slice

Goal: make ten minutes of Gemforge feel like a real game.

- Produce six original gem sprites, a board, basic environment and coherent UI.
- Add separate motion curves, particles, sound, settings and reduced motion.
- Implement line, blast and color-clear specials plus combinations.
- Add two objective families: collect gems and clear board layers.
- Build the level format/editor and author ten levels.
- Add tutorial, objective preview, pause, result flow and local save.
- Add versioned analytics and deterministic attempt seeds.

Gate: new testers understand specials and objectives; observed input errors are rare; the team can author, validate and ship a new level without code changes.

### Phase 2: Prove differentiation

Goal: determine whether Forge improves the game.

- Prototype Forge Charge with explicit targeting, cancel and activation rules.
- Compare versions with normal specials only and specials plus Forge.
- Measure comprehension, deliberate use, perceived control, enjoyment and voluntary replay.
- Remove or simplify Forge if it becomes a guaranteed bailout rather than a planning tool.
- Expand to a 20-30 level chapter with a workshop progression shell.

Gate: players can explain Forge after encountering it, use it intentionally and prefer it for reasons related to strategy rather than only easier wins.

### Phase 3: Limited external release

Goal: test audience and product fit, not maximize revenue.

- Add onboarding polish, settings, privacy disclosures, support link and save recovery.
- Complete compatibility, low-end device, offline, upgrade and store-track testing.
- Prepare accurate screenshots/video showing actual gameplay.
- Recruit a defined audience and monitor qualitative feedback plus level/technical telemetry.
- Update difficulty and onboarding by level version with documented hypotheses.

Gate: technical quality is stable, early levels teach successfully, a meaningful subset voluntarily completes the chapter and feedback identifies Gemforge's planning/forging identity without prompting. Numerical targets should be set after internal baseline data exists.

### Phase 4: Choose the business

Only after the limited release should the project choose between a finite premium expansion and ongoing service operation. A premium path adds chapters and workshop content. A service path requires the backend, economy, live-ops and organization listed above. Do not drift into the service path one feature at a time.

## What not to build yet

- Teams, chat or global leaderboards.
- Multiple currencies, energy timers or a complex booster shop.
- A season pass or rotating event framework.
- Hundreds of lightly varied procedural levels.
- Expensive narrative cinematics.
- A large workshop meta with no connection to puzzle mastery.
- Acquisition campaigns before the first-session experience is validated.

These features can increase an established game's reach or return rate. They will not rescue generic board play or an unclear product promise.

## Immediate backlog

Ordered by dependency and learning value:

1. Compile and run Main in Unity; make target-device builds.
2. Capture and review actual gameplay at phone resolution.
3. Refactor board state for color, special kind, cell layer and structured matches.
4. Define versioned level data and deterministic seeds.
5. Build a minimal level editor/validator.
6. Implement a four-match line special with deterministic placement.
7. Replace generated circles with six readable gem silhouettes.
8. Split animation timing and add first-pass sound/VFX.
9. Add objective progress, pause/settings and an authored tutorial.
10. Author ten levels using collect and clear-layer goals.
11. Add local saves, level/version analytics and PlayMode tests.
12. Conduct the vertical-slice playtest before expanding scope.

The first six items create the foundation for everything after them. The first meaningful competitive review should happen after item 10, with real footage and player sessions rather than feature-count comparison.
