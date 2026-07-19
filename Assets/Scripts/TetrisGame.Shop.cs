using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TetrisArcade
{
    /// <summary>
    /// The shop screen and everything the purchased consumables do during a run.
    /// Kept in its own partial so TetrisGame.cs kept its original shape — the
    /// game loop only calls a handful of hooks defined here.
    /// </summary>
    public partial class TetrisGame
    {
        // ============================ SHOP SCREEN ============================

        bool _inShop;                  // shop page open (only reachable from the title screen)
        string _shopMessage = "";      // transient "bought" / "can't afford" line
        float _shopMessageUntil;

        void ShopSay(string msg)
        {
            _shopMessage = msg;
            _shopMessageUntil = Time.unscaledTime + 2f;
        }

        // Drawn in place of the title menu. Reuses the pause-menu styles so the
        // shop matches the rest of the UI without defining its own skin.
        void DrawShopScreen()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuClose.fontSize = fs;
            _small.fontSize = Mathf.RoundToInt(fs * 0.8f);
            _small.normal.textColor = new Color(0.55f, 0.6f, 0.7f);

            var items = ShopCatalog.Items;
            float pad = fs * 0.9f, rowH = fs * 3.2f, gap = fs * 0.5f;
            float titleH = fs * 2.4f, balanceH = fs * 1.6f, msgH = fs * 1.4f, closeH = fs * 2.4f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 26f, 380f, Screen.width * 0.94f));
            float panelH = Mathf.Round(titleH + balanceH + gap
                                       + items.Length * (rowH + gap)
                                       + msgH + closeH + pad * 2f);
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);

            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.6f);
            _menuTitle.normal.textColor = accent;
            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "SHOP", _menuTitle);
            y += titleH;

            _stat.fontSize = fs;
            _stat.normal.textColor = Color.white;
            GUI.Label(new Rect(px, y, panelW, balanceH),
                      SaveData.Gold + " GOLD    ·    " + SaveData.Diamond + " DIAMOND", _stat);
            y += balanceH + gap;

            // One row per catalogue entry: name + blurb on the left, buy button right.
            float btnW = Mathf.Round(fs * 5.5f);
            foreach (var item in items)
            {
                int owned = SaveData.OwnedCount(item.Id);
                bool full = owned >= item.StackLimit;
                bool affordable = SaveData.Gold >= item.GoldCost && SaveData.Diamond >= item.DiamondCost;

                _label.fontSize = fs;
                _label.normal.textColor = full ? new Color(0.45f, 0.5f, 0.6f) : Color.white;
                GUI.Label(new Rect(innerX, y, innerW - btnW - gap, rowH * 0.52f), item.Name, _label);
                GUI.Label(new Rect(innerX, y + rowH * 0.48f, innerW - btnW - gap, rowH * 0.46f),
                          item.Description + "   (" + owned + "/" + item.StackLimit + ")", _small);

                GUI.enabled = !full && affordable;
                if (GUI.Button(new Rect(innerX + innerW - btnW, y + rowH * 0.15f, btnW, rowH * 0.7f),
                               full ? "MAX" : item.PriceLabel, _menuClose))
                {
                    if (SaveData.TrySpend(item.GoldCost, item.DiamondCost))
                    {
                        SaveData.AddItem(item.Id);
                        ShopSay("Bought " + item.Name);
                    }
                    else ShopSay("Not enough currency");
                }
                GUI.enabled = true;

                y += rowH + gap;
            }

            if (Time.unscaledTime < _shopMessageUntil)
            {
                _smallC.fontSize = Mathf.RoundToInt(fs * 0.9f);
                _smallC.normal.textColor = accent;
                GUI.Label(new Rect(px, y, panelW, msgH), _shopMessage, _smallC);
            }

            if (GUI.Button(new Rect(innerX, py + panelH - closeH - pad, innerW, closeH), "BACK", _menuClose))
                _inShop = false;
        }

        // ============================ GAME OVER ============================

        // Replaces the old two-line "GAME OVER / press R" text. Reports what the
        // run earned and gives the player a way out that is not the pause menu.
        void DrawGameOverPanel()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuClose.fontSize = fs;

            float pad = fs * 0.9f, btnH = fs * 2.6f, gap = fs * 0.7f;
            float titleH = fs * 2.8f, lineH = fs * 1.5f, rewardH = fs * 1.7f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 20f, 340f, Screen.width * 0.9f));
            float panelH = Mathf.Round(titleH + lineH + rewardH + gap
                                       + 3f * (btnH + gap) + pad * 2f);
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);

            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.8f);
            _menuTitle.normal.textColor = accent;
            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "GAME OVER", _menuTitle);
            y += titleH;

            _stat.fontSize = fs;
            _stat.normal.textColor = Color.white;
            GUI.Label(new Rect(px, y, panelW, lineH), "SCORE  " + score, _stat);
            y += lineH;

            _smallC.fontSize = Mathf.RoundToInt(fs * 0.95f);
            _smallC.normal.textColor = accent;
            string reward = "+" + _paidGold + " GOLD"
                          + (_paidDiamond > 0 ? "    +" + _paidDiamond + " DIAMOND" : "");
            GUI.Label(new Rect(px, y, panelW, rewardH), reward, _smallC);
            y += rewardH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "RETRY", _menuClose))
            { NewGame(); Redraw(); }
            y += btnH + gap;

            // Restocking is the main thing you want after dying, so the shop is
            // reachable here instead of only from the title screen.
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SHOP", _menuClose))
            {
                inMenu = true;
                _inShop = true;
                _shopMessage = "";
                NewGame(false);
                Redraw();
            }
            y += btnH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "MAIN MENU", _menuClose))
            {
                showSettings = false; _inSettings = false; _resOpen = false;
                inMenu = true;
                NewGame(false);   // reset the board without charging for passives
                Redraw();
            }
        }

        // ============================ RUN STATE ============================

        bool _slowStart;            // Slow Start active for this run
        bool _extraPreview;         // Extra Preview active for this run
        float _runTime;             // seconds since the run began (drives Slow Start)

        bool _holdUnlocked;         // Hold Slot paid for during this run
        int _holdType = -1;         // piece parked in the hold slot, -1 = empty
        bool _holdUsedThisPiece;    // standard rule: one swap per piece

        // Undo snapshot, captured immediately before each lock.
        bool _undoReady;
        int[,] _undoBoard;
        int _undoScore, _undoLines, _undoLevel, _undoType, _undoRot, _undoX, _undoY, _undoNext;
        float _undoFall;

        /// <summary>
        /// Called from NewGame. Spends the passive items, and clears whatever
        /// the previous run left behind. With spend false it only clears — used
        /// when the board is reset on the way back to the title screen.
        /// </summary>
        void BeginRunItems(bool spend)
        {
            _runTime = 0f;
            _holdUnlocked = false;
            _holdType = -1;
            _holdUsedThisPiece = false;
            _undoReady = false;

            // Passives are paid for up front, and only if one is actually owned.
            _slowStart = spend && SaveData.ConsumeItem(ShopCatalog.SlowStart);
            _extraPreview = spend && SaveData.ConsumeItem(ShopCatalog.ExtraPreview);
        }

        /// <summary>Gravity interval for this frame, with Slow Start folded in.</summary>
        float CurrentFallInterval() =>
            _slowStart && _runTime < 60f ? fallInterval * 2f : fallInterval;

        /// <summary>
        /// Banks the run's reward. Guarded so a game-over frame that repeats
        /// cannot pay out twice.
        /// </summary>
        bool _rewardPaid;
        int _paidGold, _paidDiamond;   // kept so an Undo can claw the payout back

        void AwardRunCurrency()
        {
            if (_rewardPaid) return;
            _rewardPaid = true;
            SaveData.AwardForRun(score, out _paidGold, out _paidDiamond);
            Toast("+" + _paidGold + " Gold" + (_paidDiamond > 0 ? "   +" + _paidDiamond + " Diamond" : ""));
        }

        // ============================ ITEM INPUT ============================

        /// <summary>
        /// Called once per frame from Update. Undo is allowed after a top-out —
        /// that is the whole point of it — but the other items are not.
        /// </summary>
        void HandleItemKeys()
        {
            if (!gameOver) _runTime += Time.deltaTime;

            ReadItemInput(out bool skip, out bool hold, out bool undo);
            if (undo) { UseUndo(); return; }
            if (gameOver) return;
            if (skip) UseSkipPiece();
            if (hold) UseHold();
        }

        void ReadItemInput(out bool skip, out bool hold, out bool undo)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) { skip = hold = undo = false; return; }
            skip = k.fKey.wasPressedThisFrame;
            hold = k.cKey.wasPressedThisFrame;
            undo = k.uKey.wasPressedThisFrame;
