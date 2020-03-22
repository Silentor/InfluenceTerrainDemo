using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace TerrainDemo.Macro
{
    public class MacroEdge : IEquatable<MacroEdge>
    {
	    public MacroVert Vertex1 => _edge.Vertex1.Data;

        public MacroVert Vertex2 => _edge.Vertex2.Data;

        public Cell Cell1 => _edge.Grid [ _edge.Cell1 ];

        public Cell Cell2 => _edge.Grid [ _edge.Cell2] ;


        public MacroEdge([NotNull] MacroMap map, MacroMap.CellMesh.EdgeHolder edge )
        {
            _edge = edge;
            _map = map ?? throw new ArgumentNullException(nameof(map));
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

        public override string ToString() => $"Edge, {Cell1?.HexPos.ToString() ?? "?"}|{Cell2?.HexPos.ToString() ?? "?"}";

        private readonly MacroMap _map;
        private readonly MacroMap.CellMesh _mesh;
        private readonly MacroMap.CellMesh.EdgeHolder _edge;
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
