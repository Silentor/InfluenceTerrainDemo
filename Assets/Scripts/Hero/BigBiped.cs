using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Hero
{
	public class BigBiped : BaseLocomotor
	{
		public override Type LocoType => Type.BigBiped;

		public override Bounds2i Bound => new Bounds2i(BlockPosition, 1);

		
		public BigBiped( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap ) : base( startPosition, startRotation, owner, map, navMap )
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

			var leftPos = blockPosition + Vector2i.Left + Vector2i.Left;
			ref readonly var left1 = ref _navMap.NavGrid.GetBlock( leftPos );
			ref readonly var left2 = ref _navMap.NavGrid.GetBlock( leftPos + Vector2i.Forward );
			ref readonly var left3 = ref _navMap.NavGrid.GetBlock( leftPos + Vector2i.Back);
			
			var rightPos = blockPosition + Vector2i.Right + Vector2i.Right;
			ref readonly var right1 = ref _navMap.NavGrid.GetBlock( rightPos );
			ref readonly var right2 = ref _navMap.NavGrid.GetBlock( rightPos + Vector2i.Forward );
			ref readonly var right3 = ref _navMap.NavGrid.GetBlock( rightPos + Vector2i.Back);

			var              forwardPos = blockPosition + Vector2i.Forward + Vector2i.Forward;
			ref readonly var forward1 = ref _navMap.NavGrid.GetBlock( forwardPos );
			ref readonly var forward2 = ref _navMap.NavGrid.GetBlock( forwardPos + Vector2i.Left );
			ref readonly var forward3 = ref _navMap.NavGrid.GetBlock( forwardPos + Vector2i.Right );

			var              backPos = blockPosition + Vector2i.Back + Vector2i.Back;
			ref readonly var back1    = ref _navMap.NavGrid.GetBlock( backPos );
			ref readonly var back2    = ref _navMap.NavGrid.GetBlock( backPos + Vector2i.Left );
			ref readonly var back3    = ref _navMap.NavGrid.GetBlock( backPos + Vector2i.Right );

			if ( fracPosX < 0.5 && (!CheckBlock( in left1 ) || !CheckBlock( in left2 ) || !CheckBlock( in left3 )) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Left, Side2d.Left );
				}
			}
			else if ( fracPosX >= 0.5 && (!CheckBlock( in right1 ) || !CheckBlock( in right2 ) || !CheckBlock( in right3 )) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Right, Side2d.Right );
				}
			}

			if ( fracPosZ < 0.5 && (!CheckBlock( in back1 ) || !CheckBlock( in back2 ) || !CheckBlock( in back3 )) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Back, Side2d.Back );
				}
			}
			else if ( fracPosZ >= 0.5 && (!CheckBlock( in forward1 ) || !CheckBlock( in forward2 ) || !CheckBlock( in forward3 )) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Forward, Side2d.Forward );
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
