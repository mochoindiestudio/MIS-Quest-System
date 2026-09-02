using System;
using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>Which way the player is facing.</summary>
    public enum Facing
    {
        Down,
        Up,
        Left,
        Right
    }

    /// <summary>
    /// The single place the demo picks sprites out of <c>Assets/Art/ClassicRPG_Sheet.png</c>. The
    /// scene builder only reads this, so you can retune every sprite in the Inspector and rebuild.
    /// Nothing is scaled: 16 px sprites at PPU 16 = one tilemap cell.
    /// </summary>
    [CreateAssetMenu(fileName = "DemoSpriteMap", menuName = "MIS Quest System Demo/Sprite Map")]
    public sealed class DemoSpriteMap : ScriptableObject
    {
        /// <summary>A 3x3 auto-tiling set for a filled area (corners, edges, centre).</summary>
        [Serializable]
        public sealed class NineSlice
        {
            public Sprite topLeft;
            public Sprite top;
            public Sprite topRight;
            public Sprite left;
            public Sprite center;
            public Sprite right;
            public Sprite bottomLeft;
            public Sprite bottom;
            public Sprite bottomRight;

            /// <summary>Picks the tile for a cell given whether it sits on each edge of the filled rect.</summary>
            public Sprite Pick(bool onLeft, bool onRight, bool onTop, bool onBottom)
            {
                if (onTop && onLeft) return Or(topLeft, center);
                if (onTop && onRight) return Or(topRight, center);
                if (onBottom && onLeft) return Or(bottomLeft, center);
                if (onBottom && onRight) return Or(bottomRight, center);
                if (onTop) return Or(top, center);
                if (onBottom) return Or(bottom, center);
                if (onLeft) return Or(left, center);
                if (onRight) return Or(right, center);
                return center;
            }

            private static Sprite Or(Sprite a, Sprite b) => a != null ? a : b;
        }

        /// <summary>4 walk frames for each of the 4 facings.</summary>
        [Serializable]
        public sealed class WalkSet
        {
            public Sprite[] down = new Sprite[4];
            public Sprite[] up = new Sprite[4];
            public Sprite[] left = new Sprite[4];
            public Sprite[] right = new Sprite[4];

            public Sprite[] Frames(Facing facing)
            {
                switch (facing)
                {
                    case Facing.Up: return up;
                    case Facing.Left: return left;
                    case Facing.Right: return right;
                    default: return down;
                }
            }
        }

        /// <summary>The well's tiles, row-major from the top-left cell.</summary>
        [Serializable]
        public sealed class WellSprites
        {
            public Vector2Int gridSize = new Vector2Int(1, 2);
            public Sprite[] tiles;

            public Sprite At(int col, int row)
            {
                int index = row * Mathf.Max(1, gridSize.x) + col;
                return tiles != null && index >= 0 && index < tiles.Length ? tiles[index] : null;
            }
        }

        [Header("Ground")]
        public NineSlice grass = new NineSlice();
        public NineSlice dirt = new NineSlice();

        [Tooltip("Single dirt-path tiles (straights / corners) surrounded by grass detail -- placed " +
                 "by hand on the Paths tilemap. Generated as tile assets for the palette.")]
        public Sprite[] dirtPath;

        [Header("Scenery")]
        [Tooltip("Big tree, 2x2, row-major: topLeft, topRight, bottomLeft, bottomRight.")]
        public Sprite[] bigTree;

        [Tooltip("Small tree, 1 wide x 2 tall: top, bottom.")]
        public Sprite[] smallTree;

        public Sprite rock1;
        public Sprite rock2;

        [Tooltip("Loose decoration tiles, placed by hand on the Decor tilemap / palette.")]
        public Sprite[] decor;

        [Header("Quest objects")]
        public WellSprites well = new WellSprites();
        public Sprite bucket;
        public Sprite rope;
        public Sprite bucketOnRope;
        public Sprite bucketOfWater;

        [Header("Player")]
        public WalkSet player = new WalkSet();
    }
}
