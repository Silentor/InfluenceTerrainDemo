using System.Collections.Generic;

namespace TerrainDemo.Spatial
{
    public interface IReadOnlyHexGrid<TCell> : IReadOnlyCollection<HexPos>
    {
        public TCell this[ HexPos position ]
        {
            get;
        }

        public TCell this[ int q, int r ]
        {
            get;
        }
    }
}
