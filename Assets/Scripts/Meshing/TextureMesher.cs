using System.Linq;
using TerrainDemo.Settings;
using TerrainDemo.Tools;
using UnityEngine;

namespace TerrainDemo.Meshing
{
    public class TextureMesher
    {
        public AverageTimer MeshTimer { get { return _meshTimer;} }
        public AverageTimer TextureTimer { get { return _textureTimer; } }

        public TextureMesher(ILandSettings settings)
        {
            _grassTex =
                settings.Blocks.Where(b => b.Block == BlockType.Grass).Select(b => b.Texture).First();
            _stoneTex = settings.Blocks.Where(b => b.Block == BlockType.Rock).Select(b => b.Texture).First();
            _waterTex = settings.Blocks.Where(b => b.Block == BlockType.Water).Select(b => b.Texture).First();
            _sandTex = settings.Blocks.Where(b => b.Block == BlockType.Sand).Select(b => b.Texture).First();

            //Legacy texture generation
            _grass = _grassTex.GetPixels();
            _stone = _stoneTex.GetPixels();
        }

        ~TextureMesher()
        {
            //if(_renderTexture != null)                    //todo Call from main thread
            //    _renderTexture.Release();     
        }

        public ChunkModel Generate(Chunk chunk)
        {
            _meshTimer.Start();

            var mesh = new Mesh();

            var verts = new Vector3[chunk.GridSize * chunk.GridSize];
            for (int x = 0; x < chunk.GridSize; x++)
                for (int z = 0; z < chunk.GridSize; z++)
                    verts[z + x * chunk.GridSize] = new Vector3(x * chunk.BlockSize, chunk.HeightMap[x, z], z * chunk.BlockSize);

            mesh.vertices = verts;

            var indx = new int[(chunk.GridSize - 1) * (chunk.GridSize - 1) * 4];
            for (int x = 0; x < chunk.GridSize - 1; x++)
                for (int z = 0; z < chunk.GridSize - 1; z++)
                {
                    var index = (z + x * (chunk.GridSize - 1)) * 4;
                    indx[index + 0] = z + x * chunk.GridSize;
                    indx[index + 1] = z + 1 + x * chunk.GridSize;
                    indx[index + 2] = z + 1 + (x + 1) * chunk.GridSize;
                    indx[index + 3] = z + (x + 1) * chunk.GridSize;
                }

            mesh.SetIndices(indx, MeshTopology.Quads, 0);

            var uv = new Vector2[mesh.vertexCount];
            for (int i = 0; i < uv.Length; i++)
                uv[i] = new Vector2(verts[i].x/chunk.GridSize, verts[i].z/chunk.GridSize);
            mesh.uv = uv;

            mesh.RecalculateNormals();

            _meshTimer.Stop();

            //Generate texture
            _textureTimer.Start();
            //var tex = GenerateTextureCPU(chunk);
            var tex = GenerateTextureShader(chunk);
            _textureTimer.Stop();

            var material = new Material(Materials.Instance.Grass);
            material.mainTexture = tex;

            return new ChunkModel {Mesh = mesh, Material = material };
        }

        private readonly Color[] _grass;
        private readonly Color[] _stone;
        private readonly Texture2D _grassTex;
        private readonly Texture2D _stoneTex;
        private readonly Texture2D _waterTex;
        private readonly Texture2D _sandTex;
        private RenderTexture _renderTexture;
        private readonly AverageTimer _meshTimer = new AverageTimer();
        private readonly AverageTimer _textureTimer = new AverageTimer();

        private RenderTexture GetRenderTexture()
        {
            //if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(1024, 1024, 0);
                _renderTexture.wrapMode = TextureWrapMode.Clamp;
                _renderTexture.enableRandomWrite = true;
                _renderTexture.useMipMap = false;
                _renderTexture.generateMips = false;
                _renderTexture.Create();
            }

            return _renderTexture;
        }

        private Texture2D GenerateTextureCPU(Chunk chunk)
        {
            var result = new Texture2D(1024, 1024, TextureFormat.RGB24, true);
            var pixels = result.GetPixels();

            for (int z = 0; z < 1024 - 1; z++)
                for (int x = 0; x < 1024 - 1; x++)
                {
                    var blockType = chunk.BlockType[x/64, z/64];
                    if (blockType == BlockType.Rock)
                        pixels[x + z*1024] = _stone[x + z * 1024];
                    else
                        pixels[x + z*1024] = _grass[x + z * 1024];
                }

            result.SetPixels(pixels);
            result.Apply();

            return result;
        }

        private Texture GenerateTextureShader(Chunk chunk)
        {
            var mask = new Texture2D(chunk.BlocksCount, chunk.BlocksCount, TextureFormat.RGB24, false);      //todo consider pass mask as ComputeBuffer, to avoid create texture costs
            mask.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[chunk.BlocksCount * chunk.BlocksCount];
            for (int z = 0; z < chunk.BlocksCount; z++)
                for (int x = 0; x < chunk.BlocksCount; x++)
                {
                    var blockType = chunk.BlockType[x, z];
                    if (blockType == BlockType.Rock)
                        pixels[x + z*16] = new Color(1, 0, 0, 0);
                    else if(blockType == BlockType.Water)
                        pixels[x + z*16] = new Color(0, 1, 0, 0);
                    else if (blockType == BlockType.Sand)
                        pixels[x + z * 16] = new Color(0, 0, 1, 0);
                    else
                        pixels[x + z * 16] = new Color(0, 0, 0, 1);
                }
            mask.SetPixels(pixels);
            mask.Apply(false);

            var renderTex = GetRenderTexture();
            var shader = Materials.Instance.TextureBlendShader;
            shader.SetTexture(0, "mask", mask);
            shader.SetTexture(0, "grass", _grassTex);
            shader.SetTexture(0, "stone", _stoneTex);
            shader.SetTexture(0, "sand", _sandTex);
            shader.SetTexture(0, "water", _waterTex);
            shader.SetTexture(0, "result", renderTex);
            shader.Dispatch(0, renderTex.width / 8, renderTex.height / 8, 1);

            //Render computed texture to another one to generate auto mipmaps
            var renderTex2 = new RenderTexture(renderTex.width, renderTex.height, 0);
            renderTex2.useMipMap = true;
            renderTex2.generateMips = true;
            renderTex2.wrapMode = TextureWrapMode.Clamp;
            renderTex2.Create();
            Graphics.Blit(renderTex, renderTex2);

            return renderTex2;
        }

        public struct ChunkModel
        {
            public Mesh Mesh;
            public Material Material;
        }
    }
}
