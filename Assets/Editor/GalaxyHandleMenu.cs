#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class GalaxyHandleMenu
{
    [MenuItem("Galaxy/Create Handles From Selection")]
    private static void CreateHandles()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is StarSystemData sys)
            {
                // Skip if a handle for this asset already exists
                bool exists = false;
                foreach (StarSystemHandle h in Object.FindObjectsOfType<StarSystemHandle>())
                    if (h.starSystem == sys) { exists = true; break; }
                if (exists) continue;

                // Spawn GameObject
                GameObject go = new GameObject($"SYS_{sys.displayName}");
                StarSystemHandle handle = go.AddComponent<StarSystemHandle>();
                handle.starSystem = sys;

                // Position it according to current mapPosition (XZ plane default)
                Vector3 pos = new Vector3(sys.mapPosition.x, 0f, sys.mapPosition.y);
                go.transform.position = pos;
                Undo.RegisterCreatedObjectUndo(go, "Create StarSystemHandle");
            }
        }
    }

    // Validate so the menu only enables when selection contains StarSystemData
    [MenuItem("Galaxy/Create Handles From Selection", true)]
    private static bool ValidateCreateHandles()
    {
        foreach (Object obj in Selection.objects)
            if (obj is StarSystemData) return true;
        return false;
    }
}
#endif