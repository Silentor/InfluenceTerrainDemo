using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;

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

            var blocksSettings = Resources.LoadAll<BlockSettings>("");
            foreach (var blockSetting in blocksSettings)
                _defaultBlockColor[blockSetting.Block] = blockSetting.DefaultColor;
        }

        /*
        public (Mesh, Texture) CreateMesh(MicroMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            bounds = bounds.Intersect(map.Bounds);

            var resultMesh = new Mesh();

            if (bounds.IsEmpty)
                return (resultMesh, Texture2D.blackTexture);

            var heights = map.GetHeightMap();
            var blocks = map.GetBlockMap();
            var vertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z +1));
            var indices = new List<int>(vertices.Count * 2);
            var uvs = new List<UnityEngine.Vector2>(vertices.Count);

            var vertCountX = bounds.Size.X + 1;
            var vertCountZ = bounds.Size.Z + 1;

            //Set vertices
            for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z + 1; worldZ++)
                for (int worldX = bounds.Min.X; worldX <= bounds.Max.X + 1; worldX++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;

                    if(renderSettings.RenderLayer == Renderer.TerrainLayerToRender.Main)
                        vertices.Add(new Vector3(worldX, heights[mapLocalX, mapLocalZ].Nominal, worldZ));
                    else if(renderSettings.RenderLayer == Renderer.TerrainLayerToRender.Underground)
                        vertices.Add(new Vector3(worldX, heights[mapLocalX, mapLocalZ].UndergroundHeight, worldZ));
                    else  //Base layer
                        vertices.Add(new Vector3(worldX, heights[mapLocalX, mapLocalZ].BaseHeight, worldZ));

                    uvs.Add(new Vector2(chunkLocalX / (float)bounds.Size.X, chunkLocalZ / (float)bounds.Size.Z));
                }


            //Set quads
            for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;
                    var startIndex = chunkLocalZ * (bounds.Size.X + 1) + chunkLocalX;

                    if( blocks[mapLocalX, mapLocalZ].IsEmpty)
                        continue;

                    float height00, height01, height11, height10;
                    if (renderSettings.RenderLayer == Renderer.TerrainLayerToRender.Main)
                    {
                        height00 = heights[mapLocalX, mapLocalZ].Nominal;
                        height01 = heights[mapLocalX, mapLocalZ + 1].Nominal;
                        height11 = heights[mapLocalX + 1, mapLocalZ + 1].Nominal;
                        height10 = heights[mapLocalX + 1, mapLocalZ].Nominal;
                    }
                    else
                    {
                        height00 = heights[mapLocalX, mapLocalZ].UndergroundHeight;
                        height01 = heights[mapLocalX, mapLocalZ + 1].UndergroundHeight;
                        height11 = heights[mapLocalX + 1, mapLocalZ + 1].UndergroundHeight;
                        height10 = heights[mapLocalX + 1, mapLocalZ].UndergroundHeight;
                    }

                    if (Mathf.Abs(height00 - height11) < Mathf.Abs(height10 - height01))
                    {
                        indices.Add(startIndex);
                        indices.Add(startIndex + vertCountX);
                        indices.Add(startIndex + vertCountX + 1);

                        indices.Add(startIndex);
                        indices.Add(startIndex + vertCountX + 1);
                        indices.Add(startIndex + 1);
                    }
                    else
                    {
                        indices.Add(startIndex);
                        indices.Add(startIndex + vertCountX);
                        indices.Add(startIndex + 1);

                        indices.Add(startIndex + vertCountX);
                        indices.Add(startIndex + vertCountX + 1);
                        indices.Add(startIndex + 1);
                    }
                }

            resultMesh.SetVertices(vertices);

            resultMesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            resultMesh.SetUVs(0, uvs);
            resultMesh.RecalculateBounds();
            resultMesh.RecalculateNormals();

            var resultTexture = CreateBlockTexture(map, bounds, renderSettings);

            return (resultMesh, resultTexture);
        }
        */

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

        public ((Mesh, Texture) Base, (Mesh, Texture) Under, (Mesh, Texture) Main) CreateTerrainMesh(MicroMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            bounds = bounds.Intersect(map.Bounds);

            if (bounds.IsEmpty)
            {
                var emptyMesh = new Mesh();
                return ((emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture));
            }

            var heightMap = map.GetHeightMap();
            var blockMap = map.GetBlockMap();

            var baseVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var baseIndices = new List<int>(baseVertices.Count * 2);
            var baseUvs = new List<UnityEngine.Vector2>(baseVertices.Count);

            var underVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var underIndices = new List<int>(underVertices.Count * 2);
            //var underUvs = new List<UnityEngine.Vector2>(underVertices.Count);

            var mainVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var mainIndices = new List<int>(mainVertices.Count * 2);
            //var mainUvs = new List<UnityEngine.Vector2>(mainVertices.Count);

            var vertCountX = bounds.Size.X + 1;
            var vertCountZ = bounds.Size.Z + 1;

            //Set quads
            bool isCaveMode = true;
            for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;
                    var startIndex = chunkLocalZ * (bounds.Size.X + 1) + chunkLocalX;
                    
                    var block = blockMap[mapLocalX, mapLocalZ];
                    if (block.IsEmpty)
                        continue;

                    //Cave mode, draw base layer (cave floor), flipped underground layer (cave ceil) amd main layer
                    //if (block.Underground == BlockType.Cave)
                    {
                        isCaveMode = true;

                        //Main layer
                        if (block.Ground != BlockType.Empty)
                        {
                            mainIndices.Add(startIndex);
                            mainIndices.Add(startIndex + vertCountX);
                            mainIndices.Add(startIndex + vertCountX + 1);

                            mainIndices.Add(startIndex);
                            mainIndices.Add(startIndex + vertCountX + 1);
                            mainIndices.Add(startIndex + 1);
                        }

                        if (block.Underground == BlockType.Cave)
                        {
                            //Flipped triangles, draw cave ceil
                            underIndices.Add(startIndex);
                            underIndices.Add(startIndex + vertCountX + 1);
                            underIndices.Add(startIndex + vertCountX);

                            underIndices.Add(startIndex);
                            underIndices.Add(startIndex + 1);
                            underIndices.Add(startIndex + vertCountX + 1);
                        }
                        else
                        {
                            if (block.Ground == BlockType.Empty && block.Underground != BlockType.Empty)
                            {
                                underIndices.Add(startIndex);
                                underIndices.Add(startIndex + vertCountX);
                                underIndices.Add(startIndex + vertCountX + 1);

                                underIndices.Add(startIndex);
                                underIndices.Add(startIndex + vertCountX + 1);
                                underIndices.Add(startIndex + 1);
                            }
                        }

                        if (block.Underground == BlockType.Cave ||
                            (block.Ground == BlockType.Empty && block.Underground == BlockType.Empty))
                        {

                            //Cave floor
                            baseIndices.Add(startIndex);
                            baseIndices.Add(startIndex + vertCountX);
                            baseIndices.Add(startIndex + vertCountX + 1);

                            baseIndices.Add(startIndex);
                            baseIndices.Add(startIndex + vertCountX + 1);
                            baseIndices.Add(startIndex + 1);
                        }

                    }
                    /*
                    else     //Simple case - combined layers to one 
                    {
                        mainIndices.Add(startIndex);
                        mainIndices.Add(startIndex + vertCountX);
                        mainIndices.Add(startIndex + vertCountX + 1);

                        mainIndices.Add(startIndex);
                        mainIndices.Add(startIndex + vertCountX + 1);
                        mainIndices.Add(startIndex + 1);
                    }*/
                }

            //Set vertices
            if (isCaveMode)
            {
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z + 1; worldZ++)
                    for (int worldX = bounds.Min.X; worldX <= bounds.Max.X + 1; worldX++)
                    {
                        var mapLocalX = worldX - map.Bounds.Min.X;
                        var mapLocalZ = worldZ - map.Bounds.Min.Z;
                        var chunkLocalX = worldX - bounds.Min.X;
                        var chunkLocalZ = worldZ - bounds.Min.Z;

                        mainVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Main, worldZ));
                        underVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Underground, worldZ));
                        baseVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Base, worldZ));

                        baseUvs.Add(new Vector2(chunkLocalX / (float)bounds.Size.X, chunkLocalZ / (float)bounds.Size.Z));
                    }
            }
            else
            {
                for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z + 1; worldZ++)
                    for (int worldX = bounds.Min.X; worldX <= bounds.Max.X + 1; worldX++)
                    {
                        var mapLocalX = worldX - map.Bounds.Min.X;
                        var mapLocalZ = worldZ - map.Bounds.Min.Z;
                        var chunkLocalX = worldX - bounds.Min.X;
                        var chunkLocalZ = worldZ - bounds.Min.Z;

                        mainVertices.Add(new Vector3(worldX, heightMap[mapLocalX, mapLocalZ].Nominal, worldZ));
                        baseUvs.Add(new Vector2(chunkLocalX / (float) bounds.Size.X, chunkLocalZ / (float) bounds.Size.Z));
                    }
            }

            if (isCaveMode)
            {
                var baseMesh = new Mesh();
                baseMesh.SetVertices(baseVertices);
                baseMesh.SetTriangles(baseIndices, 0);
                baseMesh.SetUVs(0, baseUvs);
                baseMesh.RecalculateNormals();

                var underMesh = new Mesh();
                underMesh.SetVertices(underVertices);
                underMesh.SetTriangles(underIndices, 0);
                underMesh.SetUVs(0, baseUvs);
                underMesh.RecalculateNormals();

                var mainMesh = new Mesh();
                mainMesh.SetVertices(mainVertices);
                mainMesh.SetTriangles(mainIndices, 0);
                mainMesh.SetUVs(0, baseUvs);
                mainMesh.RecalculateNormals();

                var (baseTexture, underTexture, mainTexture) = CreateBlockTexture2(map, bounds);

                return ((baseMesh, baseTexture), (underMesh, underTexture), (mainMesh, mainTexture));
            }
            else
            {
                var combinedMesh = new Mesh();
                combinedMesh.SetVertices(mainVertices);
                combinedMesh.SetTriangles(mainIndices, 0);
                combinedMesh.SetUVs(0, baseUvs);
                combinedMesh.RecalculateNormals();

                var combinedTexture = CreateBlockTexture(map, bounds, renderSettings);

                return ((null, null), (null, null), (combinedMesh, combinedTexture));
            }
        }

        public ((Mesh, Texture) Base, (Mesh, Texture) Under, (Mesh, Texture) Main) CreateMinecraftMesh(MicroMap map, Bounds2i bounds, TriRunner renderSettings)
        {
            bounds = bounds.Intersect(map.Bounds);

            if (bounds.IsEmpty)
            {
                var emptyMesh = new Mesh();
                return ((emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture), (emptyMesh, Texture2D.blackTexture));
            }

            /*
            Blocks[,] chunk;
            if (renderSettings.RenderLayer == Renderer.TerrainLayerToRender.Main)
            {
                chunk = map.GetBlockMapRegion(bounds);
            }
            else
            {
                if(renderSettings.RenderLayer == Renderer.TerrainLayerToRender.Underground)
                    chunk = map.GetBlockMapRegion(bounds, b => new Blocks(BlockType.Empty, b.Underground, b.Heights));
                else
                    chunk = map.GetBlockMapRegion(bounds, b => new Blocks(BlockType.Empty, BlockType.Empty, b.Heights));
            }
            */
            var blockMap = map.GetBlockMap();

            var baseVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var baseIndices = new List<int>(baseVertices.Count * 2);
            var baseUv = new List<UnityEngine.Vector2>(baseVertices.Count);

            var underVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var underIndices = new List<int>(underVertices.Count * 2);
            var underUv = new List<UnityEngine.Vector2>(underVertices.Count);

            var mainVertices = new List<Vector3>((bounds.Size.X + 1) * (bounds.Size.Z + 1));
            var mainIndices = new List<int>(mainVertices.Count * 2);
            var mainUv = new List<UnityEngine.Vector2>(mainVertices.Count);

            var uvXCoeff = 1f / bounds.Size.X;
            var uvYCoeff = 1f / bounds.Size.Z;

            for (int worldZ = bounds.Min.Z; worldZ <= bounds.Max.Z; worldZ++)
                for (int worldX = bounds.Min.X; worldX <= bounds.Max.X; worldX++)
                {
                    var mapLocalX = worldX - map.Bounds.Min.X;
                    var mapLocalZ = worldZ - map.Bounds.Min.Z;
                    var chunkLocalX = worldX - bounds.Min.X;
                    var chunkLocalZ = worldZ - bounds.Min.Z;

                    var block = blockMap[mapLocalX, mapLocalZ];
                    //var block = chunk[chunkLocalX, chunkLocalZ];
                    if (block.IsEmpty)
                        continue;

                    block = Filter(block, renderSettings.RenderLayer);

                    //Draw block tops (or downs)
                    if (block.Underground == BlockType.Cave)
                    {
                        //Draw cave block (ground block must be)
                        DrawFloor(mainVertices, mainIndices, mainUv, block.Height.Main);
                        DrawCeil(underVertices, underIndices, underUv, block.Height.Underground);
                        DrawFloor(baseVertices, baseIndices, baseUv, block.Height.Base);
                    }
                    else
                    {
                        //Draw block top
                        if (block.Ground != BlockType.Empty)
                        {
                            DrawFloor(mainVertices, mainIndices, mainUv, block.Height.Main);
                        }
                        else if (block.Underground != BlockType.Empty)
                        {
                            DrawFloor(underVertices, underIndices, underUv, block.Height.Underground);
                        }
                        else
                            DrawFloor(baseVertices, baseIndices, baseUv, block.Height.Base);
                    }

                    //Draw block sides
                    var neighbors = map.GetNeighborBlocks(new Vector2i(worldX, worldZ));
                    foreach (var dir in Directions.Cardinal)
                    {
                        var neigh = neighbors[dir];
                        if (!neigh.IsEmpty)
                        {
                            neigh = Filter(neigh, renderSettings.RenderLayer);

                            //Draw Ground part of block side
                            if (block.Ground != BlockType.Empty)
                            {
                                //Calculate visible layer part and draw it in a simple way
                                var visiblePart = CalculateVisiblePart(block.GetMainLayerWidth(), neigh);

                                if(!visiblePart.IsEmpty)
                                    DrawBlockSide(mainVertices, mainIndices, mainUv, dir, visiblePart.Max, visiblePart.Min);
                            }

                            //Draw Underground part of block side (if solid)
                            if (block.Underground != BlockType.Empty && block.Underground != BlockType.Cave)
                            {
                                //Calculate visible layer part and draw it in a simple way
                                var visiblePart = CalculateVisiblePart(block.GetUnderLayerWidth(), neigh);

                                if (!visiblePart.IsEmpty)
                                    DrawBlockSide(underVertices, underIndices, underUv, dir, visiblePart.Max, visiblePart.Min);
                            }

                            //Draw Base part of block side
                            {
                                //Calculate visible layer part and draw it in a simple way
                                var visiblePart = CalculateVisiblePart(block.GetBaseLayerWidth(), neigh);

                                if (!visiblePart.IsEmpty)
                                    DrawBlockSide(baseVertices, baseIndices, baseUv, dir, visiblePart.Max, visiblePart.Min);
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
                        indices.Add(vertices.Count + 3);
                        indices.Add(vertices.Count + 2);
                        indices.Add(vertices.Count + 1);

                        vertices.Add(new Vector3(worldX, height, worldZ));
                        vertices.Add(new Vector3(worldX, height, worldZ + 1));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ + 1));
                        vertices.Add(new Vector3(worldX + 1, height, worldZ));

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

                    Interval CalculateVisiblePart(Interval input, in Blocks otherBlock)
                    {
                        if (otherBlock.IsSimple)
                            return input.Subtract(otherBlock.GetTotalWidth()).minPart;

                        //Hide by Base layer (Under layer is Cave bcoz otherBlock is not simple)
                        var onlyMain = input.Subtract(otherBlock.GetBaseLayerWidth());

                        Assert.IsTrue(onlyMain.maxPart.IsEmpty, "Base layer cant divide other layer to 2 parts");

                        if (onlyMain.minPart.IsEmpty)
                            return Interval.Empty;

                        //Hide by Main layer
                        var complete = onlyMain.minPart.Subtract(otherBlock.GetMainLayerWidth());

                        if (!complete.maxPart.IsEmpty)
                            return onlyMain.minPart;

                        return complete.minPart;
                    }

                    Blocks Filter(in Blocks input, Renderer.TerrainLayerToRender layer)
                    {
                        if(layer == Renderer.TerrainLayerToRender.Base)
                            return new Blocks(BlockType.Empty, BlockType.Empty, input.Height);
                        else if(layer == Renderer.TerrainLayerToRender.Underground)
                            return new Blocks(BlockType.Empty, input.Underground, input.Height);
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

        private Texture CreateBlockTexture(MicroMap map, Bounds2i bounds, TriRunner mode)
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
                                block = map.GetBlockMap()[mapLocalX, mapLocalZ].Base;
                            }
                            colors[flatIndex] = BlockToColor(block);
                        }
                    }
            }

            result.SetPixels(colors);
            result.Apply(true, true);
            return result;
        }

        private (Texture Base, Texture Under, Texture Main) CreateBlockTexture2(MicroMap map, Bounds2i bounds)
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

                    var blocks = map.GetBlockMap()[mapLocalX, mapLocalZ];
                    var block = blocks.Base;
                    baseColors[flatIndex] = BlockToColor(block);

                    block = blocks.Underground;
                    if (block == BlockType.Cave)
                        block = blocks.Ground;
                    //underColors[flatIndex] = BlockToColor(block);
                    underColors[flatIndex] = Color.cyan;

                    block = blocks.Ground;
                    mainColors[flatIndex] = BlockToColor(block);
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
                return (Macro.Zone.InvalidId, Macro.Zone.InvalidId);

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
