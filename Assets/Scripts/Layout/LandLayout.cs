using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Geometrical representaton of land zones
    /// </summary>
    public class LandLayout
    {
        public Bounds2i Bounds { get; private set; }

        public CellMesh CellMesh { get; private set; }

        /// <summary>
        /// Sorted by Id (same as a Cells in CellMesh)
        /// </summary>
        public IEnumerable<ZoneLayout> Zones { get; private set; }

        public LandLayout(ILandSettings settings, CellMesh cellMesh, ZoneType[] zoneTypes)
        {
            _settings = settings;
            Update(cellMesh, zoneTypes);
        }

        public void Update(CellMesh cellMesh, ZoneType[] zoneTypes)
        {
            Bounds = _settings.LandBounds;
            CellMesh = cellMesh;
            _zoneSettings = _settings.ZoneTypes.ToArray();
            _zoneTypesCount = zoneTypes.Where(z => z != ZoneType.Empty).Distinct().Count();
            _zoneMaxType = (int)_settings.ZoneTypes.Max(z => z.Type);

            var zones = new ZoneLayout[zoneTypes.Length];
            for (int i = 0; i < zones.Length; i++)
                zones[i] = new ZoneLayout(zoneTypes[i], cellMesh.Cells[i], _zoneSettings.First(z => z.Type == zoneTypes[i]));

            Zones = zones;
            for (int i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                zone.Init(this);
                zones[i] = zone;
            }
        }

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
            return GetInfluence4(worldPosition);
        }

        /// <summary>
        /// Get zones influence for given layout point (simple IDW)
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public ZoneRatio GetInfluence1(Vector2 worldPosition)
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

            var values =
                influenceLookup.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray();
            var result = new ZoneRatio(values, values.Length);

            _influenceTime.Stop();

            return result;
        }

        /// <summary>
        /// Get zones influence (Gaussian blur method)
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public ZoneRatio GetInfluence2(Vector2 worldPosition)
        {
            _influenceTime.Start();

            //Prepare bitmap

            if (_sourceBitmap == null)
            {
                _sourceBitmap = new float[Bounds.Size.X, Bounds.Size.Z][];
                _targetBitmap = new float[Bounds.Size.X, Bounds.Size.Z][];

                for (int x = 0; x < Bounds.Size.X; x++)
                {
                    for (int z = 0; z < Bounds.Size.Z; z++)
                    {
                        //Apply some turbulence
                        var world = (Vector2)(Bounds.Min + new Vector2i(x, z));
                        var turbulenceX = (Mathf.PerlinNoise(world.x * 0.1f, world.y * 0.1f) - 0.5f) * 10;
                        var turbulenceZ = (Mathf.PerlinNoise(world.y * 0.1f, world.x * 0.1f) - 0.5f) * 10;
                        var worldTurbulated = world + new Vector2(turbulenceX, turbulenceZ);

                        _sourceBitmap[x, z] = GetNearestNeighborInfluence(worldTurbulated);
                    }
                }

                
                //var boxes = BoxesForGauss(5, 3);
                //boxBlur_2(_sourceBitmap, _targetBitmap, Bounds.Size.X, Bounds.Size.Z, (boxes[0] - 1)/2);
                //boxBlur_2(_targetBitmap, _sourceBitmap, Bounds.Size.X, Bounds.Size.Z, (boxes[1] - 1)/2);
                //boxBlur_2(_sourceBitmap, _targetBitmap, Bounds.Size.X, Bounds.Size.Z, (boxes[2] - 1)/2);
                boxBlur_2(_sourceBitmap, _targetBitmap, Bounds.Size.X, Bounds.Size.Z, 1);
                boxBlur_2(_targetBitmap, _sourceBitmap, Bounds.Size.X, Bounds.Size.Z, 2);
                boxBlur_2(_sourceBitmap, _targetBitmap, Bounds.Size.X, Bounds.Size.Z, 3);
                //_targetBitmap = _sourceBitmap;
            }

            var localPos = (Vector2i) worldPosition - Bounds.Min;
            var influenceLookup = _targetBitmap[localPos.X, localPos.Z];

            var values =
                influenceLookup.Select((v, i) => new ZoneValue((ZoneType) i, v)).Where(v => v.Value > 0).ToArray();
            var result = new ZoneRatio(values, values.Length);

            _influenceTime.Stop();

            return result;
        }

        /// <summary>
        /// Get zones influence (natural neighbour method). See https://3d.bk.tudelft.nl/hledoux/pdfs/04_sdh.pdf
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public ZoneRatio GetInfluence3(Vector2 worldPosition)
        {
            var centerZone = GetZoneFor(worldPosition, true);
            var centerZones = centerZone.Cell.Neighbors.Select(c => Zones.First(z => z.Cell == c)).Concat(new[] {centerZone}).ToArray();
            var centers = centerZones.Select(z => z.Center).ToList();

            //Get original areas
            var original = CellMeshGenerator.Generate(centers, CellMesh.Bounds);

            //Insert new Voronoi cell
            centers.Add(worldPosition);
            var modified = CellMeshGenerator.Generate(centers, CellMesh.Bounds);
            
            //Get natural neighbors coordinates
            float[] influenceLookup = new float[_zoneMaxType + 1];
            var newCellArea = modified.Cells.First(c => c.Center == worldPosition).GetArea();
            for (int i = 0; i < original.Cells.Length; i++)
            {
                var originalCell = original.Cells[i];
                var modifiedCell = modified.Cells.First(c => c.Center == originalCell.Center);
                var nnCoord = (originalCell.GetArea() - modifiedCell.GetArea()) / newCellArea;

                /*
                if (nnCoord > -0.1 && nnCoord < 0)
                    nnCoord = 0;
                else if (nnCoord > 1 && nnCoord < 1.1)
                    nnCoord = 1;

                if(nnCoord < 0 || nnCoord > 1)
                    Debug.Log("Bug at zone " + centerZone.Cell.Id);
                    */

                nnCoord = Mathf.Clamp01(nnCoord);

                var originalZone = centerZones.First(z => z.Center == originalCell.Center);

                influenceLookup[(int) originalZone.Type] += nnCoord;
            }

            var values =
                influenceLookup.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray();
            var result = new ZoneRatio(values, values.Length);

            return result;
        }

        /// <summary>
        /// Get zones influence for given layout point (function from Shepard IDW)
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        public ZoneRatio GetInfluence4(Vector2 worldPosition)
        {
            _influenceTime.Start();

            //Get local space
            var nearestZones = Zones.OrderBy(z => Vector2.SqrMagnitude(z.Center - worldPosition)).Take(_settings.IDWNearestPoints).ToArray();
            var searchRadius = Vector2.Distance(nearestZones.Last().Center, worldPosition) + 0.00001f;

            var influenceLookup = new float[_zoneMaxType + 1];

            //Sum up zones influence
            foreach (var zone in nearestZones)
            {
                if (zone.Type != ZoneType.Empty )
                {
                    //var zoneWeight = IDWShepardWeighting(zone.Center, worldPosition, searchRadius);
                    var zoneWeight = IDWLocalShepard(zone.Center, worldPosition, searchRadius);
                    //var zoneWeight = IDWLocalLinear(zone.Center, worldPosition, searchRadius);
                    influenceLookup[(int)zone.Type] += zoneWeight;
                }
            }

            var values =
                influenceLookup.Select((v, i) => new ZoneValue((ZoneType)i, v)).Where(v => v.Value > 0).ToArray();
            var result = new ZoneRatio(values, values.Length);

            _influenceTime.Stop();

            return result;
        }


        private float[] CubicPolate(float[] v0, float[] v1, float[] v2, float[] v3, float fracy)
        {
            //var A = (v3 - v2) - (v0 - v1);
            //var B = (v0 - v1) - A;
            //var C = v2 - v0;
            //var D = v1;

            var A = Substract(Substract(v3, v2), Substract(v0, v1));
            var B = Substract(Substract(v0, v1), A);
            var C = Substract(v2, v0);
            var D = v1;

            //return D + fracy * (C + fracy * (B + fracy * A));
            return Add(D, Mult(fracy, Add(C, Mult(fracy, Add(B, Mult(fracy, A))))));
        }

        private float[] GetNearestNeighborInfluence(Vector2 worldPosition)
        {
            var zone = GetZoneFor(worldPosition, true);
            var ratios = new float[_zoneMaxType + 1];
            ratios[(int) zone.Type] = 1;
            return ratios;
        }

        /*
        public IZoneNoiseSettings GetZoneNoiseSettings(ZoneRatio influence)
        {
            return ZoneSettings.Lerp(_zoneSettings, influence);
        }
        */

        public ZoneLayout GetZoneFor(Vector2 position, bool respectIntervals)
        {
            if(respectIntervals)
                return Zones.ElementAt(CellMesh.GetCellFor(position, c => !Zones.ElementAt(c.Id).IsInterval).Id);
            else
                return Zones.ElementAt(CellMesh.GetCellFor(position).Id);
        }

        public void PrintInfluences(Vector2 worldPosition)
        {
            /*
            //Sum up zones influence
            foreach (var zone in Zones)
            {
                if (zone.Type != ZoneType.Empty)
                {
                    var idwWeighting = IDWShepardWeighting(zone.Center, worldPosition);
                    if(idwWeighting > 0)
                        Debug.LogFormat("{0} : value {1}, distance {2}", zone.Cell.Id, idwWeighting, Vector2.Distance(zone.Center, worldPosition));
                }
            }
            */
        }

        private ILandSettings _settings;
        private readonly AverageTimer _influenceTime = new AverageTimer();
        private int _zoneTypesCount;
        private ZoneSettings[] _zoneSettings;
        private int _zoneMaxType;
        private float[,][] _sourceBitmap;
        private float[,][] _targetBitmap;

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
            var d = Vector2.Distance(interpolatePoint, point);
            var result = (float)(1 / Math.Pow(d + _settings.IDWOffset, _settings.IDWCoeff));
            //var result = 30 - d;
            //var result = 1;
            //var result = Math.Sqrt(1/(d + 0.0001));
            if (result < 0) result = 0;
            return (float)result;
        }

        private float IDWShepardWeighting(Vector2 interpolatePoint, Vector2 point, float searchRadius)
        {
            var d = Vector2.Distance(interpolatePoint, point);

            if(d > searchRadius)
                return 0;
            else if(d > searchRadius / 3)
                return (27f / (4 * searchRadius)) * (d / searchRadius - 1) * (d / searchRadius - 1);
            else if (d > float.Epsilon)
                return 1/d;
            else
                return float.MaxValue;
        }

        private float IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, float searchRadius)
        {
            var d = Vector2.Distance(interpolatePoint, point);

            var a = searchRadius - d;
            if (a < 0) a = 0;
            var b = a / (searchRadius * d);
            return b*b;
        }

        private float IDWLocalLinear(Vector2 interpolatePoint, Vector2 point, float searchRadius)
        {
            var d = Vector2.Distance(interpolatePoint, point);
            var a = (searchRadius - d)*(searchRadius - d)*(searchRadius - d);
            a = Mathf.Clamp(a, 0, searchRadius*searchRadius*searchRadius);
            return a;
        }


        /// <summary>
        /// Get box blur sizes to simulate Gaussian blur
        /// </summary>
        /// <param name="sigma"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private int[] BoxesForGauss(float sigma, int n)  // standard deviation, number of boxes
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);  // Ideal averaging filter width 
            var wl = (int)Math.Floor(wIdeal); if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = Math.Round(mIdeal);
            // var sigmaActual = Math.sqrt( (m*wl*wl + (n-m)*wu*wu - n)/12 );

            var result = new int[n];
            for (var i = 0; i < n; i++)
                result[i] = i < m ? wl : wu;
            return result;
        }

        //see http://blog.ivank.net/fastest-gaussian-blur.html
        private void boxBlur_2(float[,][] scl, float[,][] tcl, int w, int h, int r)
        {
            var div = r;
            for (var i = 0; i < h; i++)
                for (var j = 0; j < w; j++)
                {
                    //var blurRadius = Mathf.PerlinNoise(i * 0.05f + 100, j * 0.05f) * 10 + 1.5f;
                    var blurRadius = 20;
                    blurRadius /= div;
                    r = (int) blurRadius;

                    float[] val = new float[_zoneMaxType + 1];
                    for (var iy = i - r; iy < i + r + 1; iy++)
                        for (var ix = j - r; ix < j + r + 1; ix++)
                        {
                            var x = Math.Min(w - 1, Math.Max(0, ix));
                            var y = Math.Min(h - 1, Math.Max(0, iy));
                            val = Add(val, scl[x, y]);
                        }

                    tcl[j, i] = Mult(1f / ((r + r + 1) * (r + r + 1)), val);
                }
        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        private static Vector2 Convert(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }

        private static float[] Add(float[] a, float[] b)
        {
            Assert.AreEqual(a.Length, b.Length);

            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] + b[i];

            return result;
        }

        private static float[] Substract(float[] a, float[] b)
        {
            Assert.AreEqual(a.Length, b.Length);

            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] - b[i];

            return result;
        }

        private static float[] Mult(float a, float[] b)
        {
            var result = new float[b.Length];
            for (int i = 0; i < b.Length; i++)
                result[i] = a * b[i];

            return result;
        }

    }


}
