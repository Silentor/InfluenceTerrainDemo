using System;
using OpenToolkit.Mathematics;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
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
        public float Speed => 4;

        /// <summary>
        /// Max angular speed (deg)
        /// </summary>
        public float RotationSpeed => 90;

        

        public Quaternion Rotation { get; private set; }

        public Vector3 Forward => Rotation * Vector3.UnitZ;
        public Vector3 Position { get; private set; }

        public BaseBlockMap Map => _currentMap;

        public Vector2i Block => _currentBlock;

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

        private Vector2i _currentBlock;
        private BaseBlockMap _currentMap;
        private BlockOverlapState _currentOverlapState;
        private float _blockInclinationSpeedModifier = 1;
        private float _blockMaterialSpeedModifier = 1;

        //Fast n dirty fake falling
        private float _fallVelocity;
        private Vector2 _fallDirection;
        private bool _isFalling;

        public Actor(MicroMap map, Vector2 startPosition, Quaternion rotation)
        {
            _mainMap = map;
            _currentMap = map;
            _mapPosition = startPosition;
            _currentBlock = (Vector2i) _mapPosition;
            Position = new Vector3(_mapPosition.X, _mainMap.GetHeight(_mapPosition), _mapPosition.Y);
            Rotation = rotation;
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
            return (_moveDirection, _blockInclinationSpeedModifier, _blockMaterialSpeedModifier);
        }

#endif
        private bool UpdateMovement(float deltaTime)
        {
            var isChanged = false;
            if (!_isStopped && Speed > 0 && _moveDirection != Vector2.Zero)
            {
                var rotatedDirection = (Vector2) Vector3.Transform((Vector3)_moveDirection, Rotation);      //Rotate direction in XZ plane

                //DEBUG
                var currentBlockNormal = _currentMap.GetNormal(_currentBlock);           //todo cache
                _blockInclinationSpeedModifier = Vector3.Dot((Vector3) rotatedDirection, currentBlockNormal)/(rotatedDirection.Length);
                _blockInclinationSpeedModifier = (_blockInclinationSpeedModifier + 1);      //0..2

                var blockMat = _currentMap.GetBlockRef(_currentBlock).Top;
                _blockMaterialSpeedModifier = _mainMap.GetBlockSettings(blockMat).SpeedModifier;
                    //DEBUG

                var newMapPosition = _mapPosition + rotatedDirection * deltaTime * (Speed * _blockInclinationSpeedModifier * _blockMaterialSpeedModifier);

                //Check change block and map
                var newBlockPosition = (Vector2i) newMapPosition;
                if (newBlockPosition != _currentBlock)
                {
                    var (newMap, newOverlapState) = _mainMap.GetOverlapState(newBlockPosition);

                    if (newOverlapState != _currentOverlapState || newMap != _currentMap)
                    {
                        switch (newOverlapState)
                        {
                            case BlockOverlapState.Above:
                            {
                                ref readonly var newBlock = ref newMap.GetBlockRef(newBlockPosition);
                                if (newBlock.GetNominalHeight() - Position.Y < 1)       //Check prev block height difference
                                {
                                    //todo check block inclination
                                    //We can climb on block - use new map
                                    _currentMap = newMap;
                                }
                                else if (newBlock.Height.Base - Position.Y > 2)
                                {
                                    //We can go under the floating block - use old map
                                }
                                else
                                {
                                    //We stopped by the low gap or very tall wall of block, discard movement
                                    return false;
                                }

                                break;
                            }

                            case BlockOverlapState.Overlap:
                            {
                                ref readonly var newBlock = ref newMap.GetBlockRef(newBlockPosition);
                                if (newBlock.GetNominalHeight() - Position.Y < 1)       //Check prev block height difference
                                    {
                                    //todo check block inclination
                                    //We can climb on block - use new map
                                    _currentMap = newMap;
                                }
                                else
                                {
                                    //We stopped by the tall wall of block, discard movement
                                    return false;
                                }

                                break;
                            }

                            case BlockOverlapState.None:
                            case BlockOverlapState.Under:
                            {
                                _currentMap = _mainMap;

                                //Maybe we fall from the edge of object map
                                if (!_isFalling && (_currentOverlapState == BlockOverlapState.Above || _currentOverlapState == BlockOverlapState.Overlap))
                                {
                                    _isFalling = true;
                                    _fallDirection = rotatedDirection;
                                    _fallVelocity = 0;
                                }

                                break;
                            }
                        }
                    }

                    _currentBlock = newBlockPosition;
                    _currentOverlapState = newOverlapState;
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

        public event Action<Actor> Changed;

        public enum NavState
        {
            Stopped,
            Moving,
            Falling
        }
    }
}
