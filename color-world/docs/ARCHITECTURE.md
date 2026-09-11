# Native architecture

`scenes/main.tscn` creates `scripts/main.gd`.

- `ColorSystem`: quantized RGB simplex, shared exposure/white admixture, sRGB-to-OKLab, score/stars/reward functions.
- `GameModel`: data-driven levels, persistent attempts, area gates, high-score/paint state, inventory/economy transactions, streak/endgame state.
- `SaveSystem`: save schema, envelope hash, flush/check/rename, previous-slot recovery, reset preserving settings.
- `WorldSystem`: world-space areas, merged vertex-colored meshes, close-area detail queue, far landmark proxies, free camera, selection, tool reveal and ambient life.
- `GameAudio`: pooled one-shot players, aligned musical layers, ambient bed, music/SFX preferences and haptic calls.
- `main.gd`: native screen composition and UI state transitions. Shop, helpers, settings and camera orchestration still share this scene controller; they are not separate production modules.
- `paint.gdshader`: neutral material to saved player's color via height mask. Vertex tint retains object detail.

## Data contracts

Ratios are integers in tenths of a percent, totaling **1000**. Displayed values divide by ten. Targets never need arbitrary floating-point input.

World JSON has 100 ordered areas and 1500 ordered levels. Each group stores explicit mesh primitives and transforms. A level stores its target, brightness, saturation, difficulty, group ID, camera and reward table. Primitive geometry is created from those actual records at runtime.

The color mapping uses per-level brightness `b` and saturation `s`: `c_i=b*(1-s)+b*s*r_i/max(r)`. The same mapping applies to target and player. This is an exposure/value mapping, not strict equal physical luminance. It covers bright, dark, muted and pastel subsets while retaining an exact reachable target. The additional per-level saturation is not a player control.

## Economy and durable mutations

A successful match computes an immutable candidate state, updates the best color only if the score rises, and pays only the increase above that level's claimed reward. Auto Mix records assistance in the active attempt and caps the reward at 35. Reopening an assisted best starts from neutral, avoiding reuse of its saved solution as an unassisted attempt.

Attempts, purchases and helper uses are synchronously committed. A failed save does not apply the candidate to the in-memory model. Progress is committed before the paint reward sequence. A pending reveal is recoverable on launch.

## Asset origin

All world composition data and synthesized audio were authored for this project. DejaVu Sans is bundled with its license. No third-party stock models or downloaded music are used. Generated modular assets require a further production art review.
