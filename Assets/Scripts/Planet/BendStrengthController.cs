/*
 * BendStrengthController
 * ---------------------
 * • Automatically finds the material on the first direct child’s Renderer
 *   (sharedMaterial) and drives its “_BendStrength” shader property.
 * • In Edit Mode (while not playing) the bend strength is forced to 0
 *   unless you tick “Show Bend In Editor” to preview the bend.
 * • While the game is running, it applies the value in Play-mode Strength.
 *
 * Works in Unity 6 / URP.  Attach to any GameObject that has at least one
 * direct child with a Renderer (MeshRenderer, SkinnedMeshRenderer, Sprite-
 * Renderer, etc.).  No manual material assignment needed.
 */
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]                               // run in Edit or Play mode
public class BendStrengthController : MonoBehaviour
{
    /* ────────────────  CONFIG  ──────────────── */
    [Header("Play-mode Value")]
    [Tooltip("Bend strength while the game is running")]
    [SerializeField] private float playModeStrength = 1f;

    [Header("Editor Preview")]
    [Tooltip("Tick to preview the bend effect while *not* in Play Mode")]
    [SerializeField] private bool showBendInEditor = false;

    /* ────────────────  INTERNAL  ─────────────── */
    private Material targetMaterial;
    private static readonly int BendStrengthID = Shader.PropertyToID("_BendStrength");

    /* ────────────────  LIFECYCLE  ────────────── */
    private void Awake()        => CacheTargetMaterial();   // ensure ref early
    private void OnEnable()     => ApplyBend();             // object enabled / scene opened
#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheTargetMaterial();                              // inspector tweaks may reorder children
        ApplyBend();
    }
#endif
    private void Update()      => ApplyBend();              // keep it in sync every frame

    /* ────────────────  HELPERS  ─────────────── */
    /// <summary>
    /// Finds the Renderer on the first direct child and caches its material.
    /// Call this whenever hierarchy might have changed (e.g. OnValidate).
    /// </summary>
    private void CacheTargetMaterial()
    {
        // Already cached & still valid?
        if (targetMaterial != null) return;

        if (transform.childCount == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[BendStrengthController] No children found to pull a Renderer from.", this);
#endif
            return;
        }

        Transform firstChild = transform.GetChild(0);
        Renderer childRenderer = firstChild.GetComponent<Renderer>();

        if (childRenderer == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[BendStrengthController] First child has no Renderer component.", this);
#endif
            return;
        }

        targetMaterial = childRenderer.sharedMaterial;
        if (targetMaterial == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[BendStrengthController] Renderer’s sharedMaterial is null.", this);
#endif
        }
    }

    /// <summary>Calculates the correct bend amount for the current mode and applies it.</summary>
    private void ApplyBend()
    {
        if (targetMaterial == null)
        {
            // Try again in case the material became available later
            CacheTargetMaterial();
            if (targetMaterial == null) return;
        }

        float desired =
            Application.isPlaying
                ? playModeStrength                               // Play Mode
                : (showBendInEditor ? playModeStrength : 0f);    // Edit Mode

        targetMaterial.SetFloat(BendStrengthID, desired);

#if UNITY_EDITOR
        // Mark material dirty so the change persists in the scene while editing
        if (!Application.isPlaying)
            EditorUtility.SetDirty(targetMaterial);
#endif
    }
}
