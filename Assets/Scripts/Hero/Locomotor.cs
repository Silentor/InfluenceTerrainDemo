using System;
using System.Diagnostics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;

namespace TerrainDemo.Hero
{
	public class Locomotor
	{
		public Type LocoType => _type;

		public Locomotor( Type type, Actor owner, MicroMap map, NavigationMap navMap )
		{
			_type   = type;
			_owner  = owner;
			_map    = map;
			_navMap = navMap;
		}

		/// <summary>
		/// Make step on map and resolve collisions
		/// </summary>
		/// <param name="fromMap"></param>
		/// <param name="fromPos"></param>
		/// <param name="toPos"></param>
		/// <param name="toMap"></param>
		/// <returns>New position</returns>
		public Vector2 Step(/*BaseBlockMap fromMap, */Vector2 fromPos, Vector2 toPos/*, out BaseBlockMap toMap*/)
		{
			var fromBlock = (GridPos) fromPos;
			var toBlock   = (GridPos) toPos;

			//Fast pass: step inside the same block
			if (fromBlock == toBlock)
			{
				//toMap = fromMap;
				return ResolveBlockCollision(fromBlock, toPos);
			}

			//Default pass: check every intersected block
			var intersections = Intersections.GridIntersections( fromPos, toPos );
			var pathRay       = new Ray2( fromPos, toPos - fromPos );
			foreach ( var intersection in intersections )
			{
				var intersectPoint = pathRay.GetPoint( intersection.distance );
				var collidedPos    = ResolveBlockCollision( intersection.prevBlock, intersectPoint );

				if ( collidedPos == intersectPoint )
					continue;

				//Resolve collision of entire step vector
				var collidedPosEnd = ResolveBlockCollision( intersection.prevBlock, toPos );
				return Step( collidedPos, collidedPosEnd );
			}

			//No collision, pass is clear
			//toMap = fromMap;
			return toPos;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fromMap"></param>
		/// <param name="fromPos"></param>
		/// <param name="toPos"></param>
		/// <param name="toMap"></param>
		/// <returns></returns>
		public bool IsPassable(/*BaseBlockMap fromMap, */GridPos fromPos, GridPos toPos/*, out BaseBlockMap toMap*/) //todo move to Locomotor component
		{
			//ref readonly var fromData = ref fromMap.GetBlockData(fromPos);
			ref readonly var toData     = ref _map.GetBlockData(toPos);
			ref readonly var toNavBlock = ref _navMap.NavGrid.GetBlock( toPos );
			//toMap = fromMap;

			if (toData != BlockData.Empty)
			{
				if (CheckBlock( toNavBlock ))
				{
					//Can step on new block
					//Assert.IsNotNull(toMap);
					return true;
				}
			}

			//No pass 
			return false;
		}
		public float GetCost( LocalIncline edgeSlopeness )
		{
			return LocalInclinationCost[(int)_type, (int)edgeSlopeness];
		}

		private readonly Type _type;
		private readonly Actor _owner;
		private readonly MicroMap _map;
		private readonly NavigationMap _navMap;
		private static readonly float[,] LocalInclinationCost = {
																	//Biped
			                                                        {
				                                                        1, 
				                                                        1.5f, 
				                                                        10, 
				                                                        float.NaN,
																		0.5f,
																		2,
																		float.NaN
																	},
																	//Wheel
			                                                        {
				                                                        1,
				                                                        1.5f,
				                                                        float.NaN,
				                                                        float.NaN,
				                                                        1f,
				                                                        10,
				                                                        float.NaN
																	}
																};

		private bool CheckBlock( in NavigationGrid.Block block )
		{
			if ( block.Normal.Slope == Incline.Blocked )
				return false;

			if ( _type == Type.Biped )
			{
				return block.Normal.Slope <= Incline.Medium;
			}
			else
			{
				return block.Normal.Slope <= Incline.Small;
			}
		}

		private Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position)
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

		[Conditional("UNITY_EDITOR")]
		private void DebugDrawCollisionPlane( GridPos block, Side2d side )
		{
			var yPosition = _owner.Position.Y + 1;
			var blockSideLocalPos = Directions.BlockSide[(int)side];
			DrawRectangle.ForDebug(
				(blockSideLocalPos.Item1 + block).ToVector3(yPosition + 1),
				(blockSideLocalPos.Item2 + block).ToVector3(yPosition + 1),
				(blockSideLocalPos.Item2 + block).ToVector3(yPosition - 1),
				(blockSideLocalPos.Item1 + block).ToVector3(yPosition - 1),
				Color.red, 0, true
			);
		}

		public enum Type
		{
			Biped,
			Wheeled
		}
	}
}
