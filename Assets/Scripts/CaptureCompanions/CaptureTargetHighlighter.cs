using UnityEngine;

/// <summary>
/// Optional: drop a mesh/VFX under this and toggle its active state when aimed.
/// </summary>
public class CaptureTargetHighlighter : MonoBehaviour
{
    public GameObject highlightRoot;

    public void SetHighlighted(bool on)
    {
        if (highlightRoot) highlightRoot.SetActive(on);
    }

    void OnDisable()
    {
        if (highlightRoot) highlightRoot.SetActive(false);
    }
}