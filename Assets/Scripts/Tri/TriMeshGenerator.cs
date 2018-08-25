using System;
using System.Linq;
using TerrainDemo.Macro;
using TerrainDemo.Tools;
using UnityEngine;
using Random = TerrainDemo.Tools.Random;

namespace TerrainDemo.Tri
{
    public class TriMeshGenerator
    {
        public MacroMap Generate(TriRunner settings, Random random)
        {
            var id = 0;
            var bounds = settings.LandBounds;
            var side = settings.Side;
            var result = new MacroMap(settings, random);
            var xOffset = false;

            /*
            for (var z = bounds.min.z; z < bounds.max.z; z += height)
            {
                if (xOffset)
                {
                    for (var x = bounds.min.x - side / 2; x < bounds.max.x; x += side)
                    {
                        var triCell = result.AddCell(new Vector2(x + side, z), new Vector2(x + side / 2, z + height),
                            new Vector2(x + side + side / 2, z + height));

                        if (triCell != null)
                        {
                            var triCell2 = result.AddCell(triCell.V1, triCell.V2, new Vector2(x + side + side / 2, z + height));
                        }
                    }
                }
                else
                {
                    for (var x = bounds.min.x; x <= bounds.max.x - side; x += side)
                    {
                        var triCell = result.AddCell(new Vector2(x, z), new Vector2(x + side / 2, z + height),
                            new Vector2(x + side, z));
                        if (triCell != null)
                        {
                            var triCell2 = result.AddCell(triCell.V3, triCell.V2, new Vector2(x + side + side / 2, z + height));
                        }
                    }
                }

                xOffset = !xOffset;
            }

            //Set neighbors
            foreach (var cell in result.Cells)
            {
                foreach (var cellSide in cell.AllSides)
                {
                    if (cell[cellSide] == null)
                    {
                        var neighId = result.GetCellNeighborId(cell.Id, cellSide);
                        if (neighId != TriCell.InvalidCellId)
                        {
                            var neigh = result.Cells[neighId];
                            cell[cellSide] = neigh;
                            neigh[cellSide] = cell;
                        }
                    }
                }
            }
            */

            return result;
        }
    }
}
