using OpenToolkit.Mathematics;

namespace TerrainDemo.Spatial
{
    /// <summary>
    /// Cardinal directions on square grid
    /// </summary>
    public enum Side2d
    {
        Forward,        //Z+
        Right,          //X+
        Back,           //Z-
        Left            //X-
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
        /// Directions on grid as Vector2i
        /// </summary>
        public static readonly Vector2[] Vector2 =
        {
            OpenToolkit.Mathematics.Vector2.UnitY, OpenToolkit.Mathematics.Vector2.UnitX, -OpenToolkit.Mathematics.Vector2.UnitY, -OpenToolkit.Mathematics.Vector2.UnitX,
        };
    }
}
