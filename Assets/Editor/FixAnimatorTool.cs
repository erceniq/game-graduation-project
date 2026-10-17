using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FixAnimatorTool
{
    [MenuItem("Tools/Fix Animator Controller On Player")]
    public static void Fix()
    {
        string controllerPath = "Assets/MovementTest/MidasController.controller";
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError("MidasController.controller bulunamadı at: " + controllerPath);
            return;
        }

        Avatar avatar = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath("Assets/Deneme Assets/midas@Fast Run.fbx"))
        {
            if (obj is Avatar av) { avatar = av; break; }
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("Sahhnede Player objesi bulunamadı!");
            return;
        }

        // 1. Assign to LeftRightJump defaultController field
        var lrj = player.GetComponent<LeftRightJump>();
        if (lrj != null)
        {
            lrj.defaultController = controller;
            EditorUtility.SetDirty(lrj);
            Debug.Log("Assigned MidasController to LeftRightJump.defaultController!");
        }

        // 2. Assign to MidasModel Animator directly
        Transform midasModel = player.transform.Find("MidasModel");
        if (midasModel != null)
        {
            var anim = midasModel.GetComponent<Animator>();
            if (anim == null) anim = midasModel.gameObject.AddComponent<Animator>();

            anim.runtimeAnimatorController = controller;
            if (avatar != null) anim.avatar = avatar;
            anim.applyRootMotion = false;

            EditorUtility.SetDirty(anim);
            EditorUtility.SetDirty(midasModel.gameObject);
            Debug.Log("Assigned MidasController to MidasModel.Animator successfully!");
        }
        else
        {
            Debug.LogWarning("MidasModel child not found under Player!");
        }

        Scene activeScene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);
        Debug.Log("<b><color=green>Animator Controller başarıyla bağlandı ve sahne kaydedildi!</color></b>");
    }
}
