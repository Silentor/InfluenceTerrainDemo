using System;
using TerrainDemo.Macro;
using TerrainDemo.Spatial;
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
        /// Take into consideration parent map block if my block is missing. Its necessary for proper blending main map and object map meshes
        /// </summary>
        public override void GenerateHeightmap()
        {
            //Local space iteration
            for (int x = 0; x < Bounds.Size.X + 1; x++)
                for (int z = 0; z < Bounds.Size.Z + 1; z++)
                {
                    var isCommonVertex = false;             //Some neighbor block is intersected with parent map block
                    var isBorderVertex = false;

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

                            //Common height vertex case
                            ref readonly var parentBlock =
                                ref _parentMap.GetBlockRef(Local2World(new Vector2i(neighborBlockX0, neighborBlockZ0)));
                            if (b00.GetTotalWidth().IsIntersects(parentBlock.GetTotalWidth()))
                            {
                                isCommonVertex = true;
                            }
                        }
                    }
                    else
                    {
                        isBorderVertex = true;
                    }

                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ0 >= 0)
                    {
                        b10 = ref _blocks[neighborBlockX1, neighborBlockZ0];

                        if (!b10.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b10.Height;
                            blockCounter++;

                            if (!isCommonVertex)
                            {
                                //Common height vertex case
                                ref readonly var parentBlock =
                                    ref _parentMap.GetBlockRef(Local2World(new Vector2i(neighborBlockX1, neighborBlockZ0)));
                                if (b10.GetTotalWidth().IsIntersects(parentBlock.GetTotalWidth()))
                                {
                                    isCommonVertex = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        isBorderVertex = true;
                    }

                    if (neighborBlockX0 >= 0 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b01 = ref _blocks[neighborBlockX0, neighborBlockZ1];

                        if (!b01.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b01.Height;
                            blockCounter++;

                            if (!isCommonVertex)
                            {
                                //Common height vertex case
                                ref readonly var parentBlock =
                                    ref _parentMap.GetBlockRef(Local2World(new Vector2i(neighborBlockX0, neighborBlockZ1)));
                                if (b01.GetTotalWidth().IsIntersects(parentBlock.GetTotalWidth()))
                                {
                                    isCommonVertex = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        isBorderVertex = true;
                    }

                    if (neighborBlockX1 <= Bounds.Size.X - 1 && neighborBlockZ1 <= Bounds.Size.Z - 1)
                    {
                        b11 = ref _blocks[neighborBlockX1, neighborBlockZ1];

                        if (!b11.IsEmpty)
                        {
                            heightAcc += (OpenTK.Vector3) b11.Height;
                            blockCounter++;

                            if (!isCommonVertex)
                            {
                                //Common height vertex case
                                ref readonly var parentBlock =
                                    ref _parentMap.GetBlockRef(Local2World(new Vector2i(neighborBlockX1, neighborBlockZ1)));
                                if (b11.GetTotalWidth().IsIntersects(parentBlock.GetTotalWidth()))
                                {
                                    isCommonVertex = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        isBorderVertex = true;
                    }

                    heightAcc = heightAcc / blockCounter;

                    /*
                    if (isCommonVertex && isBorderVertex)
                    {
                        ref readonly var parentHeight = ref _parentMap.GetHeightRef(Local2World(new Vector2i(x, z)));
                        if(heightAcc.Z > parentHeight.Nominal)
                            _heightMap[x, z] = new Heights(parentHeight.Nominal, heightAcc.Y, heightAcc.X);
                        else
                            _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                    }
                    else*/
                    {
                        _heightMap[x, z] = new Heights(heightAcc.Z, heightAcc.Y, heightAcc.X);
                    }
                }
        }
    }
}
