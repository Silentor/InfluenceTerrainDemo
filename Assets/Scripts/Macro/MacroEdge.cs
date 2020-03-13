using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace TerrainDemo.Macro
{
    public class MacroEdge : IEquatable<MacroEdge>
    {
        public int Id => _edge.Id;
        public MacroVert Vertex1 { get; }

        public MacroVert Vertex2 { get; }

        public Cell Cell1
        {
            get
            {
                if (_cell1 == null)
                {
                    var faces = _mesh.GetAdjacentFaces(this);
                    _cell1 = faces.Item1;
                    _cell2 = faces.Item2;
                }

                return _cell1;
            }
        }

        public Cell Cell2
        {
            get
            {
                if (_cell2 == null)
                {
                    var faces = _mesh.GetAdjacentFaces(this);
                    _cell1 = faces.Item1;
                    _cell2 = faces.Item2;
                }

                return _cell2;
            }
        }

        public MacroEdge([NotNull] MacroMap map, MacroMap.CellMesh mesh, MacroMap.CellMesh.Edge edge, MacroVert vertex1, MacroVert vertex2)
        {
            _edge = edge;
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _mesh = mesh;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
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

        public override string ToString() => $"Edge {Id}, {Cell1?.HexPos.ToString() ?? "?"}|{Cell2?.HexPos.ToString() ?? "?"}, {Vertex1.Id}-{Vertex2.Id}";

        private readonly MacroMap _map;
        private readonly MacroMap.CellMesh _mesh;
        private readonly MacroMap.CellMesh.Edge _edge;
        private Cell _cell2;
        private Cell _cell1;

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
