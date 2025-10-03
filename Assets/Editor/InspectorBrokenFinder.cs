#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;

public class InspectorBrokenFinder : ScriptableObject
{
    static Queue<Object> _queue;
    static Object _current;
    static bool _running;

    [MenuItem("Tools/Diagnostics/Find Broken Inspector Target/In Open Scenes", priority = 20)]
    public static void FindInOpenScenes()
    {
        if (_running) { Debug.LogWarning("[Finder] Already running."); return; }
        _queue = new Queue<Object>();
        int sceneCount = EditorSceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            var scene = EditorSceneManager.GetSceneAt(i);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    _queue.Enqueue(t.gameObject);
        }
        StartRun($"[Finder] Queued {_queue.Count} scene objects.");
    }

    [MenuItem("Tools/Diagnostics/Find Broken Inspector Target/In Project Prefabs", priority = 21)]
    public static void FindInProjectPrefabs()
    {
        if (_running) { Debug.LogWarning("[Finder] Already running."); return; }
        _queue = new Queue<Object>();
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (obj) _queue.Enqueue(obj);
        }
        StartRun($"[Finder] Queued {_queue.Count} prefabs.");
    }

    static void StartRun(string msg)
    {
        Debug.Log(msg);
        _running = true;
        Application.logMessageReceived += OnLog;
        EditorApplication.update += Step;
        Debug.Log("[Finder] Running… will stop on the first object that triggers the Inspector error.");
    }

    static void StopRun(string msg = null)
    {
        _running = false;
        _queue = null;
        _current = null;
        EditorApplication.update -= Step;
        Application.logMessageReceived -= OnLog;
        if (!string.IsNullOrEmpty(msg)) Debug.Log(msg);
    }

    static void Step()
    {
        if (_queue == null || _queue.Count == 0)
        {
            StopRun("[Finder] Done. No inspector-breaking object found in scanned set.");
            return;
        }

        _current = _queue.Dequeue();
        Selection.activeObject = _current; // makes Inspector try to render it next update
    }

    static void OnLog(string condition, string stackTrace, LogType type)
    {
        bool looksLikeInspectorCrash =
            (type == LogType.Exception || type == LogType.Error) &&
            (
                condition.Contains("GameObjectInspector") ||
                condition.Contains("m_Targets") ||
                condition.Contains("SerializedObjectNotCreatableException") ||
                condition.Contains("CreateSerializedObject")
            );

        if (!looksLikeInspectorCrash || !_running) return;

        var obj = _current ?? Selection.activeObject;
        string assetPath = obj ? AssetDatabase.GetAssetPath(obj) : "";
        string name = obj ? obj.name : "<null>";

        StopRun();

        if (obj is GameObject go)
        {
            string sceneName = go.scene.IsValid() ? go.scene.name : "";
            string hierarchy = GetHierarchyPath(go.transform);
            Debug.LogError(
                $"[Finder] Culprit that breaks the Inspector:\n" +
                $"- Object: {name}\n" +
                (string.IsNullOrEmpty(sceneName) ? "" : $"- Scene: {sceneName}\n") +
                (string.IsNullOrEmpty(assetPath) ? "" : $"- Asset Path: {assetPath}\n") +
                $"- Hierarchy: {hierarchy}\n" +
                $"Next: Select it and run Tools ▸ Cleanup ▸ Remove Missing Scripts In Selection, or reassign bad refs.",
                go
            );
        }
        else
        {
            Debug.LogError($"[Finder] Culprit selection: {name} (Asset: {assetPath}).", obj);
        }
    }

    static string GetHierarchyPath(Transform t)
    {
        var list = new List<string>();
        while (t != null) { list.Add(t.name); t = t.parent; }
        list.Reverse();
        return string.Join("/", list);
    }
}
#endif
