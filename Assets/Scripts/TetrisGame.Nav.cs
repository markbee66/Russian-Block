using System.Collections.Generic;
using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// Single source of truth for which menu screen (if any) is on top, and
    /// whether a run is in progress. Replaces the old scattered flags
    /// (inMenu / _inSettings / _inShop / _inSkills / showSettings / _adminOpen /
    /// paused) with one navigation stack plus one run-state bool.
    ///
    /// MenuScreen does NOT include Title or GameOver:
    /// - Title is the root shown whenever !_runActive (no separate flag needed).
    /// - GameOver is run state, not navigation state — UseUndo() un-sets the
    ///   existing `gameOver` bool to resurrect a run, which would be a much
    ///   uglier operation if GameOver were a stack entry.
    /// </summary>
    public partial class TetrisGame
    {
        // Named MenuScreen, not Screen: a nested `enum Screen` shadows
        // UnityEngine.Screen for every Screen.width/height call in this class.
        enum MenuScreen { Settings, Shop, Skills, PauseMenu, Admin, Confirm }

        readonly List<MenuScreen> _nav = new List<MenuScreen>();   // stack; last = topmost
        bool _runActive;                                   // a run exists on the board

        string _confirmText;
        System.Action _confirmAction;

        bool ScreenOpen => _nav.Count > 0;
        MenuScreen Top => _nav[_nav.Count - 1];
        // Convenience predicate for callers that only need the yes/no answer.
        // Update() itself uses ordered gates instead (see the Update() method),
        // because HandleItemKeys() must still run while gameOver is true.
        bool GameFrozen => !_runActive || ScreenOpen || gameOver || _targeting;

        // Screens that black out what's behind them. Admin and Confirm are
        // deliberately non-opaque: Admin is a small parked debug box, and a
        // confirmation prompt should let the player see what they're confirming.
        static bool IsOpaque(MenuScreen s) => s != MenuScreen.Admin && s != MenuScreen.Confirm;

        void Push(MenuScreen s)
        {
            if (ScreenOpen && Top == s) return;
            _nav.Add(s);
            OnEnterScreen(s);
        }

        void Pop()
        {
            if (ScreenOpen) _nav.RemoveAt(_nav.Count - 1);
            _resOpen = false;
        }

        void PopAll()
        {
            _nav.Clear();
            _resOpen = false;
        }

        // Per-screen reset run every time that screen becomes the top of the stack.
        void OnEnterScreen(MenuScreen s)
        {
            switch (s)
            {
                case MenuScreen.Settings: _resOpen = false; break;
                case MenuScreen.Shop:     _shopMessage = ""; break;
                case MenuScreen.Skills:   _skillMessage = ""; break;
            }
        }

        void StartRun()
        {
            PopAll();
            _runActive = true;
            NewGame();
            Redraw();
        }

        // Same body as StartRun — the one and only restart implementation, so
        // the R key / pause-menu RESTART / game-over RETRY can never diverge.
        void RestartRun()
        {
            PopAll();
            _runActive = true;
            NewGame();
            Redraw();
        }

        void GoTitle()
        {
            PopAll();
            _runActive = false;
            NewGame(false);   // reset the board without charging for passives
            Redraw();
        }

        void AskConfirm(string text, System.Action onYes)
        {
            _confirmText = text;
            _confirmAction = onYes;
            Push(MenuScreen.Confirm);
        }
    }
}
