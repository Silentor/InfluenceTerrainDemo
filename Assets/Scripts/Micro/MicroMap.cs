using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using JetBrains.Annotations;
using OpenToolkit.Mathematics;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector2i = TerrainDemo.Spatial.Vector2i;
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

        public IEnumerable<Actor> Actors => _actors;
       

        public MicroMap(MacroMap macromap, TriRunner settings) : base("MicroMap", (Bounds2i)macromap.Bounds)
        {
            _macromap = macromap;
            _settings = settings;

            Cells = new Cell[macromap.Cells.Count];
            for (var i = 0; i < macromap.Cells.Count; i++)
            {
                var macroCell = macromap.Cells[i];
                var microCell = new Cell(macroCell, this);
                Cells[i] = microCell;
            }

            foreach (var blockSettingse in settings.AllBlocks) 
	            _blockSettings[blockSettingse.Block] = blockSettingse;
        }

        public void AddChild(ObjectMap childMap)
        {
            _childs.Add(childMap);
        }

        public void AddActor(Actor actor)
        {
            _actors.Add(actor);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            return Cells.First(c => c.Macro == cell);
        }

        public Cell GetCell(GridPos position)
        {
            foreach (var cell in Cells)
                if (cell.Bounds.Contains(position) && cell.BlockPositions.Contains(position))
                        return cell;

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
            var flatPosition = (Vector2)position;
            var flatBound = new Box2(flatPosition.X - radius, flatPosition.Y + radius, flatPosition.X + radius, flatPosition.Y - radius);
            var sqrRadius = radius * radius;
            var flatVertices = Rasterization.ConvexToVertices(v => Vector2.DistanceSquared(v, flatPosition) < sqrRadius, flatBound);

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
            var changedBlocks = new HashSet<GridPos>(flatVertices);
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
            var flatPosition = (Vector2)position;
            var flatBound = new Box2(flatPosition.X - radius, flatPosition.Y + radius, flatPosition.X + radius, flatPosition.Y - radius);
            var sqrRadius = radius * radius;
            var flatVertices = Rasterization.ConvexToBlocks(v => Vector2.DistanceSquared(v, flatPosition) < sqrRadius, flatBound);

            //Modify vertices
            var vertexCounter = 0;
            foreach ( var fv in flatVertices )
            {
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
            var changedBlocks = new HashSet<GridPos>(flatVertices);
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

        /// <summary>
        /// Generate heightmap from blockmap
        /// </summary>
        public override void GenerateHeightmap()
        {
            return;

            /*
            var timer = Stopwatch.StartNew();

            //Local space iteration
            for (int x = 0; x < Bounds.Size.X + 1; x++)
                for (int z = 0; z < Bounds.Size.Z + 1; z++)
                {
                    //Get neighbor blocks for given vertex
                    var neighborBlockX0 = Math.Max(x - 1, 0);
                    var neighborBlockX1 = Math.Min(x, Bounds.Size.X - 1);
                    var neighborBlockZ0 = Math.Max(z - 1, 0);
                    var neighborBlockZ1 = Math.Min(z, Bounds.Size.Z - 1);

                    ref readonly var b00 = ref _blocks[neighborBlockX0, neighborBlockZ0];
                    ref readonly var b10 = ref _blocks[neighborBlockX1, neighborBlockZ0];
                    ref readonly var b01 = ref _blocks[neighborBlockX0, neighborBlockZ1];
                    ref readonly var b11 = ref _blocks[neighborBlockX1, neighborBlockZ1];

                    //Trivial vertex height calculation
                    var heightAcc = OpenToolkit.Mathematics.Vector3.Zero;
                    int blockCounter = 0;
                    if (!b00.IsEmpty)
                    {
                        heightAcc += (OpenToolkit.Mathematics.Vector3)b00.Height;
                        blockCounter++;
                    }

                    if (!b01.IsEmpty)
                    {
                        heightAcc += (OpenToolkit.Mathematics.Vector3)b01.Height;
                        blockCounter++;
                    }

                    if (!b10.IsEmpty)
                    {
                        heightAcc += (OpenToolkit.Mathematics.Vector3)b10.Height;
                        blockCounter++;
                    }

                    if (!b11.IsEmpty)
                    {
                        heightAcc += (OpenToolkit.Mathematics.Vector3)b11.Height;
                        blockCounter++;
                    }

                    if (blockCounter > 0)
                    {
                        heightAcc = heightAcc / blockCounter;
                        _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                    }

                }

            timer.Stop();
            Debug.Log($"Heightmap of {Name} generated in {timer.ElapsedMilliseconds}");

            GenerateDataMap();
            */
        }

        public void SetOverlapState(GridPos worldPosition, ObjectMap childMap, BlockOverlapState state)
        {
            var localPos = World2Local(worldPosition);
            var objectMapId = _childs.IndexOf(childMap);
            ref var block = ref _blocks[localPos.X, localPos.Z];
            block = block.MutateOverlapState(objectMapId, state);
        }

        public override (ObjectMap map, BlockOverlapState state) GetOverlapState(GridPos worldPosition)
        {
            if (!Bounds.Contains(worldPosition))
                return (null, BlockOverlapState.None);

            var localPos = World2Local(worldPosition);
            ref readonly var blocks = ref _blocks[localPos.X, localPos.Z];
            return GetOcclusionState(in blocks);
        }

        public BlockSettings GetBlockSettings(BlockType type)
        {
            return _blockSettings[type];
        }

        public override string ToString()
        {
            return "Main map";
        }

        public void Update( float deltaTime )
        {
	        foreach ( var actor in _actors )
	        {
		        actor.Update( deltaTime );
	        }
        }

        private readonly MacroMap _macromap;
        private readonly TriRunner _settings;
        private readonly Dictionary<BlockType, BlockSettings> _blockSettings = new Dictionary<BlockType, BlockSettings>();


    }

    /// <summary>
    /// 
    /// </summary>
    public enum BlockOverlapState
    {
        /// <summary>
        /// There is no other block to intersects with this block
        /// </summary>
        None,
        /// <summary>
        /// Some other block completely under this block
        /// </summary>
        Under,                    
        /// <summary>
        /// Some other block overlap this block
        /// </summary>
        Overlap,                     
        /// <summary>
        /// Some other block completely above this block
        /// </summary>
        Above,                    
    }
}
