using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

/// <summary>
/// Scene gizmo that represents a single StarSystemData during EDIT time.
/// * Move the GameObject → updates <see cref="StarSystemData.mapPosition"/>  
/// * Rename / colour is driven by the faction colour  
/// * Draws gizmo sphere + neighbour lines so you can eyeball the jump web
/// </summary>
[ExecuteAlways]
public class StarSystemHandle : MonoBehaviour
{
    [Tooltip("ScriptableObject that stores the real data for this node.")]
    public StarSystemData starSystem;

    [Tooltip("Show system name label in Scene view.")]
    public bool showLabel = true;

    [Tooltip("Use X‑Z plane (Y=0) for the 2‑D map.  Untick to use X‑Y.")]
    public bool useXZplane = true;

#if UNITY_EDITOR
    private Vector3 _prevPos;

    /* ───────────────  SYNC  ─────────────── */
    private void OnEnable()
    {
        SyncFromAsset();
        _prevPos = transform.localPosition;
    }

    private void Update()          // called in Edit mode because of ExecuteAlways
    {
        if (Application.isPlaying) return;

        if (transform.localPosition != _prevPos)
        {
            SyncToAsset();
            _prevPos = transform.localPosition;
        }
    }

    private void OnValidate()      // catches asset changes in inspector
    {
        SyncFromAsset();
    }

    private void SyncToAsset()
    {
        if (!starSystem) return;

        Vector3 p = transform.localPosition;
        starSystem.mapPosition = useXZplane
            ? new Vector2(p.x, p.z)
            : new Vector2(p.x, p.y);

        EditorUtility.SetDirty(starSystem);
    }

    private void SyncFromAsset()
    {
        if (!starSystem) return;

        Vector2 mp = starSystem.mapPosition;
        Vector3 newPos = useXZplane
            ? new Vector3(mp.x, 0f, mp.y)
            : new Vector3(mp.x, mp.y, 0f);

        if (transform.localPosition != newPos)
        {
            transform.localPosition = newPos;
            _prevPos = newPos;
        }

        gameObject.name = $"SYS_{starSystem.displayName}";
    }

    /* ───────────────  GIZMOS  ─────────────── */
    private void OnDrawGizmos()
    {
        if (!starSystem) return;

        // node
        Gizmos.color = starSystem.ownerFaction.ToColor();
        Gizmos.DrawSphere(transform.position, 0.25f);

        // neighbours
        if (starSystem.neighborSystems != null)
        {
            foreach (var n in starSystem.neighborSystems)
            {
                if (!n) continue;
                StarSystemHandle h = FindObjectsOfType<StarSystemHandle>()
                                       .FirstOrDefault(x => x.starSystem == n);
                if (!h) continue;

                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, h.transform.position);
            }
        }

#if UNITY_EDITOR
        if (showLabel)
        {
            Handles.color = Color.white;
            Handles.Label(transform.position + Vector3.up * 0.35f, starSystem.displayName);
        }
#endif
    }
#endif // UNITY_EDITOR
}
