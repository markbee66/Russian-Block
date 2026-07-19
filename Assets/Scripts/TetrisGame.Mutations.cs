using UnityEngine;

namespace TetrisArcade
{
    /// <summary>
    /// Mutated pieces: bombs, odd shapes and inoperable pieces.
    ///
    /// Rates come from the Diamond skill tree, so they are player-tunable. A
    /// single roll picks at most one mutation from cumulative bands, which means
    /// each configured rate is the true rate — no compounding from an ordered
    /// sequence of independent rolls.
    /// </summary>
    public partial class TetrisGame
    {
        // Type indices past the seven standard pieces. The bomb kind is part of
        // the type, so a bomb sitting in the lookahead cannot change what an
        // already-falling bomb will do.
        const int BombBox = 7;       // clears the 3x3 around it
        const int BombColumn = 8;    // clears its column
        const int BombRow = 9;       // clears its row
        const int Rect2x3Type = 10;
        const int Bar1x5Type = 11;
        const int InoperableBase = 12;   // 12..18 = inoperable I O T S Z J L

        static bool IsBomb(int type) => type >= BombBox && type <= BombRow;
        static bool IsInoperable(int type) => type >= InoperableBase;

        // ============================ ROLLING ============================

        /// <summary>
        /// Turns a freshly drawn bag piece into its mutated form, or leaves it
        /// alone. One roll, cumulative bands, so at most one mutation applies.
        /// </summary>
        int ApplyMutation(int baseType)
        {
            float inoperable = MutationRates.InoperableRate();
            float bomb = MutationRates.BombRate();
            float odd = MutationRates.OddShapeRate();

            float r = Random.value;

            if (r < inoperable)
                return InoperableBase + baseType;

            r -= inoperable;
            if (r < bomb)
                return BombBox + Random.Range(0, 3);   // the three kinds, evenly

            r -= bomb;
            if (r < odd)
                return Random.value < 0.5f ? Rect2x3Type : Bar1x5Type;

            return baseType;
        }

        // ============================ BOMBS ============================

        /// <summary>
        /// Detonates a bomb of the given type that has just locked at (bx, by).
        /// Returns the number of cells destroyed so the caller can score them.
        /// </summary>
        int Detonate(int bombType, int bx, int by)
        {
            int destroyed = 0;

            if (bombType == BombBox)
            {
                for (int x = bx - 1; x <= bx + 1; x++)
                    for (int y = by - 1; y <= by + 1; y++)
                    {
                        if (x < 0 || x >= Width || y < 0 || y >= Height) continue;
                        if (board[x, y] < 0) continue;
                        board[x, y] = -1;
                        destroyed++;
                    }
            }
            else if (bombType == BombColumn)
            {
                for (int y = 0; y < Height; y++)
                    if (board[bx, y] >= 0) { board[bx, y] = -1; destroyed++; }
            }
            else // BombRow
            {
                for (int x = 0; x < Width; x++)
                    if (board[x, by] >= 0) { board[x, by] = -1; destroyed++; }
            }

            CollapseAll();
            return destroyed;
        }

        /// <summary>
        /// Settles every column, so blasted holes close up the same way a line
        /// clear does. Column-wise rather than row-wise because a bomb can leave
        /// gaps that do not span a full row.
        /// </summary>
        void CollapseAll()
        {
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (board[x, y] < 0) continue;
                    board[x, write] = board[x, y];
                    if (write != y) board[x, y] = -1;
                    write++;
                }
            }
        }

        static string BombName(int bombType) =>
            bombType == BombBox ? "3x3 BOMB"
            : bombType == BombColumn ? "COLUMN BOMB"
            : "ROW BOMB";
    }

    /// <summary>
    /// The mutation rates, derived from Diamond skill tree levels. Kept separate
    /// from the game object so the skill tree screen can show the same numbers
    /// the game will actually roll.
    /// </summary>
    public static class MutationRates
    {
        // Level 0 is the untouched baseline; levels 1-3 are bought. The last
        // entry of each table is the cap the player is promised.
        public static readonly float[] Bomb       = { 0.03f, 0.041f, 0.052f, 0.0633f };
        public static readonly float[] Inoperable = { 0.05f, 0.0433f, 0.0367f, 0.03f };
        public static readonly float[] OddShape   = { 0.0736f, 0.0591f, 0.0445f, 0.03f };

        public static float BombRate() => Bomb[SkillTree.Level(SkillTree.BombAffinity)];
        public static float InoperableRate() => Inoperable[SkillTree.Level(SkillTree.InoperableWard)];
        public static float OddShapeRate() => OddShape[SkillTree.Level(SkillTree.OddShapeWard)];

        public static float NormalRate() =>
            1f - BombRate() - InoperableRate() - OddShapeRate();

        public static float[] TableFor(string nodeId) =>
            nodeId == SkillTree.BombAffinity ? Bomb
            : nodeId == SkillTree.InoperableWard ? Inoperable
            : OddShape;

        public static string Percent(float v) => (v * 100f).ToString("0.0") + "%";
    }
}
