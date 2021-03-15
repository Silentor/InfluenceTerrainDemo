using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Generators;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Quaternion = OpenToolkit.Mathematics.Quaternion;
using Random = TerrainDemo.Tools.Random;
using Ray = TerrainDemo.Spatial.Ray;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Vector4 = OpenToolkit.Mathematics.Vector4;

#nullable enable

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Сетка макроячеек, вершин и сторон
    /// </summary>
    public class MacroMap
    {
        public readonly Box2                    Bounds;
        public          IReadOnlyList<Cell>     Cells => _mesh.GetCellsValue( );
        public readonly List<Zone>              Zones      = new List<Zone>();

        public Macro.Cell? GetCell( HexPos position ) => _mesh[position];

        public MacroMap(TriRunner settings, Random random)
        {
            _settings = settings;
            _random = random;
            _side = settings.CellSide;

            _mesh = new MacroGrid( _settings.CellSide, (int)_settings.LandSize );

            Bounds = (Box2)_mesh.Bound;

            EmptyInfluence = new double[_settings.Biomes.Length];

            _influenceTurbulance = new FastNoise(random.Seed);
            _influenceTurbulance.SetFrequency(settings.InfluencePerturbFreq);
            _influenceTurbulancePower = settings.InfluencePerturbPower;
        }

        public Cell AddCell( HexPos position, Zone zone  )
        {
	        var newCell = new Cell( position, zone, _mesh );
	        _mesh[position] = newCell;
	        return newCell;
        }

        public void FillVertices( )
        {
            _mesh.EnumerateVertices( v =>
                                     {
                                         v.Data = new MacroVert( this, _mesh, v );
                                     } );
        }
            
        
        public Influence GetInfluence(Vector2 worldPosition)
        {
            return GetIDWInfluence(worldPosition);
        }

        public Heights GetHeight(Vector2 worldPosition)
        {
            return GetIDWHeight(worldPosition);
        }

        public Cell? GetCellAt(Vector2 position)
        {
	        var gridPos = (GridPos) position;
	        var hexPos = _mesh.BlockToHex( gridPos );

	        if ( _mesh.IsContains( hexPos ) )
		        return _mesh[hexPos];
	        else
	        {
		        return null;
	        }
        }

        //public ValueTuple<Cell, Vector3> Raycast(Ray ray)
        //{
        //    foreach (var cell in Cells.OrderBy(c => Vector3.DistanceSquared(c.CenterPoint, ray.Origin)))
        //    {
        //        var intersection = cell.Raycast(ray);
        //        if (intersection.HasValue)
        //        {
        //            return new ValueTuple<Cell, Vector3>(cell, intersection.Value);
        //        }
        //    }

        //    return new ValueTuple<Cell, Vector3>(null, Vector3.Zero);
        //}

        public IEnumerable<Cell> FloodFill(HexPos startCell, MacroGrid.CheckCellPredicate fillCondition = null)
        {
            return _mesh.FloodFill(startCell, fillCondition ).Select ( hex =>_mesh[hex] );
        }

        public MacroGrid.Cluster GetSubmesh( IEnumerable<HexPos> cells )
        {
	        return _mesh.GetCluster( cells );
        }

        public string InfluenceToString(double[] influence)
        {
            return $"[{influence.ToJoinedString(i => i.ToString("F2"))}]";
        }

        public string InfluenceToBiomesString(double[] influence)
        {
            var biomes = from biome in _settings.Biomes
                where influence[biome.Index] > 0
                select $"{biome.name}: {influence[biome.Index]:G3}";
            return string.Join(", ", biomes.ToArray());
        }

        private readonly TriRunner _settings;
        private readonly Random _random;
        private readonly FastNoise _influenceTurbulance;
        private readonly float _influenceTurbulancePower;
        private readonly float _side;

        private alglib.kdtree _idwInfluence;
        private int[] _nearestCellsTags = new int[0];
        private readonly double[] EmptyInfluence;
        private readonly List<(Cell, float)> _getInfluenceBuffer = new List<(Cell, float)>();
        private readonly MacroGrid _mesh;

        private Vector2 PerturbGridPoint(Vector2 point, FastNoise gridPerturbator)
        {
            var xPerturb = (float)gridPerturbator.GetSimplex(point.X, point.Y) * _settings.GridPerturbPower;
            var zPerturb = (float)gridPerturbator.GetSimplex(point.X + 1000, point.Y - 1000) * _settings.GridPerturbPower;
            return point + new Vector2(xPerturb, zPerturb);
        }

        private void PrepareLandIDW()
        {
            if (_idwInfluence == null)
            {
                var positions = new double[_mesh.CellsCount, 2];
                var tags = new int[_mesh.CellsCount];

                for (int i = 0; i < _mesh.CellsCount; i++)
                {
                    positions[i, 0] = Cells[i].Center.X;
                    positions[i, 1] = Cells[i].Center.Y;
                    tags[i] = i;
                }

                alglib.kdtreebuildtagged(positions, tags, _mesh.CellsCount, 2, 0, 2, out _idwInfluence);
            }
        }

        private Heights GetIDWHeight(Vector2 worldPosition)
        {
            PrepareLandIDW();

            const float searchRadius = 25f;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);

            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            //Calculate height in the point
            Vector3d[] cellsHeights = new Vector3d[nearestCellsCount];
            double[] cellsWeights = new double[nearestCellsCount];
            double weightsSum = 0;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];

                cellsHeights[i] = (Vector3d)cell.DesiredHeight;
                cellsWeights[i] = IDWLocalShepard2(cell.Center, worldPosition, searchRadius);
                weightsSum += cellsWeights[i];
            }

            var result = Vector3d.Zero;
            for (int i = 0; i < nearestCellsCount; i++)
            {
                result += cellsHeights[i] * (cellsWeights[i] / weightsSum);
            }

            return (Heights)result;
        }

        private Influence GetIDWInfluence(Vector2 worldPosition)
        {
            PrepareLandIDW();

            //Turbulate position
            worldPosition = new Vector2(
                worldPosition.X + (float)_influenceTurbulance.GetSimplex(worldPosition.X, worldPosition.Y) * _influenceTurbulancePower, 
                worldPosition.Y + (float)_influenceTurbulance.GetSimplex(worldPosition.X + 1000, worldPosition.Y - 1000) * _influenceTurbulancePower);

            const float searchRadius = 25;
            var nearestCellsCount = alglib.kdtreequeryrnn(_idwInfluence, new double[] { worldPosition.X, worldPosition.Y }, searchRadius, true);
            alglib.kdtreequeryresultstags(_idwInfluence, ref _nearestCellsTags);

            //Short path - no cells in search radius, return influence of nearest cell
            if (nearestCellsCount == 0)
            {
                nearestCellsCount = alglib.kdtreequeryknn(_idwInfluence, new double[] {worldPosition.X, worldPosition.Y}, 1, true);
                if (nearestCellsCount > 0)
                {
                    var cell = Cells[_nearestCellsTags[0]];
                    return cell.Zone.Influence;
                }

                throw new InvalidOperationException("Cant calculate influence, no cells found");
            }

            _getInfluenceBuffer.Clear();

            //Sum up zones influence
            for (int i = 0; i < nearestCellsCount; i++)
            {
                var cell = Cells[_nearestCellsTags[i]];
                var cellWeight = IDWLocalShepard(cell.Center, worldPosition, searchRadius);

                //Cells from that bring very low weight
                if(cellWeight < 0.01d)
                    break;

                _getInfluenceBuffer.Add((cell, (float)cellWeight));
            }

            return new Influence(_getInfluenceBuffer);
        }

        private double IDWLocalShepard(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            Assert.IsTrue(searchRadius > 0);

            if (interpolatePoint == point)
                return 1;

            double distance = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(distance <= searchRadius);

            var ratio = 1 - distance / searchRadius;        //Inverse lerp
            return ratio * ratio * ratio * ratio;           //In quintic
        }

        private double IDWLocalShepard2(Vector2 interpolatePoint, Vector2 point, double searchRadius)
        {
            double distance = Vector2.Distance(interpolatePoint, point);

            Assert.IsTrue(distance <= searchRadius);

            var ratio = 1 - distance / searchRadius;        //Inverse lerp
            return Interpolation.SmoothStep(ratio);
            //return InQuad(ratio);
        }

        private Vector4 InfluenceToVector4(double[] influence)
        {
            if(influence == null)
                return Vector4.Zero;

            var result = Vector4.Zero;
            var sum = 0f;
            for (int i = 0; i < influence.Length && i < 4; i++)
            {
                result[i] = (float)influence[i];
                sum += result[i];
            }

            if (sum > 0)
                result /= sum;

            return result;
        }

        private double[] Vector4ToInfluence(Vector4 influence)
        {
            var result = new double[_settings.Biomes.Length];
            var sum = 0d;
            for (int i = 0; i < result.Length && i < 4; i++)
            {
                if (influence[i] < 0)
                    influence[i] = 0;
                result[i] = influence[i];
                sum += result[i];
            }

            if (sum > 0)
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] /= sum;
                }

            return result;
        }

        //Clockwise from "right" vertice (angle = 0)
        private Vector2[] CalcVertsPosition(Vector2 center, double radius)
        {
            var result = new Vector2[6];
            for (int i = 0; i < Cell.MaxNeighborsCount; i++)
            {
                const double deg2rad = Math.PI / 180;
                var angle = deg2rad * (360 - i * 360d / Cell.MaxNeighborsCount);
                result[i] = 
                    new Vector2((float) (center.X + radius * Math.Cos(angle)), (float) (center.Y + radius * Math.Sin(angle)));
            }

            return result;
        }

        private Vector2[] CalcNeighborCellsPosition(Vector2 centerCell, double radius)
        {
            var result = new Vector2[Cell.MaxNeighborsCount];
            for (int i = 0; i < Cell.MaxNeighborsCount; i++)
            {
                const double deg2rad = Math.PI / 180;
                var angle = deg2rad * (90 - (i * 360d / Cell.MaxNeighborsCount));          //Start with Z+ axis (top)
                result[i] = new Vector2((float)(centerCell.X + radius * Math.Cos(angle)), (float)(centerCell.Y + radius * Math.Sin(angle)));
            }

            return result;
        }

    }
}
