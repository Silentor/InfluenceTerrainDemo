using System;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Micro
{
    public sealed class ObjectMap : BaseBlockMap
    {
        private readonly MicroMap _parentMap;

        public ObjectMap(string name, Bounds2i bounds, MicroMap parentMap) : base(name, bounds)
        {
            _parentMap = parentMap;
        }

        /// <summary>
        /// Vertical translation of map object
        /// </summary>
        /// <param name="yOffset"></param>
        public void Translate(float yOffset)
        {
            for (int x = 0; x < Bounds.Size.X; x++)
                for (int z = 0; z < Bounds.Size.Z; z++)
                {
                    ref var block = ref _blocks[x, z];
                    block = block.MutateHeight(new Heights(block.Height.Main + yOffset,
                        block.Height.Underground + yOffset, block.Height.Base + yOffset));
                }

            GenerateHeightmap();
            DoChanged();
        }

        /// <summary>
        /// Take into consideration parent map block if my block is missing. Its necessary for proper blending main map and object map meshes
        /// </summary>
        public override void GenerateHeightmap()
        {
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

                    var heightAcc = OpenTK.Vector3.Zero;
                    int blockCounter = 0;

                    if (neighborBlockX0 >= 0 && neighborBlockZ0 >= 0)
                    {
                        b00 = ref _blocks[neighborBlockX0, neighborBlockZ0];

                        if (!b00.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3)b00.Height;
                            blockCounter++;
                        }
                    }

                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ0 >= 0)
                    {
                        b10 = ref _blocks[neighborBlockX1, neighborBlockZ0];

                        if (!b10.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b10.Height;
                            blockCounter++;
                        }
                    }

                    if (neighborBlockX0 >= 0 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b01 = ref _blocks[neighborBlockX0, neighborBlockZ1];

                        if (!b01.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b01.Height;
                            blockCounter++;
                        }
                    }
                    

                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b11 = ref _blocks[neighborBlockX1, neighborBlockZ1];

                        if (!b11.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b11.Height;
                            blockCounter++;
                        }
                    }

                    heightAcc = heightAcc / blockCounter;

                    _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                }
            
            //Modify heightmap to proper blend with parent map
            //Naive approach - iterate each block side
             //For each block that intersects main map - check its sides
            for (int x = 0; x < Bounds.Size.X; x++)
                for (int z = 0; z < Bounds.Size.Z; z++)
                {
                    var worldBlockPos = Local2World((x, z));
                    ref readonly var parentBlock = ref _parentMap.GetBlockRef(worldBlockPos);

                    if (!parentBlock.IsEmpty && !_blocks[x, z].IsEmpty/*&& _blocks[x, z].GetTotalWidth().Contains(parentBlock.GetNominalHeight())*/)
                    {
                        //Need check thats block edges
                        ref var height00 = ref _heightMap[x, z];
                        ref var height10 = ref _heightMap[x + 1, z];
                        ref var height01 = ref _heightMap[x, z + 1];
                        ref var height11 = ref _heightMap[x + 1, z + 1];

                        //Check parent block planes
                        var parentData = _parentMap.GetBlock(worldBlockPos);

                        if (parentData.HasValue)
                        {
                            var parentTopHeight00 = parentData.Value.Corner00.Nominal;
                            var parentTopHeight10 = parentData.Value.Corner10.Nominal;
                            var parentTopHeight01 = parentData.Value.Corner01.Nominal;
                            var parentTopHeight11 = parentData.Value.Corner11.Nominal;

                            SnapHeight(ref height00, ref height01, parentTopHeight00, parentTopHeight01);
                            SnapHeight(ref height00, ref height10, parentTopHeight00, parentTopHeight10);
                            SnapHeight(ref height01, ref height11, parentTopHeight01, parentTopHeight11);
                            SnapHeight(ref height10, ref height11, parentTopHeight10, parentTopHeight11);
                            SnapHeight(ref height00, ref height11, parentTopHeight00, parentTopHeight11);
                            SnapHeight(ref height10, ref height01, parentTopHeight10, parentTopHeight01);

                        }
                    }

                }

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
        }
    }
}
