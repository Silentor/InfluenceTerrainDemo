using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Geometrical representaton of land
    /// </summary>
    public abstract class LandLayout
    {
        public Bounds2i Bounds { get; private set; }

        public IEnumerable<ZoneLayout> Zones { get; private set; }

        /// <summary>
        /// Get all chunks of zone
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Vector2i> GetChunks(ZoneLayout zone)
        {
            var centerChunk = Chunk.GetPosition(zone.Center);
            var result = new List<Vector2i>();
            var processed = new List<Vector2i>();

            GetChunksFloodFill(zone, centerChunk, processed, result);
            return result;
        }

        public ZoneRatio GetInfluence(Vector2 worldPosition)
        {
            _influenceTime.Start();

            var influenceLookup = new float[_zoneMaxType + 1];

            //Sum up zones influence
            foreach (var zone in Zones)
            {
                if (zone.Type != ZoneType.Empty)
                {
                    var idwSimplestWeighting = IDWSimplestWeighting(zone.Center, worldPosition);
                    influenceLookup[(int)zone.Type] += idwSimplestWeighting;
                }
            }

            var result = new ZoneRatio(
                influenceLookup.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray(),
                _zoneTypesCount);

            //foreach (var zone in _zones.OrderBy(z => Vector3.SqrMagnitude(z.Center - worldPosition)).Take(10))
            //result[zone.Type] += IDWSimplestWeighting(zone.Center, worldPosition);

            //var result = new ZoneRatio(_zoneMaxType);
            //result.Normalize();

            _influenceTime.Stop();

            return result;
        }

        public IZoneNoiseSettings GetZoneNoiseSettings(ZoneRatio influence)
        {
            return ZoneSettings.Lerp(_zoneSettings, influence);
        }

        protected ILandSettings Settings;
        private readonly AverageTimer _influenceTime = new AverageTimer();
        private readonly float _idwCoeff;
        private readonly int _zoneTypesCount;
        private readonly ZoneSettings[] _zoneSettings;
        private readonly int _zoneMaxType;

        protected LandLayout(ILandSettings settings)
        {
            Bounds = settings.LandBounds;
            Settings = settings;

            var points = GeneratePoints(settings.ZonesCount, Bounds, settings.ZonesDensity);
            var cells = CellMeshGenerator.Generate(points, (Bounds) Bounds);
            var zonesType = SetZoneTypes(cells, settings);

            var zones = new ZoneLayout[points.Length];
            for (int i = 0; i < zones.Length; i++)
                zones[i] = new ZoneLayout(zonesType[i], cells[i]);

            Zones = zones;

            _idwCoeff = settings.IDWCoeff;
            _zoneMaxType = (int)settings.ZoneTypes.Max(z => z.Type);
            
            _zoneTypesCount = zones.Where(z => z.Type != ZoneType.Empty).Distinct(ZoneLayout.TypeComparer).Count();
            _zoneSettings = settings.ZoneTypes.ToArray();
        }

        /// <summary>
        /// Generate random centers of cells
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="density"></param>
        /// <returns></returns>
        protected abstract Vector2[] GeneratePoints(int count, Bounds2i landBounds, Vector2 density);

        protected abstract ZoneType[] SetZoneTypes(Cell[] cells, ILandSettings settings);

        private void GetChunksFloodFill(ZoneLayout zone, Vector2i from, List<Vector2i> processed, List<Vector2i> result)
        {
            if (!processed.Contains(@from))
            {
                processed.Add(from);

                if (CheckChunkConservative(from, zone))
                {
                    result.Add(from);
                    GetChunksFloodFill(zone, from + Vector2i.Forward, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Back, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Left, processed, result);
                    GetChunksFloodFill(zone, from + Vector2i.Right, processed, result);
                }
            }
        }

        /// <summary>
        /// Check chunk corners and zone vertices
        /// </summary>
        /// <param name="chunkPosition"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        private bool CheckChunkConservative(Vector2i chunkPosition, ZoneLayout zone)
        {
            if (!zone.ChunkBounds.Contains(chunkPosition))
                return false;

            var chunkBounds = Chunk.GetBounds(chunkPosition);
            var floatBounds = (Bounds)chunkBounds;
            var chunkCorner1 = Convert(floatBounds.min);
            var chunkCorner2 = new Vector2(floatBounds.min.x, floatBounds.max.z);
            var chunkCorner3 = Convert(floatBounds.max);
            var chunkCorner4 = new Vector2(floatBounds.max.x, floatBounds.min.z);

            ZoneLayout zone1 = zone, zone2 = zone, zone3 = zone, zone4 = zone;
            var distanceCorner1 = float.MaxValue;
            var distanceCorner2 = float.MaxValue;
            var distanceCorner3 = float.MaxValue;
            var distanceCorner4 = float.MaxValue;

            foreach (var z in Zones)
            {
                if (Vector2.SqrMagnitude(z.Center - chunkCorner1) < distanceCorner1)
                {
                    zone1 = z;
                    distanceCorner1 = Vector2.SqrMagnitude(z.Center - chunkCorner1);
                }
                if (Vector2.SqrMagnitude(z.Center - chunkCorner2) < distanceCorner2)
                {
                    zone2 = z;
                    distanceCorner2 = Vector2.SqrMagnitude(z.Center - chunkCorner2);
                }
                if (Vector2.SqrMagnitude(z.Center - chunkCorner3) < distanceCorner3)
                {
                    zone3 = z;
                    distanceCorner3 = Vector2.SqrMagnitude(z.Center - chunkCorner3);
                }
                if (Vector2.SqrMagnitude(z.Center - chunkCorner4) < distanceCorner4)
                {
                    zone4 = z;
                    distanceCorner4 = Vector2.SqrMagnitude(z.Center - chunkCorner4);
                }
            }

            if (zone1 == zone || zone2 == zone || zone3 == zone || zone4 == zone)
                return true;

            //Check zone vertices in chunk
            foreach (var vert in zone.Cell.Vertices)
                if (floatBounds.Contains(Convert(vert)))
                    return true;

            return false;
        }

        private float IDWSimplestWeighting(Vector2 interpolatePoint, Vector2 point)
        {
            var d = Vector2.SqrMagnitude(interpolatePoint - point);
            var result = (float)(1 / Math.Pow(d + 0.001f, _idwCoeff));
            //var result = 100 - Vector2.Distance(interpolatePoint,  point);
            return (float)result;
        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        private static Vector2 Convert(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

    }
}
