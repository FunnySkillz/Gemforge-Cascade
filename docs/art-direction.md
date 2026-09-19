# Graphics and Audio Direction

## Creative identity

Gemforge Cascade should feel like working at a magical gemsmith's bench: precise cuts, bright minerals, controlled sparks and tools with weight. The board is the work surface. Large matches forge something useful, giving the name a gameplay meaning.

Use graphite and neutral steel for supporting surfaces, with ruby, cyan, emerald, gold, violet and coral as playable accents. Keep backgrounds quiet and relatively desaturated. Avoid making the entire screen one dark blue or brown family. Reserve bright light and motion for the player's actions.

The intended style is readable, stylized 2D faceted sprites, not realistic transparent gemstones with complicated refraction. All artwork and sound must be original or have documented usage rights.

## Current gaps

| Area | Current implementation | Proposed improvement |
| --- | --- | --- |
| Gem identity | One generated circle tinted six colors | Six different silhouettes, facets and subtle symbols |
| Board | Pieces on a flat background | Recessed cell sockets and a restrained forge-bench frame |
| Selection | Scale increases by 14% | Crisp outline and small lift; preserve neighboring silhouettes |
| Movement | Shared 0.2-second interpolation | Separate swap, return, fall and landing timing |
| Match clear | Shrink, then disappear | Brief highlight, fracture/shards, score feedback and sound |
| Cascade | Score multiplier exists but is not displayed | Visible chain count, escalating chime and bounded effect intensity |
| Special pieces | Absent | Persistent, recognizable silhouette overlay when introduced |
| Sound and haptics | Absent | Quiet tactile swap/land sounds and distinct reward cues |
| Environment | No authored art | One original workbench background, subordinate to the board |

These are code observations, not a screenshot-based evaluation.

## Starter asset brief

Produce the six base gems as a cohesive set before commissioning a large environment.

| Type | Silhouette | Secondary identifier |
| --- | --- | --- |
| Red | Diamond | Central vertical facet |
| Blue | Hexagon | Three horizontal facet lines |
| Green | Square with clipped corners | Inset square |
| Yellow | Triangle | Small triangular inset |
| Purple | Tall octagon | Cross-shaped facet |
| Orange | Circle | Radial facet pattern |

Shape and internal marks must distinguish pieces without color. Verify this in grayscale and with color-vision simulations; simulation is supplementary to player feedback.

Initial deliverables:

- Six transparent 256x256 PNG sprites, centered with consistent optical weight and padding.
- A selection outline that works across all six silhouettes.
- A cell socket tile and a board frame that supports rectangular boards.
- Three small shard sprites, a short sparkle sprite sequence and a landing ring.
- One original workbench background suitable for portrait and landscape crops.
- UI icons for pause, sound, music, replay and next level, from one coherent licensed or original set.
- Short audio cues for select, accepted swap, rejected swap, clear, cascade, special creation, special activation, win and loss.

Keep editable source files and an asset/license inventory. In Unity, use a shared pixels-per-unit convention, consistent pivots and an atlas. Test sprite padding and filtering at the smallest intended cell size. Do not bake text or UI controls into background art.

## Feedback choreography

Proposed tuning ranges, to be tested in the actual game:

| Event | Timing starting point | Feedback |
| --- | --- | --- |
| Select | Immediate | Outline, slight lift, quiet click |
| Swap | 120-180 ms | Directional movement with gentle easing |
| Invalid swap | 100-150 ms return | Soft return cue, no alarming flash |
| Clear | 100-160 ms | Highlight, shrink/fracture, small local burst |
| Fall | 140-280 ms, distance dependent | Accelerate downward and settle cleanly |
| Cascade gap | 50-100 ms | Enough separation to read the next match |
| Win | 600-1,000 ms, skippable | Forge ignition, result reveal, clear next action |

Separate these settings instead of using one duration for all actions. Motion must never extend beyond the period when the board is locked. Cap cascade pitch, particle counts and brightness; a long chain should remain readable. Display points near the match briefly, then remove them before the next decision.

Ordinary matches deserve a small reward; specials and clever combinations deserve a stronger one. Constant large explosions would obscure that distinction. Pool frequently spawned particles once profiling shows the need.

## Completion checks

- Every gem is identifiable at the smallest supported display size without relying on hue.
- Selected pieces, specials and objectives have distinct visual treatments.
- Effects do not hide cells needed for the next move or spill over the HUD.
- New pieces appear from behind the board's upper edge, not over the score display.
- Reduced-motion mode removes shake, large flashes and excess particles while retaining meaningful state feedback.
- Music, effects and haptics have separate controls; haptics are optional.
- Capture portrait and landscape gameplay on target hardware before calling the art pass complete.
