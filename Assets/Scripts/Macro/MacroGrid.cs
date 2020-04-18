using TerrainDemo.Spatial;

namespace TerrainDemo.Macro
{
	public class MacroGrid : HexGrid<Cell, MacroEdge, MacroVert>
	{
		public MacroGrid( float hexSide, int gridRadius ) : base( hexSide, gridRadius )
		{
		}
	}
}
