using UnityEngine;

public class WeaponListDisplay : MonoBehaviour
{
    public Transform      slotParent;        // a HorizontalLayoutGroup
    public WeaponSlotUI   slotPrefab;

    private PlayerLoadout loadout;

    private void Start()
    {
        loadout = FindObjectOfType<PlayerLoadout>();
        if (loadout) loadout.OnLoadoutChanged += Refresh;
        Refresh();
    }
    private void OnDestroy()
    {
        if (loadout) loadout.OnLoadoutChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (Transform c in slotParent) Destroy(c.gameObject);
        if (!loadout) return;

        foreach (var w in loadout.weapons)
            Instantiate(slotPrefab, slotParent).Set(w);
    }
}