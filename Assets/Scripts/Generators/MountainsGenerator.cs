using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using TerrainDemo.Layout;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;

namespace TerrainDemo.Generators
{
    public class MountainsGenerator : ZoneGenerator
    {
        private FastNoise _scaleNoise;
        private FastNoise _rotateNoise;
        MountainClusterContext _cluster;

        public MountainsGenerator(ZoneLayout zone, LandLayout land, LandGenerator generator, LandSettings landSettings) 
            : base(ZoneType.Mountains, zone, land, generator, landSettings)
        {
            _scaleNoise = new FastNoise(landSettings.Seed + 1);   
            _rotateNoise = new FastNoise(landSettings.Seed - 1);

            if (zone.Type == ZoneType.Mountains)
            {
                //Prepare shared cluster context
                if (!generator.Clusters.TryGetValue(zone.ClusterId, out _cluster))
                {
                    //DEBUG
                    _cluster = new MountainClusterContext(land, landSettings);
                    generator.Clusters[zone.ClusterId] = _cluster;
                }
            }
            else
            {
                //Supporting zone generator (for transient blocks), use nearest mountains cluster context
                var nearestMount = land.GetNeighbors(zone).First(z => z.Type == ZoneType.Mountains);
                if (!generator.Clusters.TryGetValue(nearestMount.ClusterId, out _cluster))
                {
                    //DEBUG
                    _cluster = new MountainClusterContext(land, landSettings);
                    generator.Clusters[nearestMount.ClusterId] = _cluster;
                }
            }
        }

        public override BlockType DefaultBlock { get {return BlockType.Grass;} }

        public override double GenerateBaseHeight(float worldX, float worldZ)
        {
            var yValue = 0d;

            //Lava-like features
            var scaleRatio = (_scaleNoise.GetSimplex(worldX, worldZ) / 2) + 1;      //0.5 .. 1.5
            yValue = _noise.GetSimplexFractal(scaleRatio * worldX, (2 - scaleRatio) * worldZ) * _zoneSettings.NoiseAmp;
            //yValue = Math.Pow(yValue + 1, 2);

            yValue += Land.GetGlobalHeight(worldX, worldZ);
            yValue += _zoneSettings.Height;
            yValue += _cluster != null ? _cluster.HeightInterpolator.GetValue(new Vector2(worldX, worldZ)) : 0;

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

        /// <summary>
        /// Shared mountain generator's logic
        /// </summary>
        public class MountainClusterContext
        {
            public readonly IDWMeshInterpolator HeightInterpolator;

            public MountainClusterContext(LandLayout land, LandSettings settings)
            {
                var allMountZones = land.Zones.Where(z => z.Type == ZoneType.Mountains).ToArray();
                var allMountClusters = allMountZones.GroupBy(z => z.ClusterId);

                var allMountCells = new List<Cell>();
                var allMountHeights = new List<double>();

                var rnd = new System.Random(settings.Seed);

                foreach (var mountCluster in allMountClusters)
                {
                    var mountSubmesh = new CellMesh.Submesh(land.CellMesh, mountCluster.Select(z => z.Cell).ToArray());
                    //Get mountain zone height coeff
                    var border = mountSubmesh.GetBorderCells().ToArray();
                    var floodFiller = mountSubmesh.GetFloodFill(border);
                    var neigh1 = floodFiller.GetNeighbors(1);
                    var neigh2 = floodFiller.GetNeighbors(2);
                    var neigh3 = floodFiller.GetNeighbors(3);

                    for (int i = 0; i < mountSubmesh.Cells.Length; i++)
                    {
                        var cell = mountSubmesh[i];
                        
                        if (border.Contains(cell))
                        {
                            var heightVariance = rnd.NextDouble() * 10;                     
                            allMountHeights.Add(heightVariance);                            //0 .. 10 
                        }
                        else if (neigh1.Contains(cell))
                        {
                            var heightVariance = rnd.NextDouble() * 20;            
                            allMountHeights.Add(10 + heightVariance);                       //10 .. 30 
                        }
                        else if (neigh2.Contains(cell))
                        {
                            var heightVariance = rnd.NextDouble() * 30;            
                            allMountHeights.Add(25 + heightVariance);                       //25 .. 55 
                        }
                        else if (neigh3.Contains(cell))
                        {
                            var heightVariance = rnd.NextDouble() * 20;           
                            allMountHeights.Add(50 + heightVariance);                       //50 .. 70
                        }
                        else
                        {
                            var heightVariance = rnd.NextDouble() * 20;                     //70 .. 90
                            allMountHeights.Add(70 + heightVariance);
                        }

                        allMountCells.Add(cell);
                    }
                }

                HeightInterpolator = new IDWMeshInterpolator(new CellMesh.Submesh(land.CellMesh, allMountCells.ToArray()), allMountHeights.ToArray());
            }
        }
    }
}
