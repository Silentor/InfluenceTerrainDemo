using System;
using System.Diagnostics;
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
	public abstract class BaseLocomotor
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

		public bool IsMoving { get; private set; }

		public static BaseLocomotor Create(
			Type          type, Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map,
			NavigationMap navMap )
		{
			switch ( type )
			{
				case Type.Biped:
					return new SmallBiped( startPosition, startRotation, owner, map, navMap );
				case Type.BigBiped:
					return new BigBiped( startPosition, startRotation, owner, map, navMap );
				case Type.Wheeled:
					return new SmallWheel( startPosition, startRotation, owner, map, navMap );
				default:
					throw new ArgumentOutOfRangeException( nameof( type ), type, null );
			}
		}

		public BaseLocomotor( Vector2 startPosition, Quaternion startRotation, Actor owner, MicroMap map, NavigationMap navMap )
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
			_targetPosition = position;
			_targetVelocity = Vector2.Zero;
			IsMoving = true;
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
		public abstract float GetCost( LocalIncline edgeSlopeness );

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
		

		private Vector2 _targetVelocity;
		private Vector2? _targetPosition;
		private float _rotateDirection;
		private Quaternion _targetRotation;
		private Vector2? _lookAt;
		private float _rotation;

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

		protected abstract Vector2 ResolveBlockCollision( GridPos blockPosition, Vector2 position );

		protected abstract bool CheckBlock( in NavigationGrid.Block block );

		[Conditional("UNITY_EDITOR")]
		protected void DebugDrawCollisionPlane( GridPos block, Side2d side )
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
			BigBiped,
			Wheeled
		}
	}
}
