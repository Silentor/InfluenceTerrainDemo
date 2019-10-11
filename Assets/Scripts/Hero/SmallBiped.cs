using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Hero
{
	public class SmallBiped : BaseLocomotor
	{
		public override Type LocoType => Type.Biped;

		public override Bounds2i Bound => new Bounds2i(BlockPosition, 0);

		public override float GetCost( LocalIncline edgeSlopeness ) => LocalInclinationCost[(int)edgeSlopeness];

		public SmallBiped( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap ) : base( startPosition, startRotation, owner, map, navMap )
		{
		}

		private static readonly float[] LocalInclinationCost = 
		{
			1, 
			1.5f, 
			10, 
			float.NaN,
			0.5f,
			2,
			float.NaN
		};


		protected override Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position)
		{
			var fracPosX = position.X - blockPosition.X;
			var fracPosZ = position.Y - blockPosition.Z;

			if ( fracPosX < 0.5 && !CheckBlock( in _navMap.NavGrid.GetBlock( blockPosition + Vector2i.Left ) ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Side2d.Left );
				}
			}
			else if ( fracPosX > 0.5 && !CheckBlock( in _navMap.NavGrid.GetBlock( blockPosition + Vector2i.Right ) ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Side2d.Right );
				}
			}

			if ( fracPosZ < 0.5 && !CheckBlock( in _navMap.NavGrid.GetBlock( blockPosition + Vector2i.Back ) ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Side2d.Back );
				}
			}
			else if ( fracPosZ > 0.5 && !CheckBlock( in _navMap.NavGrid.GetBlock( blockPosition + Vector2i.Forward ) ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Side2d.Forward );
				}
			}

			return new Vector2(blockPosition.X + fracPosX, blockPosition.Z + fracPosZ);
		}

		protected override bool CheckBlock( in NavigationGrid.Block block )
		{
			if ( block.Normal.Slope == Incline.Blocked )
				return false;

			return block.Normal.Slope <= Incline.Medium;
		}

	}
}
