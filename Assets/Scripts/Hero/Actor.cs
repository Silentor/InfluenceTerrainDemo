using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEngine;
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

        public Vector3 Forward => Rotation * Vector3.UnitZ;
        public Vector3 Position { get; private set; }

        public BaseBlockMap Map => _currentMap;

        public Vector2i BlockPos => _currentBlockPos;

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

        private readonly MicroMap _mainMap;
        private Vector2 _mapPosition;
        private Vector2 _moveDirection;
        private bool _isStopped = true;
        private float _targetRotation;

        private Vector2i _currentBlockPos;
        private BaseBlockMap _currentMap;
        private BlockOverlapState _currentOverlapState;
        private float _currentBlockInclinationSpeedModifier = 1;
        private float _currentBlockMaterialSpeedModifier = 1;

        //Fast n dirty fake falling
        private float _fallVelocity;
        private Vector2 _fallDirection;
        private bool _isFalling;

        public Actor(MicroMap map, Vector2 startPosition, Vector2 direction)
        {
            _mainMap = map;
            _currentMap = map;
            _mapPosition = startPosition;
            _currentBlockPos = (Vector2i) _mapPosition;
            Position = new Vector3(_mapPosition.X, _mainMap.GetHeight(_mapPosition), _mapPosition.Y);
            Rotation = new Quaternion(0, Vector3.CalculateAngle(Vector3.UnitZ, (Vector3)direction), 0);
        }

        public void Move(Vector2 direction)
        {
            _moveDirection = direction;
            /*
            Rotation = Quaternion.FromAxisAngle(Vector3.UnitY,
                Vector3.CalculateAngle(Vector3.UnitZ, new Vector3(direction.X, 0, direction.Y)));
                */
            _isStopped = false;
        }

        public void Stop()
        {
            _isStopped = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="normalizedDelta">-1..1</param>
        public void Rotate(float normalizedDelta)
        {
            _targetRotation = normalizedDelta * RotationSpeed;
        }

        public void Update(float deltaTime)
        {
            var isChanged = false;

            if (_isFalling)
                isChanged |= UpdateFalling(deltaTime);
            else
                isChanged |= UpdateMovement(deltaTime);

            if (_targetRotation != 0)
            {
                var rotationDelta = Quaternion.FromEulerAngles(0, _targetRotation * deltaTime, 0);
                Rotation = Rotation * rotationDelta;
                isChanged = true;
            }

            if(isChanged)
                Changed?.Invoke(this);
        }

#if UNITY_EDITOR
        public (Vector2 moveDirection, float blockInclinationSpeedMod, float blockMaterialSpeedMod) GetDebugState()
        {
            return (_moveDirection,  _currentBlockInclinationSpeedModifier,  _currentBlockMaterialSpeedModifier);
        }

#endif
        private bool UpdateMovement(float deltaTime)
        {
            var isChanged = false;
            if (!_isStopped && Speed > 0 && _moveDirection != Vector2.Zero)
            {
                var rotatedDirection = (Vector2) Vector3.Transform((Vector3)_moveDirection, Rotation);      //Rotate direction in XZ plane
                //Speed modifiers - its a fast n dirty moving modification. More accurate way - calculate friction force vector
                var step = rotatedDirection * deltaTime * Speed * _currentBlockInclinationSpeedModifier *
                           _currentBlockMaterialSpeedModifier;

                var newMapPosition = _mapPosition + step;

                //Check change block (and map maybe)
                var newBlockPosition = (Vector2i) newMapPosition;
                if (newBlockPosition != _currentBlockPos)
                {
                    if (IsPassable(_currentMap, _currentBlockPos, newBlockPosition, out var toMap))
                    {
                        _currentBlockPos = newBlockPosition;
                        //_currentOverlapState = newOverlapState;
                        _currentMap = toMap;
                    }
                    else
                        return false;

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

                    if(!_isFalling)
                        Position = new Vector3(_mapPosition.X, newHeight, _mapPosition.Y);

                    isChanged = true;
                }
            }

            return isChanged;
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
        /// todo need check new block inclination too
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
            switch (newOverlapState)
            {
                case BlockOverlapState.Above:
                {
                    ref readonly var toData = ref newMap.GetBlockData(toPos);

                    //Can we climb to floating block surface?
                    if (toData.Height - fromData.Height < 1)
                    {
                        toMap = newMap;
                        return true;
                    }

                    //Can we pass under floating block?
                    if (toData.MinHeight - fromData.Height > 2)
                    {
                        toMap = fromMap;
                        return true;
                    }
                }
                    break;

                case BlockOverlapState.Overlap:
                {
                    ref readonly var toData = ref newMap.GetBlockData(toPos);

                    //Can we climb to overlap block surface?
                    if (toData.Height - fromData.Height < 1)
                    {
                        toMap = newMap;
                        return true;
                    }
                }
                    break;

                //Check from object map to main map transition
                case BlockOverlapState.Under:
                case BlockOverlapState.None:
                {
                    //ref readonly var toData = ref _mainMap.GetBlockData(toPos);
                    toMap = _mainMap;
                    return true;
                }
                    break;
            }

            //No pass 
            toMap = null;
            return false;
        }

        /*
        /// <summary>
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="blockPosition"></param>
        /// <returns>New position outside the block</returns>
        private Vector2 ResolveBlockCollision(Vector2 oldPosition, Vector2 newPosition, Vector2i blockPosition)
        {
            //var localPosition = worldPosition - (Vector2i) worldPosition;
            var roundX = Math.Round(worldPosition.X);
            var roundZ = Math.Round(worldPosition.Y);

            if(roundX)

        }
        */

        /*
        private void CacheBlockProperties(Vector2i position, BaseBlockMap map, )
        {

        }
        */

        public event Action<Actor> Changed;

        public enum NavState
        {
            Stopped,
            Moving,
            Falling
        }
    }
}
