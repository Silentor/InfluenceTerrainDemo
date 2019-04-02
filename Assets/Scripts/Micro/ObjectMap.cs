using System;
using System.Collections.Generic;
using System.Diagnostics;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

namespace TerrainDemo.Micro
{
    public sealed class ObjectMap : BaseBlockMap
    {
        public readonly MicroMap ParentMap;

        public ObjectMap(string name, Bounds2i bounds, MicroMap parentMap) : base(name, bounds)
        {
            ParentMap = parentMap;
            _originalHeightmap = new Heights[Bounds.Size.X + 1, Bounds.Size.Z + 1];
        }

        public override void SetHeights(IEnumerable<Vector2i> positions, IEnumerable<Heights> heights)
        {
            var posEnumerator = positions.GetEnumerator();
            var infEnumerator = heights.GetEnumerator();
            using (posEnumerator)
            {
                using (infEnumerator)
                {
                    while (posEnumerator.MoveNext() & infEnumerator.MoveNext())
                    {
                        var localX = posEnumerator.Current.X - Bounds.Min.X;
                        var localZ = posEnumerator.Current.Z - Bounds.Min.Z;
                        _originalHeightmap[localX, localZ] = infEnumerator.Current;
                        _heightMap[localX, localZ] = infEnumerator.Current;
                    }
                }
            }

            //Snap();
            DoChanged();
        }

        public override void SetHeights(IEnumerable<Vector2i> positions, Heights[,] heights)
        {
            var posEnumerator = positions.GetEnumerator();
            using (posEnumerator)
            {
                while (posEnumerator.MoveNext())
                {
                    var localX = posEnumerator.Current.X - Bounds.Min.X;
                    var localZ = posEnumerator.Current.Z - Bounds.Min.Z;
                    _originalHeightmap[localX, localZ] = heights[localX, localZ];
                    _heightMap[localX, localZ] = heights[localX, localZ];
                }
            }

            //Snap();
            DoChanged();
        }

        /// <summary>
        /// Vertical translation of map object
        /// </summary>
        /// <param name="yOffset"></param>
        public void Translate(float yOffset)
        {
            for (int x = 0; x < Bounds.Size.X + 1; x++)
                for (int z = 0; z < Bounds.Size.Z + 1; z++)
                {
                    ref var height = ref _originalHeightmap[x, z];
                    height = height.Translate(yOffset);
                    _heightMap[x, z] = height;
                }

            Snap();
            DoChanged();
        }

        /// <summary>
        /// Take into consideration parent map block if my block is missing. Its necessary for proper blending main map and object map meshes
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
                    var neighborBlockX0 = x - 1;
                    var neighborBlockX1 = x;
                    var neighborBlockZ0 = z - 1;
                    var neighborBlockZ1 = z;

                    ref readonly var b00 = ref Blocks.Empty;
                    ref readonly var b10 = ref Blocks.Empty;
                    ref readonly var b01 = ref Blocks.Empty;
                    ref readonly var b11 = ref Blocks.Empty;

                    var heightAcc = OpenToolkit.Mathematics.Vector3.Zero;
                    int blockCounter = 0;

                    if (neighborBlockX0 >= 0 && neighborBlockZ0 >= 0)
                    {
                        b00 = ref _blocks[neighborBlockX0, neighborBlockZ0];

                        if (!b00.IsEmpty)
                        {
                            heightAcc += (OpenToolkit.Mathematics.Vector3)b00.Height;
                            blockCounter++;
                        }
                    }

                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ0 >= 0)
                    {
                        b10 = ref _blocks[neighborBlockX1, neighborBlockZ0];

                        if (!b10.IsEmpty)
                        {
                            heightAcc += (OpenToolkit.Mathematics.Vector3)b10.Height;
                            blockCounter++;
                        }
                    }

                    if (neighborBlockX0 >= 0 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b01 = ref _blocks[neighborBlockX0, neighborBlockZ1];

                        if (!b01.IsEmpty)
                        {
                            heightAcc += (OpenToolkit.Mathematics.Vector3)b01.Height;
                            blockCounter++;
                        }
                    }


                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b11 = ref _blocks[neighborBlockX1, neighborBlockZ1];

