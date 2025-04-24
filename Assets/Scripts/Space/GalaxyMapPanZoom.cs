using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to MapViewport.  
/// • Mouse-wheel zooms MapContainer.  
/// • Left-drag pans MapContainer.  
/// Works only while GalaxyMapController.IsVisible is true.
/// </summary>
public class GalaxyMapPanZoom : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    [SerializeField] RectTransform mapContainer;   // MapContainer (dots + lines)
    [SerializeField] GalaxyMapController controller;

    [Header("Zoom")]
    [SerializeField] float zoomSpeed = 0.10f;      // wheel sensitivity
    [SerializeField] float minScale  = 0.4f;
    [SerializeField] float maxScale  = 6f;

    Vector2 lastDragPos;

    void Update()
    {
        if (!controller.IsVisible) return;

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f)
        {
            float current = mapContainer.localScale.x;          // uniform scale
            float next    = Mathf.Clamp(current * (1 + scroll * zoomSpeed),
                minScale, maxScale);
            mapContainer.localScale = Vector3.one * next;
        }
    }

    /* ───── drag handlers ───── */
    public void OnPointerDown(PointerEventData e)
    {
        if (!controller.IsVisible) return;
        lastDragPos = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (!controller.IsVisible) return;

        Vector2 delta = e.position - lastDragPos;
        lastDragPos   = e.position;

        // convert screen-space delta to local-space (accounts for Canvas scale)
        Canvas canvas = GetComponentInParent<Canvas>();
        float  factor = canvas ? canvas.scaleFactor : 1f;
        mapContainer.anchoredPosition += delta / factor;
    }
}