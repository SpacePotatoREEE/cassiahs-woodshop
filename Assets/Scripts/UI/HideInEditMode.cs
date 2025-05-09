// Assets/Scripts/UI/HideInEditMode.cs
// Unity 6 • URP
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Keeps this GameObject disabled while in the editor so it doesn’t clutter the
/// scene, but ensures it is active when entering Play-Mode.
/// </summary>
[ExecuteAlways]                   // run in Edit-Mode too
[DisallowMultipleComponent]
public sealed class HideInEditMode : MonoBehaviour
{
    [Tooltip("If true, the object is hidden in the Scene view while editing.")]
    public bool hideInEditMode = true;

    /* ─────────────────────────────────────────────────────────────── */
    /*  EDITOR + RUNTIME                                              */
    /* ─────────────────────────────────────────────────────────────── */

    void Awake()        => ApplyVisibility();   // covers domain-reload
    void OnEnable()     => ApplyVisibility();   // covers prefab instantiation
#if UNITY_EDITOR
    void OnValidate()   => ApplyVisibility();   // respond to Inspector changes
#endif

    /* ─────────────────────────────────────────────────────────────── */
    /*  IMPLEMENTATION                                                */
    /* ─────────────────────────────────────────────────────────────── */

    void ApplyVisibility()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)             // EDIT-MODE
        {
            bool shouldBeActive = !hideInEditMode;
            // Avoid the “already being activated” error:
            if (gameObject.activeSelf != shouldBeActive)
                gameObject.SetActive(shouldBeActive);
            return;
        }
#endif
        // PLAY-MODE: always enable so gameplay scripts can rely on it
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}