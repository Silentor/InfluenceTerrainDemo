using System;
using System.Collections.Generic;
using System.Linq;
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
    public class MicroMap
    {
        public readonly Bounds2i Bounds;
        public readonly Cell[] Cells;
        public IEnumerable<MicroMap> Childs => _childs;

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

        public MicroMap(Bounds2i bounds)
        {
            Bounds = bounds;

            _heightMap = new Heights[Bounds.Size.X + 1, Bounds.Size.Z + 1];
            _blocks = new Blocks[Bounds.Size.X, Bounds.Size.Z];

            Debug.LogFormat("Generated micromap {0} x {1} = {2} blocks", Bounds.Size.X, Bounds.Size.Z, Bounds.Size.X * Bounds.Size.Z);
        }

        public void AddChild(MicroMap childMap)
        {
            if(childMap == this) throw new ArgumentException();

            _childs.Add(childMap);
        }

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            return Cells.First(c => c.Macro == cell);
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

        /// <summary>
        /// Generate heightmap from blockmap
        /// </summary>
        public void GenerateHeightmap()
        {
            for (int z = 0; z < _heightMap.GetLength(1); z++)
                for (int x = 0; x < _heightMap.GetLength(0); x++)
                {
                    //Get neighbor blocks for given vertex
                    var neighborBlockX0 = x - 1;
                    var neighborBlockX1 = x;
                    var neighborBlockZ0 = z - 1;
                    var neighborBlockZ1 = z;

                    if (neighborBlockX0 < 0)
                        neighborBlockX0 = 0;
                    if (neighborBlockZ0 < 0)
                        neighborBlockZ0 = 0;
                    if (neighborBlockX1 >= _blocks.GetLength(0))
                        neighborBlockX1 = _blocks.GetLength(0) - 1;
                    if (neighborBlockZ1 >= _blocks.GetLength(1))
                        neighborBlockZ1 = _blocks.GetLength(1) - 1;

                    ref readonly var b00 = ref _blocks[neighborBlockX0, neighborBlockZ0];
                    ref readonly var b10 = ref _blocks[neighborBlockX1, neighborBlockZ0];
                    ref readonly var b01 = ref _blocks[neighborBlockX0, neighborBlockZ1];
                    ref readonly var b11 = ref _blocks[neighborBlockX1, neighborBlockZ1];

                    if (b00.IsEmpty && b10.IsEmpty && b01.IsEmpty && b11.IsEmpty)
                        continue;

                    //Trivial vertex height calculation
                    float topmostHeight = 0f;
                    int blockCounter = 0;
                    if (!b00.IsEmpty)
                    {
                        topmostHeight += b00.GetNominalHeight();
                        blockCounter++;
                    }

                    if (!b01.IsEmpty)
                    {
                        topmostHeight += b01.GetNominalHeight();
                        blockCounter++;
                    }

                    if (!b10.IsEmpty)
                    {
                        topmostHeight += b10.GetNominalHeight();
                        blockCounter++;
                    }

                    if (!b11.IsEmpty)
                    {
                        topmostHeight += b11.GetNominalHeight();
                        blockCounter++;
                    }

                    topmostHeight = topmostHeight / blockCounter;
                    _heightMap[x, z] = new Heights(topmostHeight, topmostHeight, topmostHeight);

                }
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

        public ref readonly Heights GetHeightRef(Vector2i worldPos)
        {
            var localPos = worldPos - Bounds.Min;

            if (localPos.X >= 0 && localPos.X < _heightMap.GetLength(0)
               && localPos.Z >= 0 && localPos.Z < _heightMap.GetLength(1))
                return ref _heightMap[localPos.X, localPos.Z];

            return ref Heights.Empty;
        }

        public Blocks[,] GetBlockMap()
        {
            return _blocks;
        }

        public Blocks[,] GetBlockMapRegion(Bounds2i bounds, Func<Blocks, Blocks> transform = null)
        {
            var result = new Blocks[bounds.Size.X, bounds.Size.Z];

            //Validate input bound
            bounds = Bounds.Intersect(bounds);
            var diffX = bounds.Min.X - Bounds.Min.X;
            var diffZ = bounds.Min.Z - Bounds.Min.Z;

            //Copy blocks
            if (transform == null)
            {
                for (int z = 0; z < bounds.Size.Z; z++)
                    for (int x = 0; x < bounds.Size.X; x++)
                    {
                        var mapLocalX = x + diffX;
                        var mapLocalZ = z + diffZ;
                        result[x, z] = _blocks[mapLocalX, mapLocalZ];
                    }
            }
            else
            {
                for (int z = 0; z < bounds.Size.Z; z++)
                    for (int x = 0; x < bounds.Size.X; x++)
                    {
                        var mapLocalX = x + diffZ;
                        var mapLocalZ = z + diffZ;
                        result[x, z] = transform(_blocks[mapLocalX, mapLocalZ]);
                    }
            }

            return result;
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

        public ref readonly Blocks GetBlockRef(Vector2i worldBlockPos)
        {
            if (!Bounds.Contains(worldBlockPos))
                return ref Blocks.Empty;

            var localPos = worldBlockPos - Bounds.Min;
            return ref _blocks[localPos.X, localPos.Z];
        }

        public NeighborBlocks GetNeighborBlocks(Vector2i worldBlockPos)
        {
            //Blocks forward = Blocks.Empty, right = Blocks.Empty, back = Blocks.Empty, left = Blocks.Empty;
            var localPos = worldBlockPos - Bounds.Min;

            return new NeighborBlocks(
                Bounds.Contains(worldBlockPos + Vector2i.Forward)
                    ? _blocks[localPos.X, localPos.Z + 1]
                    : Blocks.Empty,
                Bounds.Contains(worldBlockPos + Vector2i.Right)
                    ? _blocks[localPos.X + 1, localPos.Z]
                    : Blocks.Empty,
                Bounds.Contains(worldBlockPos + Vector2i.Back)
                    ? _blocks[localPos.X, localPos.Z - 1]
                    : Blocks.Empty,
                Bounds.Contains(worldBlockPos + Vector2i.Left)
                    ? _blocks[localPos.X - 1, localPos.Z]
                    : Blocks.Empty
                );
        }

        public (float distance, Vector2i position)? RaycastBlockmap(Ray ray)
        {
            //Get raycasted blocks
            var raycastedBlocks = Rasterization.DDA((Vector2)ray.origin, (Vector2)ray.GetPoint(300), true);

            var blockVolumes = from blockPos in raycastedBlocks
                               let block = GetBlockRef(blockPos)   //todo use ref
                               where !block.IsEmpty
                               select (blockPos, new Interval(block.Height.Base - 10, block.Height.Main));

            var result = Intersections.RayBlockIntersection(ray, blockVolumes);
            return result;
        }

        public (float distance, Vector2i position, MicroMap source)? RaycastBlockmap2(Ray ray)
        {
            //Get raycasted blocks
            var raycastedBlocks = Rasterization.DDA((Vector2)ray.origin, (Vector2)ray.GetPoint(300), true);

            MicroMap source = null;
            float resultDistance = -1;
            foreach (var position in raycastedBlocks)
            {
                ref readonly var testedBlock = ref GetBlockRef(position);
                if(testedBlock.IsEmpty) continue;

                var distance = Intersections.RayBlockIntersection(ray, in position, in testedBlock);
                if (distance > 0)
                {
                    resultDistance = distance;
                    source = this;
                }

                //Check map objects
                foreach (var childMap in _childs)
                {
                    testedBlock = ref childMap.GetBlockRef(position);
                    if(testedBlock.IsEmpty) continue;

                    var childDistance = Intersections.RayBlockIntersection(ray, in position, in testedBlock);
                    if (childDistance > 0 && (resultDistance < 0 || childDistance < resultDistance))
                    {
                        resultDistance = childDistance;
                        source = childMap;
                    }
                }

                if (resultDistance > 0)
                {
                    return (resultDistance, position, source);
                }
            }

            return null;
        }

        public (Vector3 hitPoint, Vector2i position)? RaycastHeightmap(Ray ray)
        {
            //Get raycasted blocks
            var rayBlocks = Rasterization.DDA((OpenTK.Vector2)ray.origin, (OpenTK.Vector2)ray.GetPoint(300), true);

            //Test each block for ray intersection
            //todo Implement some more culling
            //todo Early discard block based on block height (blocks AABB?)
            foreach (var blockPos in rayBlocks)
            {
                if (Bounds.Contains(blockPos))
                {
                    var localPos = blockPos - Bounds.Min;
                    ref readonly var block = ref _blocks[localPos.X, localPos.Z];

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
                            return (hit, blockPos);
                        }
                    }
                    else
                    {
                        if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                            || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                        {
                            return (hit, blockPos);
                        }
                    }
                }
            }

            return null;
        }

        public (Vector3 hitPoint, Vector2i position, MicroMap source)? RaycastHeightmap2(Ray ray)
        {
            //Get raycasted blocks
            var rayBlocks = Rasterization.DDA((OpenTK.Vector2)ray.origin, (OpenTK.Vector2)ray.GetPoint(300), true);

            float distance = float.MaxValue;
            MicroMap source = null;

            //Test each block for ray intersection
            //todo Implement some more culling
            //todo Early discard block based on block height (blocks AABB?)
            foreach (var blockPos in rayBlocks)
            {
                var result = CheckBlockIntersection(this, blockPos);
                if (result.HasValue)
                {
                    distance = Vector3.SqrMagnitude(result.Value);
                    source = this;
                }

                foreach (var child in _childs)
                {
                    var childResult = CheckBlockIntersection(child, blockPos);
                    if (childResult.HasValue)
                    {
                        if (Vector3.SqrMagnitude(childResult.Value) < distance)
                        {
                            distance = Vector3.SqrMagnitude(childResult.Value);
                            result = childResult;
                            source = child;
                        }
                    }
                }

                if (result.HasValue)
                {
                    return (result.Value, blockPos, source);
                }
            }

            return null;

            Vector3? CheckBlockIntersection(MicroMap map, in Vector2i position)
            {
                if (map.Bounds.Contains(position))
                {
                    var localPos = position - map.Bounds.Min;
                    //ref readonly var block = ref map._blocks[localPos.X, localPos.Z];

                    var c00 = map._heightMap[localPos.X, localPos.Z].Nominal;
                    var c01 = map._heightMap[localPos.X, localPos.Z + 1].Nominal;
                    var c11 = map._heightMap[localPos.X + 1, localPos.Z + 1].Nominal;
                    var c10 = map._heightMap[localPos.X + 1, localPos.Z].Nominal;

                    var v00 = new Vector3(position.X, c00, position.Z);
                    var v01 = new Vector3(position.X, c01, position.Z + 1);
                    var v11 = new Vector3(position.X + 1, c11, position.Z + 1);
                    var v10 = new Vector3(position.X + 1, c10, position.Z);

                    //Check block's triangles for intersection
                    Vector3 hit;
                    if (Math.Abs(c00 - c11) < Math.Abs(c01 - c10))
                    {
                        if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out hit) == 1
                            || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out hit) == 1)
                        {
                            return hit;
                        }
                    }
                    else
                    {
                        if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                            || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                        {
                            return hit;
                        }
                    }
                }

                return null;
            }
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
        private readonly List<MicroMap> _childs = new List<MicroMap>();
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
