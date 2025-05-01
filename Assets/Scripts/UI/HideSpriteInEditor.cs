// HideSpriteInEditor.cs
// -------------------------
// Attach to the GameObject that contains the SpriteRenderer
// so you can hide the sprite while you’re editing the scene.
//
//   • Tick  “Hide In Edit Mode”  ➜ sprite disappears in the editor
//   • Untick it                 ➜ sprite is visible again
//   • When you press Play, the sprite is forced ON automatically
//
// Works in Unity 6 (URP or any RP)

using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class HideSpriteInEditor : MonoBehaviour
{
    [Tooltip("If true the sprite is hidden while the editor is *not* playing.")]
    [SerializeField] private bool hideInEditMode = true;

    private SpriteRenderer _sr;

    // -------------------------------------------------------------
    private void Awake() => Cache();

    private void OnEnable() => ApplyVisibility();

#if UNITY_EDITOR
    private void Update()
    {
        // While editing, keep the visibility in sync in case you toggle the flag
        if (!Application.isPlaying)
            ApplyVisibility();
    }
#endif

    private void OnValidate()    // called when value changes in Inspector
    {
        Cache();
        ApplyVisibility();
    }

    // -------------------------------------------------------------
    private void Cache()
    {
        if (_sr == null)
            _sr = GetComponent<SpriteRenderer>();
    }

    private void ApplyVisibility()
    {
        if (_sr == null) return;

#if UNITY_EDITOR
        // In Edit mode we follow the toggle; in Play mode we always show
        bool shouldShow = Application.isPlaying || !hideInEditMode;
#else
        // In a build the editor toggle is irrelevant – always show
        bool shouldShow = true;
#endif
        _sr.enabled = shouldShow;
    }
}