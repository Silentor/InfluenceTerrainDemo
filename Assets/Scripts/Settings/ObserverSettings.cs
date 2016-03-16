using System;
using System.Collections.Generic;
using TerrainDemo.Hero;
using TerrainDemo.Tools;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Settings
{
    public class ObserverSettings : MonoBehaviour, IObserver
    {
        public CameraType Camera;
        [Range(0, 100)]
        public int AOIRange = 50;
        public bool ShowChunksValue;

        public float FOV { get { return _camera.fieldOfView; } }
        public Vector3 Position { get { return _camera.transform.position; } }
        public Quaternion Rotation { get { return _camera.transform.rotation; } }

        /// <summary>
        /// Get chunk positions order by valuable
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ChunkPositionValue> ValuableChunkPos(int range)
        {
            var chunkCenterPos = Chunk.GetPosition(new Vector2(Position.x, Position.z));
            var chunkRange = range/Chunk.Size;

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
        public float GetPositionValue(Vector2 position, int range)
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
        public float GetChunkPositionValue(Vector2i chunkPos, int range)
        {
            //Fast pass
            if (chunkPos == Chunk.GetPosition(Position))
                return 1;

            var chunkCenterPos = Chunk.GetCenter(chunkPos);
            return GetPositionValue(chunkCenterPos, range);
        }

        private Camera _camera;

        void OnValidate()
        {
            if (Application.isPlaying)
            {
                if (Camera == CameraType.SceneCamera)
                    _camera = SceneView.currentDrawingSceneView.camera;
                else
                    _camera = UnityEngine.Camera.main;
            }
        }

        void OnDrawGizmos()
        {
            if (Application.isPlaying && ShowChunksValue)
            {
                foreach (var valuableChunk in ValuableChunkPos(AOIRange))
                    DrawRectangle.ForGizmo(
                        Chunk.GetBounds(valuableChunk.Position), 
                        Color.Lerp(Color.black, Color.red, valuableChunk.Value), true);
            }
        }

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
