using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopPanelUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform     itemListParent;
    public ShopItemUI    itemButtonPrefab;
    public Image         bigImage;
    public TextMeshProUGUI descText;
    public Button        buyButton;
    public Button        doneButton;

    private WeaponDefinition selectedWeapon;
    private int selectedPrice;
    private PlayerLoadout loadout;
    
    // highlight‑selection support  (NEW)
    private ShopItemUI lastSelected;

    private void Awake()
    {
        buyButton.onClick.AddListener(BuySelected);
        doneButton.onClick.AddListener(() => gameObject.SetActive(false));
        gameObject.SetActive(false);   // start hidden
    }

    public void Populate(PlayerLoadout pLoadout, List<(WeaponDefinition,int)> stock)
    {
        loadout = pLoadout;

        foreach (Transform c in itemListParent) Destroy(c.gameObject);
        foreach (var (w,cost) in stock)
            Instantiate(itemButtonPrefab, itemListParent).Init(w, cost, this);

        ClearDescription();
        selectedWeapon = null;
    }

    /* ---------- called from ShopItemUI ---------- */
    public void ShowDescription(WeaponDefinition w, int cost)
    {
        var stats = w.GetBulletStats();

        bigImage.sprite = w.thumbnail;
        descText.text =
$@"{w.weaponName}

Damage  : {stats?.damage}
Speed   : {stats?.speed}
Lifetime: {stats?.lifetime}
Homing  : {stats?.isHoming}

Price   : ₡ {cost:n0}";
    }
    public void ClearDescription()
    {
        bigImage.sprite = null;
        descText.text = "";
    }
    public void SelectItem(WeaponDefinition w,int cost)
    {
        selectedWeapon = w;
        selectedPrice  = cost;
        ShowDescription(w,cost);
    }
    
    public void NotifyItemClicked(ShopItemUI clicked)
    {
        if (lastSelected) lastSelected.SetSelected(false);  // turn off previous frame
        clicked.SetSelected(true);                          // highlight current
        lastSelected = clicked;
    }
    // ─────────────────────────────────────────────
    /* -------------------------------------------- */

    private void BuySelected()
    {
        if (!selectedWeapon) return;

        if (GameManager.Instance.SpendCredits(selectedPrice))
        {
            loadout.AddWeapon(selectedWeapon);
            ClearDescription();
            selectedWeapon = null;
        }
        else
        {
            // TODO show "not enough credits" pop‑up
        }
    }
}
