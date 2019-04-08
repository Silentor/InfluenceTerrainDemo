using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
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
        /// <summary>
        /// Max speed (m/s)
        /// </summary>
        public float Speed => 6;

        /// <summary>
        /// Max angular speed (deg)
        /// </summary>
        public float RotationSpeed => 90;

        public Quaternion Rotation { get; private set; }

        public Vector3 Forward => Vector3.Transform(Vector3.UnitZ, Rotation);

        public Vector3 Position { get; private set; }

        public BaseBlockMap Map => _currentMap;

        public Vector2i BlockPosition => _currentBlockPos;

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

        public bool IsHero => _fpsLocomotion;

        private readonly MicroMap _mainMap;
        private Vector2 _mapPosition;
        private Vector2 _inputVelocity;
        private bool _isStopped = true;
        private float _rotateDirection;
        private Vector2? _targetPosition;
        private Quaternion _targetRotation;

        private Vector2i _currentBlockPos;
        private BaseBlockMap _currentMap;
        private BlockOverlapState _currentOverlapState;
        private float _currentBlockInclinationSpeedModifier = 1;
        private float _currentBlockMaterialSpeedModifier = 1;

        //Fast n dirty fake falling
        private float _fallVelocity;
        private Vector2 _fallDirection;
        private bool _isFalling;

        private static readonly float SafeAngle = MathHelper.DegreesToRadians(60);
        private bool _fpsLocomotion;

        public Actor(MicroMap map, Vector2 startPosition, Vector2 direction, bool fpsLocomotion)
        {
            _mainMap = map;
            _currentMap = map;
            _mapPosition = startPosition;
            _currentBlockPos = (Vector2i) _mapPosition;
            Position = new Vector3(_mapPosition.X, _mainMap.GetHeight(_mapPosition), _mapPosition.Y);
            Rotation = Quaternion.FromEulerAngles(0, Vector3.CalculateAngle(Vector3.UnitZ, (Vector3)direction), 0);
            Nav = new Navigator(this, map);
            _fpsLocomotion = fpsLocomotion;
        }

        public Actor(MicroMap map, Vector2i startPosition, Vector2 direction, bool fpsMode) 
            : this(map, BlockInfo.GetWorldCenter(startPosition), direction, fpsMode) { }

        #region FPS locomotion

        public void Move(Vector2 direction)
        {
            if (!_fpsLocomotion) return;
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
            if (!_fpsLocomotion) return;
            direction = Mathf.Clamp(direction, -1, 1);
            _rotateDirection = direction;
        }

        #endregion

        #region NPC locomotion

        public void MoveTo(Vector2 worldPosition)
        {
            if (_fpsLocomotion) return;
            _targetPosition = worldPosition;
            _inputVelocity = Vector2.Zero;
            _rotateDirection = 0;
            _isStopped = false;
            Rotate(worldPosition);
        }

        public void Rotate(Vector2 worldPosition)
        {
            if (_fpsLocomotion) return;
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
                

            if (_fpsLocomotion)
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
            return (_moveDirection: _inputVelocity,  _currentBlockInclinationSpeedModifier,  _currentBlockMaterialSpeedModifier);
        }

