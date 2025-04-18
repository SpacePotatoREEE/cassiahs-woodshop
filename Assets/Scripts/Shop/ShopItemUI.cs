using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// One button in the shop’s scroll list.
public class ShopItemUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image            thumb;
    public TextMeshProUGUI  nameText;
    public TextMeshProUGUI  priceText;
    public Image            highlightFrame;        // optional – outline for “selected”

    private WeaponDefinition weapon;
    private int              price;
    private ShopPanelUI      panel;

    /* ─────────────  public API  ───────────── */
    public void Init(WeaponDefinition w, int cost, ShopPanelUI owner)
    {
        weapon = w;
        price  = cost;
        panel  = owner;

        if (thumb)     thumb.sprite  = w.thumbnail;
        if (nameText)  nameText.text = w.weaponName;
        if (priceText) priceText.text = $"₡ {cost:n0}";

        SetSelected(false);                // reset visual state
    }

    public void SetSelected(bool value)
    {
        if (highlightFrame)
            highlightFrame.enabled = value;
    }

    /* ─────────────  events  ───────────── */
    public void OnPointerClick(PointerEventData _)
    {
        panel.SelectItem(weapon, price);   // panel also calls ShowDescription
        panel.NotifyItemClicked(this);     // so panel can toggle highlight frames
    }
    
}