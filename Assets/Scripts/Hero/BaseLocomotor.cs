using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = System.Diagnostics.Debug;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Quaternion = OpenToolkit.Mathematics.Quaternion;
using Vector2i = TerrainDemo.Spatial.Vector2i;
using Vector3 = OpenToolkit.Mathematics.Vector3;

namespace TerrainDemo.Hero
{
	public abstract class BaseLocomotor : IAStarGraph<NavNode>, IAStarGraph<GridPos>
	{
		public float MaxSpeed = 5;
		public float MaxRotationAngle = 180;

		public abstract Type LocoType { get; }

		public Vector2 Position { get; private set; }

		/// <summary>
		/// Degrees
		/// </summary>
		public Quaternion Rotation => Quaternion.FromEulerAngles( 0, _rotation, 0 );

		public GridPos BlockPosition => (GridPos) Position;

		public abstract Bounds2i Bound { get; }

		public bool IsMoving
		{
			get; 
			private set;
		}

		public static BaseLocomotor Create(
			Type          type, Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map,
			NavigationMap navMap )
		{
			switch ( type )
			{
				case Type.Biped:
					return new SmallBiped( startPosition, startRotation, owner, map, navMap );
                case Type.MedBiped:
                    return new MedBiped(startPosition, startRotation, owner, map, navMap);
                case Type.BigBiped:
					return new BigBiped( startPosition, startRotation, owner, map, navMap );
				//case Type.Wheeled:
					//return new SmallWheel( startPosition, startRotation, owner, map, navMap );
				default:
					throw new ArgumentOutOfRangeException( nameof( type ), type, null );
			}
		}

        protected BaseLocomotor( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap )
		{
			Position = startPosition;
			_rotation = startRotation.ToEulerAngles( ).Y;
			_owner  = owner;
			_map    = map;
			_navMap = navMap;
		}

		public void Move( Vector2 direction )
		{
			if ( direction != Vector2.Zero )
			{
				_targetVelocity = direction.Normalized( ) * MaxSpeed;
				_targetPosition = null;
				IsMoving = true;
			}
		}
		public void MoveTo( Vector2 position )
		{
			if ( Position == position )
			{
				Stop(  );
				return;
			}

			_targetPosition = position;
			_targetVelocity = Vector2.Zero;
			IsMoving = true;
		}

		public Task MoveToAsync( Vector2 position )
		{
			MoveTo( position );

			//Already here
			if ( !IsMoving )
			{
				return Task.CompletedTask;
			}

			if(_moveToCompletionSource != null && !_moveToCompletionSource.Task.IsCompleted)
				_moveToCompletionSource.SetCanceled(  );

			_moveToCompletionSource = new TaskCompletionSource<object>();

			UnityEngine.Debug.Log( $"Moving to {position} async" );

			return _moveToCompletionSource.Task;
		}

