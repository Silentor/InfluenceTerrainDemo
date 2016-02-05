using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Tools;
using TerrainDemo.Voronoi;
using UnityEngine;

namespace TerrainDemo.Layout
{
    /// <summary>
    /// Desciption of Zone layout
    /// </summary>
    public struct ZoneLayout
    {
        public readonly Vector2 Center;
        public readonly ZoneType Type;
        public readonly Cell Cell;

        /// <summary>
        /// World bounds
        /// </summary>
        public readonly Bounds2i Bounds;

        public readonly Bounds2i ChunkBounds;

        public IEnumerable<ZoneLayout> Neighbors { get { return _neighbors; } }

        public static readonly IEqualityComparer<ZoneLayout> TypeComparer = new ZoneTypeComparer();

        public ZoneLayout(ZoneType type, Cell cell)
        {
            Cell = cell;
            Center = cell.Center;
            Type = type;
            Bounds = (Bounds2i)cell.Bounds;
            ChunkBounds = new Bounds2i(Chunk.GetPosition(Bounds.Min), Chunk.GetPosition(Bounds.Max));
            _neighbors = new ZoneLayout[0];
        }


        /// <summary>
        /// Rasterize zone to blocks (triangles method)
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Vector2i> GetBlocks()
        {
            for (int i = 1; i < Cell.Vertices.Length - 1; i++)
                foreach (var pos in Rasterization.Triangle(Cell.Vertices[0], Cell.Vertices[i], Cell.Vertices[i + 1]))
                    if (Bounds.Contains(pos))
                        yield return pos;
        }

        /// <summary>
        /// Rasterize zone to blocks (polygon method)
        /// </summary>
        /// <returns></returns>
        [Pure]
        public IEnumerable<Vector2i> GetBlocks2()                   //Todo consider move to Rasterization class
        {
            var edges = new List<Vector2i>();
            foreach (var edge in Cell.Edges)
                edges.AddRange(Rasterization.DDA(edge.Vertex1, edge.Vertex2, false));

            var bounds = Bounds;
            edges = edges.Where(e => bounds.Contains(e)).Distinct().ToList();
            edges.Sort();

            for (int i = 0; i < edges.Count;)
            {
                if (i == edges.Count - 1)
                {
                    yield return edges.Last();
                    yield break;
                }

                var z1 = edges[i].Z;
                var j = i;
                while (j < edges.Count - 1 && edges[j + 1].Z == z1)
                    j++;

                for (var x = edges[i].X; x <= edges[j].X; x++)
                    yield return new Vector2i(x, z1);

                i = j + 1;
            }

            //foreach (var vector2I in edges)
            //{
            //    yield return vector2I;
            //}
        }

        private readonly ZoneLayout[] _neighbors;

        public static bool operator ==(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell == z2.Cell;
        }

        public static bool operator !=(ZoneLayout z1, ZoneLayout z2)
        {
            return z1.Cell != z2.Cell;
        }

        private class ZoneTypeComparer : IEqualityComparer<ZoneLayout>
        {
            public bool Equals(ZoneLayout x, ZoneLayout y)
            {
                return x.Type == y.Type;
            }

            public int GetHashCode(ZoneLayout obj)
            {
                return (int)obj.Type;
            }
        }
    }
}
