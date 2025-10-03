#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;   // <-- needed for EditorSceneManager
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public static class MissingScriptsCleaner
{
    [MenuItem("Tools/Cleanup/Report Missing Scripts In Selection")]
    public static void ReportMissingInSelection()
    {
        int count = 0;
        foreach (var go in Selection.gameObjects)
            count += ReportMissingOnHierarchy(go);
        Debug.Log($"[Cleanup] Reported {count} GameObjects with missing scripts in Selection.");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Selection")]
    public static void RemoveMissingInSelection()
    {
        int removed = 0;
        foreach (var go in Selection.gameObjects)
            removed += RemoveMissingOnHierarchy(go);
        Debug.Log($"[Cleanup] Removed missing scripts on {removed} components in Selection.");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Open Scenes")]
    public static void RemoveMissingInOpenScenes()
    {
        int removed = 0;
        // Use EditorSceneManager, not SceneManager, when editing open scenes from the editor.
        int sceneCount = EditorSceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                removed += RemoveMissingOnHierarchy(root);

            if (removed > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }
        Debug.Log($"[Cleanup] Removed {removed} missing scripts in open scenes.");
    }

    [MenuItem("Tools/Cleanup/Remove Missing Scripts In Project Prefabs")]
    public static void RemoveMissingInProjectPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null) continue;

            int removed = RemoveMissingOnHierarchy(root);
            totalRemoved += removed;

            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[Cleanup] {removed} missing components removed in {path}");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"[Cleanup] Finished. Total removed: {totalRemoved}");
    }

    // --- helpers ---
    static int ReportMissingOnHierarchy(GameObject root)
    {
        int count = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var comps = t.gameObject.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null)
                {
                    Debug.LogWarning($"[Missing] {GetPath(t)} has a missing script.", t.gameObject);
                    count++;
                }
            }
        }
        return count;
    }

    static int RemoveMissingOnHierarchy(GameObject root)
    {
        int removed = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        return removed;
    }

    static string GetPath(Transform t)
    {
        var stack = new List<string>();
        while (t != null) { stack.Add(t.name); t = t.parent; }
        stack.Reverse();
        return string.Join("/", stack);
    }
}
#endif
