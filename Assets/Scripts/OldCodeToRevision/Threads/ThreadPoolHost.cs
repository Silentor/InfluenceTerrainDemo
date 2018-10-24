using UnityEngine;

namespace TerrainDemo.OldCodeToRevision.Threads
{
    public class ThreadPoolHost : MonoBehaviour
    {
        public WorkerPool Pool
        {
            get { return _pool ?? (_pool = new WorkerPool()); }
        }

#if UNITY_EDITOR
        public void ShowInspectorGUI()
        {
            _pool?.ShowInspectorGUI();
        }
#endif

        private WorkerPool _pool;

        void Update()
        {
            Pool.Update();
        }
    }
}
