#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Reactional.Core;

namespace Reactional
{
    [CustomEditor(typeof(ReactionalManager))]
    internal class ReactionalManagerEditor : UnityEditor.Editor
    {
        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                return;
            }

            ReactionalManager manager = (ReactionalManager)target;
            if (manager != null && manager.enabled && manager.bundles.Count == 0)
            {
                manager.UpdateBundles();
            }
        }

        public override void OnInspectorGUI()
        {
            ReactionalManager manager = (ReactionalManager)target;
            if (GUILayout.Button("Reload bundles"))
            {
                manager.UpdateBundles();
                EditorUtility.SetDirty(target);
            }

            DrawDefaultInspector();
        }
    }
}
#endif
