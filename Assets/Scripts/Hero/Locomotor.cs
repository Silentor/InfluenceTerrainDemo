using System;
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
		public Vector2 Step(BaseBlockMap fromMap, Vector2 fromPos, Vector2 toPos, out BaseBlockMap toMap)
		{
			for (var i = 0; i < 2; i++)
			{
				var fromBlock = (GridPos) fromPos;
				var toBlock   = (GridPos) toPos;

				//Debug.Log($"{Time.frameCount} - {i}: From pos {fromPos} block {fromBlock} to pos {toPos} block {toBlock}");

				//Count from block as passable apriori
				if (fromBlock == toBlock)
				{
					//Debug.Log($"{Time.frameCount} - {i}: Blocks are equal");

					toMap = fromMap;
					return toPos;
				}

				var intersections = Intersections.GridIntersections(fromPos, toPos);
				var pathRay       = new Ray2(fromPos, toPos - fromPos);
				foreach (var intersection in intersections)
				{
					toBlock = intersection.blockPosition;
					//Debug.Log($"{Time.frameCount} - {i}: Check pass from {fromBlock} to {toBlock}");
					if (!IsPassable(fromMap, fromBlock, toBlock, out var toMap2))
					{
						//Respond to collision
						var hitPoint = pathRay.GetPoint(intersection.distance);

						//Debug.Log($"{Time.frameCount} - {i}: Inpassable block {intersection.blockPosition}, hit {hitPoint}, normal {intersection.normal}");

						var collisionVector = toPos - hitPoint;

						//Draw collision plane
						if (_owner.DebugLocomotion)
						{
							var yPosition = _owner.Position.Y + 1;
							DebugExtension.DebugPoint(hitPoint.ToVector3(yPosition), Color.red, 0.1f);
							Debug.DrawLine(fromPos.ToVector3(yPosition), hitPoint.ToVector3(yPosition), Color.white);
							DrawArrow.ForDebug(hitPoint.ToVector3(yPosition), collisionVector.Normalized(), Color.red);
							//Draw collision plane
							var blockSide = Directions.BlockSide[(int) intersection.normal];
							DrawRectangle.ForDebug(
								(blockSide.Item1 + intersection.blockPosition).ToVector3(yPosition + 1),
								(blockSide.Item2 + intersection.blockPosition).ToVector3(yPosition + 1),
								(blockSide.Item2 + intersection.blockPosition).ToVector3(yPosition - 1),
								(blockSide.Item1 + intersection.blockPosition).ToVector3(yPosition - 1),
								Color.red, 0, true
							);
						}

						//Get collision resolve veсtor
						var normal                   = intersection.normal.ToVector2();
						var projectedCollisionVector = collisionVector - Vector2.Dot(collisionVector, normal) * normal;

						//Recheck resolved point for another collision (one more time only)
						fromPos = hitPoint + normal * 0.01f; // a little bit inside into "from block" 

						if (projectedCollisionVector == Vector2.Zero) //Resolving finished, return calculated toPos and toMap
						{
							//Debug.Log($"{Time.frameCount} - {i}: Resolving completed, result {toPos}");

							toMap = fromMap;
							return fromPos;
						}
						else
						{
							//Draw collision resolve vector
							if(_owner.DebugLocomotion)
								DrawArrow.ForDebug(hitPoint.ToVector3(_owner.Position.Y + 1), projectedCollisionVector.Normalized(), Color.green);

							if (i < 1)
							{
								//Debug.Log($"{Time.frameCount} - {i}: Projected vector {projectedCollisionVector} not zero, make next iter");
								toPos = fromPos + projectedCollisionVector;
								break;
							}
							else
							{
								//Debug.LogWarning($"{Time.frameCount} - {i} Actor collision still not resolved completely on second check, use last good hit point");

								toMap = fromMap;
								return fromPos;
							}

						}
					}
					else
					{
						//Debug.Log($"{Time.frameCount} - {i}: There is pass from {fromBlock} to {toBlock}, check next pass");
						//Continue check intersections
						fromBlock = toBlock;
						fromMap   = toMap2;
					}
				}

				if (fromBlock == toBlock)
					break;
			}

			//No collision, pass is clear
			toMap = fromMap;
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
		public bool IsPassable(BaseBlockMap fromMap, GridPos fromPos, GridPos toPos, out BaseBlockMap toMap) //todo move to Locomotor component
		{
			//Assume that fromPos -> toPos is small for simplicity
			/*
            if (Vector2i.ManhattanDistance(fromPos, toPos) > 1)
            {
                toMap = null;
                return false;
            }
            */

			//var (newMap, newOverlapState) = _mainMap.GetOverlapState(toPos);

			//Check special cases with map change
			//ref readonly var fromData = ref fromMap.GetBlockData(fromPos);
			//ref readonly var toData = ref BlockData.Empty;
			//toMap = null;
			//switch (newOverlapState)
			//{
			//    //Check from object map to main map transition
			//    case BlockOverlapState.Under:
			//    case BlockOverlapState.None:
			//    {
			//        toData = ref _mainMap.GetBlockData(toPos);
			//        toMap = _mainMap;
			//    }
			//        break;

			//    case BlockOverlapState.Above:
			//    {
			//        ref readonly var aboveBlockData = ref newMap.GetBlockData(toPos);

			//        //Can we pass under floating block?
			//        if (aboveBlockData.MinHeight > fromData.MaxHeight + 2)
			//        {
			//            toData = ref fromMap.GetBlockData(toPos);
			//            toMap = fromMap;
			//        }
			//        else
			//        {
			//            toData = ref aboveBlockData;
			//            toMap = newMap;
			//        }
			//    }
			//        break;

			//    case BlockOverlapState.Overlap:
			//    {
			//        toData = ref newMap.GetBlockData(toPos);
			//        toMap = newMap;
			//    }
			//        break;

			//    default:
			//        throw new ArgumentOutOfRangeException();
			//}

			//ref readonly var fromData = ref fromMap.GetBlockData(fromPos);
			ref readonly var toData     = ref fromMap.GetBlockData(toPos);
			ref readonly var toNavBlock = ref _navMap.NavGrid.GetBlock( toPos );

			toMap = fromMap;

			if (toData != BlockData.Empty)
			{
				if (CheckBlock( toNavBlock))
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

		protected bool CheckBlock( in NavigationGrid.Block block )
		{
			if ( _type == Type.Biped )
			{
				return block.Normal.Slope <= Incline.Medium;
			}
			else
			{
				return block.Normal.Slope <= Incline.Small;
			}
		}

		public enum Type
		{
			Biped,
			Wheeled
		}
	}
}
