using JetBrains.Annotations;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using UnityEngine;

namespace TerrainDemo.Generators.Debug
{
    public class CheckboardGenerator : ZoneGenerator
    {
        public CheckboardGenerator(ZoneLayout zone, [NotNull] LandLayout land, [NotNull] LandSettings landSettings) : base(ZoneType.Checkboard, zone, land, landSettings)
        {
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            return GetDiagonalLines(worldPosition);
        }

        private BlockType GetDiagonalLines(Vector2i worldPosition)
        {
            return (worldPosition.Z + worldPosition.X) % 3 == 0 ? BlockType.Rock : BlockType.Grass;
        }
    }
}
