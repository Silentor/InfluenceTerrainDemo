using System;
using System.Collections.Generic;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using TerrainDemo.Tri;
using UnityEngine;
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

        public Mesher(MacroMap macroMap, TriRunner settings)
        {
            _settings = settings;
            _macroMap = macroMap;

            var blocksSettings = Resources.LoadAll<BlockSettings>("");
            foreach (var biome in blocksSettings)
            {
                _defaultBlockColor[biome.Block] = biome.DefaultColor;
            }
        }

        public Tuple<Mesh, Texture> CreateMesh(MicroMap map, Renderer.MicroRenderMode mode)
        {
            return CreateMesh(map, map.Bounds, mode);
        }

        public Tuple<Mesh, Texture> CreateMesh(MicroMap map, Bounds2i bounds, Renderer.MicroRenderMode mode)
        {
            bounds = bounds.Intersect(map.Bounds);

            var resultMesh = new Mesh();

            if (bounds.IsEmpty)
                return new Tuple<Mesh, Texture>(resultMesh, Texture2D.blackTexture);

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

                    if(mode.RenderMainLayer)
                        vertices.Add(new Vector3(worldX, heights[mapLocalX, mapLocalZ].Nominal, worldZ));
                    else
                        vertices.Add(new Vector3(worldX, heights[mapLocalX, mapLocalZ].BaseHeight, worldZ));

                    uvs.Add(new UnityEngine.Vector2(chunkLocalX / (float)bounds.Size.X, chunkLocalZ / (float)bounds.Size.Z));
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
                    if (mode.RenderMainLayer)
                    {
                        height00 = heights[mapLocalX, mapLocalZ].Nominal;
                        height01 = heights[mapLocalX, mapLocalZ + 1].Nominal;
                        height11 = heights[mapLocalX + 1, mapLocalZ + 1].Nominal;
                        height10 = heights[mapLocalX + 1, mapLocalZ].Nominal;
                    }
                    else
                    {
                        height00 = heights[mapLocalX, mapLocalZ].BaseHeight;
                        height01 = heights[mapLocalX, mapLocalZ + 1].BaseHeight;
                        height11 = heights[mapLocalX + 1, mapLocalZ + 1].BaseHeight;
                        height10 = heights[mapLocalX + 1, mapLocalZ].BaseHeight;
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

            var resultTexture = CreateBlockTexture(map, bounds, mode);

            return new Tuple<Mesh, Texture>(resultMesh, resultTexture);
        }

        public Mesh CreateMesh(MacroMap mapMesh, Renderer.MacroCellInfluenceMode influence)
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

        private Texture CreateBlockTexture(MicroMap map, Bounds2i bounds, Renderer.MicroRenderMode mode)
        {
            bounds = bounds.Intersect(map.Bounds);

            Texture2D result;
            Color[] colors;
            if (mode.BlockMode == Renderer.BlockRenderMode.InfluenceDither)
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

            if (mode.BlockMode == Renderer.BlockRenderMode.InfluenceDither)
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

                        if (mode.BlockMode == Renderer.BlockRenderMode.InfluenceSmooth)
                        {
                            var influence = _macroMap.GetInfluence(new Vector2i(worldX, worldZ));
                            var influenceColor = InfluenceToColorSmooth(influence);
                            colors[flatIndex] = influenceColor;
                        }
                        else if (mode.BlockMode == Renderer.BlockRenderMode.InfluenceHard)
                        {
                            var influence = _macroMap.GetInfluence(new Vector2i(worldX, worldZ));
                            var influenceColor = InfluenceToColorHard(influence);
                            colors[flatIndex] = influenceColor;
                        }
                        else if (mode.BlockMode == Renderer.BlockRenderMode.Blocks)
                        {
                            BlockType block;
                            if (mode.RenderMainLayer)
                                block = map.GetBlockMap()[mapLocalX, mapLocalZ].Top;
                            else
                                block = map.GetBlockMap()[mapLocalX, mapLocalZ].Base;
                            colors[flatIndex] = BlockToColor(block);
                        }
                    }
            }

            result.SetPixels(colors);
            result.Apply(true, true);
            return result;
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

        private Tuple<int, int> InfluenceToColorsIndex(Influence influence)
        {
            if (influence.IsEmpty)
                return new Tuple<int, int>(Macro.Zone.InvalidId, Macro.Zone.InvalidId);

            if (influence.Count == 1)
                return new Tuple<int, int>(influence.Zone1Id, influence.Zone1Id);

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

            return new Tuple<int, int>(maxInfluenceIndex1, maxInfluenceIndex2);
        }

        private Color BlockToColor(BlockType block)
        {
            if (block == BlockType.Empty)
                return Color.clear;

            return _defaultBlockColor[block];
        }
    }
}
