using System.Collections.Generic;

namespace TerrainDemo.Meshing
{
    public abstract class BaseMesher
    {
        public abstract ChunkModel Generate(Chunk chunk, Dictionary<Vector2i, Chunk> map);

        /// <summary>
        /// Called when mesh generation stage finished. Release resources
        /// </summary>
        public virtual void Dispose()
        {
            
        }

        public virtual void DebugLogStatistic()
        {
            
        }
    }
}
