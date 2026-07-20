using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TetrisArcade
{
    /// <summary>
    /// The skill tree screen and the three unlockable skills. Separate from the
    /// shop partial because the two systems share nothing but the currency:
    /// skills are permanent unlocks on cooldowns, shop items are consumables.
    /// </summary>
    public partial class TetrisGame
    {
        // ============================ SKILL TREE SCREEN ============================

        // Skill tree page is a MenuScreen entry on _nav now.
        string _skillMessage = "";
        float _skillMessageUntil;

        void SkillSay(string msg)
        {
            _skillMessage = msg;
            _skillMessageUntil = Time.unscaledTime + 2f;
        }

        void DrawSkillTreeScreen()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.82f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _whiteTex);
            GUI.color = Color.white;

            int fs = Mathf.Max(12, Mathf.RoundToInt(Screen.height * 0.026f * _uiScale));
            _menuClose.fontSize = fs;
            _small.fontSize = Mathf.RoundToInt(fs * 0.8f);

            int rows = SkillTree.GoldBranch.Length + SkillTree.DiamondBranch.Length;
            float pad = fs * 0.9f, rowH = fs * 3.2f, gap = fs * 0.5f;
            float titleH = fs * 2.4f, balanceH = fs * 1.6f, headH = fs * 1.5f;
            float msgH = fs * 1.4f, closeH = fs * 2.4f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 26f, 380f, Screen.width * 0.94f));
            float panelH = Mathf.Round(titleH + balanceH + gap + 2f * headH
                                       + rows * (rowH + gap) + msgH + closeH + pad * 2f);
            float px = Mathf.Round((Screen.width - panelW) * 0.5f);
            float py = Mathf.Round((Screen.height - panelH) * 0.5f);
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);

            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.6f);
            _menuTitle.normal.textColor = accent;
            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "SKILL TREE", _menuTitle);
            y += titleH;

            _stat.fontSize = fs;
            _stat.normal.textColor = Color.white;
            GUI.Label(new Rect(px, y, panelW, balanceH),
                      SaveData.Gold + " GOLD    ·    " + SaveData.Diamond + " DIAMOND", _stat);
            y += balanceH + gap;

            y = DrawBranch("BRANCH A  ·  GOLD", SkillTree.GoldBranch,
                           innerX, y, innerW, fs, rowH, gap, headH);
            y = DrawLevelledBranch("BRANCH B  ·  DIAMOND  ·  MUTATIONS", SkillTree.DiamondBranch,
                                   innerX, y, innerW, fs, rowH, gap, headH);

            if (Time.unscaledTime < _skillMessageUntil)
            {
                _smallC.fontSize = Mathf.RoundToInt(fs * 0.9f);
                _smallC.normal.textColor = accent;
                GUI.Label(new Rect(px, y, panelW, msgH), _skillMessage, _smallC);
            }

            if (GUI.Button(new Rect(innerX, py + panelH - closeH - pad, innerW, closeH), "BACK", _menuClose))
                Pop();
        }

        // One branch heading plus a row per node. Returns the next free y.
        float DrawBranch(string heading, SkillTree.Node[] nodes,
                         float x, float y, float w, int fs, float rowH, float gap, float headH)
        {
            _label.fontSize = Mathf.RoundToInt(fs * 0.9f);
            _label.normal.textColor = new Color(0.6f, 0.75f, 0.85f);
            GUI.Label(new Rect(x, y, w, headH), heading, _label);
            y += headH;

            float btnW = Mathf.Round(fs * 5.5f);
            foreach (var node in nodes)
            {
                bool unlocked = SkillTree.IsUnlocked(node.Id);
                bool ready = SkillTree.PrerequisiteMet(node);
                bool affordable = SaveData.Gold >= node.GoldCost && SaveData.Diamond >= node.DiamondCost;

                _label.fontSize = fs;
                _label.normal.textColor = unlocked ? new Color(0.45f, 0.85f, 0.5f)
                                        : ready ? Color.white
                                                : new Color(0.4f, 0.44f, 0.52f);
                GUI.Label(new Rect(x, y, w - btnW - gap, rowH * 0.52f), node.Name, _label);

                _small.normal.textColor = new Color(0.55f, 0.6f, 0.7f);
                string sub = ready ? node.Description
                                   : "Locked — unlock the previous skill first";
                GUI.Label(new Rect(x, y + rowH * 0.48f, w - btnW - gap, rowH * 0.46f), sub, _small);

                GUI.enabled = !unlocked && ready && affordable;
                string btn = unlocked ? "OWNED" : !ready ? "LOCKED" : node.PriceLabel;
                if (GUI.Button(new Rect(x + w - btnW, y + rowH * 0.15f, btnW, rowH * 0.7f), btn, _menuClose))
                {
                    if (SaveData.TrySpend(node.GoldCost, node.DiamondCost))
                    {
                        SkillTree.Unlock(node.Id);
                        SkillSay("Unlocked " + node.Name);
                    }
                    else SkillSay("Not enough currency");
                }
                GUI.enabled = true;

                y += rowH + gap;
            }
            return y;
        }

        // A levelled branch: each row shows the rate it is on now, what the next
        // level buys, and pips for progress. Buying shows the real numbers rather
        // than "level 2", so the player can see whether it is worth the Diamond.
        float DrawLevelledBranch(string heading, SkillTree.Node[] nodes,
                                 float x, float y, float w, int fs, float rowH, float gap, float headH)
        {
            _label.fontSize = Mathf.RoundToInt(fs * 0.9f);
            _label.normal.textColor = new Color(0.6f, 0.75f, 0.85f);
            GUI.Label(new Rect(x, y, w, headH), heading, _label);
            y += headH;

            float btnW = Mathf.Round(fs * 5.5f);
            foreach (var node in nodes)
            {
                int lv = SkillTree.Level(node.Id);
                int cost = SkillTree.NextLevelCost(node.Id);
                bool maxed = cost < 0;
                bool affordable = !maxed && SaveData.Diamond >= cost;
                var table = MutationRates.TableFor(node.Id);

                _label.fontSize = fs;
                _label.normal.textColor = maxed ? new Color(0.45f, 0.85f, 0.5f) : Color.white;
                GUI.Label(new Rect(x, y, w - btnW - gap, rowH * 0.52f),
                          node.Name + "   Lv " + lv + "/" + SkillTree.MaxLevel, _label);

                _small.normal.textColor = new Color(0.55f, 0.6f, 0.7f);
                string sub = node.Description + "   ·   now " + MutationRates.Percent(table[lv])
                           + (maxed ? "  (max)" : "  →  " + MutationRates.Percent(table[lv + 1]));
                GUI.Label(new Rect(x, y + rowH * 0.48f, w - btnW - gap, rowH * 0.46f), sub, _small);

                GUI.enabled = !maxed && affordable;
                if (GUI.Button(new Rect(x + w - btnW, y + rowH * 0.15f, btnW, rowH * 0.7f),
                               maxed ? "MAX" : cost + " D", _menuClose))
                {
                    if (SaveData.TrySpend(0, cost))
                    {
                        SkillTree.LevelUp(node.Id);
                        SkillSay(node.Name + " Lv " + SkillTree.Level(node.Id));
                    }
                    else SkillSay("Not enough Diamond");
                }
                GUI.enabled = true;

                y += rowH + gap;
            }
            return y;
        }

        // ============================ RUN STATE ============================

        const float BlockRemoveCooldown = 60f;
        const float LineRemoveCooldown = 120f;

        float _blockRemoveCd, _lineRemoveCd;
        bool _targeting;        // Block Remove is waiting for the player to click a cell
        bool _reviveUsed;       // Revive is once per run

        /// <summary>Called from NewGame. Skills start the run ready to use.</summary>
        void BeginRunSkills()
        {
            _blockRemoveCd = 0f;
            _lineRemoveCd = 0f;
            _targeting = false;
            _reviveUsed = false;
        }

        void HandleSkillKeys()
        {
            float dt = Time.deltaTime;
            if (_blockRemoveCd > 0f) _blockRemoveCd = Mathf.Max(0f, _blockRemoveCd - dt);
            if (_lineRemoveCd > 0f) _lineRemoveCd = Mathf.Max(0f, _lineRemoveCd - dt);

            ReadSkillInput(out bool blockKey, out bool lineKey, out bool click, out Vector2 mouse);

            if (_targeting)
            {
                UpdateHoverCell(mouse);
                // Q again cancels, without spending the cooldown.
                if (blockKey) { _targeting = false; Toast("Cancelled"); return; }
                if (click) ResolveBlockRemove(mouse);
                return;
            }

            if (blockKey) StartBlockRemove();
            if (lineKey) UseLineRemove();
        }

        void ReadSkillInput(out bool blockKey, out bool lineKey, out bool click, out Vector2 mouse)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            var m = Mouse.current;
            blockKey = k != null && k.qKey.wasPressedThisFrame;
            lineKey  = k != null && k.eKey.wasPressedThisFrame;
            click    = m != null && m.leftButton.wasPressedThisFrame;
            mouse    = m != null ? m.position.ReadValue() : Vector2.zero;
