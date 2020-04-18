using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrainDemo.Generators;
using TerrainDemo.Macro;
using TerrainDemo.Settings;
using TerrainDemo.Spatial;
using Random = TerrainDemo.Tools.Random;

namespace TerrainDemo.Assets.Scripts.Generators
{
	/// <summary>
	/// Create zome layout and prepares zone generator
	/// </summary>
	/// <typeparam name="TZoneGenerator"></typeparam>
	public class ZoneLayout//<TZoneGenerator> where TZoneGenerator : ZoneGenerator3
	{
		private readonly BiomeSettings _biomeSettings;
		private readonly Random _random;

		private List<CapturedCell> _myCells = new List<CapturedCell>();

		public ZoneLayout( BiomeSettings biomeSettings, int seed )
		{
			_biomeSettings = biomeSettings;
			_random = new Tools.Random(seed);
		}

		public bool CaptureLayout( LayoutGrid layoutMap, HexPos startPosition )
		{
			var zoneSize  = _random.Range(_biomeSettings.SizeRange);
			var startCell = startPosition;

			var zonePositions = layoutMap.FloodFill(startCell, (_, c) => c.Owner == null).Take(zoneSize).ToArray();

			//todo check minimum size constraint

			foreach ( var zonePosition in zonePositions )
			{
				var capturedCell = new CapturedCell(_biomeSettings.DefaultCell, this );
				layoutMap[zonePosition] = capturedCell;
				_myCells.Add( capturedCell );
			}

			return true;
		}

		public ZoneGenerator3 GetZoneGenerator( )
		{
			return new ZoneGenerator3( _myCells );
		}
	}

	/// <summary>
	/// A
	/// </summary>
	public class ZoneGenerator3
	{
		private readonly IEnumerable<CapturedCell> _zoneCells;
		private readonly MacroGrid _macro;

		public ZoneGenerator3( IEnumerable<CapturedCell> zoneCells, MacroGrid macro)
		{
			_zoneCells = zoneCells;
			_macro = macro;
		}

		public Zone GenerateMacroZone( int zoneId,  )
		{
			var zone = new Zone(  );

			new Macro.Cell(  )

			var result = new Zone(  );
		}
	}

	public class LayoutGrid : HexGrid<CapturedCell, bool, bool>
	{
		public LayoutGrid( float hexSide, int gridRadius ) : base( hexSide, gridRadius )
		{
		}
	}

	public readonly struct CapturedCell
	{
		public readonly MacroCellType Type;
		public readonly ZoneLayout Owner;
		public CapturedCell( MacroCellType type, ZoneLayout owner  )
		{
			Type = type;
			Owner = owner;
		}
	}
}
