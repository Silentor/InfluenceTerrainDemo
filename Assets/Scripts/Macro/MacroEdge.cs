using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Tri;
using TerrainDemo.Voronoi;
using UnityEngine.Assertions;

namespace TerrainDemo.Macro
{
    public class MacroEdge : IEquatable<MacroEdge>
    {
        public readonly int Id;
        public MacroVert Vertex1 { get; }

        public MacroVert Vertex2 { get; }

        public Cell Cell1 { get; private set; }

        public Cell Cell2 { get; private set; }

        public MacroEdge([NotNull] MacroMap mesh, MacroMap.CellMesh.Edge edge, IEnumerable<MacroVert> vertices)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));

            _edge = edge;
            _mesh = mesh;
            Id = edge.Id;
            Vertex1 = vertices.First(v => v.Id == edge.Vertex1.Id);
            Vertex2 = vertices.First(v => v.Id == edge.Vertex2.Id);
        }

        public bool IsConnects(MacroVert vert1, MacroVert vert2)
        {
            return (vert1 == Vertex1 && vert2 == Vertex2) || (vert1 == Vertex2 && vert2 == Vertex1);
        }

        public Cell GetOppositeOf([NotNull] Cell cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            if (Cell1 == cell)
                return Cell2;
            else if (Cell2 == cell)
                return Cell1;

            throw new ArgumentOutOfRangeException(nameof(cell), "Unknown cell");
        }

        public void Init(Cell cell1, Cell cell2)
        {
            Cell1 = cell1;
            Cell2 = cell2;
        }

        public override string ToString() => $"Edge {Id}, {Cell1?.Position.ToString() ?? "?"}|{Cell2?.Position.ToString() ?? "?"}, {Vertex1.Id}-{Vertex2.Id}";

        private readonly MacroMap _mesh;
        private readonly Mesh<Cell>.Edge _edge;

        #region IEquatable

        public bool Equals(MacroEdge other)
        {
            if (other == null) return false;
            return _edge.Equals(other._edge);
        }

        public override bool Equals(object obj)
        {
            return _edge.Equals(obj);
        }

        public override int GetHashCode()
        {
            return _edge.GetHashCode();
        }

        public static bool operator ==(MacroEdge left, MacroEdge right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MacroEdge left, MacroEdge right)
        {
            return !Equals(left, right);
        }

        #endregion
    }
}