                        if (!b11.IsEmpty)
                        {
                            heightAcc += (OpenToolkit.Mathematics.Vector3)b11.Height;
                            blockCounter++;
                        }
                    }

                    heightAcc = heightAcc / blockCounter;

                    _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                }

            //Modify heightmap to proper blend with parent map
            for (int x = 0; x < Bounds.Size.X; x++)
                for (int z = 0; z < Bounds.Size.Z; z++)
                {
                    var worldBlockPos = Local2World((x, z));
                    ref readonly var parentBlock = ref ParentMap.GetBlockRef(worldBlockPos);

                    if (!parentBlock.IsEmpty && !_blocks[x, z].IsEmpty)
                    {
                        //Need check thats block edges
                        ref var height00 = ref _heightMap[x, z];
                        ref var height01 = ref _heightMap[x, z + 1];
                        ref var height10 = ref _heightMap[x + 1, z];
                        ref var height11 = ref _heightMap[x + 1, z + 1];

                        //Snap object's block to main map block. No intersections allowed!
                        var parentHeights = ParentMap.GetBlockNominalHeights(worldBlockPos);
                        if (parentHeights.HasValue)
                        {
                            var (parentHeight00, parentHeight01, parentHeight10, parentHeight11) = parentHeights.Value;
                            SnapHeight(ref height00, ref height01, parentHeight00, parentHeight01);
                            SnapHeight(ref height00, ref height10, parentHeight00, parentHeight10);
                            SnapHeight(ref height01, ref height11, parentHeight01, parentHeight11);
                            SnapHeight(ref height10, ref height11, parentHeight10, parentHeight11);
                            SnapHeight(ref height00, ref height11, parentHeight00, parentHeight11);
                            SnapHeight(ref height10, ref height01, parentHeight10, parentHeight01);


                            //Check occlusion
                            if (height00.Base > parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height00.Main < parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height00.Main > parentHeight00 && height00.Base < parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height01.Base > parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height01.Main < parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height01.Main > parentHeight01 && height01.Base < parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height10.Base > parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height10.Main < parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height10.Main > parentHeight10 && height10.Base < parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height11.Base > parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height11.Main < parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height11.Main > parentHeight11 && height11.Base < parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                        }
                    }

                }

            //Snap intersecting sides of object to main map
            void SnapHeight(ref Heights height1, ref Heights height2, float parentHeight1, float parentHeight2)
            {
                var diff1 = parentHeight1 - height1.Main;
                var diff2 = parentHeight2 - height2.Main;

                if (diff1 * diff2 < 0)            //Check the signs of differences
                {
                    if (Math.Abs(diff1) < Math.Abs(diff2))
                        height1 = new Heights(parentHeight1, height1.Base);
                    else
                        height2 = new Heights(parentHeight2, height2.Base);
                }

                diff1 = parentHeight1 - height1.Base;
                diff2 = parentHeight2 - height2.Base;

                if (diff1 * diff2 < 0)            //Check the signs of differences
                {
                    if (Math.Abs(diff1) < Math.Abs(diff2))
                        height1 = new Heights(height1.Main, parentHeight1);
                    else
                        height2 = new Heights(height2.Main, parentHeight2);
                }
            }

            timer.Stop();

            UnityEngine.Debug.Log($"Heightmap of {Name} generated in {timer.ElapsedMilliseconds}");

            GenerateDataMap();
            */
        }

        public void Snap()
        {
            var timer = Stopwatch.StartNew();

            //Modify heightmap to proper blend with parent map
            for (int x = 0; x < Bounds.Size.X; x++)
                for (int z = 0; z < Bounds.Size.Z; z++)
                {
                    var worldBlockPos = Local2World((x, z));
                    ref readonly var parentBlock = ref ParentMap.GetBlockRef(worldBlockPos);

                    if (!parentBlock.IsEmpty && !_blocks[x, z].IsEmpty)
                    {
                        //Need check thats block edges
                        ref readonly var inputHeight00 = ref _originalHeightmap[x, z];
                        ref readonly var inputHeight01 = ref _originalHeightmap[x, z + 1];
                        ref readonly var inputHeight10 = ref _originalHeightmap[x + 1, z];
                        ref readonly var inputHeight11 = ref _originalHeightmap[x + 1, z + 1];
                        ref var height00 = ref _heightMap[x, z];
                        ref var height01 = ref _heightMap[x, z + 1];
                        ref var height10 = ref _heightMap[x + 1, z];
                        ref var height11 = ref _heightMap[x + 1, z + 1];

                        //Snap object's block to main map block. No intersections allowed!
                        var parentHeights = ParentMap.GetBlockNominalHeights(worldBlockPos);
                        if (parentHeights.HasValue)
                        {
                            var (parentHeight00, parentHeight01, parentHeight10, parentHeight11) = parentHeights.Value;
                            SnapHeight(inputHeight00, inputHeight01, 
                                ref height00, ref height01, parentHeight00, parentHeight01);
                            SnapHeight(inputHeight00, inputHeight10,
                                ref height00, ref height10, parentHeight00, parentHeight10);
                            SnapHeight(inputHeight01, inputHeight11,
                                ref height01, ref height11, parentHeight01, parentHeight11);
                            SnapHeight(inputHeight10, inputHeight11, 
                                ref height10, ref height11, parentHeight10, parentHeight11);
                            SnapHeight(inputHeight00, inputHeight11, 
                                ref height00, ref height11, parentHeight00, parentHeight11);
                            SnapHeight(inputHeight10, inputHeight01, 
                                ref height10, ref height01, parentHeight10, parentHeight01);

                            //Check occlusion
                            if (height00.Base > parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height00.Main < parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height00.Main > parentHeight00 && height00.Base < parentHeight00)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height01.Base > parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height01.Main < parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height01.Main > parentHeight01 && height01.Base < parentHeight01)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height10.Base > parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height10.Main < parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height10.Main > parentHeight10 && height10.Base < parentHeight10)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            if (height11.Base > parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Above);
                                continue;
                            }
                            else if (height11.Main < parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Under);
                                continue;
                            }
                            else if (height11.Main > parentHeight11 && height11.Base < parentHeight11)
                            {
                                ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                                continue;
                            }

                            ParentMap.SetOcclusionState(worldBlockPos, this, BlockOverlapState.Overlap);
                        }
                    }

                }

            //Snap intersecting sides of object to main map
            void SnapHeight(in Heights input1, in Heights input2, ref Heights height1, ref Heights height2, float parentHeight1, float parentHeight2)
            {
                var diff1 = parentHeight1 - input1.Main;
                var diff2 = parentHeight2 - input2.Main;

                if (diff1 * diff2 < 0)            //Check the signs of differences
                {
                    if (Math.Abs(diff1) < Math.Abs(diff2))
                        height1 = new Heights(parentHeight1, input1.Base);
                    else
                        height2 = new Heights(parentHeight2, input2.Base);
                }

                diff1 = parentHeight1 - input1.Base;
                diff2 = parentHeight2 - input2.Base;

                if (diff1 * diff2 < 0)            //Check the signs of differences
                {
                    if (Math.Abs(diff1) < Math.Abs(diff2))
                        height1 = new Heights(input1.Main, parentHeight1);
                    else
                        height2 = new Heights(input2.Main, parentHeight2);
                }
            }

            timer.Stop();

            UnityEngine.Debug.Log($"{Name} snapped in {timer.ElapsedMilliseconds}");
        }

        public override (ObjectMap map, BlockOverlapState state) GetOverlapState(Vector2i worldPosition)
        {
            return ParentMap.GetOverlapState(worldPosition);
        }

        public override string ToString()
        {
            return $"{Name} at {Bounds}";
        }

        private readonly Heights[,] _originalHeightmap;
    }
}
