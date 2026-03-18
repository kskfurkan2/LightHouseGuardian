using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kalender.EditorScripts
{
    public class TrailRendererFinder : EditorWindow
    {
        [MenuItem("Tools/Find Trail Renderers")]
        public static void ShowWindow()
        {
            FindAndLogTrailRenderers();
        }

        private static void FindAndLogTrailRenderers()
        {
            TrailRenderer[] renderers = Resources.FindObjectsOfTypeAll<TrailRenderer>();
            int count = 0;
            foreach (var tr in renderers)
            {
                // Only log those in the scene
                if (tr.gameObject.scene.IsValid())
                {
                    Debug.LogWarning($"Found TrailRenderer on GameObject: {tr.gameObject.name} in Scene: {tr.gameObject.scene.name}", tr.gameObject);
                    count++;
                }
            }
            Debug.Log($"Total TrailRenderers found in open scenes: {count}");
        }
    }
}
