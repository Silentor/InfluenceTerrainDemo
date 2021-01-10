using System;
using JetBrains.Annotations;
using TerrainDemo.Generators;
using TerrainDemo.Macro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using Random = TerrainDemo.Tools.Random;

namespace TerrainDemo.Generators
{
	public class LayoutGrid : HexGrid<CapturedCell, bool, bool>
	{
		public LayoutGrid( float hexSide, int gridSide ) : base( hexSide, gridSide )
		{
		}
	}

	public readonly struct CapturedCell
	{
		public readonly MacroCellType Type;
		public readonly BaseZoneGenerator Owner;

		public Boolean IsEmpty				=> Owner == null;

		public CapturedCell( MacroCellType type, [NotNull] BaseZoneGenerator owner  )
		{
			Type  = type;
			Owner = owner ?? throw new ArgumentNullException( nameof( owner ) );
		}
	}
}
