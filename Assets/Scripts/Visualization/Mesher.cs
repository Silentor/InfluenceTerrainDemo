using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Hero;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Object = UnityEngine.Object;

namespace TerrainDemo.Visualization
{
    /// <summary>
    /// Converts cells to Unity mesh and texture
    /// </summary>
    public class Mesher
    {
        private readonly TriRunner _settings;
        private readonly MacroMap _macroMap;
        private readonly Dictionary<BlockType, Color> _defaultBlockColor = new Dictionary<BlockType, Color>();

        /// <summary>
        /// <see cref="Directions.Cardinal"/>
        /// </summary>
        private static readonly (Vector2 pos1, Vector2 pos2)[] BlockSideFromDirection = 
        {
            (new Vector2(0, 1), new Vector2(1, 1)),    //Block side for Forward direction
            (new Vector2(1, 1), new Vector2(1, 0)),    //Block side for Right direction
            (new Vector2(1, 0), new Vector2(0, 0)),    //Block side for Back direction
            (new Vector2(0, 0), new Vector2(0, 1)),    //Block side for Left direction
        };

        public Mesher(MacroMap macroMap, TriRunner settings)
        {
            _settings = settings;
            _macroMap = macroMap;

            foreach (var blockSetting in settings.AllBlocks)
                _defaultBlockColor[blockSetting.Block] = blockSetting.DefaultColor;
        }

        public Mesh CreateMacroMesh(MacroMap mapMesh, Renderer.MacroCellInfluenceMode influence)
        {
            var result = new Mesh();
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();

            foreach (var cell in mapMesh.Cells)
            {
                CreateMacroCell(vertices, colors, indices, cell, influence);
            }

            result.SetVertices(vertices);
            result.SetColors(colors);
            result.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            result.RecalculateNormals();

            return result;
        }

        public (Mesh mesh, Texture texture) CreateTerrainMesh(MicroMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            bounds = bounds.Intersect(map.Bounds);

            if (bounds.IsEmpty)
            {
                var emptyMesh = new Mesh();
                return (emptyMesh, Texture2D.blackTexture);
            }

            var heightMap = map.GetHeightMap();
            var blockMap = map.GetBlockMap();

            var groundVertIndexBuffer = new int[bounds.Size.X + 1, bounds.Size.Z + 1];
            var groundVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1))
                { Vector3.zero};        //Dummy value to make default 0 values of baseVertIndexBuffer a invalid
            var groundIndices = new List<int>(groundVertices.Count * 2);
            var groundUv = new List<Vector2>(groundVertices.Count){Vector2.zero};
            //var groundNormals = new List<Vector3>(groundVertices.Count) {Vector3.zero};

            //Set quads
            for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;
                    
                    ref readonly var block = ref blockMap[mapLocalX, mapLocalZ];
                    if (block.IsEmpty)
                        continue;

                    //Block culling
                    if (block.GetOverlapState().state == BlockOverlapState.Overlap)
                        continue;

