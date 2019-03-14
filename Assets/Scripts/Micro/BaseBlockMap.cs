using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Vector2 = OpenTK.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace TerrainDemo.Micro
{
    public abstract class BaseBlockMap
    {
        public readonly string Name;
        public readonly Bounds2i Bounds;

        protected BaseBlockMap(string name, Bounds2i bounds)
        {
            Name = name;
            Bounds = bounds;

            _heightMap = new Heights[Bounds.Size.X + 1, Bounds.Size.Z + 1];
            _blocks = new Blocks[Bounds.Size.X, Bounds.Size.Z];

            Debug.Log($"Created {Name} blockmap {Bounds.Size.X} x {Bounds.Size.Z} = {Bounds.Size.X * Bounds.Size.Z} blocks");
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

            DoChanged();
        }

        /// <summary>
        /// Generate heightmap from blockmap
        /// </summary>
        public virtual void GenerateHeightmap()
        {
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
                    var heightAcc = OpenTK.Vector3.Zero;
                    int blockCounter = 0;
                    if (!b00.IsEmpty)
                    {
                        heightAcc += (OpenTK.Vector3)b00.Height;
                        blockCounter++;
                    }

                    if (!b01.IsEmpty)
                    {
                        heightAcc += (OpenTK.Vector3)b01.Height;
                        blockCounter++;
                    }

                    if (!b10.IsEmpty)
                    {
                        heightAcc += (OpenTK.Vector3)b10.Height;
                        blockCounter++;
                    }

                    if (!b11.IsEmpty)
                    {
                        heightAcc += (OpenTK.Vector3)b11.Height;
                        blockCounter++;
                    }

                    if (blockCounter > 0)
                    {
                        heightAcc = heightAcc / blockCounter;
                        _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                    }

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

            DoChanged();
        }

        public Heights[,] GetHeightMap()
        {
            return _heightMap;
        }

        public ref readonly Heights GetHeightRef(Vector2i worldPos)
        {
            var localPos = worldPos - Bounds.Min;

            if (localPos.X >= 0 && localPos.X < Bounds.Size.X + 1
                                && localPos.Z >= 0 && localPos.Z < Bounds.Size.Z + 1)
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

        public ref readonly Blocks GetBlockRef(int xWorldBlockPos, int zWorldBlockPos)
        {
            if(xWorldBlockPos < Bounds.Min.X || xWorldBlockPos > Bounds.Max.X || zWorldBlockPos < Bounds.Min.Z || zWorldBlockPos > Bounds.Max.Z)
                return ref Blocks.Empty;

            return ref _blocks[xWorldBlockPos - Bounds.Min.X, zWorldBlockPos - Bounds.Min.Z];
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

        public (float distance, Vector2i position, BaseBlockMap source)? RaycastBlockmap2(Ray ray)
        {
            //Get raycasted blocks
            var raycastedBlocks = Rasterization.DDA((Vector2)ray.origin, (Vector2)ray.GetPoint(300), true);

            BaseBlockMap source = null;
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
                    if (Math.Abs((float) (c00 - c11)) < Math.Abs((float) (c01 - c10)))
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

        public (Vector3 hitPoint, Vector2i position, BaseBlockMap source)? RaycastHeightmap2(Ray ray)
        {
            //Get raycasted blocks
            var rayBlocks = Rasterization.DDA((OpenTK.Vector2)ray.origin, (OpenTK.Vector2)ray.GetPoint(300), true);

            float distance = float.MaxValue;
            BaseBlockMap source = null;

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

            Vector3? CheckBlockIntersection(BaseBlockMap map, in Vector2i position)
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
                    if (Math.Abs((float) (c00 - c11)) < Math.Abs((float) (c01 - c10)))
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

        protected Vector2i Local2World(Vector2i localPosition)
        {
            return localPosition + Bounds.Min;
        }

        protected Vector2i World2Local(Vector2i worldPosition)
        {
            return worldPosition - Bounds.Min;
        }


        public event Action Changed;

        protected readonly Heights[,] _heightMap;           //todo Optimize as vector array
        protected readonly Blocks[,] _blocks;                   //todo Optimize as vector array
        protected readonly List<BaseBlockMap> _childs = new List<BaseBlockMap>();

        protected void DoChanged()
        {
            Changed?.Invoke();
        }
    }
}