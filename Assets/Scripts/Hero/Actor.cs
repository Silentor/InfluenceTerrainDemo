using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Navigation;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
using UnityEngine.Assertions;
using Vector2 = OpenToolkit.Mathematics.Vector2;
using Vector3 = OpenToolkit.Mathematics.Vector3;
using Quaternion = OpenToolkit.Mathematics.Quaternion;

namespace TerrainDemo.Hero
{
    public class Actor
    {
        public string Name { get; }

        /// <summary>
        /// Max speed (m/s)
        /// </summary>
        public float Speed;

        /// <summary>
        /// Max angular speed (deg)
        /// </summary>
        public float RotationSpeed => 180;

        public Quaternion Rotation { get; private set; }

        public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);

        public Vector3 Position { get; private set; }

        public BaseBlockMap Map => _currentMap;

        public GridPos BlockPosition => _currentBlockPos;

        public NavState State
        {
            get
            {
                if (_isFalling)
                    return NavState.Falling;
                else if (!_isStopped)
                    return NavState.Moving;
                else
                    return NavState.Stopped;
            }
        }

        public Navigator Nav { get; }
        public Locomotor Locomotor { get; }

        public bool IsHero => _isHero;

        public bool DebugLocomotion = true;
        public bool DebugNavigation = true;

        private readonly MicroMap _mainMap;
        private Vector2 _mapPosition;
        private Vector2 _inputVelocity;
        private bool _isStopped = true;
        private float _rotateDirection;
        private Vector2? _targetPosition;
        private Quaternion _targetRotation;

        private GridPos _currentBlockPos;
        private BaseBlockMap _currentMap;
        private BlockOverlapState _currentOverlapState;
        private float _currentBlockInclinationSpeedModifier = 1;
        private float _currentBlockMaterialSpeedModifier = 1;

        //Fast n dirty fake falling
        private float _fallVelocity;
        private Vector2 _fallDirection;
        private bool _isFalling;

        private static readonly float SafeAngle = MathHelper.DegreesToRadians(60);
        private bool _isHero;
        private bool _autoStop;

        public Actor(MicroMap map, NavigationMap navMap, Vector2 startPosition, Vector2 direction, bool isHero, string name, Locomotor.Type loco)
        {
            Name = name;
            _mainMap = map;
            _currentMap = map;
            _mapPosition = startPosition;
            _currentBlockPos = (GridPos) _mapPosition;
            Position = new Vector3(_mapPosition.X, _mainMap.GetHeight(_mapPosition), _mapPosition.Y);
            Rotation = Quaternion.FromEulerAngles(0, Vector3.CalculateAngle(Vector3.UnitZ, (Vector3)direction), 0);
			Locomotor = new Locomotor( loco, this, map, navMap );
            Nav = new Navigator(this, map, navMap);
            _isHero = isHero;

            Speed = 3f;
        }

        public Actor(MicroMap map, NavigationMap navMap, GridPos startPosition, Vector2 direction, bool fpsMode, string name, Locomotor.Type loco) 
            : this(map, navMap, BlockInfo.GetWorldCenter(startPosition), direction, fpsMode, name, loco) { }

