using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// A single entry in the HUD weapon bar.
public class HUDItemSlotUI : MonoBehaviour
{
    public Image           thumb;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI qtyText;      // drag the small TMP label here

    /// <summary>Fill visuals: total copies + how many are mounted.</summary>
    public void Set(WeaponDefinition wd, int totalQty, int activeQty)
    {
        if (thumb)    thumb.sprite  = wd.thumbnail;
        if (nameText) nameText.text = wd.weaponName;

        if (qtyText)
        {
            // shows “×10  (4/10 active)” or just “×1”
            qtyText.gameObject.SetActive(true);
            qtyText.text = (activeQty > 0 && totalQty > 1)
                ? $"×{totalQty}  ({activeQty}/{totalQty} active)"
                : $"×{totalQty}";
        }
    }
}