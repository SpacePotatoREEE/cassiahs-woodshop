#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ClearSelectionOnPlay
{
    static ClearSelectionOnPlay()
    {
        // Clear immediately after domain reload & just before entering play.
        EditorApplication.delayCall += () => Selection.activeObject = null;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode ||
                state == PlayModeStateChange.EnteredPlayMode)
            {
                Selection.activeObject = null;
            }
        };
    }

    [MenuItem("Tools/Editor/Clear Selection Now %#0")]
    public static void ClearNow()
    {
        Selection.activeObject = null;
        Debug.Log("[Editor] Selection cleared.");
    }
}
#endif