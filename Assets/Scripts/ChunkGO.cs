using System.Collections.Generic;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainDemo
{
    public class ChunkGO : MonoBehaviour
    {
        public Vector2i Position { get; private set; }

        public static ChunkGO Create(Chunk chunk, ChunkModel model)
        {
            var chunkGo = Get(chunk);
            chunkGo.Init(model);
            return chunkGo;
        }

        public static void Clear()
        {
            foreach (var chunkGo in _allChunksGO)
            {
                Destroy(chunkGo._filter.sharedMesh);
                Destroy(chunkGo._renderer.sharedMaterial.mainTexture);
                Destroy(chunkGo._renderer.sharedMaterial);
                Destroy(chunkGo.gameObject);
            }
            _allChunksGO.Clear();
        }

        private void Init(ChunkModel model)
        {
            _filter.sharedMesh = model.Mesh;
            _renderer.sharedMaterial = model.Material;
        }

        public void CreateFlora(ILandSettings settings, IEnumerable<Vector3> positions)
        {
            if(positions != null)
                foreach (var position in positions)
                {
                    var newTree = Instantiate(settings.Tree);
                    newTree.transform.parent = transform;
                    newTree.transform.position = position;
                    newTree.transform.rotation = Quaternion.Euler(0, Random.Range(0, 359), 0);
                }
        }

        public void CreateStones(ILandSettings settings, IEnumerable<Vector3> positions)
        {
            if (positions != null)
                foreach (var position in positions)
                {
                    var newStone = Instantiate(settings.Stone);
                    newStone.transform.parent = transform;
                    newStone.transform.localPosition = position;
                    newStone.transform.rotation = Random.rotation;
                    newStone.transform.localScale = Vector3.one*Random.Range(1, 5);
                }
        }

        private MeshFilter _filter;
        private MeshRenderer _renderer;
        private static readonly List<ChunkGO> _allChunksGO = new List<ChunkGO>();

        private static ChunkGO Get(Chunk chunk)
        {
            var existingGO = _allChunksGO.Find(cgo => cgo.Position == chunk.Position);
            if (existingGO == null)
            {
                var go = new GameObject();
                existingGO = go.AddComponent<ChunkGO>();
                existingGO._filter = go.AddComponent<MeshFilter>();
                existingGO._renderer = go.AddComponent<MeshRenderer>();
                existingGO._renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                existingGO._renderer.lightProbeUsage = LightProbeUsage.Off;
                existingGO.Position = chunk.Position;
                go.name = chunk.Position.X + " : " + chunk.Position.Z;
                _allChunksGO.Add(existingGO);

                var minCorner = Chunk.GetBounds(chunk.Position).Min;
                existingGO.transform.position = new Vector3(minCorner.X, 0, minCorner.Z);
            }

            return existingGO;
        }

        private static Vector3 Convert(Vector2 v)
        {
            return new Vector3(v.x, 0, v.y);
        }

        private static Vector2 Convert(Vector3 v)
        {
            return new Vector2(v.x, v.z);
        }
    }
}
