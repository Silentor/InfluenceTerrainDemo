using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Cardinal directions on square grid
    /// </summary>
    public enum Side2d
    {
        /// <summary>
        /// Z+ direction
        /// </summary>
        Forward,        //Z+
        /// <summary>
        /// X+ direction
        /// </summary>
        Right,          //X+
        /// <summary>
        /// Z- direction
        /// </summary>
        Back,           //Z-
        /// <summary>
        /// X- direction
        /// </summary>
        Left,            //X-

    }

    public static class Directions
    {
        /// <summary>
        /// Cardinal directions on 2D grid
        /// </summary>
        public static readonly Side2d[] Cardinal =
        {
            Side2d.Forward, Side2d.Right, Side2d.Back, Side2d.Left
        };

        /// <summary>
        /// Directions on grid as Vector2i
        /// </summary>
        public static readonly Vector2i[] Vector2I =
        {
            Vector2i.Forward, Vector2i.Right, Vector2i.Back, Vector2i.Left,
        };

        /// <summary>
        /// Directions on grid as Vector2
        /// </summary>
        public static readonly Vector2[] Vector2 =
        {
            OpenToolkit.Mathematics.Vector2.UnitY, OpenToolkit.Mathematics.Vector2.UnitX, -OpenToolkit.Mathematics.Vector2.UnitY, -OpenToolkit.Mathematics.Vector2.UnitX,
        };

        public static readonly (Vector2, Vector2)[] BlockSide =
        {
            (new Vector2(0, 1), new Vector2(1, 1)),
            (new Vector2(1, 1), new Vector2(1, 0)),
            (new Vector2(1, 0), new Vector2(0, 0)),
            (new Vector2(0, 0), new Vector2(0, 1)),
        };
    }

    public static class DirectionsExtensions
    {
        public static Vector2 ToVector2(this Side2d direction)
        {
            return Directions.Vector2[(int) direction];
        }
    }
}
