using UnityEngine;

/// <summary>
/// Attach to any UI / HUD root.
/// • When NOT playing, the object is disabled if <see cref="hideInEditMode"/> is true.  
/// • When entering Play Mode, it re-enables itself automatically.
/// </summary>
[ExecuteAlways]              // run in Edit mode too
public class HideInEditMode : MonoBehaviour
{
    [Tooltip("Hide this UI object while in the Editor (Play Mode ignores this).")]
    [SerializeField] private bool hideInEditMode = true;

    private void OnEnable()   => Refresh();
    private void OnValidate() => Refresh();   // called when you tick/untick in Inspector

    private void Refresh()
    {
        bool playing = Application.isPlaying;
        gameObject.SetActive(playing || !hideInEditMode);
    }
}