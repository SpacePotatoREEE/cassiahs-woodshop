// Assets/Editor/ClearSelectionNow.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClearSelectionNow
{
    static ClearSelectionNow()
    {
        // Clear selection after domain reload to avoid the inspector enabling on a dead target
        EditorApplication.delayCall += () => Selection.activeObject = null;
    }

    [MenuItem("Tools/Editor/Clear Selection Now %#0")] // Ctrl/Cmd+Shift+0
    public static void ClearNow()
    {
        Selection.activeObject = null;
        Debug.Log("[Editor] Selection cleared.");
    }
}
#endif