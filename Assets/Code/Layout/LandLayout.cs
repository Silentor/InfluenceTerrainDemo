using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Assets.Code.Settings;
using Assets.Code.Tools;
using Assets.Code.Voronoi;
using UnityEngine;
using Random = System.Random;

namespace Assets.Code.Layout
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

            Array.Clear(_influenceLookup, 0, _influenceLookup.Length);

            //Sum up zones influence
            foreach (var zone in Zones)
            {
                if (zone.Type != ZoneType.Empty)
                {
                    var idwSimplestWeighting = IDWSimplestWeighting(zone.Center, worldPosition);
                    _influenceLookup[(int)zone.Type] += idwSimplestWeighting;
                }
            }

            var result = new ZoneRatio(
                _influenceLookup.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray(),
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
        private float[] _influenceLookup;
        private float _idwCoeff;
        private int _zoneTypesCount;
        private ZoneSettings[] _zoneSettings;

        protected LandLayout(ILandSettings settings)
        {
            Bounds = settings.LandBounds;
            Settings = settings;

            var points = GeneratePoints(settings.ZonesCount, Bounds, settings.ZoneCenterMinDistance);
            var cells = CellMeshGenerator.Generate(points, (Bounds) Bounds);
            var zonesType = SetZoneTypes(cells, settings);

            var zones = new ZoneLayout[points.Length];
            for (int i = 0; i < zones.Length; i++)
                zones[i] = new ZoneLayout(zonesType[i], cells[i]);

            Zones = zones;

            _idwCoeff = settings.IDWCoeff;
            var zoneMaxType = (int)settings.ZoneTypes.Max(z => z.Type);
            _influenceLookup = new float[zoneMaxType + 1];
            _zoneTypesCount = zones.Where(z => z.Type != ZoneType.Empty).Distinct(ZoneLayout.TypeComparer).Count();
            _zoneSettings = settings.ZoneTypes.ToArray();
        }

        /// <summary>
        /// Generate random centers of cells
        /// </summary>
        /// <param name="count"></param>
        /// <param name="landBounds"></param>
        /// <param name="minDistance"></param>
        /// <returns></returns>
        protected virtual Vector2[] GeneratePoints(int count, Bounds2i landBounds, float minDistance = 32)
        {
            //Prepare input data
            var infiniteLoopChecker = 0;
            //var chunksGrid = new bool[gridSize * 2, gridSize * 2];

            //Generate zones center coords, check that only one zone occupies one chunk
            var zonesCoords = new List<Vector2>(count);
            for (var i = 0; i < count; i++)
            {
                var zoneCenterX = UnityEngine.Random.Range((float)landBounds.Min.X, landBounds.Max.X);
                var zoneCenterY = UnityEngine.Random.Range((float)landBounds.Min.Z, landBounds.Max.Z);
                var newCenter = new Vector2(zoneCenterX, zoneCenterY);
                if (zonesCoords.All(zc => Vector2.SqrMagnitude(zc - newCenter) > minDistance * minDistance))
                //if (IsZoneAllowed(chunksGrid, new Vector2i(zoneCenterX / 16, zoneCenterY / 16), minDistance))
                {
                    //chunksGrid[zoneCenterX / 16, zoneCenterY / 16] = true;
                    zonesCoords.Add(new Vector2 { x = zoneCenterX, y = zoneCenterY });
                }
                else
                {
                    if (infiniteLoopChecker++ < 100)
                        i--;
                    else
                        break;
                }
            }

            return zonesCoords.ToArray();
        }

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
                if (floatBounds.Contains(vert))
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
