using TerrainDemo.Spatial;

namespace TerrainDemo.Micro
{
    public class MicroHexGrid : HexGrid<Cell, bool, bool>
    {
        public MicroHexGrid( float hexSide, int gridSide ) : base( hexSide, gridSide )
        {
            
        }
    }
}
