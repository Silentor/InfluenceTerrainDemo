using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using Vector2i = TerrainDemo.Spatial.Vector2i;

namespace TerrainDemo.Hero
{
	public class SmallWheel : BaseLocomotor
	{
		public override Type LocoType => Type.Wheeled;

		public override Bounds2i Bound => new Bounds2i(BlockPosition, 0);

		public SmallWheel( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap ) : base( startPosition, startRotation, owner, map, navMap )
		{
		}

		private static readonly float[] LocalInclinationCost = 
			                                                        //Wheel
			                                                        {
				                                                        1,
				                                                        1.5f,
				                                                        float.NaN,
				                                                        float.NaN,
				                                                        1f,
				                                                        10,
				                                                        float.NaN
			                                                        };

		protected override Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position)
		{
			var fracPosX = position.X - blockPosition.X;
			var fracPosZ = position.Y - blockPosition.Z;

			if ( fracPosX < 0.5 && !CheckBlock( blockPosition + Vector2i.Left, Direction.Left ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Left );
				}
			}
			else if ( fracPosX >= 0.5 && !CheckBlock( blockPosition + Vector2i.Right, Direction.Right ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Right );
				}
			}

			if ( fracPosZ < 0.5 && !CheckBlock( blockPosition + Vector2i.Back, Direction.Back ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Back );
				}
			}
			else if ( fracPosZ >= 0.5 && !CheckBlock( blockPosition + Vector2i.Forward, Direction.Forward ) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Forward );
				}
			}

			return new Vector2(blockPosition.X + fracPosX, blockPosition.Z + fracPosZ);
		}
	}
}
