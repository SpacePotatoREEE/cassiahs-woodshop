using System.Collections.Generic;
using UnityEngine;

public class PlanetShop : MonoBehaviour
{
    [System.Serializable]
    public class StockEntry
    {
        public WeaponDefinition weapon;
        public int price = 200;
    }

    [Header("Shop Stock")]
    public List<StockEntry> stock = new();

    [Header("UI")]
    public ShopPanelUI shopPanel;
    public UnityEngine.UI.Button shopButton;   // button on your existing landing panel

    private void Start()
    {
        shopButton.onClick.AddListener(OpenShop);
    }

    private void OpenShop()
    {
        var player = FindObjectOfType<PlayerLoadout>(true);  
        var list = new List<(WeaponDefinition,int)>();
        foreach (var e in stock) list.Add((e.weapon, e.price));

        shopPanel.Populate(player, list);
        shopPanel.gameObject.SetActive(true);
    }
}