#endif
        private Vector2 GetThrustVelocity()
        {
            if(_isStopped || Speed <= 0.01)
                return Vector2.Zero;

            if (_fpsLocomotion)
            {
                if (_inputVelocity != Vector2.Zero)
                {
                    var rotatedVelocity = _inputVelocity.Length * (Vector2) Forward; //Rotate direction in XZ plane
                    return rotatedVelocity * Speed;
                }
            }
            else
            {
                if (_targetPosition.HasValue)
                {
                    var direction = _targetPosition.Value - _mapPosition;
                    return direction.Length > Speed ? direction.Normalized() * Speed : direction;
                }
            }

            return Vector2.Zero;
        }

        private bool UpdateMovement(float deltaTime)
        {
            var thrustVelocity = GetThrustVelocity();
            var step = thrustVelocity * deltaTime;
            var newMapPosition = _mapPosition + step;

            //Check change block(and map maybe)
            var newBlockPosition = (Vector2i)newMapPosition;
            if (newBlockPosition != _currentBlockPos)
            {
                var collidedPos = CheckPass(_currentMap, _mapPosition, newMapPosition, out _currentMap);
                newMapPosition = collidedPos;
                _currentBlockPos = (Vector2i) collidedPos;

                //return false;

                //Cache new block properties
                //_currentBlockPos = newBlockPosition;
                //_currentOverlapState = newOverlapState;
                //var currentBlockNormal = _currentMap.GetBlockData(_currentBlockPos).Normal;           
                //var incline  = Vector3.Dot((Vector3) rotatedDirection, currentBlockNormal);
                //Remap dot value to speed modifier -0.5 = 0, 0 = 1, 1 = 2 (prevent climb to over 60 deg)
                //_currentBlockInclinationSpeedModifier = Mathf.Clamp(
                //-0.66666666666f * incline * incline + 1.66666666666f * incline + 1, 0, 2);
                //var blockMat = _currentMap.GetBlockRef(_currentBlockPos).Top;
                //_currentBlockMaterialSpeedModifier = _mainMap.GetBlockSettings(blockMat).SpeedModifier;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fromMap"></param>
        /// <param name="fromPos"></param>
        /// <param name="toPos"></param>
        /// <param name="toMap"></param>
        /// <returns></returns>
        private bool IsPassable(BaseBlockMap fromMap, Vector2i fromPos, Vector2i toPos, out BaseBlockMap toMap)
        {
            //Assume that fromPos -> toPos is small for simplicity
            /*
            if (Vector2i.ManhattanDistance(fromPos, toPos) > 1)
            {
                toMap = null;
                return false;
            }
            */

            var (newMap, newOverlapState) = _mainMap.GetOverlapState(toPos);

            //Check special cases with map change
            ref readonly var fromData = ref fromMap.GetBlockData(fromPos);
            ref readonly var toData = ref BlockData.Empty;
            toMap = null;
            switch (newOverlapState)
            {
                //Check from object map to main map transition
                case BlockOverlapState.Under:
                case BlockOverlapState.None:
                {
                    toData = ref _mainMap.GetBlockData(toPos);
                    toMap = _mainMap;
                }
                    break;

                case BlockOverlapState.Above:
                {
                    ref readonly var aboveBlockData = ref newMap.GetBlockData(toPos);

                    //Can we pass under floating block?
                    if (aboveBlockData.MinHeight > fromData.MaxHeight + 2)
                    {
                        toData = ref fromMap.GetBlockData(toPos);
                        toMap = fromMap;
                    }
                    else
                    {
                        toData = ref aboveBlockData;
                        toMap = newMap;
                    }
                }
                    break;

                case BlockOverlapState.Overlap:
                {
                    toData = ref newMap.GetBlockData(toPos);
                    toMap = newMap;
                }
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (toData != BlockData.Empty)
            {
                if (toData.Height < fromData.Height + 1 &&
                    Vector3.CalculateAngle(Vector3.UnitY, toData.Normal) < SafeAngle)
                {
                    //Can step on new block
                    Assert.IsNotNull(toMap);
                    return true;
                }
            }

            //No pass 
            return false;
        }

        private Vector2 CheckPass(BaseBlockMap fromMap, Vector2 fromPos, Vector2 toPos, out BaseBlockMap toMap)
        {
            for (var i = 0; i < 2; i++)
            {
                var fromBlock = (Vector2i) fromPos;
                var toBlock = (Vector2i) toPos;

                Debug.Log($"{Time.frameCount} - {i}: From pos {fromPos} block {fromBlock} to pos {toPos} block {toBlock}");

                //Count from block as passable apriori
                if (fromBlock == toBlock)
                {
                    Debug.Log($"{Time.frameCount} - {i}: Blocks are equal");

                    toMap = fromMap;
                    return toPos;
                }

                var intersections = Intersections.GridIntersections(fromPos, toPos);
                var pathRay = new Ray2(fromPos, toPos - fromPos);
                foreach (var intersection in intersections)
                {
                    toBlock = intersection.blockPosition;
                    Debug.Log($"{Time.frameCount} - {i}: Check pass from {fromBlock} to {toBlock}");
                    if (!IsPassable(fromMap, fromBlock, toBlock, out var toMap2))
                    {
                        //Respond to collision
                        var hitPoint = pathRay.GetPoint(intersection.distance);

                        Debug.Log($"{Time.frameCount} - {i}: Inpassable block {intersection.blockPosition}, hit {hitPoint}, normal {intersection.normal}");

                        DebugExtension.DebugPoint(hitPoint, Color.red, 0.1f);

                        var collisionVector = toPos - hitPoint;

                        DrawArrow.ForDebug(hitPoint, collisionVector.Normalized(), Color.red);

                        var projectedCollisionVector = collisionVector -
                                                      Vector2.Dot(collisionVector, intersection.normal) *
                                                      (Vector2) intersection.normal;

                        DrawArrow.ForDebug(hitPoint, projectedCollisionVector.Normalized(), Color.green);
                        DrawArrow.ForDebug(fromPos, hitPoint - fromPos, Color.white);

                        //Recheck resolved point for another collision (one more time only)
                        fromPos = hitPoint + (Vector2)intersection.normal * 0.01f; // a little bit inside into "from block" 

                        if (projectedCollisionVector == Vector2.Zero) //Resolving finished, return calculated toPos and toMap
                        {
                            Debug.Log($"{Time.frameCount} - {i}: Resolving completed, result {toPos}");

                            toMap = fromMap;
                            return fromPos;
                        }
                        else
                        {
                            if (i < 1)
                            {
                                Debug.Log($"{Time.frameCount} - {i}: Projected vector {projectedCollisionVector} not zero, make next iter");
                                toPos = fromPos + projectedCollisionVector;
                                break;
                            }
                            else
                            {
                                Debug.LogWarning($"{Time.frameCount} - {i} Actor collision still not resolved completely on second check, use last good hit point");

                                toMap = fromMap;
                                return fromPos;
                            }

                        }
                    }
                    else
                    {
                        Debug.Log($"{Time.frameCount} - {i}: There is pass from {fromBlock} to {toBlock}, check next pass");
                        //Continue check intersections
                        fromBlock = toBlock;
                        fromMap = toMap2;
                    }
                }

                if (fromBlock == toBlock)
                    break;
            }

            //No collision, pass is clear
            toMap = fromMap;
            return toPos;
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
