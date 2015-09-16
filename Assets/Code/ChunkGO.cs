using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Assets.Code
{
    public class ChunkGO : MonoBehaviour
    {
        public static ChunkGO Create(Chunk chunk, Mesh mesh)
        {
            var chunkGo = Get(chunk);
            chunkGo._filter.sharedMesh = mesh;  
            return chunkGo;
        }

        public static void Clear()
        {
            foreach (var chunkGo in _cache)
            {
                chunkGo.Value.gameObject.SetActive(false);
            }
        }

        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private static readonly Dictionary<Vector2i, ChunkGO> _cache = new Dictionary<Vector2i, ChunkGO>();

        private static ChunkGO Get(Chunk chunk)
        {
            ChunkGO chunkGo;
            if (!_cache.TryGetValue(chunk.Position, out chunkGo))
            {
                var go = new GameObject();
                chunkGo = go.AddComponent<ChunkGO>();
                chunkGo._filter = go.AddComponent<MeshFilter>();
                chunkGo._renderer = go.AddComponent<MeshRenderer>();
                chunkGo._renderer.sharedMaterial = Materials.Instance.Grass;
                chunkGo._renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                chunkGo._renderer.useLightProbes = false;
                go.name = chunk.Position.X + " : " + chunk.Position.Z;
                _cache.Add(chunk.Position, chunkGo);
            }
            else
            {
                Destroy(chunkGo._filter.sharedMesh);
                chunkGo._filter.sharedMesh = null;
                chunkGo.gameObject.SetActive(true);
            }

            chunkGo.transform.position = new Vector3(chunk.Position.X*chunk.Size, 0, chunk.Position.Z*chunk.Size);

            return chunkGo;
        }
    }
}
