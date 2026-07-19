using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// The permanent upgrade tree.
    ///
    /// Branch A (Gold) holds the active skills, bought once and kept forever.
    /// Branch B (Diamond) tunes the mutation rates and is levelled: each node
    /// goes up to MaxLevel, getting dearer each step.
    /// </summary>
    public static class SkillTree
    {
        const string PrefUnlockPrefix = "TetrisArcade.Skill.";
        const string PrefLevelPrefix  = "TetrisArcade.SkillLv.";

        public const int MaxLevel = 3;

        public sealed class Node
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Description;
            public readonly int GoldCost;
            public readonly int DiamondCost;
            public readonly string Requires;

            public Node(string id, string name, string description,
                        int goldCost, int diamondCost, string requires)
            {
                Id = id;
                Name = name;
                Description = description;
                GoldCost = goldCost;
                DiamondCost = diamondCost;
                Requires = requires;
            }

            public bool IsDiamond => DiamondCost > 0;
            public string PriceLabel => IsDiamond ? DiamondCost + " D" : GoldCost + " G";
        }

        // ---- Branch A: Gold, one-off unlocks ----
        public const string BlockRemove = "block_remove";
        public const string LineRemove  = "line_remove";
        public const string Revive      = "revive";
        public const string SandBomb    = "sand_bomb";

        // ---- Branch B: Diamond, levelled mutation tuning ----
        public const string BombAffinity    = "bomb_affinity";
        public const string InoperableWard  = "inoperable_ward";
        public const string OddShapeWard    = "odd_shape_ward";

        public static readonly Node[] GoldBranch =
        {
            new Node(BlockRemove, "BLOCK REMOVE",
                     "Click one block to destroy it   ·   60s", 10, 0, null),
            new Node(LineRemove, "LINE REMOVE",
                     "Clear the topmost occupied row   ·   120s", 20, 0, BlockRemove),
            new Node(Revive, "REVIVE",
                     "Clear the board instead of dying   ·   once per run", 40, 0, LineRemove),
            // Stands outside the removal chain: it upgrades a mutation rather
            // than granting a skill, so it has no prerequisite of its own.
            new Node(SandBomb, "SAND BOMB",
                     "Upgrade every bomb   ·   the stack falls in after the blast", 30, 0, null),
        };

        // Diamond drops are scarce, so these stay cheap: 2/3/5 per node, 30 to
        // max all three.
        public static readonly int[] LevelCosts = { 2, 3, 5 };

        public static readonly Node[] DiamondBranch =
        {
            new Node(BombAffinity, "BOMB AFFINITY", "Bombs appear more often", 0, 1, null),
            new Node(InoperableWard, "INOPERABLE WARD", "Fewer pieces you cannot steer", 0, 1, null),
            new Node(OddShapeWard, "ODD SHAPE WARD", "Fewer 2x3 and 1x5 pieces", 0, 1, null),
        };

        // ============================ GOLD BRANCH ============================

        public static bool IsUnlocked(string id) =>
            PlayerPrefs.GetInt(PrefUnlockPrefix + id, 0) == 1;

        public static void Unlock(string id)
        {
            PlayerPrefs.SetInt(PrefUnlockPrefix + id, 1);
            PlayerPrefs.Save();
        }

        public static bool PrerequisiteMet(Node node) =>
            node.Requires == null || IsUnlocked(node.Requires);

        // ============================ DIAMOND BRANCH ============================

        /// <summary>Current level of a levelled node, 0 (baseline) to MaxLevel.</summary>
        public static int Level(string id) =>
            Mathf.Clamp(PlayerPrefs.GetInt(PrefLevelPrefix + id, 0), 0, MaxLevel);

        /// <summary>Diamond cost of the next level, or -1 when already maxed.</summary>
        public static int NextLevelCost(string id)
        {
            int lv = Level(id);
            return lv >= MaxLevel ? -1 : LevelCosts[lv];
        }

        public static void LevelUp(string id)
        {
            int lv = Level(id);
            if (lv >= MaxLevel) return;
            PlayerPrefs.SetInt(PrefLevelPrefix + id, lv + 1);
            PlayerPrefs.Save();
        }

        public static void ResetUnlocks()
        {
            foreach (var n in GoldBranch) PlayerPrefs.DeleteKey(PrefUnlockPrefix + n.Id);
            foreach (var n in DiamondBranch) PlayerPrefs.DeleteKey(PrefLevelPrefix + n.Id);
            PlayerPrefs.Save();
        }
    }
}
