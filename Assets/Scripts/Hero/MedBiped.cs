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

		public override Bounds2i Bound
        {
            get
            {
                var blockX = BlockPosition.X;
                var blockZ = BlockPosition.Z;
                var fracX = Position.X - blockX;
                var fracZ = Position.Y - blockZ;

                if ( fracX < 0.5 && fracZ < 0.5 )
                {
                    return new Bounds2i(new GridPos(blockX - 1, blockZ - 1), 2, 2);
                }
                else if (fracX >= 0.5 && fracZ < 0.5)
                {
                    return new Bounds2i(new GridPos(blockX, blockZ - 1), 2, 2);
                }
                else if (fracX < 0.5 && fracZ >= 0.5)
                {
                    return new Bounds2i(new GridPos(blockX - 1, blockZ), 2, 2);
                }
                else
                {
                    return new Bounds2i(new GridPos(blockX, blockZ), 2, 2);
                }
            }
        }

		public override float GetCost( LocalIncline edgeSlopeness ) => LocalInclinationCost[(int)edgeSlopeness];

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
			var fracPosX = position.X - blockPosition.X;
			var fracPosZ = position.Y - blockPosition.Z;
            var boundsMin = Bound.Min;

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

            wip понять как ограничить актора
			if ( fracPosX < ? && (!CheckBlock( in left1 ) || !CheckBlock( in left2 )) )
			{
				fracPosX = 0;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Left, Side2d.Left );
				}
			}
			else if ( fracPosX >= 0.5 && (!CheckBlock( in right1 ) || !CheckBlock( in right2 ) ) )
			{
				fracPosX = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Right, Side2d.Right );
				}
			}

			if ( fracPosZ < 0.5 && (!CheckBlock( in back1 ) || !CheckBlock( in back2 )) )
			{
				fracPosZ = 0.5f;

				if ( _owner.DebugLocomotion )
				{
					DebugDrawCollisionPlane( blockPosition + Vector2i.Back, Side2d.Back );
				}
			}
			else if ( fracPosZ >= 0.5 && (!CheckBlock( in forward1 ) || !CheckBlock( in forward2 )) )
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
