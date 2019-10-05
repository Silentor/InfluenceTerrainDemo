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

	    public Quaternion Rotation => Locomotor.Rotation;

	    public Vector3 Forward => Vector3.Transform( Vector3.UnitZ, Rotation );

	    public Vector3 Position { get; private set; }

	    public BaseBlockMap Map => _currentMap;

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
        private Vector2 _inputVelocity;
        private bool _isStopped = true;
        private float _rotateDirection;
        private Vector2? _targetPosition;
        private Quaternion _targetRotation;

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
      
        public Actor(MicroMap  map, NavigationMap navMap, GridPos startPosition, Quaternion startRotation, bool fpsMode,
	        string         name,
	        Locomotor.Type loco )
        {
	        Name        = name;
	        _mainMap    = map;
	        _currentMap = map;
	        Locomotor   = new Locomotor( loco, startPosition, startRotation, this, map, navMap );
	        Nav         = new Navigator(this, map, navMap);

	        _isHero = fpsMode;

			Update( 0.1f );
        }


        public void Stop()
        {
			Nav.Cancel( true );
			Locomotor.Stop(  );
        }

        public void Update( float deltaTime )
        {
	        var isLocomotorChanged = Locomotor.Update( deltaTime );

	        if ( isLocomotorChanged )
	        {
		        var newPos    = Locomotor.Position;
		        var newHeight = _currentMap.GetHeight( newPos );
		        Position = new Vector3( newPos.X, newHeight, newPos.Y );
		        Changed?.Invoke( this );
	        }
        }

#if UNITY_EDITOR
        public (Vector2 moveDirection, float blockInclinationSpeedMod, float blockMaterialSpeedMod) GetDebugState()
        {
            return (moveDirection: _inputVelocity,  blockInclinationSpeedMod: _currentBlockInclinationSpeedModifier,  blockMaterialSpeedMod: _currentBlockMaterialSpeedModifier);
        }

#endif
    
        //private bool UpdateFalling(float deltaTime)
        //{
        //    _fallVelocity = _fallVelocity + (-9.81f) * deltaTime;
        //    var fallHeight = Position.Y + _fallVelocity * deltaTime;
        //    _mapPosition = _mapPosition + _fallDirection * deltaTime * Speed;
        //    var mapHeight = _currentMap.GetHeight(_mapPosition);

        //    if (fallHeight < mapHeight)
        //    {
        //        //Fall is over
        //        Position = new Vector3(_mapPosition.X, mapHeight, _mapPosition.Y);
        //        _isFalling = false;
        //        _fallVelocity = 0;
        //        _fallDirection = Vector2.Zero;
        //    }
        //    else
        //    {
        //        //Still falling
        //        Position = new Vector3(_mapPosition.X, fallHeight, _mapPosition.Y);
        //    }

        //    return true;
        //}

        public override string ToString( )
        {
	        return $"{Name} at {Locomotor.BlockPosition} ({State})";
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