#else
            skip = Input.GetKeyDown(KeyCode.F);
            hold = Input.GetKeyDown(KeyCode.C);
            undo = Input.GetKeyDown(KeyCode.U);
#endif
        }

        // Drops the current piece on the floor of the shop, not the board: it is
        // simply discarded and the next one spawns. The bag is untouched, so
        // piece order carries on as normal.
        void UseSkipPiece()
        {
            if (!SaveData.ConsumeItem(ShopCatalog.SkipPiece)) { Toast("No Skip Piece left"); return; }
            SpawnPiece();
            Toast("Skipped");
        }

        void UseHold()
        {
            // First press of the run is what actually pays for the slot.
            if (!_holdUnlocked)
            {
                if (!SaveData.ConsumeItem(ShopCatalog.HoldSlot)) { Toast("No Hold Slot"); return; }
                _holdUnlocked = true;
            }
            if (_holdUsedThisPiece) { Toast("Already held"); return; }

            if (_holdType < 0)
            {
                _holdType = curType;
                SpawnPiece();
            }
            else
            {
                int swap = _holdType;
                _holdType = curType;
                curType = swap;
                curRot = 0;
                curX = 3;
                int maxOy = 0;
                var s = SHAPES[curType][0];
                for (int i = 0; i < 4; i++) maxOy = Mathf.Max(maxOy, s[i * 2 + 1]);
                curY = (Height - 1) - maxOy;
                if (!CanPlace(curType, curRot, curX, curY)) gameOver = true;
                gravityTimer = 0; lockTimer = 0;
            }
            _holdUsedThisPiece = true;
        }

        // ============================ EXTRA PREVIEW ============================

        // A second 4x4 panel under the NEXT panel, created the first time a run
        // actually has the item so the objects cost nothing otherwise.
        SpriteRenderer[,] _prev2;
        SpriteRenderer _prev2BG;

        Vector3 Preview2Origin => new Vector3(previewOrigin.x, previewOrigin.y - 5f, 0);

        void EnsurePreview2()
        {
            if (_prev2 != null) return;
            _prev2 = new SpriteRenderer[4, 4];
            Vector3 o = Preview2Origin;
            _prev2BG = MakeSR("Preview2BG", transform, new Vector3(o.x + 1.5f, o.y + 1.5f, 0),
                              new Vector3(4.6f, 4.6f, 1), wellColor, 1);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    _prev2[x, y] = MakeSR($"Prev2_{x}_{y}", transform, new Vector3(o.x + x, o.y + y, 0),
                                          new Vector3(0.85f, 0.85f, 1), panelEmpty, 2);
        }

        /// <summary>Keeps the second panel glued to the first when the layout changes.</summary>
        void LayoutPreview2()
        {
            if (_prev2 == null) return;
            Vector3 o = Preview2Origin;
            _prev2BG.transform.position = new Vector3(o.x + 1.5f, o.y + 1.5f, 0);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    _prev2[x, y].transform.position = new Vector3(o.x + x, o.y + y, 0);
        }

        /// <summary>Called at the end of Redraw. Draws the piece after NEXT.</summary>
        void RedrawPreview2()
        {
            bool show = _extraPreview && !inMenu && !gameOver;
            if (!show)
            {
                if (_prev2 != null)
                {
                    _prev2BG.enabled = false;
                    foreach (var sr in _prev2) sr.enabled = false;
                }
                return;
            }

            EnsurePreview2();
            _prev2BG.enabled = true;
            foreach (var sr in _prev2) sr.enabled = true;

            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    _prev2[x, y].color = panelEmpty;

            int after = PeekAfterNext();
            if (after < 0) return;
            var s = SHAPES[after][0];
            for (int i = 0; i < 4; i++)
            {
                int x = s[i * 2], y = s[i * 2 + 1];
                if (x >= 0 && x < 4 && y >= 0 && y < 4) _prev2[x, y].color = COLORS[after];
            }
        }

        /// <summary>
        /// The piece queued behind nextType, without consuming it. Refills the
        /// bag first so the answer is never "unknown" on a bag boundary.
        /// </summary>
        int PeekAfterNext()
        {
            EnsureBag();
            return bag.Count > 0 ? bag[bag.Count - 1] : -1;
        }

        /// <summary>Captures the pre-lock state so Undo can rewind to it.</summary>
        void SnapshotForUndo()
        {
            _undoBoard ??= new int[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    _undoBoard[x, y] = board[x, y];

            _undoScore = score; _undoLines = lines; _undoLevel = level; _undoFall = fallInterval;
            _undoType = curType; _undoRot = curRot; _undoX = curX; _undoY = curY;
            _undoNext = nextType;
            _undoReady = true;
        }

        void UseUndo()
        {
            if (!_undoReady) { Toast("Nothing to undo"); return; }
            if (!SaveData.ConsumeItem(ShopCatalog.UndoLock)) { Toast("No Undo Lock"); return; }

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    board[x, y] = _undoBoard[x, y];

            score = _undoScore; lines = _undoLines; level = _undoLevel; fallInterval = _undoFall;
            curType = _undoType; curRot = _undoRot; curX = _undoX; curY = _undoY;
            nextType = _undoNext;
            gravityTimer = 0; lockTimer = 0;

            // Rewinding out of a top-out puts the run back in play. The payout has
            // already been banked, so take it back and re-arm it for the real end;
            // otherwise the run would pay out twice.
            if (gameOver)
            {
                gameOver = false;
                SaveData.TrySpend(_paidGold, _paidDiamond);
                _paidGold = _paidDiamond = 0;
                _rewardPaid = false;
            }

            _undoReady = false;   // one rewind per snapshot
            Toast("Undone");
        }
    }
}
