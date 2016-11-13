using System.Collections.Generic;

namespace TerrainDemo.Meshing
{
    /// <summary>
    /// Generate ready to visualization mesh from terrain chunk data
    /// </summary>
    public abstract class BaseMesher
    {
        public abstract ChunkModel Generate(Chunk chunk, Dictionary<Vector2i, Chunk> map);

        public virtual void Clear()
        {
            ChunkGO.Clear();
            Dispose();
        }

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
