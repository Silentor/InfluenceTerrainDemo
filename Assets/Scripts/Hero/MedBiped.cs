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

		protected override Bounds2i GetBound( GridPos position ) => new Bounds2i(position, 2, 2);
		
		protected override Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position)
		{
			var newBound = GetBound( position );
            var boundsMin = newBound.Min;

            var leftPos = boundsMin + Vector2i.Left;
            var left1 = leftPos;
			var left2 = leftPos + Vector2i.Forward;
			
			var rightPos = boundsMin + Vector2i.Right + Vector2i.Right;
			var right1 = rightPos;
			var right2 = rightPos + Vector2i.Forward;

			var              forwardPos = boundsMin + Vector2i.Forward + Vector2i.Forward;
			var forward1 = forwardPos;
			var forward2 = forwardPos + Vector2i.Right;

			var              backPos = boundsMin + Vector2i.Back;
			var back1    = backPos;
			var back2    = backPos + Vector2i.Right;

			var restrictedPosition = (Vector2)(boundsMin + Vector2i.One);

			if ( position.X < restrictedPosition.X && !CheckBlock( left1, left2, Direction.Left ) )
			{
				position.X = restrictedPosition.X;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Left, Direction.Left );
				}
			}
			else if ( position.X > restrictedPosition.X && !CheckBlock( right1, right2, Direction.Right ) )
			{
				position.X = restrictedPosition.X;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Right, Direction.Right );
				}
			}

			if ( position.Y < restrictedPosition.Y && !CheckBlock( back1, back2, Direction.Back ) )
			{
				position.Y = restrictedPosition.Y;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Back, Direction.Back );
				}
			}
			else if ( position.Y > restrictedPosition.Y && !CheckBlock( forward1, forward2, Direction.Forward ) )
			{
				position.Y = restrictedPosition.Y;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( restrictedPosition + Vector2i.Forward, Direction.Forward );
				}
			}

			return position;
		}

		private static Bounds2i GetBound( Vector2 position )
		{
			var minBlockPosition = (GridPos) ( position - new Vector2( 0.5f, 0.5f ) );
			return new Bounds2i( minBlockPosition, 2, 2 );
		}
	}
}
