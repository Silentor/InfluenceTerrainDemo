using UnityEngine;

namespace TerrainDemo.Threads
{
    public class ThreadPoolHost : MonoBehaviour
    {
        public WorkerPool Pool
        {
            get { return _pool ?? (_pool = new WorkerPool()); }
        }

        public void ShowInspectorGUI()
        {
            if(_pool != null)
                _pool.ShowInspectorGUI();
        }

        private WorkerPool _pool;

        void Update()
        {
            Pool.Update();
        }
    }
}
