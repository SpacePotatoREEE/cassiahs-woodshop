using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MinimapAutoSize : MonoBehaviour
{
    [SerializeField] private float referencePixels = 200f; // size on a 1080‑pixel tall screen

    private RectTransform rt;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        Refresh();
    }
    private void OnRectTransformDimensionsChange() => Refresh();

    private void Refresh()
    {
        float scale = Screen.height / 1080f;          // based on height
        float size  = referencePixels * scale;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   size);
    }
}