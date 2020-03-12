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

		public override Bound2i Bound => GetBound( BlockPosition );

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


		protected override Bound2i GetBound( GridPos position ) => new Bound2i(position, 0);
		
		protected override (bool wasCollision, Vector2 resolvedPoint) ResolveBlockCollision( GridPos blockPosition, Vector2 position)
		{
			var wasCollision = false;

			var fracPosX = position.X - blockPosition.X;
			var fracPosZ = position.Y - blockPosition.Z;

			if ( fracPosX < 0.5 && !CheckBlock( blockPosition + Vector2i.Left, Direction.Left ) )
			{
				position.X = blockPosition.X + 0.5f;
				wasCollision = true;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Left );
				}
			}
			else if ( fracPosX > 0.5 && !CheckBlock( blockPosition + Vector2i.Right,  Direction.Right ) )
			{
				position.X = blockPosition.X + 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Right );
				}
			}

			if ( fracPosZ < 0.5 && !CheckBlock( blockPosition + Vector2i.Back, Direction.Back ) )
			{
				position.Y = blockPosition.Z + 0.5f;
				wasCollision = true;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Back );
				}
			}
			else if ( fracPosZ > 0.5 && !CheckBlock( blockPosition + Vector2i.Forward, Direction.Forward ) )
			{
				position.Y = blockPosition.Z + 0.5f;
				wasCollision = true;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition, Direction.Forward );
				}
			}

			return  ( wasCollision, position );
		}
	}
}
