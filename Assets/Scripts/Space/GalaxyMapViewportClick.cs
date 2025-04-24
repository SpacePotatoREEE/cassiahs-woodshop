using UnityEngine;
using UnityEngine.EventSystems;

/// Attach to MapViewport.
/// • Left-drag pans (handled by GalaxyMapPanZoom)
/// • Left-release with *no real drag* clears the current selection.
public class GalaxyMapViewportClick : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField] GalaxyMapController controller;
    [Tooltip("Pixels mouse must move before we treat it as a drag.")]
    [SerializeField] float dragThreshold = 5f;

    Vector2 downPos;
    bool    dragged;

    public void OnPointerDown(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        downPos = e.position;
        dragged = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;

        // mark as drag when movement exceeds threshold (screen-space)
        if (!dragged && (e.position - downPos).sqrMagnitude > dragThreshold * dragThreshold)
            dragged = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;

        // only a *click* if we never dragged
        if (!dragged)
            controller.ClearSelection();
    }
}