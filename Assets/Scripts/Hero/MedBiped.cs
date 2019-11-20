using OpenToolkit.Mathematics;
using System;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Hero
{
	public class MedBiped : BaseLocomotor
	{
		public override Type LocoType => Type.MedBiped;

		public override Bounds2i Bound => GetBound( Position );

		public MedBiped( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap ) : base( startPosition, startRotation, owner, map, navMap )
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
			var newBound = GetBound( position );
            var boundsMin = newBound.Min;

            var leftPos = boundsMin + Vector2i.Left;
            ref readonly var left1 = ref _navMap.NavGrid.GetBlock( leftPos );
			ref readonly var left2 = ref _navMap.NavGrid.GetBlock( leftPos + Vector2i.Forward );
			
			var rightPos = boundsMin + Vector2i.Right + Vector2i.Right;
			ref readonly var right1 = ref _navMap.NavGrid.GetBlock( rightPos );
			ref readonly var right2 = ref _navMap.NavGrid.GetBlock( rightPos + Vector2i.Forward );

			var              forwardPos = boundsMin + Vector2i.Forward + Vector2i.Forward;
			ref readonly var forward1 = ref _navMap.NavGrid.GetBlock( forwardPos );
			ref readonly var forward2 = ref _navMap.NavGrid.GetBlock( forwardPos + Vector2i.Right );

			var              backPos = boundsMin + Vector2i.Back;
			ref readonly var back1    = ref _navMap.NavGrid.GetBlock( backPos );
			ref readonly var back2    = ref _navMap.NavGrid.GetBlock( backPos + Vector2i.Right );

			var restrictedPosition = (Vector2)(boundsMin + Vector2i.One);

			if ( position.X < restrictedPosition.X && (!CheckBlock( in left1 ) || !CheckBlock( in left2 )) )
			{
				position.X = restrictedPosition.X;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Left, Side2d.Left );
				}
			}
			else if ( position.X > restrictedPosition.X && (!CheckBlock( in right1 ) || !CheckBlock( in right2 ) ) )
			{
				position.X = restrictedPosition.X;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Right, Side2d.Right );
				}
			}

			if ( position.Y < restrictedPosition.Y && (!CheckBlock( in back1 ) || !CheckBlock( in back2 )) )
			{
				position.Y = restrictedPosition.Y;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Back, Side2d.Back );
				}
			}
			else if ( position.Y > restrictedPosition.Y && (!CheckBlock( in forward1 ) || !CheckBlock( in forward2 )) )
			{
				position.Y = restrictedPosition.Y;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Forward, Side2d.Forward );
				}
			}

			return position;
		}

		protected override bool CheckBlock( in NavigationGrid.Block block )
		{
			if ( block.Normal.Slope == Incline.Blocked )
				return false;

			return block.Normal.Slope <= Incline.Medium;
		}
		private static Bounds2i GetBound( Vector2 position )
		{
			var minBlockPosition = (GridPos) ( position - new Vector2( 0.5f, 0.5f ) );
			return new Bounds2i( minBlockPosition, 2, 2 );
		}
	}
}
