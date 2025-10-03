#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// Traces which object/prefab causes the Inspector to throw (m_Targets / GameObjectInspector / SerializedObjectNotCreatableException).
/// Auto-armed on domain reload. Toggle via menu.
[InitializeOnLoad]
public static class InspectorErrorTracer
{
    private static bool _armed = true;                 // auto-armed by default
    private static Object _lastSelection;

    static InspectorErrorTracer()
    {
        Application.logMessageReceived += OnLog;
        Selection.selectionChanged += () => _lastSelection = Selection.activeObject;
        Debug.Log("[Tracer] InspectorErrorTracer armed. Use Tools ▸ Diagnostics ▸ Disarm if needed.");
    }

    [MenuItem("Tools/Diagnostics/Arm Inspector Error Tracer", priority = 10)]
    public static void Arm()  { _armed = true;  Debug.Log("[Tracer] Armed."); }

    [MenuItem("Tools/Diagnostics/Disarm Inspector Error Tracer", priority = 11)]
    public static void Disarm(){ _armed = false; Debug.Log("[Tracer] Disarmed."); }

    private static void OnLog(string condition, string stackTrace, LogType type)
    {
        if (!_armed) return;

        // Match the editor errors you’re seeing (wider net)
        bool looksLikeInspectorCrash =
            (type == LogType.Exception || type == LogType.Error) &&
            (
                condition.Contains("GameObjectInspector") ||
                condition.Contains("m_Targets") ||
                condition.Contains("SerializedObjectNotCreatableException") ||
                condition.Contains("CreateSerializedObject")
            );

        if (!looksLikeInspectorCrash) return;

        var sel = Selection.objects;
        if (sel == null || sel.Length == 0)
            sel = _lastSelection ? new[] { _lastSelection } : null;

        if (sel == null)
        {
            Debug.LogWarning("[Tracer] Inspector threw, but selection was empty (likely cleared). Try the Finder (Tools ▸ Diagnostics ▸ Find Broken...).");
            return;
        }

        foreach (var obj in sel)
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);

            if (obj is GameObject go)
            {
                string sceneName = go.scene.IsValid() ? go.scene.name : "";
                string hierarchyPath = GetHierarchyPath(go.transform);

                // Count missing scripts on this object + children (no side effects)
                int missingCount = 0;
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                {
                    var comps = t.GetComponents<Component>();
                    foreach (var c in comps) if (c == null) missingCount++;
                }

                Debug.LogError(
                    $"[Tracer] Inspector error likely caused by:\n" +
                    $"- Object: {go.name}\n" +
                    (string.IsNullOrEmpty(sceneName) ? "" : $"- Scene: {sceneName}\n") +
                    (string.IsNullOrEmpty(assetPath) ? "" : $"- Asset Path: {assetPath}\n") +
                    $"- Hierarchy: {hierarchyPath}\n" +
                    $"- Missing (MonoBehaviour) count on object+children: {missingCount}\n" +
                    $"Fix: Select it and run Tools ▸ Cleanup ▸ Remove Missing Scripts In Selection, or reassign broken refs.",
                    go
                );
            }
            else
            {
                Debug.LogError($"[Tracer] Inspector error while selection was: {obj} (Asset: {assetPath}).", obj);
            }
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        System.Collections.Generic.List<string> parts = new();
        while (t != null) { parts.Add(t.name); t = t.parent; }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
#endif
