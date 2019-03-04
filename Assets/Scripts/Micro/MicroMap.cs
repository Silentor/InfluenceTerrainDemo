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

        public Cell GetCell([NotNull] Macro.Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (cell.Map != _macromap) throw new ArgumentException();

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

                    //Analyze neighbor blocks
                    var caveBlocksCounter = 0;
                    if (b00.Underground == BlockType.Cave)
                        caveBlocksCounter++;
                    if (b10.Underground == BlockType.Cave)
                        caveBlocksCounter++;
                    if (b01.Underground == BlockType.Cave)
                        caveBlocksCounter++;
                    if (b11.Underground == BlockType.Cave)
                        caveBlocksCounter++;

                    if (caveBlocksCounter == 0)
                    {
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
                    else if(caveBlocksCounter == 4)  //Completely underground
                    {
                        //Just average heights of every layer
                        var groundHeight = (b00.Height.Main + b01.Height.Main + b10.Height.Main + b11.Height.Main) / 4;
                        var caveHeight = (b00.Height.Underground + b01.Height.Underground + b10.Height.Underground + b11.Height.Underground) / 4;
                        var baseHeight = (b00.Height.Base + b01.Height.Base + b10.Height.Base + b11.Height.Base) / 4;
                        _heightMap[x, z] = new Heights(groundHeight, caveHeight, baseHeight);
                    }
                    else
                    {
                        //Hard cases - some blocks are cave entrance (under == empty && ground == empty) or dead end (under != cave && ground != empty)
                        //So we need join ground-under or under-base heights (or ground-under-base in mixed case)
                        var groundHeight = 0f;
                        var caveHeight = 0f;

                        //Check cave entrance and dead end cases
                        var openAirBlocksCounter = 0;
                        var deadEndBlocksCounter = 0;
                        var groundHeightAccum = 0f;
                        var caveHeightAccum = 0f;
                        var baseHeightAccum = 0f;

                        if (b00.Ground == BlockType.Empty && b00.Underground == BlockType.Empty)
                            openAirBlocksCounter++;
                        else if (b00.Ground != BlockType.Empty && b00.Underground != BlockType.Cave)
                        {
                            deadEndBlocksCounter++;
                            groundHeightAccum += b00.Height.Main;
                        }
                        else
                        {
                            groundHeightAccum += b00.Height.Main;
                            caveHeightAccum += b00.Height.Underground;
                        }

                        if (b01.Ground == BlockType.Empty)
                            openAirBlocksCounter++;
                        else if (b01.Ground != BlockType.Empty && b01.Underground != BlockType.Cave)
                        {
                            deadEndBlocksCounter++;
                            groundHeightAccum += b01.Height.Main;
                        }
                        else
                        {
                            groundHeightAccum += b01.Height.Main;
                            caveHeightAccum += b01.Height.Underground;
                        }

                        if (b10.Ground == BlockType.Empty)
                            openAirBlocksCounter++;
                        else if (b10.Ground != BlockType.Empty && b10.Underground != BlockType.Cave)
                        {
                            deadEndBlocksCounter++;
                            groundHeightAccum += b10.Height.Main;
                        }
                        else
                        {
                            groundHeightAccum += b10.Height.Main;
                            caveHeightAccum += b10.Height.Underground;
                        }

                        if (b11.Ground == BlockType.Empty)
                            openAirBlocksCounter++;
                        else if (b11.Ground != BlockType.Empty && b11.Underground != BlockType.Cave)
                        {
                            deadEndBlocksCounter++;
                            groundHeightAccum += b11.Height.Main;
                        }
                        else
                        {
                            groundHeightAccum += b11.Height.Main;
                            caveHeightAccum += b11.Height.Underground;
                        }

                        Assert.IsTrue(openAirBlocksCounter < 4 && deadEndBlocksCounter < 4);
                        Assert.IsTrue(openAirBlocksCounter > 0 || deadEndBlocksCounter > 0);

                        var baseHeight = (b00.Height.Base + b01.Height.Base + b10.Height.Base + b11.Height.Base) / 4;

                        //Catch rare mixed case
                        if (openAirBlocksCounter > 0 && deadEndBlocksCounter > 0)
                        {
                            var worldPos = new Vector2i(x, z) + Bounds.Min;
                            Debug.Log($"Vertex {worldPos} is a complex case, {openAirBlocksCounter} open blocks, {deadEndBlocksCounter} dead ends");

                            groundHeightAccum = groundHeightAccum / (openAirBlocksCounter - 4);
                            caveHeightAccum = caveHeightAccum / (openAirBlocksCounter + deadEndBlocksCounter - 4);

                            groundHeight = groundHeightAccum;
                            caveHeight = caveHeightAccum;

                            var commonHeight = (baseHeight + caveHeight + groundHeight) / 3;
                            groundHeight = caveHeight = baseHeight = commonHeight;
                        }

                        //Cave entrance case
                        if (openAirBlocksCounter > 0 && deadEndBlocksCounter == 0)
                        {
                            groundHeightAccum = groundHeightAccum / caveBlocksCounter;
                            caveHeightAccum = caveHeightAccum / caveBlocksCounter;

                            caveHeight = Mathf.Lerp(groundHeightAccum, caveHeightAccum, caveBlocksCounter / 4f);     //Lerp on part of range (1/4..3/4)
                            groundHeight = caveHeight;
                        }

                        //Dead end case
                        if (deadEndBlocksCounter > 0 && openAirBlocksCounter == 0)
                        {
                            groundHeightAccum = groundHeightAccum / 4;
                            caveHeightAccum = caveHeightAccum / caveBlocksCounter;
                            caveHeight = Mathf.Lerp(baseHeight, caveHeightAccum, caveBlocksCounter / 4f);
                            baseHeight = caveHeight;
                            groundHeight = groundHeightAccum;
                        }
                        
                        _heightMap[x, z] = new Heights(groundHeight, caveHeight, baseHeight);

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

        public (float distance, Vector2i hitBlock)? RaycastBlockmap(Ray ray)
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

        public (Vector3 hitPoint, Vector2i hitBlock)? RaycastHeightmap(Ray ray)
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

                    if (block.Underground != BlockType.Cave)
                    {
                        float c00, c01, c10, c11;
                        if (block.Ground != BlockType.Empty)
                        {
                            c00 = _heightMap[localPos.X, localPos.Z].Main;
                            c01 = _heightMap[localPos.X, localPos.Z + 1].Main;
                            c11 = _heightMap[localPos.X + 1, localPos.Z + 1].Main;
                            c10 = _heightMap[localPos.X + 1, localPos.Z].Main;
                        }
                        else if (block.Underground != BlockType.Empty)
                        {
                            c00 = _heightMap[localPos.X, localPos.Z].Underground;
                            c01 = _heightMap[localPos.X, localPos.Z + 1].Underground;
                            c11 = _heightMap[localPos.X + 1, localPos.Z + 1].Underground;
                            c10 = _heightMap[localPos.X + 1, localPos.Z].Underground;
                        }
                        else
                        {
                            c00 = _heightMap[localPos.X, localPos.Z].Base;
                            c01 = _heightMap[localPos.X, localPos.Z + 1].Base;
                            c11 = _heightMap[localPos.X + 1, localPos.Z + 1].Base;
                            c10 = _heightMap[localPos.X + 1, localPos.Z].Base;
                        }

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
                    else
                    {
                        //Cave block, need check 3 layers
                        ref readonly var c00 = ref _heightMap[localPos.X, localPos.Z];
                        ref readonly var c01 = ref _heightMap[localPos.X, localPos.Z + 1];
                        ref readonly var c11 = ref _heightMap[localPos.X + 1, localPos.Z + 1];
                        ref readonly var c10 = ref _heightMap[localPos.X + 1, localPos.Z];

                        //Naive ground
                        var v00 = new Vector3(blockPos.X, c00.Main, blockPos.Z);
                        var v01 = new Vector3(blockPos.X, c01.Main, blockPos.Z + 1);
                        var v11 = new Vector3(blockPos.X + 1, c11.Main, blockPos.Z + 1);
                        var v10 = new Vector3(blockPos.X + 1, c10.Main, blockPos.Z);

                        //Check block's triangles for intersection
                        Vector3 hit;
                        var distance = float.MaxValue;
                        Vector3 resultHit = Vector3.zero;
                        if (Math.Abs(c00.Main - c11.Main) < Math.Abs(c01.Main - c10.Main))
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }
                        else
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }

                        //Naive cave
                        v00.y = c00.Underground;
                        v01.y = c01.Underground;
                        v11.y = c11.Underground;
                        v10.y = c10.Underground;

                        //Check block's triangles for intersection
                        if (Math.Abs(c00.Underground - c11.Underground) < Math.Abs(c01.Underground - c10.Underground))
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }
                        else
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }

                        //Naive base
                        v00.y = c00.Base;
                        v01.y = c01.Base;
                        v11.y = c11.Base;
                        v10.y = c10.Base;

                        //Check block's triangles for intersection
                        if (Math.Abs(c00.Base - c11.Base) < Math.Abs(c01.Base - c10.Base))
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v11, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v00, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }
                        else
                        {
                            if (Intersections.LineTriangleIntersection(ray, v00, v01, v10, out hit) == 1
                                || Intersections.LineTriangleIntersection(ray, v01, v11, v10, out hit) == 1)
                            {
                                if (Vector3.SqrMagnitude(hit - ray.origin) < distance)
                                {
                                    distance = Vector3.SqrMagnitude(hit - ray.origin);
                                    resultHit = hit;
                                }
                            }
                        }

                        if (distance < float.MaxValue)
                        {
                            return (resultHit, blockPos);
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
