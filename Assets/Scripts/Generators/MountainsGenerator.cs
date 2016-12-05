using System;
using System.Linq;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Generators
{
    public class MountainsGenerator : ZoneGenerator
    {
        public MountainsGenerator(ZoneLayout zone, LandLayout land, LandSettings landSettings) : base(ZoneType.Mountains, zone, land, landSettings)
        {
            
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            if (_landSettings.BypassHeight)
                return 0;

            var yValue = 0d;

            yValue = _noise.GetSimplexFractal(worldX, worldZ) * _zoneSettings.NoiseAmp;
            yValue = Math.Pow(yValue + 1, 2);

            yValue += Land.GetGlobalHeight(worldX, worldZ);
            yValue += _zoneSettings.Height;

            return yValue;

            var vertex = new Vector2(worldX, worldZ);
            
            //Find nearest ridge
            var distance = float.MaxValue;
            foreach (var zoneNeighbor in _zone.Neighbors)
            {
                var proj = Math3d.ProjectPointOnLineSegment(_zone.Center, zoneNeighbor.Center, vertex);
                var d = Vector3.Distance(vertex, proj);
                if (d < distance)
                    distance = d;
            }

            var ridgeHeight = Mathf.Clamp(20 - distance, 0, 20) / 20 + 1;
            
            //var mountInfluence = influence[ZoneType.Mountains];

            //Slightly raise up heightmap of Mountain zone
            //if (mountInfluence > 0.7f)
            {
                //var additional = 1.5f;
                yValue *= ridgeHeight;
            }
            
            return yValue;
        }

        protected override BlockType GenerateBlock(Vector2i worldPosition, Vector2i turbulence, Vector3 normal, ZoneRatio influence)
        {
            var mountInfluence = influence[ZoneType.Mountains];

            if (mountInfluence > 0.85f && Vector3.Angle(Vector3.up, normal) < 45)
                return BlockType.Snow;

            if (mountInfluence < 0.6f && Vector3.Angle(Vector3.up, normal) < 45)
                return BlockType.Grass;
            
            return base.GenerateBlock(worldPosition, turbulence, normal, influence);
        }

        //protected override void DecorateZone(Chunk chunk)
        //{
        //    base.DecorateZone(chunk);

        //    var stones = new List<Vector3>();
        //    for (int x = 0; x < chunk.GridSize; x++)
        //        for (int z = 0; z < chunk.GridSize; z++)
        //        {
        //            var mountInfluence = chunk.Influence[x, z][ZoneType.Mountains];
        //            if (mountInfluence < 0.8f)
        //                if (UnityEngine.Random.value < 0.05f * mountInfluence)
        //                    stones.Add(new Vector3(x, chunk.HeightMap[x, z], z));
        //        }

        //    chunk.Stones = stones.ToArray();
        //}


    }

}
