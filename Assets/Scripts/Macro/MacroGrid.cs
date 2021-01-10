using TerrainDemo.Spatial;

namespace TerrainDemo.Macro
{
	public class MacroGrid : HexGrid<Cell, MacroEdge, MacroVert>
	{
		public MacroGrid( float hexSide, int gridSide ) : base( hexSide, gridSide )
		{
		}
	}
}