		public void Warp( GridPos position )
		{
			Position = position;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="direction">-1..1</param>
		public void Rotate(float direction)
		{
			direction        = Mathf.Clamp(direction, -1, 1);
			_rotateDirection = direction;
		}

		public void LookAt(Vector2 worldPosition)
		{
			_lookAt = worldPosition;

			//var angle = UnityEngine.Vector2.SignedAngle(worldPosition - (Vector2)Position, UnityEngine.Vector2.up);
			//_targetRotation = Quaternion.FromEulerAngles(0, MathHelper.DegreesToRadians(angle), 0);
		}

		/// <summary>
		/// Cancel movement and rotations
		/// </summary>
		public void Stop( )
		{
			_targetVelocity = Vector2.Zero;
			_targetPosition = null;
			IsMoving = false;

			UnityEngine.Debug.Log( $"Stop" );

			var completion = _moveToCompletionSource;
			_moveToCompletionSource = null;
			completion?.SetResult( null );	//todo distinct complete and cancel
		}

		public float GetBlockCost( GridPos blockPosition, Direction walkDirection )
		{
			var block    = _map.GetBlockRef( blockPosition );
			var navBlock = _navMap.NavGrid.GetBlock( blockPosition );

			//Get block inclination relative to direction
			//var direct = Directions.From( walkDirection );
			var walkIncline = navBlock.Normal.Project( walkDirection );

			var inclineCost = GetInclineCost( walkIncline );
			var surfaceCost = GetMaterialCost( block.Top );

			return inclineCost * surfaceCost;
		}

		public bool CheckBlock( GridPos blockPosition, Direction walkDirection )
		{
			var cost = GetBlockCost( blockPosition, walkDirection );
			return !float.IsNaN( cost ) && !float.IsInfinity( cost );
		}
		public bool CheckBlock( GridPos blockPosition1, GridPos blockPosition2, Direction walkDirection )
		{
			return CheckBlock( blockPosition1, walkDirection ) && CheckBlock( blockPosition2, walkDirection );
		}

		public bool CheckBlock( GridPos blockPosition1, GridPos blockPosition2, GridPos blockPosition3, Direction walkDirection )
		{
			return CheckBlock( blockPosition1, walkDirection ) && CheckBlock( blockPosition2, walkDirection ) &&
			       CheckBlock( blockPosition3, walkDirection );
		}

		public virtual float GetInclineCost ( LocalIncline incline )
		{
			switch ( incline )
			{
				case LocalIncline.Flat:           return 1;

				case LocalIncline.SmallUphill:    return 1.5f;
				case LocalIncline.MediumUphill:   return 5;
				case LocalIncline.SteepUphill:    return 100;

				case LocalIncline.SmallDownhill:  return 0.9f;
				case LocalIncline.MediumDownhill: return 1.5f;
				case LocalIncline.SteepDownhill:  return 10;

				case LocalIncline.SmallSidehill:  return 1f;
				case LocalIncline.MediumSidehill: return 2f;
				case LocalIncline.SteepSidehill:  return 10;

				case LocalIncline.Blocked:        return float.PositiveInfinity;
				default:
					throw new ArgumentOutOfRangeException ( nameof (incline), incline, null );
			}
		}

		public float GetMaterialCost ( BlockType block )
		{
			return _map.GetBlockSettings ( block ).MaterialCost;
		}

		public float GetRoughnessCost ( float roughness )
		{
			return roughness;
		}

		public bool Update( float deltaTime )
		{
			var isChanged = false;

			isChanged |= UpdateMovement( deltaTime );
			isChanged |= UpdateRotation( deltaTime );

			if ( _owner.DebugLocomotion )
				DebugDrawBounds( );

			return isChanged;
		}

		protected readonly Actor _owner;
		private readonly MicroMap _map;
		protected readonly NavigationMap _navMap;


		private Vector2                      _targetVelocity;
		private Vector2?                     _targetPosition;
		private float                        _rotateDirection;
		private Quaternion                   _targetRotation;
		private Vector2?                     _lookAt;
		private float                        _rotation;
		private TaskCompletionSource<object> _moveToCompletionSource;


		protected abstract Bounds2i GetBound( GridPos position );

		private bool UpdateRotation( float deltaTime )
		{
			if ( _rotateDirection != 0 )
			{
				var step = _rotateDirection * MathHelper.DegreesToRadians( MaxRotationAngle ) * deltaTime;
				_rotation += step;

				return true;
			}

			return false;

			//if ( _lookAt.HasValue )
			//{
			//	if ( _lookAt != Position )
			//	{
			//		Rotation = 
			//	}
			//}
		}

		private bool UpdateMovement( float deltaTime )
		{
			if ( _targetVelocity != Vector2.Zero )
			{
				IsMoving = true;

				var velocity        = (Vector3) _targetVelocity;
				var rotatedVelocity = Vector3.Transform( velocity, Rotation );
				var planeVelocity   = (Vector2) rotatedVelocity;
				var step            = planeVelocity * deltaTime;

				var newPosition = Step( Position, Position + step );

				if ( newPosition != Position )
				{
					Position = newPosition;
					return true;
				}
			}
			else if ( _targetPosition.HasValue )
			{
				IsMoving = true;
				var targetPos      = _targetPosition.Value;
				var targetDir      = ( targetPos - Position ).Normalized( );
				var targetDistance = ( targetPos - Position ).Length;

				Vector2 newPosition;
				if ( targetDistance > deltaTime * MaxSpeed )
				{
					var step = targetDir * deltaTime * MaxSpeed;
					newPosition = Position + step;
				}
				else
				{
					newPosition = targetPos;
				}

				newPosition = Step( Position, newPosition );
				if ( newPosition != Position )
				{
					Position = newPosition;

					if ( Position == targetPos )
						Stop( );

					return true;
				}
			}

			if ( _owner.DebugLocomotion )
				DebugDrawBounds( );
			return false;
		}
		/// <summary>
		/// Make step on map and resolve collisions
		/// </summary>
		/// <param name="fromMap"></param>
		/// <param name="fromPos"></param>
		/// <param name="toPos"></param>
		/// <param name="toMap"></param>
		/// <returns>New position</returns>
		private Vector2 Step(/*BaseBlockMap fromMap, */Vector2 fromPos, Vector2 toPos/*, out BaseBlockMap toMap*/)
		{
			var fromBlock = (GridPos) fromPos;
			var toBlock   = (GridPos) toPos;

			//Fast pass: step inside the same block
			if (fromBlock == toBlock)
			{
				//toMap = fromMap;
				return ResolveBlockCollision(fromBlock, toPos).resolvedPoint;
			}

			//Default pass: check every intersected block
			var intersections = Intersections.GridIntersections( fromPos, toPos );
			var pathRay       = new Ray2( fromPos, toPos - fromPos );
			foreach ( var intersection in intersections )
			{
				var intersectPoint			= pathRay.GetPoint( intersection.distance );
				var (wasCollision, resolvedPoint)		= ResolveBlockCollision( intersection.prevBlock, intersectPoint );

				if ( !wasCollision )
					continue;

				//Resolve collision of entire step vector
				var collidedPosEnd = ResolveBlockCollision( intersection.prevBlock, toPos );
				return Step( resolvedPoint, collidedPosEnd.resolvedPoint );
			}

			//No collision, pass is clear
			//toMap = fromMap;
			return toPos;
		}

		protected abstract ( bool wasCollision, Vector2 resolvedPoint ) ResolveBlockCollision( GridPos blockPosition, Vector2 position );

		[Conditional("UNITY_EDITOR")]
		protected void DebugDrawCollisionPlane( GridPos block, Direction side )
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

		private static readonly (Vector3, Vector3, Vector3, Vector3) VertRectangleX =
			( new Vector3( -0.5f,  -1, 0 ), new Vector3( -0.5f, 1, 0 ), new Vector3( 0.5f, 1, 0 ),
				new Vector3( 0.5f, -1, 0 ) );
		private static readonly (Vector3, Vector3, Vector3, Vector3) VertRectangleZ =
			( new Vector3( 0,  -1, -0.5f ), new Vector3( 0, 1, -0.5f ), new Vector3( 0, 1, 0.5f ),
				new Vector3( 0, -1, 0.5f ) );

		[Conditional( "UNITY_EDITOR")]
		protected void DebugDrawCollisionPlane( Vector2 position, Direction side )
		{
			var yPosition         = _owner.Position.Y + 1;
			var centerPosition = position.ToVector3( yPosition );

			var points = side == Direction.Forward || side == Direction.Back ? VertRectangleX : VertRectangleZ;
			DrawRectangle.ForDebug(
				(points.Item1 + centerPosition),
				(points.Item2 + centerPosition),
				(points.Item3 + centerPosition),
				(points.Item4 + centerPosition),
				Color.red, 0, true
			);
		}

		[Conditional( "UNITY_EDITOR")]
		private void DebugDrawBounds( )
		{
			var yPosition = _owner.Position.Y + 1;
			DrawRectangle.ForDebug(Bound, yPosition, Color.blue);

			if ( IsMoving )
			{
				var direction = Vector3.Transform( Vector3.UnitZ, Rotation );
				DrawArrow.ForDebug( Position.ToVector3( yPosition ) + Vector3.UnitY, direction * MaxSpeed, Color.blue);
			}
		}

		public enum Type
		{
			Biped,
            MedBiped,
			BigBiped,
			Wheeled
		}

		#region IAStarGraph

		public IEnumerable<(NavNode neighbor, float neighborCost)> Neighbors( NavNode @from )
		{
			foreach ( var (edge, neighbor) in _navMap.NavGraph.GetNeighbors(@from) )
			{
				var edgeSlopeCost = GetInclineCost( edge.Slopeness );
				var roughnessCost = GetRoughnessCost( edge.Roughness );
				var speedCost     = neighbor.MaterialCost;

				var result = edge.Distance * edgeSlopeCost * roughnessCost * speedCost;
				if ( float.IsNaN( result ) )
					continue;

				yield return (neighbor, Math.Max(result, 0));
			}
		}
		public float Heuristic( NavNode @from, NavNode to )
		{
			return Vector3.Distance( from.Position3d, to.Position3d );
		}
		public virtual IEnumerable<(GridPos neighbor, float neighborCost)> Neighbors( GridPos @from )
		{
			foreach (var dir in Directions.Cardinal)
			{
				var neighborPos = from + dir.ToVector2i(  );
				var cost = GetCost( neighborPos, dir );
				if(!float.IsInfinity( cost ))
					yield return ( neighborPos, cost );
			}
		}

		public float GetCost( GridPos position, Direction walkDirection )
		{
			var neighborBound = GetBound(position);
			if ( _map.Bounds.Contains( neighborBound ) )
			{
				var cost = neighborBound.Average( blockPos => GetBlockCost( blockPos, walkDirection ) );
				return cost;
			}

			return float.PositiveInfinity;
		}

		public float Heuristic( GridPos @from, GridPos to )
		{
			//return Vector2i.ManhattanDistance(from.Position, to.Position); //7980 blocks, 186 msec, 203 meters
			return GridPos.Distance(@from, to); //11463 blocks, 194 msec, 171 meters
			//return Vector2i.RoughDistance(from.Position, to.Position);  //11703 blocks, 198 msec, 171 meters
			//return Vector2i.DistanceSquared(from.Position, to.Position); //231 blocks, 8 msec, 177 meters
		}

		#endregion
	}
}
