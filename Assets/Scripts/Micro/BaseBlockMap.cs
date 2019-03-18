﻿using System;
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
        public abstract void GenerateHeightmap();

        public void SetBlocks(IEnumerable<Vector2i> positions, IEnumerable<Blocks> blocks, bool regenerateHeightmap)
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

            //if(regenerateHeightmap)
                //GenerateHeightmap();
                if(regenerateHeightmap)
                    throw new NotImplementedException("Its not allowed to autoregenerate heightmap before adding child map to main map");

            DoChanged();
        }

        public Heights[,] GetHeightMap()
        {
            return _heightMap;
        }

        public ref readonly Heights GetHeightRef(Vector2i worldPos)
        {
            if (worldPos.X >= Bounds.Min.X && worldPos.X <= Bounds.Max.X + 1
                                           && worldPos.Z >= Bounds.Min.Z && worldPos.Z <= Bounds.Max.Z + 1)
            {
                var localPos = worldPos - Bounds.Min;
                return ref _heightMap[localPos.X, localPos.Z];
            }

            return ref Heights.Empty;
        }

        public BlockHeights? GetBlockHeights(Vector2i worldPos)
        {
            if (!Bounds.Contains(worldPos))
                return null;

            var localPos = World2Local(worldPos);
            return new BlockHeights(
                _heightMap[localPos.X, localPos.Z],
                _heightMap[localPos.X, localPos.Z + 1],
                _heightMap[localPos.X + 1, localPos.Z],
                _heightMap[localPos.X + 1, localPos.Z + 1]
                );
        }

        /// <summary>
        /// Get height of 2d position on map
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public float GetHeight(Vector2 position)
        {
            var blockPos = (Vector2i) position;
            var cornersResult = GetBlockHeights(blockPos);
            if (cornersResult.HasValue)
            {
                var localPos = position - blockPos;
                var corners = cornersResult.Value;
                if (Math.Abs(corners.H00.Nominal - corners.H11.Nominal) <
                    Math.Abs(corners.H10.Nominal - corners.H01.Nominal))
                {
                    //Check the proper triangle of quad (from reduced cross product) (х3 - х1) * (у2 - у1) - (у3 - у1) * (х2 - х1)
                    //var side = (localPos.X - 0) * (1 - 0) - (localPos.Y - 0) * (1 - 0);
                    if (localPos.Y >= localPos.X)
                    {
                        var (u, v, w) = Intersections.Barycentric2DCoordsOptimized_00_01_11(localPos);
                        return corners.H00.Nominal * u + corners.H01.Nominal * v +
                               corners.H11.Nominal * w;
                    }
                    else
                    {
                        var (u, v, w) = Intersections.Barycentric2DCoordsOptimized_00_11_10(localPos);
                        return corners.H00.Nominal * u + corners.H11.Nominal * v +
                               corners.H10.Nominal * w;
                    }
                }
                else
                {
                    //Check the proper triangle of quad (from reduced cross product) (х3 - х1) * (у2 - у1) - (у3 - у1) * (х2 - х1)
                    //var side = (localPos.X - 0) * (0 - 1) - (localPos.Y - 1) * (1 - 0);
                    if (-localPos.X > localPos.Y - 1)
                    {
                        var (u, v, w) = Intersections.Barycentric2DCoordsOptimized_00_01_10(localPos);
                        return corners.H00.Nominal * u + corners.H01.Nominal * v +
                               corners.H10.Nominal * w;
                    }
                    else
                    {
                        var (u, v, w) = Intersections.Barycentric2DCoordsOptimized_01_11_10(localPos);
                        return corners.H01.Nominal * u + corners.H11.Nominal * v +
                               corners.H10.Nominal * w;
                    }
                }
            }

            return 0;
        }

        public BlockNominalHeights? GetBlockNominalHeights(Vector2i worldPos)
        {
            if (!Bounds.Contains(worldPos))
                return null;

            var localPos = World2Local(worldPos);
            return new BlockNominalHeights(
                _heightMap[localPos.X, localPos.Z].Nominal,
                _heightMap[localPos.X, localPos.Z + 1].Nominal,
                _heightMap[localPos.X + 1, localPos.Z].Nominal,
                _heightMap[localPos.X + 1, localPos.Z + 1].Nominal
            );
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

        public BlockInfo? GetBlock(Vector2i blockPos)
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
            var blocksSize = Bounds.Size;

            return new NeighborBlocks(
                localPos.Z + 1 < blocksSize.Z
                    ? _blocks[localPos.X, localPos.Z + 1]
                    : Blocks.Empty,
                localPos.X + 1 < blocksSize.X
                    ? _blocks[localPos.X + 1, localPos.Z]
                    : Blocks.Empty,
                localPos.Z - 1 >= 0
                    ? _blocks[localPos.X, localPos.Z - 1]
                    : Blocks.Empty,
                localPos.X - 1 >= 0
                    ? _blocks[localPos.X - 1, localPos.Z]
                    : Blocks.Empty
            );
        }

        public (float distance, Vector2i position, BaseBlockMap source)? RaycastBlockmap(Ray ray)
        {
            //Get raycasted blocks
            var raycastedBlocks = Rasterization.DDA((Vector2)ray.origin, (Vector2)ray.GetPoint(300), true);

            BaseBlockMap source = null;
            float resultDistance = -1;
            foreach (var position in raycastedBlocks)
            {
                ref readonly var testedBlock = ref GetBlockRef(position);
                if(testedBlock.IsEmpty) continue;

                var occludeState = GetOcclusionState(in testedBlock);

                if (occludeState.state != BlockOverlapState.Overlap)
                {
                    var distance = Intersections.RayBlockIntersection(ray, in position, in testedBlock);
                    if (distance > 0)
                    {
                        resultDistance = distance;
                        source = this;
                    }
                }

                //Check map objects
                if (occludeState.state == BlockOverlapState.Overlap || occludeState.state == BlockOverlapState.Above)
                {
                    testedBlock = ref occludeState.map.GetBlockRef(position);
                    if(testedBlock.IsEmpty) continue;

                    var childDistance = Intersections.RayBlockIntersection(ray, in position, in testedBlock);
                    if (childDistance > 0 && (resultDistance < 0 || childDistance < resultDistance))
                    {
                        resultDistance = childDistance;
                        source = occludeState.map;
                    }
                }

                if (resultDistance > 0)
                {
                    return (resultDistance, position, source);
                }
            }

            return null;
        }

        public (Vector3 hitPoint, Vector2i position, BaseBlockMap source)? RaycastHeightmap(Ray ray)
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
                var occludeState = GetOcclusionState(blockPos);

                if (occludeState.state != BlockOverlapState.Overlap)
                {
                    var blockHitDistance = CheckBlockIntersection(this, blockPos);
                    if (blockHitDistance > 0)
                    {
                        distance = blockHitDistance;
                        source = this;
                    }
                }

                if (occludeState.state == BlockOverlapState.Overlap || occludeState.state == BlockOverlapState.Above)
                {
                    var childBlockHitDistance = CheckBlockIntersection(occludeState.map, blockPos);
                    if (childBlockHitDistance > 0)
                    {
                        if (childBlockHitDistance < distance)
                        {
                            distance = childBlockHitDistance;
                            source = occludeState.map;
                        }
                    }
                }

                if (source != null)
                {
                    return (ray.GetPoint(distance), blockPos, source);
                }
            }

            return null;

            float CheckBlockIntersection(BaseBlockMap map, in Vector2i position)
            {
                const float NoIntersection = -1f;
                if (!map.Bounds.Contains(position)) return NoIntersection;

                var localPos = position - map.Bounds.Min;

                if (map._blocks[localPos.X, localPos.Z].IsEmpty)
                    return NoIntersection;

                var c00 = map._heightMap[localPos.X, localPos.Z].Nominal;
                var c01 = map._heightMap[localPos.X, localPos.Z + 1].Nominal;
                var c11 = map._heightMap[localPos.X + 1, localPos.Z + 1].Nominal;
                var c10 = map._heightMap[localPos.X + 1, localPos.Z].Nominal;

                var v00 = new Vector3(position.X, c00, position.Z);
                var v01 = new Vector3(position.X, c01, position.Z + 1);
                var v11 = new Vector3(position.X + 1, c11, position.Z + 1);
                var v10 = new Vector3(position.X + 1, c10, position.Z);

                //Check block's triangles for intersection
                float resultDistance;
                if (Math.Abs((float) (c00 - c11)) < Math.Abs((float) (c01 - c10)))
                {
                    if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out resultDistance) == 1
                        || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out resultDistance) == 1)
                    {
                        return resultDistance;
                    }
                }
                else
                {
                    if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out resultDistance) == 1
                        || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out resultDistance) == 1)
                    {
                        return resultDistance;
                    }
                }

                return NoIntersection;
            }
        }

        protected Vector2i Local2World(Vector2i localPosition)
        {
            return localPosition + Bounds.Min;
        }

        protected Vector2i Local2World(int x, int z)
        {
            return new Vector2i(x, z) + Bounds.Min;
        }


        protected Vector2i World2Local(Vector2i worldPosition)
        {
            return worldPosition - Bounds.Min;
        }

        public abstract (ObjectMap map, BlockOverlapState state) GetOcclusionState(Vector2i worldPosition);


        public event Action Changed;

        protected readonly Heights[,] _heightMap;           //todo Optimize as vector array
        protected readonly Blocks[,] _blocks;                   //todo Optimize as vector array
        protected readonly List<BaseBlockMap> _childs = new List<BaseBlockMap>();

        protected void DoChanged()
        {
            Changed?.Invoke();
        }

        protected (ObjectMap map, BlockOverlapState state) GetOcclusionState(in Blocks block)
        {
            if(block.IsEmpty)
                return (null, BlockOverlapState.None);

            var state = block.GetOverlapState();
            if (state.state == BlockOverlapState.None)
                return (null, BlockOverlapState.None);
            else
                return ((ObjectMap)_childs[state.mapId], state.state);
        }
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



    public readonly struct BlockHeights
    {
        public readonly Heights H00;
        public readonly Heights H01;
        public readonly Heights H10;
        public readonly Heights H11;

        public BlockHeights(in Heights h00, in Heights h01, in Heights h10, in Heights h11)
        {
            H00 = h00;
            H01 = h01;
            H10 = h10;
            H11 = h11;
        }
    }

    public readonly struct BlockNominalHeights
    {
        public readonly float H00;
        public readonly float H01;
        public readonly float H10;
        public readonly float H11;

        public BlockNominalHeights(float h00, float h01, float h10, float h11)
        {
            H00 = h00;
            H01 = h01;
            H10 = h10;
            H11 = h11;
        }

        public void Deconstruct(out float height00, out float height01, out float height10, out float height11)
        {
            height00 = H00;
            height01 = H01;
            height10 = H10;
            height11 = H11;
        }
    }
}