using System;
using System.Collections.Generic;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Micro;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Cell = TerrainDemo.Macro.Cell;
using Vector2 = OpenTK.Vector2;

namespace TerrainDemo.Tri
{
    /// <summary>
    /// Converts cells to Unity mesh
    /// </summary>
    public class TriMesher
    {
        private readonly TriRunner _settings;
        private readonly MacroMap _macroMap;

        public TriMesher(MacroMap macroMap, TriRunner settings)
        {
            _settings = settings;
            _macroMap = macroMap;
        }

        public Mesh CreateMesh(MicroMap map, TriRenderer.MicroInfluenceRenderMode mode)
        {
            return CreateMesh(map, map.Bounds, mode);
        }

        public Mesh CreateMesh(MicroMap map, Bounds2i bounds, TriRenderer.MicroInfluenceRenderMode mode)
        {
            var result = new Mesh();

            var allInfluence = map.GetInfluenceChunk();
            var heights = map.GetHeightMap();
            var vertices = new List<Vector3>(bounds.Size.X * bounds.Size.Z * 4);
            var indices = new List<int>(vertices.Count);
            var colors = new List<Color>(vertices.Count);

            for (int z = bounds.Min.Z; z <= bounds.Max.Z; z++)
                for (int x = bounds.Min.X; x <= bounds.Max.X; x++)
                {
                    if(!map.Bounds.Contains(new Vector2i(x, z)))
                        continue;

                    var startIndex = vertices.Count;
                    var localX = x - map.Bounds.Min.X;
                    var localZ = z - map.Bounds.Min.Z;

                    if (Mathf.Abs(heights[localX, localZ] - heights[localX + 1, localZ + 1]) <
                        Mathf.Abs(heights[localX + 1, localZ] - heights[localX, localZ + 1]))
                    {
                        vertices.Add(new Vector3(x, heights[localX, localZ], z));
                        vertices.Add(new Vector3(x, heights[localX, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x, heights[localX, localZ], z));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ], z));
                    }
                    else
                    {
                        vertices.Add(new Vector3(x, heights[localX, localZ], z));
                        vertices.Add(new Vector3(x, heights[localX, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ], z));
                        vertices.Add(new Vector3(x, heights[localX, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ + 1], z + 1));
                        vertices.Add(new Vector3(x + 1, heights[localX + 1, localZ], z));
                    }

                    indices.Add(startIndex);
                    indices.Add(startIndex + 1);
                    indices.Add(startIndex + 2);

                    indices.Add(startIndex + 3);
                    indices.Add(startIndex + 4);
                    indices.Add(startIndex + 5);

                    if (mode == TriRenderer.MicroInfluenceRenderMode.Smooth)
                    {
                        var influenceColor = InfluenceToColorSmooth(allInfluence[localX, localZ]);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                    }
                    else if (mode == TriRenderer.MicroInfluenceRenderMode.Hard)
                    {
                        var influenceColorIndex = InfluenceToColorEdgy(allInfluence[localX, localZ]);
                        Color influenceColor = Color.clear;
                        if (influenceColorIndex > -1)
                        {
                            influenceColor = _settings.Biomes[influenceColorIndex].LayoutColor;
                        }
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                        colors.Add(influenceColor);
                    }
                    else if (mode == TriRenderer.MicroInfluenceRenderMode.Dither_80)
                    {
                        var influenceColorIndex = InfluenceToColorsEdgy(allInfluence[localX, localZ]);
                        Color influenceColor1 = Color.clear, influenceColor2 = Color.clear;
                        if (influenceColorIndex.Item1 > -1)
                        {
                            influenceColor1 = _settings.Biomes[influenceColorIndex.Item1].LayoutColor;
                        }
                        if (influenceColorIndex.Item2 > -1)
                        {
                            influenceColor2 = _settings.Biomes[influenceColorIndex.Item2].LayoutColor;
                        }
                        colors.Add(influenceColor1);
                        colors.Add(influenceColor1);
                        colors.Add(influenceColor1);
                        colors.Add(influenceColor2);
                        colors.Add(influenceColor2);
                        colors.Add(influenceColor2);
                    }
                }

            result.SetVertices(vertices);
            result.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            result.SetColors(colors);
            result.RecalculateBounds();
            result.RecalculateNormals();

