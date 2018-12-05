﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using TerrainDemo.Tri;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Chunk-based grid-map of land, based on <see cref="MacroMap"/>
    /// </summary>
    public class MicroMap
    {
        public readonly Bounds2i Bounds;
        public readonly Cell[] Cells;

        public MicroMap(MacroMap macromap, TriRunner settings)
        {
            Bounds = (Bounds2i) macromap.Bounds;

            _macromap = macromap;
            _settings = settings;

            Cells = new Cell[macromap.Cells.Count];
            for (var i = 0; i < macromap.Cells.Count; i++)
            {
                var macroCell = macromap.Cells[i];
                var microCell = new Cell(macroCell, this);
                Cells[i] = microCell;
            }

            _heightMap = new Heights[Bounds.Size.X + 1, Bounds.Size.Z + 1];
            _blocks = new Blocks[Bounds.Size.X, Bounds.Size.Z];

            Debug.LogFormat("Generated micromap {0} x {1} = {2} blocks", Bounds.Size.X, Bounds.Size.Z, Bounds.Size.X * Bounds.Size.Z);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if(cell.Map != _macromap) throw new ArgumentException();

            return Cells[_macromap.Cells.IndexOf(cell)];
        }

        public void SetHeights(IEnumerable<Vector2i> positions, IEnumerable<Heights> heights)
        {
            var posEnumerator = positions.GetEnumerator();
            var infEnumerator = heights.GetEnumerator();
            using (posEnumerator)
            {
                using (infEnumerator)
                {
                    while (posEnumerator.MoveNext() & infEnumerator.MoveNext())
                    {
                        var arrayX = posEnumerator.Current.X - Bounds.Min.X;
                        var arrayZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _heightMap[arrayX, arrayZ] = infEnumerator.Current;
                    }
                }
            }

            Changed();
        }

        public void SetBlocks(IEnumerable<Vector2i> positions, IEnumerable<Blocks> blocks)
        {
            var posEnumerator = positions.GetEnumerator();
            var blockEnumerator = blocks.GetEnumerator();
            using (posEnumerator)
            {
                using (blockEnumerator)
                {
                    while (posEnumerator.MoveNext() & blockEnumerator.MoveNext())
                    {
                        var localX = posEnumerator.Current.X - Bounds.Min.X;
                        var localZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _blocks[localX, localZ] = blockEnumerator.Current;
                    }
                }
            }

            Changed();
        }

        public Heights[,] GetHeightMap()
        {
            return _heightMap;
        }

        public Blocks[,] GetBlockMap()
        {
            return _blocks;
        }

        public BlockInfo GetBlock(Vector2i blockPos)
        {
            if (!Bounds.Contains(blockPos))
                return null;

            var localPos = blockPos - Bounds.Min;
            var result = new BlockInfo(blockPos, _blocks[localPos.X, localPos.Z], 
                _heightMap[localPos.X, localPos.Z],
                _heightMap[localPos.X, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z]);
            return result;
        }

        public ValueTuple<Vector3, Vector2i>? Raycast(Ray ray)
        {
            //Get raycasted blocks
            var rayBlocks = Rasterization.DDA((OpenTK.Vector2)ray.origin, (OpenTK.Vector2)ray.GetPoint(300), true);

            //Test each block for ray intersection
            //todo Implement some more culling
            foreach (var blockPos in rayBlocks)
            {
                if (Bounds.Contains(blockPos))
                {
                    var localPos = blockPos - Bounds.Min;
                    var c00 = _heightMap[localPos.X, localPos.Z].Nominal;
                    var c01 = _heightMap[localPos.X, localPos.Z + 1].Nominal;
                    var c11 = _heightMap[localPos.X + 1, localPos.Z + 1].Nominal;
                    var c10 = _heightMap[localPos.X + 1, localPos.Z].Nominal;
                    var v00 = new Vector3(blockPos.X, c00, blockPos.Z);
                    var v01 = new Vector3(blockPos.X, c01, blockPos.Z + 1);
                    var v11 = new Vector3(blockPos.X + 1, c11, blockPos.Z + 1);
                    var v10 = new Vector3(blockPos.X + 1, c10, blockPos.Z);

                    //Check block's triangles for intersection
                    Vector3 hit;
                    if (Math.Abs(c00 - c11) < Math.Abs(c01 - c10))
                    {
                        if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out hit) == 1
                            || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out hit) == 1)
                        {
                            return new ValueTuple<Vector3, Vector2i>(hit, blockPos);
                        }
                    }
                    else
                    {
                        if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                            || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                        {
                            return new ValueTuple<Vector3, Vector2i>(hit, blockPos); 
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Dig sphere from the ground (do not touch base layer)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        public void DigSphere(Vector3 position, float radius)
        {
            //Get influenced vertices
            var flatPosition = (OpenTK.Vector2) position;
            var flatBound = new Box2(flatPosition.X - radius, flatPosition.Y + radius, flatPosition.X + radius, flatPosition.Y - radius);
            var sqrRadius = radius * radius;
            var flatVertices = Rasterization.ConvexToVertices(v => OpenTK.Vector2.DistanceSquared(v, flatPosition) < sqrRadius, flatBound);

            //Modify vertices
            var vertexCounter = 0;
            for (int i = 0; i < flatVertices.Length; i++)
            {
                var fv = flatVertices[i];
                var localPos = fv - Bounds.Min;
                var h = _heightMap[localPos.X, localPos.Z];
                var vertex = new Vector3(fv.X, h.Nominal, fv.Z);
                if (Vector3.SqrMagnitude(vertex - position) < sqrRadius)
                {
                    //Right triangle
                    var catheti = Vector2.DistanceSquared(flatPosition, fv);
                    var hypotenuse = sqrRadius;
                    var catheti2 = Mathf.Sqrt(hypotenuse - catheti);
                    _heightMap[localPos.X, localPos.Z] = h.Dig(catheti2);
                    vertexCounter++;
                }
            }
            
            //Update adjoined blocks
            var changedBlocks = new HashSet<Vector2i>(flatVertices);
            foreach (var vertex in flatVertices)
            {
                //Add adjoined blocks (except vertex own block)
                changedBlocks.Add(vertex - Vector2i.Forward);
                changedBlocks.Add(vertex - Vector2i.Right);
                changedBlocks.Add(vertex - Vector2i.One);
            }

            var modifiedBlocksCounter = 0;
            foreach (var blockPosition in changedBlocks)
            {
                var localPos = blockPosition - Bounds.Min;
                var block = _blocks[localPos.X, localPos.Z];

                if (!_heightMap[localPos.X, localPos.Z].IsLayer1Present
                    && !_heightMap[localPos.X, localPos.Z + 1].IsLayer1Present
                    && !_heightMap[localPos.X + 1, localPos.Z].IsLayer1Present
                    && !_heightMap[localPos.X + 1, localPos.Z + 1].IsLayer1Present)
                {
                    block.Layer1 = BlockType.Empty;
                    _blocks[localPos.X, localPos.Z] = block;
                    modifiedBlocksCounter++;
                }
            }

            Debug.LogFormat("Dig, modified {0} vertices and {1} blocks", vertexCounter, modifiedBlocksCounter);
            DebugExtension.DebugWireSphere(position, radius, 1);
            
            Changed();
        }

        public void Build(Vector3 position, float radius)
        {
            //Get influenced vertices
            var flatPosition = (OpenTK.Vector2)position;
            var flatBound = new Box2(flatPosition.X - radius, flatPosition.Y + radius, flatPosition.X + radius, flatPosition.Y - radius);
            var sqrRadius = radius * radius;
            var flatVertices = Rasterization.ConvexToBlocks(v => OpenTK.Vector2.DistanceSquared(v, flatPosition) < sqrRadius, flatBound);

            //Modify vertices
            var counter = 0;
            for (int i = 0; i < flatVertices.Length; i++)
            {
                var fv = flatVertices[i];
                var localPos = fv - Bounds.Min;
                var h = _heightMap[localPos.X, localPos.Z];
                var vertex = new Vector3(fv.X, h.Nominal, fv.Z);
                if (Vector3.SqrMagnitude(vertex - position) < sqrRadius)
                {
                    //Right triangle
                    var catheti = Vector2.DistanceSquared(flatPosition, fv);
                    var hypotenuse = sqrRadius;
                    var catheti2 = Mathf.Sqrt(hypotenuse - catheti);
                    _heightMap[localPos.X, localPos.Z] = new Heights(h.BaseHeight, position.y + catheti2);
                    counter++;
                }
            }

            Debug.LogFormat("Build, modified {0} vertices", counter);
            DebugExtension.DebugWireSphere(position, radius, 1);

            Changed();
        }



        public event Action Changed = delegate { };

        /*
        /// <summary>
        /// Clockwise
        /// </summary>
        /// <param name="blockPos"></param>
        /// <returns></returns>
        public ValueTuple<MicroHeight, MicroHeight, MicroHeight, MicroHeight> GetBlockVertices(Vector2i blockPos)
        {
            var localPos = blockPos - Bounds.Min;

            return new ValueTuple<MicroHeight, MicroHeight, MicroHeight, MicroHeight>(
                _heightMap[localPos.X, localPos.Z],
                _heightMap[localPos.X, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z]);
        }
        */

        private readonly MacroMap _macromap;
        private readonly TriRunner _settings;
        private readonly Heights[,] _heightMap;
        private readonly Blocks[,] _blocks;
        //private readonly double[,][] _influences;
    }
}