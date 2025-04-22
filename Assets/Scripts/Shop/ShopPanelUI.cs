using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopPanelUI : MonoBehaviour
{
    [Header("UI Refs")]
    public Transform     itemListParent;
    public ShopItemUI    itemButtonPrefab;
    public Image         bigImage;
    public TextMeshProUGUI descText;
    public Button        buyButton;
    public Button        sellButton;            // ensure this exists
    public Button        doneButton;

    private PlanetShop           shop;
    private PlayerLoadout        loadout;
    private PlanetShop.StockEntry selectedEntry;
    
    // remember which row is currently highlighted
    private ShopItemUI lastSelected;
    public bool IsSelected(PlanetShop.StockEntry e) => selectedEntry == e;

    private readonly Dictionary<PlanetShop.StockEntry,ShopItemUI> buttonMap = new();

    /* ---------- lifecycle ---------- */
    private void Awake()
    {
        buyButton .onClick.AddListener(BuySelected);
        sellButton.onClick.AddListener(SellSelected);
        doneButton.onClick.AddListener(() => gameObject.SetActive(false));
        gameObject.SetActive(false);
    }

    public void Open(PlanetShop host, PlayerLoadout pl)
    {
        shop    = host;
        loadout = pl;

        BuildList();
        ClearSelection();

        loadout.OnLoadoutChanged += RefreshAllRows;    // keep “You” counts live
    }
    private void OnDisable()
    {
        if (loadout) loadout.OnLoadoutChanged -= RefreshAllRows;
    }

    /* ---------- build & refresh ---------- */
    private void BuildList()
    {
        foreach (Transform c in itemListParent) Destroy(c.gameObject);
        buttonMap.Clear();

        foreach (var entry in shop.stock)
        {
            var ui = Instantiate(itemButtonPrefab, itemListParent);
            ui.Init(entry, this);
            ui.SetSelected(false);
            buttonMap.Add(entry, ui);
        }
    }
    private void RefreshAllRows()
    {
        foreach (var kv in buttonMap) kv.Value.Refresh();
        UpdateButtonStates();
    }

    private void RefreshRow(PlanetShop.StockEntry e)
    {
        if (buttonMap.TryGetValue(e, out var ui)) ui.Refresh();
        UpdateButtonStates();
    }

    /* ---------- selection ---------- */
    public void OnItemClicked(PlanetShop.StockEntry entry, ShopItemUI ui)
    {
        // turn off previous highlight
        if (lastSelected && lastSelected != ui)
            lastSelected.SetSelected(false);

        selectedEntry = entry;
        ui.SetSelected(true);          // turn on new highlight
        lastSelected = ui;

        ShowDescription(entry);
        UpdateButtonStates();
    }

    private void ClearSelection()
    {
        selectedEntry = null;
        bigImage.sprite = null;
        descText.text   = "Select an item…";
        UpdateButtonStates();
        if (lastSelected) lastSelected.SetSelected(false);
        lastSelected = null;
    }

    /* ---------- buy / sell ---------- */
    private void BuySelected()
    {
        if (selectedEntry == null) return;
        if (selectedEntry.quantity <= 0) return;
        if (!GameManager.Instance.SpendCredits(selectedEntry.buyPrice)) return;

        loadout.AddWeapon(selectedEntry.weapon, 1);
        shop.OnBought(selectedEntry);
        RefreshRow(selectedEntry);
        ShowDescription(selectedEntry);          // keep panel on same item
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void SellSelected()
    {
        if (selectedEntry == null) return;
        if (!loadout.RemoveWeapon(selectedEntry.weapon, 1)) return;

        GameManager.Instance.AddCredits(selectedEntry.buyPrice);
        shop.OnSold(selectedEntry);
        RefreshRow(selectedEntry);
        ShowDescription(selectedEntry);
        EventSystem.current.SetSelectedGameObject(null);
    }

    /* ---------- UI helpers ---------- */
    private void UpdateButtonStates()
    {
        if (selectedEntry == null)
        {
            buyButton.interactable = sellButton.interactable = false;
            return;
        }
        int youOwn = loadout.GetQuantity(selectedEntry.weapon);
        buyButton .interactable = selectedEntry.quantity > 0;
        sellButton.interactable = youOwn               > 0;
    }

    private void ShowDescription(PlanetShop.StockEntry e)
    {
        var stats = e.weapon.GetBulletStats();
        bigImage.sprite = e.weapon.thumbnail;

        descText.text =
$@"{e.weapon.weaponName}

{(string.IsNullOrWhiteSpace(e.weapon.description) ? "" : e.weapon.description + "\n\n")}
Damage   : {stats?.damage}
Speed    : {stats?.speed}
Lifetime : {stats?.lifetime}
Fire rate: {stats?.fireRate}

Shop stock : {e.quantity}
You own    : {loadout.GetQuantity(e.weapon)}

Price: ₡ {e.buyPrice:n0}";
    }
    
    public int GetPlayerOwns(WeaponDefinition w)
    {
        return loadout ? loadout.GetQuantity(w) : 0;
    }
}
