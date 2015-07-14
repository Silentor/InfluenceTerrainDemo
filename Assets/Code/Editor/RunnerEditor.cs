using UnityEditor;
using UnityEngine;

namespace Assets.Code.Editor
{
    [CustomEditor(typeof(Runner))]
    public class RunnerEditor : UnityEditor.Editor 
    {
        private Runner _target;

        void OnEnable()
        {
            _target = (Runner) target;
        }
        

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Generate"))
                {
                    _target.Generate();
                }
            }
        }
    }
}
