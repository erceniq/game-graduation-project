using UnityEngine;
using UnityEditor;

public static class VisualizerMenuTool
{
    [MenuItem("Tools/Add Collider Visualizer to Scene Camera")]
    public static void AddVisualizer()
    {
        // 1. Add to Player directly (LineRenderer guaranteed visible in Game View)
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            if (player.GetComponent<PlayerCapsuleVisualizer>() == null)
            {
                player.AddComponent<PlayerCapsuleVisualizer>();
                EditorUtility.SetDirty(player);
                Debug.Log("<color=green>PlayerCapsuleVisualizer Player'a eklendi!</color>");
            }
        }

        // 2. Add to Main Camera (URP endCameraRendering for all scene colliders)
        Camera cam = Object.FindFirstObjectByType<Camera>();
        if (cam != null)
        {
            if (cam.GetComponent<RuntimeColliderVisualizer>() == null)
            {
                cam.gameObject.AddComponent<RuntimeColliderVisualizer>();
                EditorUtility.SetDirty(cam.gameObject);
                Debug.Log("<color=green>RuntimeColliderVisualizer Main Camera'ya eklendi!</color>");
            }
        }

        Debug.Log("<b>Collider Görselleştirici hazır! Play modunda F1 ile kontrol edebilirsiniz.</b>");
    }
}
