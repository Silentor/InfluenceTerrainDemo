using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using TerrainDemo.Voronoi;
using UnityEngine.Assertions;

namespace TerrainDemo.Tri
{
    public class TriSubMesh
    {
        /*
        public TriSubMesh([NotNull] TriMesh mesh, [NotNull] IEnumerable<TriCell> cells)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (cells == null) throw new ArgumentNullException("cells");

            _mesh = mesh;
            _cells = cells.ToArray();
            Assert.IsTrue(_cells.All(c => c.Mesh == mesh));

            Array.Sort(_cells, TriCell.IdIncComparer);

            _borderCells = _cells.Where(c => !c.NeighborsSafe.All(Contains)).ToArray();
        }

        public bool Contains(TriCell cell)
        {
            var result = Array.BinarySearch(_cells, cell, TriCell.IdIncComparer);
            return result >= 0;
        }

        /// <summary>
        /// Get cells that has neighbors outside submesh
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TriCell> GetBorderCells()
        {
            return _borderCells;
        }

        /// <summary>
        /// All cells that's not border
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TriCell> GetInnerCells()
        {
            return _cells.Except(_borderCells, TriCell.CellComparer);
        }

        public FloodFiller FloodFill([NotNull] Cell[] start, Predicate<Cell> searchFor = null)
        {
            if (start == null) throw new ArgumentNullException("start");
            Assert.IsTrue(start.All(c => Cells.Contains(c)));

            if (searchFor != null)
                return _mesh.FloodFill(start, cell => Contains(cell) && searchFor(cell));
            else
                return _mesh.FloodFill(start, Contains);
        }

        public override FloodFiller FloodFill(Cell start, Predicate<Cell> searchFor = null)
        {
            if (start == null) throw new ArgumentNullException("start");
            Assert.IsTrue(Contains(start));

            if (searchFor != null)
                return _mesh.FloodFill(start, cell => Contains(cell) && searchFor(cell));
            else
                return _mesh.FloodFill(start, Contains);
        }

        [NotNull]
        private readonly TriMesh _mesh;

        private TriCell[] _cells;

        private TriCell[] _borderCells;
        */
    }
}
