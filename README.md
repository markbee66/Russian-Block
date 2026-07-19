# Russian-Block · 經典街機俄羅斯方塊 (Unity)

A classic arcade-style Tetris built in Unity 6 (URP 2D). Fully code-driven —
the whole game lives in one script that generates its own sprites, board,
preview and HUD at runtime.

## Play
1. Open the project in Unity (6000.0.x).
2. Open the scene `Assets/Scenes/Tetris.unity`.
3. Press **▶ Play**.

## Controls
| Key | Action |
|-----|--------|
| ← → | Move left / right (hold to auto-repeat) |
| ↑ / X / W | Rotate clockwise |
| Z / Ctrl | Rotate counter-clockwise |
| ↓ | Soft drop |
| Space | Hard drop |
| P | Pause |
| R | Restart |
| Q | Skill: Block Remove (requires unlock) |
| E | Skill: Line Remove (requires unlock) |
| F | Item: Skip Piece (requires purchase) |
| C | Item: Hold (requires purchase) |
| U | Item: Undo Lock (requires purchase) |

## Features
- 7 tetrominoes (I, O, T, S, Z, J, L) with standard colors
- 7-bag randomizer, next-piece preview
- Rotation with simple wall kicks, ghost-piece landing preview
- Line clears with classic scoring (40 / 100 / 300 / 1200 × level)
- Level speed-up every 10 lines
- Game Over + restart, pause, arcade HUD

## Mutated Pieces

Every spawn rolls once against cumulative bands, so at most one mutation applies
and each rate below is the true rate rather than a nominal one.

Rates are set by the Diamond skill tree, so they change as the player levels it:

| Mutation | Level 0 | Level 1 | Level 2 | Level 3 |
|----------|---------|---------|---------|---------|
| Inoperable | 5.00% | 4.33% | 3.67% | 3.00% |
| Bomb | 3.00% | 4.10% | 5.20% | 6.33% |
| Odd shape | 7.36% | 5.91% | 4.45% | 3.00% |

Normal pieces take whatever is left — 84.6% at level 0 across the board, 87.7%
with every ward maxed and bombs left alone.

The piece type is still drawn from the 7-bag first; the mutation then transforms
that draw, so bag fairness is preserved. The NEXT panel shows the mutated result.

### Bomb (1x1)
- Single cell in one of three kinds at equal 1/3 odds, each its own colour so the
  blast is predictable before it lands:
  - **Box bomb** (pink) — clears the 3x3 area centred on the bomb
  - **Column bomb** (amber) — clears the bomb's entire column
  - **Row bomb** (yellow) — clears the bomb's entire row
- The kind is part of the piece type, not a field, so a bomb waiting in the
  preview cannot change what an already-falling bomb will do.
- Detonates the moment the piece locks.
- Destroyed cells collapse (everything above falls down), same as a line clear.
- Scores 10 points per destroyed cell. Does **not** count toward the line counter,
  so it never advances the level on its own.
- Normal line-clear detection still runs after the collapse — a bomb can set up a
  regular line clear, which scores and counts as usual.

### Odd shape
- Solid rectangles outside the standard set: **2x3** and **1x5**, 50/50.
- Rotate and wall-kick with the same rules as normal pieces.

### Inoperable
- Uses a standard shape (I, O, T, S, Z, J, L) but rendered in a single distinct
  colour so it reads as "locked" at a glance.
- Move and rotate are disabled. **Hard drop (Space) is the only input accepted**,
  so the player can always skip it rather than waiting out the gravity timer.

## Currency

Awarded on game over, based on final score. Quitting mid-run awards nothing.

| Currency | Formula | Range |
|----------|---------|-------|
| Gold | `1 + floor(score / 600)` | 1–10 |
| Diamond | `floor(score / 3000)` | 0–5 |

Both are clamped to their range, so a single huge run cannot bankroll everything —
Gold caps out around 5400 points and Diamond around 15000. Balances persist in
`PlayerPrefs` alongside the existing settings, and are shown in the shop and on
the game-over screen.

