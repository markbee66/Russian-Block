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

## Features
- 7 tetrominoes (I, O, T, S, Z, J, L) with standard colors
- 7-bag randomizer, next-piece preview
- Rotation with simple wall kicks, ghost-piece landing preview
- Line clears with classic scoring (40 / 100 / 300 / 1200 × level)
- Level speed-up every 10 lines
- Game Over + restart, pause, arcade HUD

## Project layout
- `Assets/Scripts/TetrisGame.cs` — the entire game
- `Assets/Scenes/Tetris.unity` — the playable scene

Built with Unity + Claude Code.
