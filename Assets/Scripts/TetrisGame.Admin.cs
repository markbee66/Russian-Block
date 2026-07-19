using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TetrisArcade
{
    /// <summary>
    /// A testing panel for handing out currency and items without grinding runs.
    /// Toggled with F12, from the title screen or mid-game.
    ///
    /// Kept in its own file with no hooks into the game rules, so it can be
    /// deleted wholesale before a real release — the only traces elsewhere are
    /// the two calls in TetrisGame.cs and SaveData.Grant.
    /// </summary>
    public partial class TetrisGame
    {
        bool _adminOpen;

        void HandleAdminHotkey()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.f12Key.wasPressedThisFrame) _adminOpen = !_adminOpen;
#else
            if (Input.GetKeyDown(KeyCode.F12)) _adminOpen = !_adminOpen;
#endif
        }

        void DrawAdminPanel()
        {
            int fs = Mathf.Max(11, Mathf.RoundToInt(Screen.height * 0.021f * _uiScale));
            _menuClose.fontSize = fs;

            float pad = fs * 0.8f, btnH = fs * 2.2f, gap = fs * 0.5f;
            float titleH = fs * 2f, lineH = fs * 1.6f;
            float panelW = Mathf.Round(Mathf.Clamp(fs * 20f, 300f, Screen.width * 0.5f));
            float panelH = Mathf.Round(titleH + lineH + 5f * (btnH + gap) + pad * 2f);
            float px = 12f, py = 12f;   // parked top-left, clear of the MENU button
            float innerX = px + pad, innerW = panelW - pad * 2f;

            GUI.Box(new Rect(px, py, panelW, panelH), GUIContent.none, _menuBox);

            _menuTitle.fontSize = Mathf.RoundToInt(fs * 1.3f);
            _menuTitle.normal.textColor = new Color(1f, 0.75f, 0.2f);
            float y = py + pad;
            GUI.Label(new Rect(px, y, panelW, titleH), "ADMIN  (F12)", _menuTitle);
            y += titleH;

            _stat.fontSize = fs;
            _stat.normal.textColor = Color.white;
            GUI.Label(new Rect(px, y, panelW, lineH),
                      SaveData.Gold + " G    " + SaveData.Diamond + " D", _stat);
            y += lineH + gap;

            float halfW = (innerW - gap) * 0.5f;
            if (GUI.Button(new Rect(innerX, y, halfW, btnH), "+50 GOLD", _menuClose))
                SaveData.Grant(50, 0);
            if (GUI.Button(new Rect(innerX + halfW + gap, y, halfW, btnH), "+50 DIA", _menuClose))
                SaveData.Grant(0, 50);
            y += btnH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "FILL EVERY ITEM", _menuClose))
            {
                foreach (var item in ShopCatalog.Items)
                {
                    int missing = item.StackLimit - SaveData.OwnedCount(item.Id);
                    if (missing > 0) SaveData.AddItem(item.Id, missing);
                }
            }
            y += btnH + gap;

            // Handy for checking the payout formula without playing a full run.
            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "SET SCORE 10000", _menuClose))
            {
                score = 10000;
                Redraw();
            }
            y += btnH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "WIPE PROGRESS", _menuClose))
                SaveData.ResetProgress();
            y += btnH + gap;

            if (GUI.Button(new Rect(innerX, y, innerW, btnH), "CLOSE", _menuClose))
                _adminOpen = false;
        }
    }
}
