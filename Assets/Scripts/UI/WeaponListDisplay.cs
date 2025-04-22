using UnityEngine;
using System.Collections.Generic;

public class WeaponListDisplay : MonoBehaviour
{
    public Transform    slotParent;   // H‑Layout group
    public HUDItemSlotUI slotPrefab;   // Prefab_WeaponSlotUI

    private PlayerLoadout loadout;
    
    private Dictionary<WeaponDefinition,int> activeMap = new();

    private void Awake()
    {
        loadout = FindObjectOfType<PlayerLoadout>(true);
        if (loadout)
        {
            loadout.OnLoadoutChanged       += Refresh;
            loadout.OnActiveCountsChanged  += m => { activeMap = m; Refresh(); };
        }
        Refresh();
    }
    private void Start()
    {
        loadout = FindObjectOfType<PlayerLoadout>(true);  // include inactive
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

        foreach (var stack in loadout.inventory)
        {
            int active = activeMap.TryGetValue(stack.weapon, out int val) ? val : 0;
            Instantiate(slotPrefab, slotParent).Set(stack.weapon, stack.qty, active);
        }
    }
}