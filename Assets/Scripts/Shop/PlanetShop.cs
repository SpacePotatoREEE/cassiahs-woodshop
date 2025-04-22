using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// Scene‑local “store front” that owns the planet’s stock list.
public class PlanetShop : MonoBehaviour
{
    [System.Serializable]
    public class StockEntry
    {
        public WeaponDefinition weapon;
        public int buyPrice = 200;      // what player pays
        public int quantity = 6;        // how many items the shop carries
    }

    [Header("Stock (editable per planet)")]
    public List<StockEntry> stock = new();

    [Header("UI")]
    public ShopPanelUI shopPanel;
    public Button      shopButton;

    private PlayerLoadout playerLoadout;

    private void Start()
    {
        playerLoadout = FindObjectOfType<PlayerLoadout>(true);
        shopButton.onClick.AddListener(OpenShop);
    }

    private void OpenShop()
    {
        shopPanel.Open(this, playerLoadout);
        shopPanel.gameObject.SetActive(true);
    }

    /* ---------- called by ShopPanelUI ---------- */
    public void OnBought(StockEntry entry) => entry.quantity--;
    public void OnSold(StockEntry entry) => entry.quantity++;
}