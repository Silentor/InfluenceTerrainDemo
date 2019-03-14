using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Micro
{
    /// <summary>
    /// Chunk-based grid-map of land, based on <see cref="MacroMap"/>
    /// </summary>
    public sealed class MicroMap : BaseBlockMap
    {
        public readonly Cell[] Cells;
        public IEnumerable<BaseBlockMap> Childs => _childs;

        public MicroMap(MacroMap macromap, TriRunner settings)
        {
            Bounds = (Bounds2i)macromap.Bounds;

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

        

        public void AddChild(ObjectMap childMap)
        {
            _childs.Add(childMap);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            return Cells.First(c => c.Macro == cell);
        }

        /// <summary>
        /// Dig sphere from the ground (do not touch base layer)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="radius"></param>
        public void DigSphere(Vector3 position, float radius)
        {
            //Get influenced vertices
            var flatPosition = (OpenTK.Vector2)position;
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
                var blockWasModified = false;
                var localPos = blockPosition - Bounds.Min;
                var block = _blocks[localPos.X, localPos.Z];

                if (block.Ground != BlockType.Empty
                    && !_heightMap[localPos.X, localPos.Z].IsMainLayerPresent
                    && !_heightMap[localPos.X, localPos.Z + 1].IsMainLayerPresent
                    && !_heightMap[localPos.X + 1, localPos.Z].IsMainLayerPresent
                    && !_heightMap[localPos.X + 1, localPos.Z + 1].IsMainLayerPresent)
                {
                    //block.Layer1 = BlockType.Empty;
                    blockWasModified = true;
                }

                if (block.Underground != BlockType.Empty
                    && !_heightMap[localPos.X, localPos.Z].IsUndergroundLayerPresent
                    && !_heightMap[localPos.X, localPos.Z + 1].IsUndergroundLayerPresent
                    && !_heightMap[localPos.X + 1, localPos.Z].IsUndergroundLayerPresent
                    && !_heightMap[localPos.X + 1, localPos.Z + 1].IsUndergroundLayerPresent)
                {
                    //block.Underground = BlockType.Empty;
                    blockWasModified = true;
                }

                if (blockWasModified)
                {
                    _blocks[localPos.X, localPos.Z] = block;
                    modifiedBlocksCounter++;
                }
            }

            Debug.LogFormat("Dig, modified {0} vertices and {1} blocks", vertexCounter, modifiedBlocksCounter);
            DebugExtension.DebugWireSphere(position, radius, 1);

            DoChanged();
        }

        public void Build(Vector3 position, float radius)
        {
            //Get influenced vertices
            var flatPosition = (OpenTK.Vector2)position;
            var flatBound = new Box2(flatPosition.X - radius, flatPosition.Y + radius, flatPosition.X + radius, flatPosition.Y - radius);
            var sqrRadius = radius * radius;
            var flatVertices = Rasterization.ConvexToBlocks(v => OpenTK.Vector2.DistanceSquared(v, flatPosition) < sqrRadius, flatBound);

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
                    _heightMap[localPos.X, localPos.Z] = _heightMap[localPos.X, localPos.Z].Build(catheti2);
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

                if (_heightMap[localPos.X, localPos.Z].IsMainLayerPresent
                    || _heightMap[localPos.X, localPos.Z + 1].IsMainLayerPresent
                    || _heightMap[localPos.X + 1, localPos.Z].IsMainLayerPresent
                    || _heightMap[localPos.X + 1, localPos.Z + 1].IsMainLayerPresent)
                {
                    var block = _blocks[localPos.X, localPos.Z];
                    //block.Layer1 = BlockType.Grass;
                    _blocks[localPos.X, localPos.Z] = block;
                    modifiedBlocksCounter++;
                }
            }

            Debug.LogFormat("Build, modified {0} vertices and {1} blocks", vertexCounter, modifiedBlocksCounter);
            DebugExtension.DebugWireSphere(position, radius, 1);

            DoChanged();
        }

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
        
    }

    public readonly struct NeighborBlocks
    {
        public readonly Blocks Forward;
        public readonly Blocks Right;
        public readonly Blocks Back;
        public readonly Blocks Left;

        public Blocks this[Side2d dir]
        {
            get
            {
                switch (dir)
                {
                    case Side2d.Forward: return Forward;
                    case Side2d.Right: return Right;
                    case Side2d.Back: return Back;
                    case Side2d.Left: return Left;
                    default: throw new ArgumentOutOfRangeException(nameof(dir));
                }
            }
        }

        public NeighborBlocks(in Blocks forward, in Blocks right, in Blocks back, in Blocks left)
        {
            Forward = forward;
            Right = right;
            Back = back;
            Left = left;
        }
    }
}
