using System;
using TerrainDemo.Generators;
using TerrainDemo.Macro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using Random = TerrainDemo.Tools.Random;

namespace TerrainDemo.Assets.Scripts.Generators
{
	public class LayoutGrid : HexGrid<CapturedCell, bool, bool>
	{
		public LayoutGrid( float hexSide, int gridRadius ) : base( hexSide, gridRadius )
		{
		}
	}

	public class CapturedCell
	{
		public readonly MacroCellType Type;
		public readonly BaseZoneGenerator Owner;
		public CapturedCell( MacroCellType type, BaseZoneGenerator owner  )
		{
			Type = type;
			Owner = owner;
		}
	}
}
