using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image           thumb;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI stockText;
    public TextMeshProUGUI ownText;
    public Image           background;          // drag the root Image here
    public Image highlightFrame;    // drag the overlay frame here

    private PlanetShop.StockEntry entry;
    private ShopPanelUI           panel;

    public void Init(PlanetShop.StockEntry e, ShopPanelUI p)
    {
        entry = e;
        panel = p;
        Refresh();
        SetSelected(false);
    }

    public void Refresh()
    {
        if (entry == null) return;

        thumb.sprite  = entry.weapon.thumbnail;
        nameText.text = entry.weapon.weaponName;
        stockText.text = $"Stock: {entry.quantity}";
        ownText.text   = $"You: {panel.GetPlayerOwns(entry.weapon)}";

        // grey‑out if out of stock
        bool outOfStock = entry.quantity == 0;
        background.color = outOfStock ? new Color(0.6f,0.6f,0.6f,1f) : Color.white;
        GetComponent<Button>().interactable = !outOfStock;
        
        // keep current selection tint
        var btn = GetComponent<Button>();
        if (btn && btn.targetGraphic)
            btn.targetGraphic.color = (panel.IsSelected(entry))
                ? new Color(0.8f,0.9f,1f,1f)          // selected tint
                : Color.white;                        // normal
    }

    /* ---------- selection visuals ---------- */
    public void SetSelected(bool on)
    {
        // Button tint
        var btn = GetComponent<Button>();
        if (btn && btn.targetGraphic)
            btn.targetGraphic.color = on ? new Color(0.8f,0.9f,1f,1f)
                : Color.white;

        // Custom frame
        if (highlightFrame) highlightFrame.enabled = on;
    }

    public void OnPointerClick(PointerEventData _)
    {
        panel.OnItemClicked(entry, this);
    }
}