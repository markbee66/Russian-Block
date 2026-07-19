namespace TetrisArcade
{
    /// <summary>
    /// The shop's stock, as plain data. Adding an item here makes it appear in
    /// the shop automatically — the only other thing needed is the code that
    /// acts on its id in TetrisGame.Shop.cs.
    /// </summary>
    public static class ShopCatalog
    {
        public sealed class Item
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Description;
            public readonly int GoldCost;
            public readonly int DiamondCost;
            public readonly int StackLimit;   // most the player may hold at once

            public Item(string id, string name, string description,
                        int goldCost, int diamondCost, int stackLimit)
            {
                Id = id;
                Name = name;
                Description = description;
                GoldCost = goldCost;
                DiamondCost = diamondCost;
                StackLimit = stackLimit;
            }

            public bool IsDiamond => DiamondCost > 0;

            /// <summary>Price as shown on the button, e.g. "5 G" or "3 D".</summary>
            public string PriceLabel => IsDiamond ? DiamondCost + " D" : GoldCost + " G";
        }

        // Item ids — referenced from the game loop, so they live as constants
        // rather than loose strings.
        public const string SkipPiece    = "skip";
        public const string SlowStart    = "slow";
        public const string HoldSlot     = "hold";
        public const string UndoLock     = "undo";

        public static readonly Item[] Items =
        {
            new Item(SkipPiece, "SKIP PIECE",
                     "Throw away the current piece", 5, 0, 3),
            new Item(SlowStart, "SLOW START",
                     "Half gravity for the first 60s", 8, 0, 1),
            new Item(HoldSlot, "HOLD SLOT",
                     "Classic hold slot for the run", 0, 3, 1),
            new Item(UndoLock, "UNDO LOCK",
                     "Rewind the last locked piece", 0, 4, 1),
        };

        public static Item Find(string id)
        {
            foreach (var item in Items)
                if (item.Id == id) return item;
            return null;
        }
    }
}
