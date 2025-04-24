using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LegendUI : MonoBehaviour
{
    [SerializeField] private RectTransform contentRoot; // LegendPanel
    [SerializeField] private GameObject     rowPrefab;   // Prefab_Map_LegendRow

    void Awake()
    {
        foreach (Faction fac in System.Enum.GetValues(typeof(Faction)))
            AddRow(fac);
    }

    private void AddRow(Faction fac)
    {
        GameObject row = Instantiate(rowPrefab, contentRoot);

        // find first Image & Text components in the row’s hierarchy
        Image img = row.GetComponentInChildren<Image>(true);
        if (img) img.color = fac.ToColor();

        TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
        if (label) label.text = fac.ToString();
    }
}