        #region FPS locomotion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="direction">magnitude 0..1 = 0..max speed</param>
        public void Move(Vector2 direction)
        {
            if (!_isHero) return;
            _inputVelocity = direction.LengthSquared > 1 ? Vector2.Normalize(direction) : direction;
            _isStopped = false;
            _targetPosition = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="direction">-1..1</param>
        public void Rotate(float direction)
        {
            if (!_isHero) return;
            direction = Mathf.Clamp(direction, -1, 1);
            _rotateDirection = direction;
        }

        #endregion

        #region NPC locomotion

        //todo consider move to Locomotor/Navigator component
        public void MoveTo(Vector2 worldPosition, bool autoStop)
        {
            if (_isHero) return;
            _targetPosition = worldPosition;
            _inputVelocity = Vector2.Zero;
            _rotateDirection = 0;
            _isStopped = false;
            _autoStop = autoStop;
            RotateTo(worldPosition);
        }

        public void RotateTo(Vector2 worldPosition)
        {
            if (_isHero) return;
            var angle = UnityEngine.Vector2.SignedAngle(worldPosition - (Vector2)Position, UnityEngine.Vector2.up);
            _targetRotation = Quaternion.FromEulerAngles(0, MathHelper.DegreesToRadians(angle), 0);
        }

        #endregion

        public void Stop()
        {
            _isStopped = true;
        }

        public void Update(float deltaTime)
        {
            var isChanged = false;
            
            if (_isFalling)
                isChanged |= UpdateFalling(deltaTime);
            else
                isChanged |= UpdateMovement(deltaTime);
                

            if (_isHero)
            {
                if (_rotateDirection != 0)
                {
                    Rotation = Rotation * Quaternion.FromEulerAngles(0,
                            MathHelper.DegreesToRadians(_rotateDirection * RotationSpeed * deltaTime), 0);
                    isChanged = true;
                }
            }
            else
            {
                if (Rotation != _targetRotation)
                {
                    var unityQuat = UnityEngine.Quaternion.RotateTowards(Rotation, _targetRotation, RotationSpeed * deltaTime);  //todo rewrite on OpenTK
                    Rotation = unityQuat;
                }
                isChanged = true;
            }

            if(isChanged)
                Changed?.Invoke(this);
        }

#if UNITY_EDITOR
        public (Vector2 moveDirection, float blockInclinationSpeedMod, float blockMaterialSpeedMod) GetDebugState()
        {
            return (moveDirection: _inputVelocity,  blockInclinationSpeedMod: _currentBlockInclinationSpeedModifier,  blockMaterialSpeedMod: _currentBlockMaterialSpeedModifier);
        }

#endif
        private Vector2 GetThrustVelocity()
        {
            if(_isStopped || Speed <= 0.01)
                return Vector2.Zero;

            if (_isHero)
            {
                if (_inputVelocity != Vector2.Zero)
                {
                    var rotatedVelocity = (Vector2)Vector3.Transform((Vector3)_inputVelocity, Rotation); //Rotate direction in XZ plane
                    return rotatedVelocity * Speed;
                }
            }
            else
            {
                if (_targetPosition.HasValue)
                {
                    var direction = _targetPosition.Value - _mapPosition;
					if(direction == Vector2.Zero)
						return Vector2.Zero;
					else
					{
						if ( _autoStop )
							return direction.Length > Speed ? direction.Normalized( ) * Speed : direction;
						else
							return direction.Normalized( ) * Speed;
					}
                }
            }

            return Vector2.Zero;
        }

        private Vector2 GetNewPosition(Vector2 oldPosition, Vector2 velocity, float deltaTime)
        {
	        if(_isStopped || Speed <= 0.01)
		        return oldPosition;

	        if (_isHero)
	        {
		        if (velocity != Vector2.Zero)
		        {
			        return oldPosition + velocity * deltaTime;
		        }
	        }
	        else
	        {
		        if (_targetPosition.HasValue)
		        {
			        var step = velocity * deltaTime;
			        var directionToTarget = _targetPosition.Value - oldPosition;
			        if ( step.LengthSquared >= directionToTarget.LengthSquared )
			        {
				        return _targetPosition.Value;
			        }
			        else
				        return oldPosition + step;
		        }
	        }

	        return oldPosition;
        }

        private bool UpdateMovement(float deltaTime)
        {
            var thrustVelocity = GetThrustVelocity();
            //var newMapPosition = _mapPosition + thrustVelocity * deltaTime;
            var newMapPosition = GetNewPosition( _mapPosition, thrustVelocity, deltaTime );

            //Check change block(and map maybe)
            var newBlockPosition = (GridPos)newMapPosition;
            if (newBlockPosition != _currentBlockPos)
            {
                var collidedPos = Locomotor.Step(_mapPosition, newMapPosition);
                newMapPosition = collidedPos;
                _currentBlockPos = (GridPos) collidedPos;
            }

            if (DebugLocomotion)
            {
                var currentBlockBounds = BlockInfo.GetBounds(_currentBlockPos);
                DrawRectangle.ForDebug(currentBlockBounds, Position.Y, Color.blue);
            }

            if (newMapPosition != _mapPosition)
            {
                _mapPosition = newMapPosition;
                var newHeight = _currentMap.GetHeight(_mapPosition);

                if (_isFalling)
                {
                    if (Position.Y - newHeight > 0.1f)
                        //Falling confirmed
                        Position = new Vector3(_mapPosition.X, Position.Y, _mapPosition.Y);
                    else
                    {
                        //Falling is not confirmed
                        _isFalling = false;
                    }
                }

                if (!_isFalling)
                    Position = new Vector3(_mapPosition.X, newHeight, _mapPosition.Y);

                return true;
            }

            return false;
        }

        private bool UpdateFalling(float deltaTime)
        {
            _fallVelocity = _fallVelocity + (-9.81f) * deltaTime;
            var fallHeight = Position.Y + _fallVelocity * deltaTime;
            _mapPosition = _mapPosition + _fallDirection * deltaTime * Speed;
            var mapHeight = _currentMap.GetHeight(_mapPosition);

            if (fallHeight < mapHeight)
            {
                //Fall is over
                Position = new Vector3(_mapPosition.X, mapHeight, _mapPosition.Y);
                _isFalling = false;
                _fallVelocity = 0;
                _fallDirection = Vector2.Zero;
            }
            else
            {
                //Still falling
                Position = new Vector3(_mapPosition.X, fallHeight, _mapPosition.Y);
            }

            return true;
        }

        public override string ToString( )
        {
	        return $"{Name} {BlockPosition} ({State})";
        }

        public event Action<Actor> Changed;

        public enum NavState
        {
            Stopped,
            Moving,
            Falling
        }
    }
}
