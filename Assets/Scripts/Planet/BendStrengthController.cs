/*
 * BendStrengthController
 * ---------------------
 * • Sets the shader property “_BendStrength” to **0** whenever you’re in the
 *   Editor (Scene/Game view) so long as Play Mode isn’t running.
 * • When you hit Play, it automatically restores whatever strength you choose.
 * • Includes an inspector toggle “Show Bend In Editor” so you can preview
 *   the bend effect while editing if you want to.
 *
 * Works in Unity 6 / URP.  Put this on any GameObject, assign the material
 * that owns the _BendStrength property, and hit Play.
 */
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]                               // run in Edit or Play
public class BendStrengthController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Material that has the _BendStrength property")]
    [SerializeField] private Material targetMaterial;

    [Header("Play-mode Value")]
    [Tooltip("Bend strength to use while the game is running")]
    [SerializeField] private float playModeStrength = 1f;

    [Header("Editor Preview")]
    [Tooltip("Tick to preview the bend effect while *not* in Play Mode")]
    [SerializeField] private bool showBendInEditor = false;

    // Cache the property ID for speed
    private static readonly int BendStrengthID = Shader.PropertyToID("_BendStrength");

    /* ─────────────────────────  LIFECYCLE  ───────────────────────── */

    private void OnEnable()       => ApplyBend();      // scene opened / script enabled
#if UNITY_EDITOR
    private void OnValidate()     => ApplyBend();      // inspector value changed
#endif
    private void Update()         => ApplyBend();      // keep it in sync each frame

    /* ────────────────────────  INTERNAL  ────────────────────────── */
    private void ApplyBend()
    {
        if (targetMaterial == null) return;

        // Decide what strength should be right now
        float desired =
            Application.isPlaying
            ? playModeStrength                                   // Play Mode
            : (showBendInEditor ? playModeStrength : 0f);        // Edit Mode

        targetMaterial.SetFloat(BendStrengthID, desired);

#if UNITY_EDITOR
        // Mark the material dirty so the change is saved in the scene
        if (!Application.isPlaying)
            EditorUtility.SetDirty(targetMaterial);
#endif
    }
}
