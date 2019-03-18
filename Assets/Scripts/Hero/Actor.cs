using System;
using OpenTK;
using TerrainDemo.Micro;
using TerrainDemo.Spatial;
using Vector2 = OpenTK.Vector2;
using Vector3 = OpenTK.Vector3;
using Quaternion = OpenTK.Quaternion;

namespace TerrainDemo.Hero
{
    public class Actor
    {
        public float Speed => 4;
        public float RotationSpeed => 90;
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public Vector3 Forward => DebugRotateVector(Rotation, Vector3.UnitZ);

        private readonly MicroMap _mainMap;
        private Vector2 _mapPosition;
        private Vector2 _moveDirection;
        private bool _isStopped = true;
        private float _targetRotation;

        private Vector2i _currentBlock;
        private BaseBlockMap _currentMap;

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

        private bool UpdateMovement(float deltaTime)
        {
            var isChanged = false;
            if (!_isStopped && Speed > 0 && _moveDirection != Vector2.Zero)
            {
                var rotatedDirection = (Vector2) DebugRotateVector(Rotation, (Vector3) _moveDirection);
                var newMapPosition = _mapPosition + rotatedDirection * deltaTime * Speed;

                //Check change block and map
                var blockPos = (Vector2i) newMapPosition;
                if (blockPos != _currentBlock)
                {
                    var newBlockState = _mainMap.GetOcclusionState(blockPos);

                    if (newBlockState.map != _currentMap)
                    {
                        //Check blocks above current 
                        if (newBlockState.state == BlockOverlapState.Above)
                        {
                            ref readonly var newBlock = ref newBlockState.map.GetBlockRef(blockPos);
                            if (newBlock.GetNominalHeight() - Position.Y < 1)
                            {
                                //todo check block inclination
                                //We can climb on block - use new map and current moving
                                _currentMap = newBlockState.map;
                            }
                            else if (newBlock.Height.Base - Position.Y > 2)
                            {
                                //We can go under the floating block - use old map and current moving
                                ;
                            }
                            else
                            {
                                //We stopped by the block, discard movement
                                newMapPosition = _mapPosition;
                                blockPos = _currentBlock;
                            }
                        }

                        //Check block overlapped with current
                        if (newBlockState.state == BlockOverlapState.Overlap)
                        {
                            ref readonly var newBlock = ref newBlockState.map.GetBlockRef(blockPos);
                            if (newBlock.GetNominalHeight() - Position.Y < 1)
                            {
                                //todo check block inclination
                                //We can climb on block - use new map and current moving
                                _currentMap = newBlockState.map;
                            }
                            else
                            {
                                //We stopped by the block, discard movement
                                newMapPosition = _mapPosition;
                                blockPos = _currentBlock;
                            }
                        }

                        //Check no block of current map above main map block, return to main map
                        if (newBlockState.state == BlockOverlapState.Under || newBlockState.state == BlockOverlapState.None)
                        {
                            _currentMap = _mainMap;

                            //maybe we fall from the edge
                            if (!_isFalling)
                            {
                                _isFalling = true;
                                _fallDirection = rotatedDirection;
                                _fallVelocity = 0;
                            }
                        }
                    }

                    _currentBlock = blockPos;
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

        private Vector3 DebugRotateVector(Quaternion rotation, Vector3 direction)
        {
            //DEBUG workaround of OpenTK Quaternion*Vector3 operator bug :(
            return (UnityEngine.Quaternion) rotation * (UnityEngine.Vector3) direction;
        }
    }
}
