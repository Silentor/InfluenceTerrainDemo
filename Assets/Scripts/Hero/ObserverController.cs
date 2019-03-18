using System;
using System.Collections.Generic;
using TerrainDemo.Micro;
using TerrainDemo.OldCodeToRevision;
using TerrainDemo.Spatial;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Hero
{
    public class ObserverController : MonoBehaviour//, IObserver
    {
        public CameraType Camera;
        [Range(0, 100)]
        public int AOIRange = 50;
        public bool ShowChunksValue;

        public float Speed = 10;
        public float RotationSpeed = 180;

        public bool DebugDraw;

        public GameObject CursorObjectPrefab;

        public float FOV { get { return _camera.fieldOfView; } }
        public Vector3 Position { get { return _camera.transform.position; } }
        public Quaternion Rotation { get { return _camera.transform.rotation; } }

        public float Range { get { return AOIRange; } }

        /// <summary>
        /// Get chunk positions order by valuable
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ChunkPositionValue> ValuableChunkPos(float range)
        {
            var chunkCenterPos = Chunk.GetPositionFromWorld(new Vector2(Position.x, Position.z));
            var chunkRange = (int)(range/Chunk.Size);

            var result = new List<ChunkPositionValue>();
            //Get all chunk positions in a range
            foreach (var chunkPos in new Bounds2i(chunkCenterPos, chunkRange))
                if (Vector2i.Distance(chunkCenterPos, chunkPos)*Chunk.Size < range)
                    result.Add(new ChunkPositionValue()
                    {
                        Position = chunkPos,
                        Value = GetChunkPositionValue(chunkPos, range)
                    });

            result.Sort();
            return result;
        }

        /// <summary>
        /// Get 'flat' value (dont take account on height)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public float GetPositionValue(Vector2 position, float range)
        {
            var observerPos = new Vector2(Position.x, Position.z);

            //Distance estimation
            var distanceEst = Mathf.Sqrt(Mathf.Clamp01(1 - Vector2.Distance(position, observerPos)/range));
            if (distanceEst < 0.0001f) return 0;

            //Angle estimation
            var chunkDir = position - observerPos;
            var observerDir = new Vector2(_camera.transform.forward.x, _camera.transform.forward.z);

            //Decrease estimation for position behind my back
            if (Vector2.Angle(chunkDir, observerDir) > FOV / 2)
                distanceEst *= Mathf.InverseLerp(180, FOV / 2, Vector2.Angle(chunkDir, observerDir));

            return distanceEst;
        }

        /// <summary>
        /// Get 'flat' value (dont take account on height)
        /// </summary>
        /// <param name="chunkPos"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public float GetChunkPositionValue(Vector2i chunkPos, float range)
        {
            //Fast pass
            if (chunkPos == Chunk.GetPositionFromWorld(Position))
                return 1;

            var chunkCenterPos = Chunk.GetCenter(chunkPos);
            return GetPositionValue(chunkCenterPos, range);
        }

        /*
        public void SetLand(LandLayout land)
        {
            _land = land;
        }
        */

        /*
        public bool IsZoneVisible(ZoneLayout zone)
        {
            //todo check Zone with Observer position and neighbour zones
            
            var observerPos = new Vector2(Position.x, Position.z);

            foreach (var zoneVert in zone.Cell.Vertices)
                if (Vector2.Distance(zoneVert, observerPos) < Range)
                    return true;
                    

            return false;
        }
      */

        public bool IsBoundVisible(Bounds2i bounds)
        {
            var observerPos = new Vector2(Position.x, Position.z);

            if (Vector2.Distance((Vector2)bounds.Corner1, observerPos) < Range) return true;
            if (Vector2.Distance((Vector2)bounds.Corner2, observerPos) < Range) return true;
            if (Vector2.Distance((Vector2)bounds.Corner3, observerPos) < Range) return true;
            if (Vector2.Distance((Vector2)bounds.Corner4, observerPos) < Range) return true;

            return false;
        }

        public void DrawCursor(Vector3? worldPosition)
        {
            if (_cursorObj == null && worldPosition.HasValue)
            {
                _cursorObj = Instantiate(CursorObjectPrefab, worldPosition.Value, Quaternion.identity);
            }

            if (_cursorObj)
            {
                if (worldPosition.HasValue)
                {
                    _cursorObj.SetActive(true);
                    _cursorObj.transform.position = worldPosition.Value;
                }
                else
                {
                    _cursorObj.SetActive(false);
                }
            }
        }

        public event Action Changed = delegate {};

        private Camera _camera;

        private Vector3 _oldPosition;
        private Quaternion _oldRotation;
        private float _lastOldCheck;
        private float _currentRotation;

        private MicroMap _microMap;

        private GameObject _cursorObj;
        //private LandLayout _land;

        private void InputOnRotate(float rotateDir)
        {
            _currentRotation += rotateDir*RotationSpeed*Time.deltaTime;
            var rotation = Quaternion.Euler(25, _currentRotation, 0);
            transform.rotation = rotation;
        }

        private void InputOnMove(Vector3 moveDir)
        {
            moveDir = Rotation * moveDir;
            moveDir.y = 0;
            transform.position += moveDir*Time.deltaTime*Speed;
        }

        private void InputOnFire()
        {
            if (_microMap != null)
            {
                var worldRay = (Spatial.Ray)_camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                var hitPoint = _microMap.RaycastHeightmap(worldRay);

                if(hitPoint.HasValue)
                    _microMap.DigSphere(hitPoint.Value.Item1, 5);
            }
        }

        private void InputOnBuild()
        {
            if (_microMap != null)
            {
                var worldRay = (Spatial.Ray)_camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                var hitPoint = _microMap.RaycastHeightmap(worldRay);

                if (hitPoint.HasValue)
                    _microMap.Build(hitPoint.Value.Item1, 5);
            }

        }

        #region Unity

        void Start()
        {
            Changed();

            var input = GetComponent<Input>();
            input.Move += InputOnMove;
            input.Rotate += InputOnRotate;
            input.Fire += InputOnFire;
            input.Build += InputOnBuild;

            _microMap = GameObject.FindObjectOfType<TriRunner>().Micro;

        }

        void Update()
        {
            if (Time.time - _lastOldCheck > 0.5f && (Position != _oldPosition || Rotation != _oldRotation))
            {
                _lastOldCheck = Time.time;
                _oldPosition = Position;
                _oldRotation = Rotation;
                Changed();
            }

            if(_microMap == null)
                _microMap = GameObject.FindObjectOfType<TriRunner>().Micro;
            else
            {
                var worldRay = (Spatial.Ray)_camera.ScreenPointToRay(UnityEngine.Input.mousePosition);
                var hitPoint = _microMap.RaycastHeightmap(worldRay);

                if (hitPoint.HasValue)
                {
                    DrawCursor(hitPoint.Value.hitPoint);
                }
                else
                {
                    DrawCursor(null);
                }
            }
        }

        void OnValidate()
        {
            if (Application.isPlaying)
            {
#if UNITY_EDITOR
                if (Camera == CameraType.SceneCamera)
                    _camera = SceneView.lastActiveSceneView.camera;
                else
#endif
                    _camera = UnityEngine.Camera.main;
            }
            else
                _camera = GetComponent<Camera>();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (ShowChunksValue)
            {
                foreach (var valuableChunk in ValuableChunkPos(Range))
                    DrawRectangle.ForGizmo(
                        Chunk.GetBounds(valuableChunk.Position), 
                        Color.Lerp(Color.black, Color.red, valuableChunk.Value), true);
            }

            if (DebugDraw)
                DrawArrow.ForGizmo(transform.position, transform.forward*10, Color.magenta, 3);
        }
#endif

#endregion

        public enum CameraType
        {
            SceneCamera,
            MainCamera,
        }

        public struct ChunkPositionValue : IComparable<ChunkPositionValue>
        {
            public Vector2i Position;
            public float Value;
            public int CompareTo(ChunkPositionValue other)
            {
                return other.Value.CompareTo(Value);
            }
        }
    }
}
