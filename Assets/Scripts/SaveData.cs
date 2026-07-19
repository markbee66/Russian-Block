using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// Everything that outlives a single run: currency balances and owned
    /// consumables. Backed by PlayerPrefs under the same "TetrisArcade." key
    /// prefix the display settings already use.
    ///
    /// Deliberately static and free of any dependency on TetrisGame, so the
    /// shop screen and the game loop can both talk to it without one owning
    /// the other.
    /// </summary>
    public static class SaveData
    {
        const string PrefGold    = "TetrisArcade.Gold";
        const string PrefDiamond = "TetrisArcade.Diamond";

        // Inventory keys are built per item id, e.g. "TetrisArcade.Item.skip".
        const string PrefItemPrefix = "TetrisArcade.Item.";

        // ============================ CURRENCY ============================

        public static int Gold
        {
            get => PlayerPrefs.GetInt(PrefGold, 0);
            private set { PlayerPrefs.SetInt(PrefGold, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int Diamond
        {
            get => PlayerPrefs.GetInt(PrefDiamond, 0);
            private set { PlayerPrefs.SetInt(PrefDiamond, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        /// <summary>
        /// Converts a finished run's score into currency and banks it.
        /// Gold is the steady drip (always at least 1); Diamond only shows up
        /// once the player is scoring properly. Both are clamped so a single
        /// monster run cannot bankroll everything at once.
        /// </summary>
        public static void AwardForRun(int score, out int gold, out int diamond)
        {
            gold    = Mathf.Clamp(1 + score / 1500, 1, 10);
            diamond = Mathf.Clamp(score / 8000, 0, 5);

            Gold += gold;
            Diamond += diamond;
        }

        /// <summary>
        /// Spends the given amounts if the player can afford them. Returns false
        /// and touches nothing if they cannot, so callers can use it directly as
        /// the "can I buy this?" check.
        /// </summary>
        public static bool TrySpend(int gold, int diamond)
        {
            if (Gold < gold || Diamond < diamond) return false;
            Gold -= gold;
            Diamond -= diamond;
            return true;
        }

        // ============================ INVENTORY ============================

        public static int OwnedCount(string itemId) =>
            PlayerPrefs.GetInt(PrefItemPrefix + itemId, 0);

        public static void AddItem(string itemId, int count = 1)
        {
            PlayerPrefs.SetInt(PrefItemPrefix + itemId, Mathf.Max(0, OwnedCount(itemId) + count));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Consumes one of an item. Returns false if the player has none, so the
        /// caller can skip applying the effect.
        /// </summary>
        public static bool ConsumeItem(string itemId)
        {
            int owned = OwnedCount(itemId);
            if (owned <= 0) return false;
            PlayerPrefs.SetInt(PrefItemPrefix + itemId, owned - 1);
            PlayerPrefs.Save();
            return true;
        }

        // ============================ DEBUG ============================

        /// <summary>Wipes currency and inventory. Settings are left alone.</summary>
        public static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PrefGold);
            PlayerPrefs.DeleteKey(PrefDiamond);
            foreach (var item in ShopCatalog.Items)
                PlayerPrefs.DeleteKey(PrefItemPrefix + item.Id);
            PlayerPrefs.Save();
        }
    }
}