#else
            blockKey = Input.GetKeyDown(KeyCode.Q);
            lineKey  = Input.GetKeyDown(KeyCode.E);
            click    = Input.GetMouseButtonDown(0);
            mouse    = Input.mousePosition;
#endif
        }

        // ============================ BLOCK REMOVE ============================

        void StartBlockRemove()
        {
            if (!SkillTree.IsUnlocked(SkillTree.BlockRemove)) { Toast("Block Remove locked"); return; }
            if (_blockRemoveCd > 0f) { Toast("Block Remove  " + Mathf.CeilToInt(_blockRemoveCd) + "s"); return; }
            _targeting = true;
            Toast("Pick a block  ·  Q or Esc cancels");
        }

        // Turns the click into a board cell and destroys it if it holds a block.
        void ResolveBlockRemove(Vector2 screenPos)
        {
            if (cam == null) return;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            int bx = Mathf.RoundToInt(world.x);
            int by = Mathf.RoundToInt(world.y);

            // Clicking off the board backs out rather than leaving the run frozen.
            if (bx < 0 || bx >= Width || by < 0 || by >= Height)
            {
                _targeting = false;
                Toast("Cancelled");
                return;
            }
            // An empty cell is a misclick, not a cancel: stay armed so the player
            // can just click again. The cooldown is untouched either way.
            if (board[bx, by] < 0) { Toast("Nothing there — pick a block"); return; }

            board[bx, by] = -1;
            CollapseColumn(bx, by);

            _targeting = false;
            _blockRemoveCd = BlockRemoveCooldown;
            Redraw();
        }

        /// <summary>Drops everything above a hole down one, so no new gap is left.</summary>
        void CollapseColumn(int x, int fromY)
        {
            for (int y = fromY; y < Height - 1; y++)
                board[x, y] = board[x, y + 1];
            board[x, Height - 1] = -1;
        }

        // ============================ LINE REMOVE ============================

        void UseLineRemove()
        {
            if (!SkillTree.IsUnlocked(SkillTree.LineRemove)) { Toast("Line Remove locked"); return; }
            if (_lineRemoveCd > 0f) { Toast("Line Remove  " + Mathf.CeilToInt(_lineRemoveCd) + "s"); return; }

            int target = TopmostOccupiedRow();
            if (target < 0) { Toast("Board is empty"); return; }

            // Same shift the normal line clear uses, but scores nothing and does
            // not count toward the line counter.
            for (int y = target; y < Height - 1; y++)
                for (int x = 0; x < Width; x++)
                    board[x, y] = board[x, y + 1];
            for (int x = 0; x < Width; x++) board[x, Height - 1] = -1;

            _lineRemoveCd = LineRemoveCooldown;
            Toast("Line removed");
            Redraw();
        }

        int TopmostOccupiedRow()
        {
            for (int y = Height - 1; y >= 0; y--)
                for (int x = 0; x < Width; x++)
                    if (board[x, y] >= 0) return y;
            return -1;
        }

        // ============================ REVIVE ============================

        /// <summary>
        /// Called when a spawn fails. Wipes the board and lets play continue, once
        /// per run. Returns false if the run really is over.
        /// </summary>
        bool TryRevive()
        {
            if (_reviveUsed) return false;
            if (!SkillTree.IsUnlocked(SkillTree.Revive)) return false;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    board[x, y] = -1;

            _reviveUsed = true;
            Toast("REVIVED");
            return true;
        }

        // ============================ HUD ============================

        // A compact skill readout under the MENU button, only for unlocked skills.
        void DrawSkillHud()
        {
            bool hasBlock = SkillTree.IsUnlocked(SkillTree.BlockRemove);
            bool hasLine = SkillTree.IsUnlocked(SkillTree.LineRemove);
            bool hasRevive = SkillTree.IsUnlocked(SkillTree.Revive);
            if (!hasBlock && !hasLine && !hasRevive) return;

            int fs = Mathf.Max(11, Mathf.RoundToInt(Screen.height * 0.02f * _uiScale));
            _small.fontSize = fs;

            float bh = Mathf.Max(40f, Screen.height * 0.07f);
            float y = 12f + bh + 8f;   // clear of the MENU button
            float w = fs * 12f, x = Screen.width - w - 12f;

            if (hasBlock)
            {
                _small.normal.textColor = _blockRemoveCd > 0f
                    ? new Color(0.5f, 0.55f, 0.65f) : accent;
                GUI.Label(new Rect(x, y, w, fs * 1.4f),
                          "Q  Block Remove  " + (_blockRemoveCd > 0f
                              ? Mathf.CeilToInt(_blockRemoveCd) + "s" : "READY"), _small);
                y += fs * 1.4f;
            }
            if (hasLine)
            {
                _small.normal.textColor = _lineRemoveCd > 0f
                    ? new Color(0.5f, 0.55f, 0.65f) : accent;
                GUI.Label(new Rect(x, y, w, fs * 1.4f),
                          "E  Line Remove  " + (_lineRemoveCd > 0f
                              ? Mathf.CeilToInt(_lineRemoveCd) + "s" : "READY"), _small);
                y += fs * 1.4f;
            }
            if (hasRevive)
            {
                // Passive, so there is no key — but the player still needs to know
                // whether the safety net is still there.
                _small.normal.textColor = _reviveUsed ? new Color(0.5f, 0.55f, 0.65f) : accent;
                GUI.Label(new Rect(x, y, w, fs * 1.4f),
                          "Revive  " + (_reviveUsed ? "USED" : "READY"), _small);
            }
        }
    }
}
