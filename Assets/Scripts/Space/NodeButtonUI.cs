using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class NodeButtonUI : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    /* ------------- Inspector knobs ------------- */
    [Header("Label Settings")]
    [SerializeField] int     labelFontSize = 16;
    [SerializeField] Vector2 labelOffset   = new Vector2(8f, 0f);   // px from dot centre

    /* ------------- Public read-only ------------- */
    public StarSystemData StarSystem { get; private set; }

    /* ------------- Private refs ------------- */
    GalaxyMapController map;
    Outline outline;
    Image   currentMarker;            // green pip
    TextMeshProUGUI nameLabel;        // planet name

    /* ------------- Init from controller ------------- */
    public void Init(StarSystemData sys, GalaxyMapController owner, float dotSizePx)
    {
        StarSystem = sys;
        map        = owner;

        /* main coloured dot */
        Image dot = GetComponent<Image>();
        dot.color = sys.ownerFaction.ToColor();
        ((RectTransform)transform).sizeDelta = Vector2.one * dotSizePx;

        /* yellow selection outline */
        outline = gameObject.AddComponent<Outline>();
        outline.enabled = false;

        /* green “you-are-here” pip */
        currentMarker = new GameObject("CurrentMarker", typeof(Image))
                        .GetComponent<Image>();
        currentMarker.transform.SetParent(transform, false);
        currentMarker.raycastTarget = false;
        currentMarker.color = Color.green;
        currentMarker.rectTransform.sizeDelta = Vector2.one * (dotSizePx * 0.5f);
        currentMarker.enabled = false;

        /* name label */
        GameObject labelGO = new GameObject("NameLabel");
        labelGO.transform.SetParent(transform, false);
        nameLabel = labelGO.AddComponent<TextMeshProUGUI>();
        nameLabel.text               = sys.displayName;
        nameLabel.fontSize           = labelFontSize;
        nameLabel.enableWordWrapping = false;
        nameLabel.alignment          = TextAlignmentOptions.MidlineLeft;
        nameLabel.color              = Color.white;
        nameLabel.raycastTarget      = false;

        RectTransform nrt = nameLabel.rectTransform;
        nrt.pivot       = new Vector2(0, 0.5f);
        nrt.anchorMin   = nrt.anchorMax = new Vector2(0, 0.5f);
        nrt.anchoredPosition = new Vector2(dotSizePx * 0.5f, 0) + labelOffset;
    }

    /* ------------- Helpers ------------- */
    public void SetOutline(Color c, float w)
    {
        outline.effectColor    = c;
        outline.effectDistance = Vector2.one * w;
        outline.enabled        = true;
    }
    public void ClearOutline() => outline.enabled = false;

    public void ShowCurrentMarker(bool on) => currentMarker.enabled = on;

    /* ------------- Pointer events ------------- */
    public void OnPointerEnter(PointerEventData _) => map.ShowHover(StarSystem);
    public void OnPointerExit (PointerEventData _) => map.HideHover();
    public void OnPointerClick(PointerEventData _) => map.OnNodeClicked(StarSystem, this);
}
