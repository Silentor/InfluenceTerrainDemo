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

		public override Bounds2i Bound => GetBound( BlockPosition );

		
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

		protected override Bounds2i GetBound( GridPos position ) => new Bounds2i(position, 1);
		
		protected override Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position)
		{
			var fracPosX = position.X - blockPosition.X;
			var fracPosZ = position.Y - blockPosition.Z;

			var leftPos = blockPosition + Vector2i.Left + Vector2i.Left;
			var left1 = leftPos;
			var left2 = leftPos + Vector2i.Forward;
			var left3 = leftPos + Vector2i.Back;
			
			var rightPos = blockPosition + Vector2i.Right + Vector2i.Right;
			var right1 = rightPos;
			var right2 = rightPos + Vector2i.Forward;
			var right3 = rightPos + Vector2i.Back;

			var              forwardPos = blockPosition + Vector2i.Forward + Vector2i.Forward;
			var forward1 = forwardPos;
			var forward2 = forwardPos + Vector2i.Left;
			var forward3 = forwardPos + Vector2i.Right;

			var              backPos = blockPosition + Vector2i.Back + Vector2i.Back;
			var back1    = backPos;
			var back2    = backPos + Vector2i.Left;
			var back3    = backPos + Vector2i.Right;

			if ( fracPosX < 0.5 && !CheckBlock( left1, left2, left3, Direction.Left ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Left, Direction.Left );
				}
			}
			else if ( fracPosX >= 0.5 && !CheckBlock( right1, right2, right3, Direction.Right ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Right, Direction.Right );
				}
			}

			if ( fracPosZ < 0.5 && !CheckBlock( back1, back2, back3, Direction.Back ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Back, Direction.Back );
				}
			}
			else if ( fracPosZ >= 0.5 && !CheckBlock( forward1, forward2, forward3, Direction.Forward ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Forward, Direction.Forward );
				}
			}

			return new Vector2(blockPosition.X + fracPosX, blockPosition.Z + fracPosZ);
		}
	}
}
