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

Every spawn rolls for a mutation. The rolls are **ordered from rarest to most
common, and stop at the first hit** — so at most one mutation applies per piece.

| Order | Mutation | Chance | Effective rate |
|-------|----------|--------|----------------|
| 1 | Inoperable | 1/20 | 5.00% |
| 2 | Bomb | 1/15 | 6.33% |
| 3 | Odd shape | 1/12 | 7.36% |
| — | Normal piece | — | 81.31% |

The piece type is still drawn from the 7-bag first; the mutation then transforms
that draw, so bag fairness is preserved. The next-piece preview always shows the
mutated result.

### Bomb (1x1)
- Single cell, standard tetromino colors, one of three kinds at equal 1/3 odds:
  - **Box bomb** — clears the 3x3 area centred on the bomb
  - **I bomb** — clears the bomb's entire column
  - **Line bomb** — clears the bomb's entire row
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

**Branch A — Gold**
1. Block Remove — 10 Gold
2. Line Remove — 20 Gold *(requires node 1)*

**Branch B — Diamond**
1. Revive — 10 Diamond

## Skills

| Skill | Effect | Cooldown | Uses | Type | Key |
|-------|--------|----------|------|------|-----|
| Block Remove | Click any single block to destroy it | 60s | Unlimited | Active | Q |
| Line Remove | Destroys the topmost row containing any block | 120s | Unlimited | Active | E |
| Revive | On game over, clears the whole board and play continues | — | Once per run | Passive | — |

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
| Extra Preview | Shows one additional upcoming piece for the whole run | 12 Gold | 1 | Passive |

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
- `Assets/Scripts/TetrisGame.Admin.cs` — F12 testing panel, safe to delete
- `Assets/Scripts/SaveData.cs` — currency and inventory persistence
- `Assets/Scripts/ShopCatalog.cs` — shop stock, as data
- `Assets/Scenes/Tetris.unity` — the playable scene

Built with Unity + Claude Code.