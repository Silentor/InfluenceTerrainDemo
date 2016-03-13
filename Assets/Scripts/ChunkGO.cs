using System.Collections.Generic;
using TerrainDemo.Meshing;
using TerrainDemo.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace TerrainDemo
{
    public class ChunkGO : MonoBehaviour
    {
        public static ChunkGO Create(Chunk chunk, TextureMesher.ChunkModel model)
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

        public void Init(TextureMesher.ChunkModel model)
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
            var go = new GameObject();
            var chunkGo = go.AddComponent<ChunkGO>();
            chunkGo._filter = go.AddComponent<MeshFilter>();
            chunkGo._renderer = go.AddComponent<MeshRenderer>();
            chunkGo._renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            chunkGo._renderer.useLightProbes = false;
            go.name = chunk.Position.X + " : " + chunk.Position.Z;
            _allChunksGO.Add(chunkGo);

            var minCorner = Chunk.GetBounds(chunk.Position).Min;
            chunkGo.transform.position = new Vector3(minCorner.X, 0, minCorner.Z);

            return chunkGo;
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
