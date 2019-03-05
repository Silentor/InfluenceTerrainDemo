using TerrainDemo.OldCodeToRevision.Threads;
using UnityEditor;
using UnityEngine;

namespace TerrainDemo.Editor
{
    [CustomEditor(typeof(ThreadPoolHost))]
    public class ThreadPoolHostInspector : UnityEditor.Editor
    {
        private ThreadPoolHost _target;

        void OnEnable()
        {
            _target = (ThreadPoolHost) target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if(Application.isPlaying)
                _target.ShowInspectorGUI();

            Repaint();
        }
    }
}
