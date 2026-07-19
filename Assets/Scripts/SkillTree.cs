using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// The permanent unlock tree, as data plus its persistence. Unlike the shop
    /// these are bought once and kept forever, so ownership is a flag rather
    /// than a count.
    /// </summary>
    public static class SkillTree
    {
        const string PrefUnlockPrefix = "TetrisArcade.Skill.";

        public sealed class Node
        {
            public readonly string Id;
            public readonly string Name;
            public readonly string Description;
            public readonly int GoldCost;
            public readonly int DiamondCost;
            public readonly string Requires;   // node id that must be unlocked first, or null

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

        public const string BlockRemove = "block_remove";
        public const string LineRemove  = "line_remove";
        public const string Revive      = "revive";

        // Branch A is Gold and ordered; Branch B is a single Diamond node.
        public static readonly Node[] GoldBranch =
        {
            new Node(BlockRemove, "BLOCK REMOVE",
                     "Click one block to destroy it   ·   60s", 10, 0, null),
            new Node(LineRemove, "LINE REMOVE",
                     "Clear the topmost occupied row   ·   120s", 20, 0, BlockRemove),
        };

        public static readonly Node[] DiamondBranch =
        {
            new Node(Revive, "REVIVE",
                     "Clear the board instead of dying   ·   once per run", 0, 10, null),
        };

        public static bool IsUnlocked(string id) =>
            PlayerPrefs.GetInt(PrefUnlockPrefix + id, 0) == 1;

        public static void Unlock(string id)
        {
            PlayerPrefs.SetInt(PrefUnlockPrefix + id, 1);
            PlayerPrefs.Save();
        }

        /// <summary>A node is buyable once its prerequisite is unlocked.</summary>
        public static bool PrerequisiteMet(Node node) =>
            node.Requires == null || IsUnlocked(node.Requires);

        public static void ResetUnlocks()
        {
            foreach (var n in GoldBranch) PlayerPrefs.DeleteKey(PrefUnlockPrefix + n.Id);
            foreach (var n in DiamondBranch) PlayerPrefs.DeleteKey(PrefUnlockPrefix + n.Id);
            PlayerPrefs.Save();
        }
    }
}