                    //Prepare block vertices
                    int v00, v01, v10, v11;
                    int startIndexCounter = groundVertices.Count;
                    var h00 = heightMap[mapLocalX, mapLocalZ].Nominal;
                    if (groundVertIndexBuffer[chunkLocalX, chunkLocalZ] == 0)
                    {
                        v00 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX, chunkLocalZ] = v00;
                        groundVertices.Add(new Vector3(worldX, h00, worldZ));
                        groundUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            chunkLocalZ / (float) bounds.Size.Z));

                    }
                    else
                    {
                        v00 = groundVertIndexBuffer[chunkLocalX, chunkLocalZ];
                    }

                    var h01 = heightMap[mapLocalX, mapLocalZ + 1].Nominal;
                    if (groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1] == 0)
                    {
                        v01 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1] = v01;
                        groundVertices.Add(new Vector3(worldX, h01, worldZ + 1));
                        groundUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        v01 = groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1];
                    }

                    var h10 = heightMap[mapLocalX + 1, mapLocalZ].Nominal;
                    if (groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ] == 0)
                    {
                        v10 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ] = v10;
                        groundVertices.Add(new Vector3(worldX + 1, h10, worldZ));
                        groundUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        v10 = groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ];
                    }

                    var h11 = heightMap[mapLocalX + 1, mapLocalZ + 1].Nominal;
                    if (groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1] == 0)
                    {
                        v11 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1] = v11;
                        groundVertices.Add(new Vector3(worldX + 1, h11, worldZ + 1));
                        groundUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        v11 = groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1];
                    }

                    //Make proper quad
                    if (Mathf.Abs(h00 - h11) < Mathf.Abs(h10 - h01))
                    {
                        groundIndices.Add(v00);
                        groundIndices.Add(v01);
                        groundIndices.Add(v11);

                        groundIndices.Add(v00);
                        groundIndices.Add(v11);
                        groundIndices.Add(v10);
                    }
                    else
                    {
                        groundIndices.Add(v00);
                        groundIndices.Add(v01);
                        groundIndices.Add(v10);

                        groundIndices.Add(v10);
                        groundIndices.Add(v01);
                        groundIndices.Add(v11);
                    }

                    /*
                    //Calculate normals, check nearby map objects
                    foreach (var mapChild in map.Childs)
                    {
                        if (worldX >= mapChild.Bounds.Min.X && worldX <= mapChild.Bounds.Max.X + 1
                                                            && worldZ >= mapChild.Bounds.Min.Z &&
                                                            worldZ <= mapChild.Bounds.Max.Z + 1)
                        {
                            ref readonly var childHeight = ref mapChild.get
                        }
                    }
                    */
                }


            var groundMesh = new Mesh();
            groundMesh.SetVertices(groundVertices);
            groundMesh.SetTriangles(groundIndices, 0);
            groundMesh.SetUVs(0, groundUv);
            //groundMesh.SetNormals(groundNormals);
            groundMesh.RecalculateNormals();

            var mainTexture = CreateBlockTexture(map, bounds, renderSettings);

            return (groundMesh, mainTexture);
        }

        public ((Mesh, Texture) Base, (Mesh, Texture) Under, (Mesh, Texture) Main) CreateMinecraftMesh(BaseBlockMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            const float baseLayerVisibleWidth = 0.1f;

            bounds = bounds.Intersect(map.Bounds);

            if (bounds.IsEmpty)
            {
                var emptyMesh = new Mesh();
                return ((emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture));
            }

            var blockMap = map.GetBlockMap();

            var baseVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var baseIndices = new List<int>(baseVertices.Count * 2);
            var baseUv = new List<Vector2>(baseVertices.Count);

            var underVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var underIndices = new List<int>(underVertices.Count * 2);
            var underUv = new List<Vector2>(underVertices.Count);

            var mainVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var mainIndices = new List<int>(mainVertices.Count * 2);
            var mainUv = new List<Vector2>(mainVertices.Count);

            var uvXCoeff = 1f / bounds.Size.X;
            var uvYCoeff = 1f / bounds.Size.Z;

            for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;
                    var worldPosition = new Vector2i(worldX, worldZ);

                    var block = blockMap[mapLocalX, mapLocalZ];
                    //var block = chunk[chunkLocalX, chunkLocalZ];
                    if (block.IsEmpty)
                        continue;

                    block = Filter(block, renderSettings.RenderLayer);
                    ref readonly var blockData = ref map.GetBlockData(worldPosition);

                    //Draw block top
                    if (block.Ground != BlockType.Empty)
                    {
                        DrawFloor(mainVertices, mainIndices, mainUv, blockData.Height);
                    }
                    else if (block.Underground != BlockType.Empty)
                    {
                        DrawFloor(underVertices, underIndices, underUv, blockData.Height);
                    }
                    else
                        DrawFloor(baseVertices, baseIndices, baseUv, blockData.Height);

                    DrawCeil(baseVertices, baseIndices, baseUv, blockData.MinHeight - baseLayerVisibleWidth);

                    //Draw block sides
                    var neighbors = map.GetNeighborBlocks(worldPosition);
                    foreach (var dir in Directions.Cardinal)
                    {
                        var neigh = neighbors[dir];
                        //if (!neigh.IsEmpty)
                        {
                            neigh = Filter(neigh, renderSettings.RenderLayer);
                            ref readonly var neighData =
                                ref map.GetBlockData(worldPosition + Directions.Vector2I[(int) dir]);

                            //Draw Ground part of block side
                            if (block.Ground != BlockType.Empty)
                            {
                                //Calculate visible layer part
                                var mainLayerWidth = new Interval(blockData.MinHeight, blockData.Height);
                                var visiblePart = CalculateVisiblePart(mainLayerWidth, neighData);

                                if(!visiblePart.Item1.IsEmpty)   
                                    DrawBlockSide(mainVertices, mainIndices, mainUv, dir, visiblePart.Item1.Max, visiblePart.Item1.Min);
                                if(!visiblePart.Item2.IsEmpty)
                                    DrawBlockSide(mainVertices, mainIndices, mainUv, dir, visiblePart.Item2.Max, visiblePart.Item2.Min);
                            }

                            //Draw Underground part of block side (if solid)
                            if (block.Underground != BlockType.Empty)
                            {
                                //Calculate visible layer part
                                var underLayerWidth = new Interval(blockData.MinHeight, blockData.Height);
                                var visiblePart = CalculateVisiblePart(underLayerWidth, neighData);

                                if (!visiblePart.Item1.IsEmpty)
                                    DrawBlockSide(underVertices, underIndices, underUv, dir, visiblePart.Item1.Max, visiblePart.Item1.Min);
                                if (!visiblePart.Item2.IsEmpty)
                                    DrawBlockSide(underVertices, underIndices, underUv, dir, visiblePart.Item2.Max, visiblePart.Item2.Min);
                            }

                            //Draw Base part of block side
                            {
                                //Calculate visible layer part
                                var baseLayerWidth = new Interval(blockData.MinHeight - baseLayerVisibleWidth, blockData.Height);           //Base layer width is fictional, just for BLock mode visualization
                                var visiblePart = CalculateVisiblePart(baseLayerWidth, neighData);

                                if (!visiblePart.Item1.IsEmpty)
                                    DrawBlockSide(baseVertices, baseIndices, baseUv, dir, visiblePart.Item1.Max, visiblePart.Item1.Min);
                                if (!visiblePart.Item2.IsEmpty)
                                    DrawBlockSide(baseVertices, baseIndices, baseUv, dir, visiblePart.Item2.Max, visiblePart.Item2.Min);
                            }
                        }
                    }

                    void DrawFloor(List<Vector3> vertices, List<int> indices, List<Vector2> uv, float height)
                    {
                        indices.Add(vertices.Count);
                        indices.Add(vertices.Count + 1);
                        indices.Add(vertices.Count + 2);
                        indices.Add(vertices.Count + 3);

                        vertices.Add(new Vector3(worldX, height, worldZ));
                        vertices.Add(new Vector3(worldX, height, worldZ + 1));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ + 1));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ));

                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, chunkLocalZ * uvYCoeff));
                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, chunkLocalZ * uvYCoeff));
                    }

                    void DrawCeil(List<Vector3> vertices, List<int> indices, List<Vector2> uv, float height)
                    {
                        indices.Add(vertices.Count);
                        indices.Add(vertices.Count + 1);
                        indices.Add(vertices.Count + 2);
                        indices.Add(vertices.Count + 3);

                        vertices.Add(new Vector3(worldX, height, worldZ));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ + 1));
                        vertices.Add(new Vector3(worldX, height, worldZ + 1));

                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, chunkLocalZ * uvYCoeff));
                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, chunkLocalZ * uvYCoeff));
                    }

                    void DrawBlockSide(List<Vector3> vertices, List<int> indices, List<Vector2> uv, Side2d direction, float topHeight, float bottomHeight)
                    {
                        indices.Add(vertices.Count);
                        indices.Add(vertices.Count + 1);
                        indices.Add(vertices.Count + 2);
                        indices.Add(vertices.Count + 3);

                        //Normal outside
                        var (pos1, pos2) = BlockSideFromDirection[(int)direction];
                        pos1.x += worldX;
                        pos1.y += worldZ;
                        pos2.x += worldX;
                        pos2.y += worldZ;
                        vertices.Add(new Vector3(pos1.x, bottomHeight, pos1.y));
                        vertices.Add(new Vector3(pos2.x, bottomHeight, pos2.y));
                        vertices.Add(new Vector3(pos2.x, topHeight, pos2.y));
                        vertices.Add(new Vector3(pos1.x, topHeight, pos1.y));

                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, chunkLocalZ * uvYCoeff));
                        uv.Add(new Vector2(chunkLocalX * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, (chunkLocalZ + 1) * uvYCoeff));
                        uv.Add(new Vector2((chunkLocalX + 1) * uvXCoeff, chunkLocalZ * uvYCoeff));
                    }

                    (Interval, Interval) CalculateVisiblePart(in Interval input, in BlockData otherBlock)
                    {
                        if (otherBlock.IsEmpty)
                            return (input, Interval.Empty);
                        return input.Subtract(new Interval(otherBlock.MinHeight - baseLayerVisibleWidth, otherBlock.Height));
                    }

                    Blocks Filter(in Blocks input, Renderer.TerrainLayerToRender layer)
                    {
                        if(layer == Renderer.TerrainLayerToRender.Base)
                            return new Blocks(BlockType.Empty, BlockType.Empty);
                        else if(layer == Renderer.TerrainLayerToRender.Underground)
                            return new Blocks(BlockType.Empty, input.Underground);
                        else
                            return input;
                    }
                }

            var baseMesh = new Mesh();
            baseMesh.SetVertices(baseVertices);
            baseMesh.SetIndices(baseIndices.ToArray(), MeshTopology.Quads, 0);
            baseMesh.SetUVs(0, baseUv);
            baseMesh.RecalculateNormals();

            var underMesh = new Mesh();
            underMesh.SetVertices(underVertices);
            underMesh.SetIndices(underIndices.ToArray(), MeshTopology.Quads, 0);
            underMesh.SetUVs(0, underUv);
            underMesh.RecalculateNormals();

            var mainMesh = new Mesh();
            mainMesh.SetVertices(mainVertices);
            mainMesh.SetIndices(mainIndices.ToArray(), MeshTopology.Quads, 0);
            mainMesh.SetUVs(0, mainUv);
            mainMesh.RecalculateNormals();

            var (baseTexture, underTexture, mainTexture) = CreateBlockTexture2(map, bounds);

            return ((baseMesh, baseTexture), (underMesh, underTexture), (mainMesh, mainTexture));
        }

        public (Mesh mesh, Texture texture) CreateObjectMesh(ObjectMap mapObject, TriRunner renderSettings)
        {
            var bounds = mapObject.Bounds;
            var heightMap = mapObject.GetHeightMap();
            var blockMap = mapObject.GetBlockMap();

            var groundVertIndexBuffer = new int[bounds.Size.X + 1, bounds.Size.Z + 1];
            var groundVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1))
                { Vector3.zero};        //Dummy value to make default 0 values of baseVertIndexBuffer a invalid
            var groundIndices = new List<int>(groundVertices.Count * 2);
            var commonUv = new List<Vector2>(groundVertices.Count) { Vector2.zero };

            //Set quads
            for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                {
                    var mapLocalX = worldX - bounds.Min.X;
                    var mapLocalZ = worldZ - bounds.Min.Z;
                    var chunkLocalX = mapLocalX;
                    var chunkLocalZ = mapLocalZ;

                    ref readonly var block = ref blockMap[mapLocalX, mapLocalZ];
                    if (block.IsEmpty)
                        continue;

                    //Block culling
                    var occlusionState = mapObject.GetOverlapState(new Vector2i(worldX, worldZ));
                    if (occlusionState.state == BlockOverlapState.Under && occlusionState.map == mapObject)
                        continue;

                    //Prepare block vertices
                    int index00, index01, index10, index11;
                    int startIndexCounter = groundVertices.Count;
                    if (groundVertIndexBuffer[chunkLocalX, chunkLocalZ] == 0)
                    {
                        index00 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX, chunkLocalZ] = index00;
                        groundVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Nominal, worldZ));
                        commonUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            chunkLocalZ / (float) bounds.Size.Z));

                        startIndexCounter++;
                        groundVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Base, worldZ));
                        commonUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            chunkLocalZ / (float) bounds.Size.Z));
                    }
                    else
                    {
                        index00 = groundVertIndexBuffer[chunkLocalX, chunkLocalZ];
                    }

                    if (groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1] == 0)
                    {
                        index01 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1] = index01;
                        groundVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ + 1].Nominal,
                            worldZ + 1));
                        commonUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));

                        startIndexCounter++;
                        groundVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ + 1].Base,
                            worldZ + 1));
                        commonUv.Add(new Vector2(chunkLocalX / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        index01 = groundVertIndexBuffer[chunkLocalX, chunkLocalZ + 1];
                    }

                    if (groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ] == 0)
                    {
                        index10 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ] = index10;
                        groundVertices.Add(new Vector3(worldX + 1, heightMap[mapLocalX + 1, mapLocalZ].Nominal,
                            worldZ));
                        commonUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ) / (float) bounds.Size.Z));

                        startIndexCounter++;
                        groundVertices.Add(new Vector3(worldX + 1, heightMap[mapLocalX + 1, mapLocalZ].Base,
                            worldZ));
                        commonUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        index10 = groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ];
                    }

                    if (groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1] == 0)
                    {
                        index11 = startIndexCounter++;
                        groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1] = index11;
                        groundVertices.Add(new Vector3(worldX + 1, heightMap[mapLocalX + 1, mapLocalZ + 1].Nominal,
                            worldZ + 1));
                        commonUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));

                        startIndexCounter++;
                        groundVertices.Add(new Vector3(worldX + 1, heightMap[mapLocalX + 1, mapLocalZ + 1].Base,
                            worldZ + 1));
                        commonUv.Add(new Vector2((chunkLocalX + 1) / (float) bounds.Size.X,
                            (chunkLocalZ + 1) / (float) bounds.Size.Z));
                    }
                    else
                    {
                        index11 = groundVertIndexBuffer[chunkLocalX + 1, chunkLocalZ + 1];
                    }

                    //Make proper top quad
                    if (Mathf.Abs(heightMap[mapLocalX, mapLocalZ].Nominal -
                                  heightMap[mapLocalX + 1, mapLocalZ + 1].Nominal) <
                        Mathf.Abs(heightMap[mapLocalX + 1, mapLocalZ].Nominal -
                                  heightMap[mapLocalX, mapLocalZ + 1].Nominal))
                    {
                        groundIndices.Add(index00);
                        groundIndices.Add(index01);
                        groundIndices.Add(index11);

                        groundIndices.Add(index00);
                        groundIndices.Add(index11);
                        groundIndices.Add(index10);
                    }
                    else
                    {
                        groundIndices.Add(index00);
                        groundIndices.Add(index01);
                        groundIndices.Add(index10);

                        groundIndices.Add(index10);
                        groundIndices.Add(index01);
                        groundIndices.Add(index11);
                    }

                    //Make proper bottom quad
                    if (Mathf.Abs(heightMap[mapLocalX, mapLocalZ].Base -
                                  heightMap[mapLocalX + 1, mapLocalZ + 1].Base) <
                        Mathf.Abs(heightMap[mapLocalX + 1, mapLocalZ].Base -
                                  heightMap[mapLocalX, mapLocalZ + 1].Base))
                    {
                        groundIndices.Add(index00 + 1);
                        groundIndices.Add(index11 + 1);
                        groundIndices.Add(index01 + 1);

                        groundIndices.Add(index00 + 1);
                        groundIndices.Add(index10 + 1);
                        groundIndices.Add(index11 + 1);
                    }
                    else
                    {
                        groundIndices.Add(index00 + 1);
                        groundIndices.Add(index10 + 1);
                        groundIndices.Add(index01 + 1);

                        groundIndices.Add(index10 + 1);
                        groundIndices.Add(index11 + 1);
                        groundIndices.Add(index01 + 1);
                    }

                    //Draw sides if needed
                    var neighbors = mapObject.GetNeighborBlocks(new Vector2i(worldX, worldZ));
                    foreach (var dir in Directions.Cardinal)
                    {
                        if (neighbors[dir].IsEmpty)
                        {
                            switch (dir)
                            {
                                case Side2d.Forward:
                                    DrawObjectSide(index01, index11);
                                    break;
                                case Side2d.Right:
                                    DrawObjectSide(index11, index10);
                                    break;
                                case Side2d.Back:
                                    DrawObjectSide(index10, index00);
                                    break;
                                case Side2d.Left:
                                    DrawObjectSide(index00, index01);
                                    break;
                            }
                        }
                    }
                }


            var groundMesh = new Mesh();
            groundMesh.SetVertices(groundVertices);
            groundMesh.SetTriangles(groundIndices, 0);
            groundMesh.SetUVs(0, commonUv);
            groundMesh.RecalculateNormals();

            var mainTexture = CreateBlockTexture(mapObject, bounds, renderSettings);

            return (groundMesh, mainTexture);

            //Draw side of object block. Parameters: top and bottom point indices
            void DrawObjectSide(int topPoint1, int topPoint2)
            {
                var bottomPoint1 = topPoint1 + 1;
                var bottomPoint2 = topPoint2 + 1;

                groundIndices.Add(bottomPoint1);
                groundIndices.Add(bottomPoint2);
                groundIndices.Add(topPoint2);

                groundIndices.Add(bottomPoint1);
                groundIndices.Add(topPoint2);
                groundIndices.Add(topPoint1);
            }
        }

        public GameObject CreateActorObject(Actor actor, TriRunner settings)
        {
            var newActor = Object.Instantiate(settings.ActorPrefab, actor.Position, actor.Rotation);

            //Setup actor view...
            var view = newActor.GetComponent<ActorView>();
            view.Init(actor);

            return newActor;
        }

        private Texture CreateBlockTexture(BaseBlockMap map, Bounds2i bounds, TriRunner mode)
        {
            bounds = bounds.Intersect(map.Bounds);

            Texture2D result;
            Color[] colors;
            if (mode.TextureMode == Renderer.BlockTextureMode.InfluenceDither)
            {
                //Dither mode texture more detailed (4 pixels for every block)
                result = new Texture2D(bounds.Size.X * 2, bounds.Size.Z * 2, TextureFormat.RGBA32, true, true);
                result.filterMode = FilterMode.Point;
                result.wrapMode = TextureWrapMode.Clamp;
                colors = new Color[bounds.Size.X * bounds.Size.Z * 4];
            }
            else
            {
                result = new Texture2D(bounds.Size.X, bounds.Size.Z, TextureFormat.RGBA32, true, true);
                result.filterMode = FilterMode.Bilinear;
                result.wrapMode = TextureWrapMode.Clamp;
                colors = new Color[bounds.Size.X * bounds.Size.Z];
            }

            if (mode.TextureMode == Renderer.BlockTextureMode.InfluenceDither)
            {
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                    for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                    {
                        var chunkLocalX = worldX - bounds.Min.X;
                        var chunkLocalZ = worldZ - bounds.Min.Z;
                        var flatIndex1 = 4 * chunkLocalZ * bounds.Size.X + 2 * chunkLocalX;
                        var flatIndex2 = flatIndex1 + 1;
                        var flatIndex3 = flatIndex1 + 2 * bounds.Size.X;
                        var flatIndex4 = flatIndex3 + 1;

                        var blockColors = InfluenceToColorDither(_macroMap.GetInfluence(new Vector2i(worldX, worldZ)));
                        colors[flatIndex1] = blockColors.Item1;
                        colors[flatIndex2] = blockColors.Item2;
                        colors[flatIndex3] = blockColors.Item3;
                        colors[flatIndex4] = blockColors.Item4;
                    }
            }
            else
            {

                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                    for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                    {
                        var mapLocalX = worldX - map.Bounds.Min.X;
                        var mapLocalZ = worldZ - map.Bounds.Min.Z;
                        var chunkLocalX = worldX - bounds.Min.X;
                        var chunkLocalZ = worldZ - bounds.Min.Z;
                        var flatIndex = chunkLocalZ * bounds.Size.X + chunkLocalX;

                        if (mode.TextureMode == Renderer.BlockTextureMode.InfluenceSmooth)
                        {
                            var influence = _macroMap.GetInfluence(new Vector2i(worldX, worldZ));
                            var influenceColor = InfluenceToColorSmooth(influence);
                            colors[flatIndex] = influenceColor;
                        }
                        else if (mode.TextureMode == Renderer.BlockTextureMode.InfluenceHard)
                        {
                            var influence = _macroMap.GetInfluence(new Vector2i(worldX, worldZ));
                            var influenceColor = InfluenceToColorHard(influence);
                            colors[flatIndex] = influenceColor;
                        }
                        else if (mode.TextureMode == Renderer.BlockTextureMode.Blocks)
                        {
                            BlockType block;
                            if (mode.RenderLayer == Renderer.TerrainLayerToRender.Main)
                                block = map.GetBlockMap()[mapLocalX, mapLocalZ].Top;
                            else if (mode.RenderLayer == Renderer.TerrainLayerToRender.Underground)
                            {
                                var blocks = map.GetBlockMap()[mapLocalX, mapLocalZ];
                                if (blocks.Underground != BlockType.Empty)
                                    block = blocks.Underground;
                                else
                                    block = blocks.Base;
                            }
                            else
                            {
                                block = map.GetBlockMap()[mapLocalX, mapLocalZ].Top;
                            }
                            colors[flatIndex] = BlockToColor(block);
                        }
                    }
            }

            result.SetPixels(colors);
            result.Apply(true, true);
            return result;
        }

        private (Texture Base, Texture Under, Texture Main) CreateBlockTexture2(BaseBlockMap map, Bounds2i bounds)
        {
            bounds = bounds.Intersect(map.Bounds);

            if (bounds.IsEmpty)
                return (Texture2D.blackTexture, Texture2D.blackTexture, Texture2D.blackTexture);

            var baseTexture = new Texture2D(bounds.Size.X, bounds.Size.Z, TextureFormat.RGBA32, true, true);
            baseTexture.filterMode = FilterMode.Point;
            baseTexture.wrapMode = TextureWrapMode.Clamp;
            var baseColors = new Color[bounds.Size.X * bounds.Size.Z];

            var underTexture = new Texture2D(bounds.Size.X, bounds.Size.Z, TextureFormat.RGBA32, true, true);
            underTexture.filterMode = FilterMode.Point;
            underTexture.wrapMode = TextureWrapMode.Clamp;
            var underColors = new Color[bounds.Size.X * bounds.Size.Z];

            var mainTexture = new Texture2D(bounds.Size.X, bounds.Size.Z, TextureFormat.RGBA32, true, true);
            mainTexture.filterMode = FilterMode.Point;
            mainTexture.wrapMode = TextureWrapMode.Clamp;
            var mainColors = new Color[bounds.Size.X * bounds.Size.Z];


            for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;
                    var flatIndex = chunkLocalZ * bounds.Size.X + chunkLocalX;

                    ref readonly var blocks = ref map.GetBlockMap()[mapLocalX, mapLocalZ];
                    baseColors[flatIndex] = BlockToColor(blocks.Base);

                    var block = blocks.Underground;
                    underColors[flatIndex] = BlockToColor(block);

                    mainColors[flatIndex] = BlockToColor(blocks.Ground);
                }

            baseTexture.SetPixels(baseColors);
            baseTexture.Apply(true, true);

            underTexture.SetPixels(underColors);
            underTexture.Apply(true, true);

            mainTexture.SetPixels(mainColors);
            mainTexture.Apply(true, true);

            return (baseTexture, underTexture, mainTexture);
        }


        /// <summary>
        /// Create macro cell mesh with influence vertex coloring
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="colors"></param>
        /// <param name="indices"></param>
        /// <param name="cell"></param>
        /// <param name="influence"></param>
        private void CreateMacroCell(List<Vector3> vertices, List<Color> colors, List<int> indices, Cell cell,
            Renderer.MacroCellInfluenceMode influence)
        {
            var baseIndex = vertices.Count;

            vertices.Add(cell.CenterPoint);
            vertices.Add(new Vector3(cell.Vertices[0].Position.X, cell.Vertices[0].Height.Nominal,
                cell.Vertices[0].Position.Y));
            vertices.Add(new Vector3(cell.Vertices[1].Position.X, cell.Vertices[1].Height.Nominal,
                cell.Vertices[1].Position.Y));
            vertices.Add(new Vector3(cell.Vertices[2].Position.X, cell.Vertices[2].Height.Nominal,
                cell.Vertices[2].Position.Y));
            vertices.Add(new Vector3(cell.Vertices[3].Position.X, cell.Vertices[3].Height.Nominal,
                cell.Vertices[3].Position.Y));
            vertices.Add(new Vector3(cell.Vertices[4].Position.X, cell.Vertices[4].Height.Nominal,
                cell.Vertices[4].Position.Y));
            vertices.Add(new Vector3(cell.Vertices[5].Position.X, cell.Vertices[5].Height.Nominal,
                cell.Vertices[5].Position.Y));

            indices.Add(baseIndex);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 4);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 4);
            indices.Add(baseIndex + 5);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 5);
            indices.Add(baseIndex + 6);
            indices.Add(baseIndex);
            indices.Add(baseIndex + 6);
            indices.Add(baseIndex + 1);

            if (influence == Renderer.MacroCellInfluenceMode.Interpolated)
            {
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(InfluenceToColorSmooth(cell.Vertices[0].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices[1].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices[2].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices[3].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices[4].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices[5].Influence));
            }
            else
            {
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(cell.Zone.Biome.LayoutColor);
            }
        }

        private Color InfluenceToColorSmooth(Influence influence)
        {
            if (influence.IsEmpty)
                return Color.magenta;

            var result = Color.clear;
            foreach (var infl in influence)
                result += _macroMap.Zones[infl.Item1].Biome.LayoutColor * infl.Item2;

            return result;
        }

        private Color InfluenceToColorHard(Influence influence)
        {
            if (influence.IsEmpty)
                return Color.magenta;

            return _macroMap.Zones[influence.GetMostInfluenceZone()].Biome.LayoutColor;
        }

        private ValueTuple<Color, Color, Color, Color> InfluenceToColorDither(Influence influence)
        {
            if (influence.IsEmpty)
                return new ValueTuple<Color, Color, Color, Color>(Color.magenta, Color.magenta, Color.magenta, Color.magenta);

            if (influence.Count == 1)
            {
                var color = _macroMap.Zones[influence.Zone1Id].Biome.LayoutColor;
                return new ValueTuple<Color, Color, Color, Color>(color, color, color,color);
            }
            else if (influence.Count == 2)
            {
                var color1 = _macroMap.Zones[influence.Zone1Id].Biome.LayoutColor;
                var color2 = _macroMap.Zones[influence.Zone2Id].Biome.LayoutColor;
                return new ValueTuple<Color, Color, Color, Color>(color1, color2, color2, color1);
            }
            else if (influence.Count == 3)
            {
                var color1 = _macroMap.Zones[influence.Zone1Id].Biome.LayoutColor;
                var color2 = _macroMap.Zones[influence.Zone2Id].Biome.LayoutColor;
                var color3 = _macroMap.Zones[influence.Zone3Id].Biome.LayoutColor;
                return new ValueTuple<Color, Color, Color, Color>(color1, color2, color3, color1);
            }
            else
            {
                var color1 = _macroMap.Zones[influence.Zone1Id].Biome.LayoutColor;
                var color2 = _macroMap.Zones[influence.Zone2Id].Biome.LayoutColor;
                var color3 = _macroMap.Zones[influence.Zone3Id].Biome.LayoutColor;
                var color4 = _macroMap.Zones[influence.Zone4Id].Biome.LayoutColor;
                return new ValueTuple<Color, Color, Color, Color>(color1, color2, color3, color4);
            }
        }

        private (int, int) InfluenceToColorsIndex(Influence influence)
        {
            if (influence.IsEmpty)
                return (Zone.InvalidId, Zone.InvalidId);

            if (influence.Count == 1)
                return (influence.Zone1Id, influence.Zone1Id);

            var maxInfluenceIndex1 = -1;
            var maxInfluenceValue1 = float.MinValue;
            var maxInfluenceIndex2 = -1;
            var maxInfluenceValue2 = float.MinValue;

            for (int i = 0; i < influence.Count; i++)
            {
                if (influence.GetWeight(i) > maxInfluenceValue1)
                {
                    maxInfluenceValue1 = influence.GetWeight(i);
                    maxInfluenceIndex1 = i;
                }
            }

            if (maxInfluenceValue1 > 0.8)
            {
                maxInfluenceIndex2 = maxInfluenceIndex1;
            }
            else
            {
                for (int i = 0; i < influence.Count; i++)
                {
                    var influenceValue2 = influence.GetWeight(i);
                    if (influenceValue2 > maxInfluenceValue2 && influenceValue2 < maxInfluenceValue1)
                    {
                        maxInfluenceValue2 = influenceValue2;
                        maxInfluenceIndex2 = i;
                    }
                }

                if (maxInfluenceIndex2 == -1)
                    maxInfluenceIndex2 = maxInfluenceIndex1;
            }

            return (maxInfluenceIndex1, maxInfluenceIndex2);
        }

        private Color BlockToColor(BlockType block)
        {
            if (block == BlockType.Empty)
                return Color.clear;

            return _defaultBlockColor[block];
        }
    }
}