            return result;
        }

        public Mesh CreateMesh(MacroMap mapMesh, TriRenderer.MacroCellInfluenceMode influence, TriRenderer.MacroCellReliefMode relief)
        {
            var result = new Mesh();
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var indices = new List<int>();

            foreach (var cell in mapMesh.Cells)
            {
                CreateMacroCell(vertices, colors, indices, cell, influence, relief);
            }

            result.SetVertices(vertices);
            result.SetColors(colors);
            result.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            result.RecalculateNormals();

            return result;
        }

        /// <summary>
        /// Create flat cell mesh from 2 triangles with influence vertex coloring
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="colors"></param>
        /// <param name="indices"></param>
        /// <param name="cell"></param>
        /// <param name="influence"></param>
        private void CreateMacroCell(List<Vector3> vertices, List<Color> colors, List<int> indices, Cell cell, 
            TriRenderer.MacroCellInfluenceMode influence, TriRenderer.MacroCellReliefMode relief)
        {
            var baseIndex = vertices.Count;

            if (relief == TriRenderer.MacroCellReliefMode.Flat)
            {
                vertices.Add(new Vector3(cell.Center.X, 0, cell.Center.Y));
                vertices.Add(new Vector3(cell.Vertices3[0].Coords.X, 0, cell.Vertices3[0].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[1].Coords.X, 0, cell.Vertices3[1].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[2].Coords.X, 0, cell.Vertices3[2].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[3].Coords.X, 0, cell.Vertices3[3].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[4].Coords.X, 0, cell.Vertices3[4].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[5].Coords.X, 0, cell.Vertices3[5].Coords.Y));
            }
            else
            {
                vertices.Add(new Vector3(cell.Center.X, _macroMap.GetHeight(cell.Center), cell.Center.Y));
                vertices.Add(new Vector3(cell.Vertices3[0].Coords.X, cell.Vertices3[0].Height, cell.Vertices3[0].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[1].Coords.X, cell.Vertices3[1].Height, cell.Vertices3[1].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[2].Coords.X, cell.Vertices3[2].Height, cell.Vertices3[2].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[3].Coords.X, cell.Vertices3[3].Height, cell.Vertices3[3].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[4].Coords.X, cell.Vertices3[4].Height, cell.Vertices3[4].Coords.Y));
                vertices.Add(new Vector3(cell.Vertices3[5].Coords.X, cell.Vertices3[5].Height, cell.Vertices3[5].Coords.Y));
            }

            indices.Add(baseIndex);
            indices.Add(baseIndex+1);
            indices.Add(baseIndex+2);
            indices.Add(baseIndex);
            indices.Add(baseIndex+2);
            indices.Add(baseIndex+3);
            indices.Add(baseIndex);
            indices.Add(baseIndex+3);
            indices.Add(baseIndex+4);
            indices.Add(baseIndex);
            indices.Add(baseIndex+4);
            indices.Add(baseIndex+5);
            indices.Add(baseIndex);
            indices.Add(baseIndex+5);
            indices.Add(baseIndex+6);
            indices.Add(baseIndex);
            indices.Add(baseIndex+6);
            indices.Add(baseIndex+1);

            if (influence == TriRenderer.MacroCellInfluenceMode.Interpolated)
            {
                colors.Add(cell.Zone.Biome.LayoutColor);
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[0].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[1].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[2].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[3].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[4].Influence));
                colors.Add(InfluenceToColorSmooth(cell.Vertices3[5].Influence));
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

        private Color InfluenceToColorSmooth(double[] influence)
        {
            var result = Color.clear;
            if (influence == null)
                return result;

            for (int i = 0; i < _settings.Biomes.Length; i++)
                result += _settings.Biomes[i].LayoutColor * (float)influence[i];

            return result;
        }

        private int InfluenceToColorEdgy(double[] influence)
        {
            if (influence == null)
                return -1;

            var maxInfluenceIndex = 0;
            var maxInfluenceValue = influence[0];
            for (int i = 1; i < influence.Length; i++)
            {
                if (influence[i] > maxInfluenceValue)
                {
                    maxInfluenceValue = influence[i];
                    maxInfluenceIndex = i;
                }
            }

            return maxInfluenceIndex;
        }

        private Tuple<int, int> InfluenceToColorsEdgy(double[] influence)
        {
            if (influence == null || influence.Length == 0)
                return new Tuple<int, int>(-1, -1);
            if (influence.Length == 1)
                return new Tuple<int, int>(0, 0);

            var maxInfluenceIndex1 = -1;
            var maxInfluenceValue1 = double.MinValue;
            var maxInfluenceIndex2 = -1;
            var maxInfluenceValue2 = double.MinValue;

            for (int i = 0; i < influence.Length; i++)
            {
                if (influence[i] > maxInfluenceValue1)
                {
                    maxInfluenceValue1 = influence[i];
                    maxInfluenceIndex1 = i;
                }
            }

            if (maxInfluenceValue1 > 0.8)
            {
                maxInfluenceIndex2 = maxInfluenceIndex1;
            }
            else
            {
                for (int i = 0; i < influence.Length; i++)
                {
                    if (influence[i] > maxInfluenceValue2 && influence[i] < maxInfluenceValue1)
                    {
                        maxInfluenceValue2 = influence[i];
                        maxInfluenceIndex2 = i;
                    }
                }

                if (maxInfluenceIndex2 == -1)
                    maxInfluenceIndex2 = maxInfluenceIndex1;
            }

            return new Tuple<int, int>(maxInfluenceIndex1, maxInfluenceIndex2);
        }
    }
}