## Skill Tree

Entered from the **main menu**. Unlocks are permanent across runs and stored in
`PlayerPrefs`. Two independent branches; within a branch, nodes unlock in order.

**Branch A — Gold.** One-off unlocks, bought in order.

1. Block Remove — 10 Gold
2. Line Remove — 20 Gold *(requires node 1)*
3. Revive — 40 Gold *(requires node 2)*

**Branch B — Diamond.** Mutation tuning, levelled 0–3. Diamond drops are scarce,
so levels stay cheap: **2 / 3 / 5** Diamond, 30 to max all three nodes.

1. Bomb Affinity — bombs from 3% up to 6.33%
2. Inoperable Ward — inoperable pieces from 5% down to 3%
3. Odd Shape Ward — odd shapes from 7.36% down to 3%

Wards floor at 3% rather than reaching zero, so mutations never disappear
entirely.

## Skills

| Skill | Effect | Cooldown | Uses | Type | Key |
|-------|--------|----------|------|------|-----|
| Block Remove | Click any single block to destroy it | 60s | Unlimited | Active | Q |
| Line Remove | Destroys the topmost row containing any block | 120s | Unlimited | Active | E |
| Revive | On game over, clears the whole board and play continues | — | Once per run | Passive | — |

Unlocked skills show their state in the HUD, Revive included: READY until it
fires, USED afterwards.

- Both active skills collapse the blocks above the removed cells, so they leave no
  new holes.
- **Block Remove** pauses gravity while the player picks a target; pressing Q again
  or Esc cancels without spending the cooldown.
- Cooldowns start ready at the beginning of a run and are shown in the HUD.
- Skill removals award no score and do not count toward the line counter.

## Shop

Entered from the **main menu**, separate from the skill tree. Everything here is a
single-run consumable; the skill tree remains the home of permanent unlocks.

**Gold — steady, run-shaping**

| Item | Effect | Price | Stack | Type |
|------|--------|-------|-------|------|
| Skip Piece | Discards the current piece and pulls the next one | 5 Gold | 3 | Active (F) |
| Slow Start | Halves gravity for the first 60 seconds of the run | 8 Gold | 1 | Passive |

**Diamond — scarce, run-saving**

| Item | Effect | Price | Stack | Type |
|------|--------|-------|-------|------|
| Hold Slot | Enables the classic hold slot for the run, used any number of times | 3 Diamond | 1 | Active (C) |
| Undo Lock | Rewinds the last locked piece, board and score included | 4 Diamond | 1 | Active (U) |

- Owned counts persist in `PlayerPrefs` and survive between runs.
- Passive items are spent automatically when a run starts, and only if one is
  owned. Active items are spent on first use, not at run start.
- Skipping does not reroll the 7-bag — the discarded piece is simply consumed, so
  bag order is untouched.
- Hold follows the standard rule: one swap per piece, no holding twice in a row.
- Undo restores the board snapshot taken immediately before the last lock, so any
  line clears and score from that lock are rolled back too.
- Consumables are independent of the skill tree; the two systems share only the
  currency balances.

## Project layout
- `Assets/Scripts/TetrisGame.cs` — board, pieces, input, HUD and menus
- `Assets/Scripts/TetrisGame.Shop.cs` — shop screen and consumable behaviour
- `Assets/Scripts/TetrisGame.Skills.cs` — skill tree screen and the three skills
- `Assets/Scripts/TetrisGame.Mutations.cs` — mutation rolling, bombs, rate tables
- `Assets/Scripts/TetrisGame.Hud.cs` — toast, item/skill readouts, targeting
- `Assets/Scripts/TetrisGame.Admin.cs` — F9 testing panel, safe to delete
- `Assets/Scripts/SaveData.cs` — currency and inventory persistence
- `Assets/Scripts/ShopCatalog.cs` — shop stock, as data
- `Assets/Scripts/SkillTree.cs` — skill nodes and unlock persistence
- `Assets/Scenes/Tetris.unity` — the playable scene

Built with Unity + Claude Code.