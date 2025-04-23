using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI element that represents a single star‑system dot on the map.
/// Spawns from GalaxyMapController.
/// </summary>
[RequireComponent(typeof(Image))]
public class NodeButtonUI : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public StarSystemData StarSystem { get; private set; }

    private Image icon;
    private GalaxyMapController map;

    /// <param name="sizePx">diameter in pixels</param>
    public void Init(StarSystemData system, GalaxyMapController owner, float sizePx)
    {
        StarSystem = system;
        map        = owner;

        icon = GetComponent<Image>();
        icon.color = system.ownerFaction.ToColor();

        RectTransform rt = transform as RectTransform;
        rt.sizeDelta = new Vector2(sizePx, sizePx);
    }

    public void OnPointerClick(PointerEventData e)
    {
        map.OnNodeClicked(StarSystem);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        map.ShowTooltip(StarSystem.displayName);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        map.HideTooltip();
    }
}