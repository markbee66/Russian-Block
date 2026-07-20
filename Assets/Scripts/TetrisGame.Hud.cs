using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// In-game feedback: the toast line, the item/skill readouts, and the
    /// targeting highlight.
    ///
    /// Toast() existed before but was only ever drawn inside the settings panel,
    /// so every message raised during play was invisible. DrawToast fixes that.
    /// </summary>
    public partial class TetrisGame
    {
        // ============================ IN-GAME HUD ============================

        // The world-anchored readouts drawn behind every menu: NEXT, the score
        // block and the controls hint. Extracted from OnGUI so the screen
        // dispatcher can call it as one unit.
        void DrawGameHud()
        {
            if (portrait)
            {
                // NEXT panel (top-left) and stats (top-right)
                WLabel(previewOrigin.x, previewOrigin.y + 3.9f, "NEXT", _label, 200);
                WLabel(6.8f, 23.6f, "SCORE " + score, _stat, 340);
                WLabel(6.8f, 22.5f, "LEVEL " + level, _stat, 340);
                WLabel(6.8f, 21.4f, "LINES " + lines, _stat, 340);

                // Controls (below the board)
                WLabel(4.5f, -2.3f, "← →  Move    ↑ / X  Rotate    ↓  Soft", _smallC, 900);
                WLabel(4.5f, -3.4f, "Space  Hard drop  ·  P / Esc  Menu  ·  R Restart", _smallC, 900);
            }
            else
            {
                WLabel(4.5f, 20.6f, "T E T R I S", _title, 400);
                WLabel(previewOrigin.x, previewOrigin.y + 4.2f, "NEXT", _label, 200);

                WLabel(11f, 12.3f, "SCORE", _label, 200);
                WLabel(11f, 11.5f, score.ToString(), _value, 200);
                WLabel(11f, 10.0f, "LEVEL", _label, 200);
                WLabel(11f, 9.2f, level.ToString(), _value, 200);
                WLabel(11f, 7.7f, "LINES", _label, 200);
                WLabel(11f, 6.9f, lines.ToString(), _value, 200);

                WLabel(11f, 4.6f, "← →  Move", _small, 220);
                WLabel(11f, 4.0f, "↑ / X  Rotate", _small, 220);
                WLabel(11f, 3.4f, "↓  Soft drop", _small, 220);
                WLabel(11f, 2.8f, "Space  Hard drop", _small, 220);
                WLabel(11f, 2.2f, "P / Esc  Menu · R Restart", _small, 220);
            }
        }

        // ============================ TOAST ============================

        // Centred just under the board, so it reads without covering the stack.
        void DrawToast()
        {
            if (Time.realtimeSinceStartup - _appliedAt >= 2.5f || _appliedMsg.Length == 0) return;

            int fs = Mathf.Max(13, Mathf.RoundToInt(Screen.height * 0.028f * _uiScale));
            _stat.fontSize = fs;
            _stat.normal.textColor = accent;

            float w = Screen.width * 0.8f, h = fs * 2f;
            float x = (Screen.width - w) * 0.5f, y = Screen.height * 0.82f;

            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(x, y, w, h), _whiteTex);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, w, h), _appliedMsg, _stat);
        }

        // ============================ ITEM / SKILL READOUT ============================

        // Bottom-left list of what the player is actually carrying this run, with
        // the key for each. Without this the consumables are invisible.
        void DrawItemHud()
        {
            int fs = Mathf.Max(11, Mathf.RoundToInt(Screen.height * 0.02f * _uiScale));
            _small.fontSize = fs;

            // Build the lines first so the panel can be sized to fit.
            var lines = new System.Collections.Generic.List<string>();
            var dim = new System.Collections.Generic.List<bool>();

            int skips = SaveData.OwnedCount(ShopCatalog.SkipPiece);
            if (skips > 0) { lines.Add("F  Skip Piece  x" + skips); dim.Add(false); }

            if (_holdUnlocked || SaveData.OwnedCount(ShopCatalog.HoldSlot) > 0)
            {
                string state = _holdUnlocked
                    ? (_holdType >= 0 ? "holding" : "empty")
                    : "x" + SaveData.OwnedCount(ShopCatalog.HoldSlot);
                lines.Add("C  Hold  " + state);
                dim.Add(_holdUsedThisPiece);
            }

            if (SaveData.OwnedCount(ShopCatalog.UndoLock) > 0)
            {
                lines.Add("U  Undo Lock  x" + SaveData.OwnedCount(ShopCatalog.UndoLock));
                dim.Add(!_undoReady);
            }

            if (_slowStart && _runTime < 60f)
            {
                lines.Add("Slow Start  " + Mathf.CeilToInt(60f - _runTime) + "s");
                dim.Add(false);
            }

            if (lines.Count == 0) return;

            float lineH = fs * 1.5f;
            float x = 12f, y = Screen.height - 12f - lines.Count * lineH;

            for (int i = 0; i < lines.Count; i++)
            {
                _small.normal.textColor = dim[i]
                    ? new Color(0.45f, 0.5f, 0.6f)
                    : new Color(0.75f, 0.82f, 0.9f);
                GUI.Label(new Rect(x, y + i * lineH, fs * 14f, lineH), lines[i], _small);
            }
        }

        // ============================ TARGETING ============================

        int _hoverX = -1, _hoverY = -1;

        /// <summary>Tracks which cell the mouse is over while Block Remove is armed.</summary>
        void UpdateHoverCell(Vector2 screenPos)
        {
            if (cam == null) { _hoverX = _hoverY = -1; return; }
            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            _hoverX = Mathf.RoundToInt(world.x);
            _hoverY = Mathf.RoundToInt(world.y);
        }

        /// <summary>
        /// Called at the end of Redraw. Tints the cell under the cursor so the
        /// player can see the skill is waiting for a click, and which block it
        /// would take.
        /// </summary>
        void ApplyTargetHighlight()
        {
            if (!_targeting) return;
            if (_hoverX < 0 || _hoverX >= Width || _hoverY < 0 || _hoverY >= Height) return;

            bool occupied = board[_hoverX, _hoverY] >= 0;
            cells[_hoverX, _hoverY].color = occupied
                ? Color.Lerp(COLORS[board[_hoverX, _hoverY]], Color.white, 0.6f)
                : new Color(0.35f, 0.12f, 0.12f);   // dull red: nothing to remove here
        }

        // A banner making the targeting mode obvious, since the run is frozen.
        void DrawTargetingBanner()
        {
            if (!_targeting) return;

            int fs = Mathf.Max(13, Mathf.RoundToInt(Screen.height * 0.03f * _uiScale));
            _stat.fontSize = fs;
            _stat.normal.textColor = accent;

            float w = Screen.width, h = fs * 2.4f;
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, w, h), _whiteTex);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 0, w, h),
                      "BLOCK REMOVE — click a block   ·   Q or Esc to cancel", _stat);
        }
    }
}
