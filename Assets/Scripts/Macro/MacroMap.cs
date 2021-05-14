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

namespace TerrainDemo.Macro
{
    /// <summary>
    /// Сетка макроячеек, вершин и сторон
    /// </summary>
    public class MacroMap
    {
        public readonly Box2                    Bounds;
        public          MacroGrid.CellsValue    Cells => _grid.GetCellsValue( );
        public readonly List<Zone>              Zones      = new List<Zone>();

        public Macro.Cell? GetCell( HexPos position ) => _grid[position];

        public MacroMap(TriRunner settings, Random random)
        {
            _settings = settings;
            _random = random;
            _side = settings.CellSide;

            _grid = new MacroGrid( _settings.CellSide, (int)_settings.LandSize );

            Bounds = (Box2)_grid.Bound;

            EmptyInfluence = new double[_settings.Biomes.Length];

            _influenceTurbulance = new FastNoise(random.Seed);
            _influenceTurbulance.SetFrequency(settings.InfluencePerturbFreq);
            _influenceTurbulancePower = settings.InfluencePerturbPower;
        }

        public Cell AddCell( HexPos position, Zone zone  )
        {
	        var newCell = new Cell( position, zone, _grid );
	        _grid[position] = newCell;
	        return newCell;
        }

        public void FillVertices( )
        {
            _grid.EnumerateVertices( v =>
                                     {
                                         v.Data = new MacroVert( this, _grid, v );
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
	        var hexPos = _grid.BlockToHex( gridPos );

	        if ( _grid.IsContains( hexPos ) )
		        return _grid[hexPos];
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
            return _grid.FloodFill(startCell, fillCondition ).Select ( hex =>_grid[hex] );
        }

        public MacroGrid.Cluster GetSubmesh( IEnumerable<HexPos> cells )
        {
	        return _grid.GetCluster( cells );
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
        private readonly MacroGrid _grid;

        private Vector2 PerturbGridPoint(Vector2 point, FastNoise gridPerturbator)
        {
            var xPerturb = (float)gridPerturbator.GetSimplex(point.X, point.Y) * _settings.GridPerturbPower;
            var zPerturb = (float)gridPerturbator.GetSimplex(point.X + 1000, point.Y - 1000) * _settings.GridPerturbPower;
            return point + new Vector2(xPerturb, zPerturb);
        }

        private Heights GetIDWHeight(Vector2 worldPosition)
        {
            const float searchRadius = 25f;
            var nearestCells = _grid.FindNearestNeighbors( worldPosition, searchRadius ).ToArray( );
            
            
            //Calculate height in the point
            Vector3d[] cellsHeights = new Vector3d[nearestCells.Length];
            double[]   cellsWeights = new double[nearestCells.Length];
            double     weightsSum   = 0;
            for (int i = 0; i < nearestCells.Length; i++)
            {
                var cell  = nearestCells[i];
                var cellData = _grid[cell];
                cellsHeights[i] =  (Vector3d)cellData.DesiredHeight;
                cellsWeights[i] =  IDWLocalShepard2( cellData.Center, worldPosition, searchRadius);
                weightsSum      += cellsWeights[i];
            }

            var result = Vector3d.Zero;
            for (int i = 0; i < nearestCells.Length; i++)
            {
                result += cellsHeights[i] * (cellsWeights[i] / weightsSum);
            }

            return (Heights)result;
        }

        private Influence GetIDWInfluence(Vector2 worldPosition)
        {
            //Turbulate position
            worldPosition = new Vector2(
                worldPosition.X + (float)_influenceTurbulance.GetSimplex(worldPosition.X, worldPosition.Y) * _influenceTurbulancePower, 
                worldPosition.Y + (float)_influenceTurbulance.GetSimplex(worldPosition.X + 1000, worldPosition.Y - 1000) * _influenceTurbulancePower);

            const float searchRadius = 25;
            var         nearestCells = _grid.FindNearestNeighbors( worldPosition, searchRadius ).ToArray( );
            
            //Short path - no cells in search radius, return influence of nearest cell
            if (nearestCells.Length == 0)
            {
                // nearestCellsCount = alglib.kdtreequeryknn(_idwInfluence, new double[] {worldPosition.X, worldPosition.Y}, 1, true);
                // if (nearestCellsCount > 0)
                // {
                //     var cell = Cells[_nearestCellsTags[0]];
                //     return cell.Zone.Influence;
                // }
                //
                // throw new InvalidOperationException("Cant calculate influence, no cells found");
                return Influence.Empty;
            }

            _getInfluenceBuffer.Clear();

            //Sum up zones influence
            for (int i = 0; i < nearestCells.Length; i++)
            {
                var cell       = nearestCells[i];
                var cellData   = _grid[cell];
                var cellWeight = IDWLocalShepard(cellData.Center, worldPosition, searchRadius);

                //Cells from that bring very low weight
                if(cellWeight < 0.01d)
                    break;

                _getInfluenceBuffer.Add((cellData, (float)cellWeight));
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
            //return Interpolation.SmoothStep(ratio);
            return Interpolation.InQuad( ratio );
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
