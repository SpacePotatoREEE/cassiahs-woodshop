using System.Collections.Generic;
using UnityEngine;

/// Keeps a list of owned weapons and exposes the “current” one.
public class PlayerLoadout : MonoBehaviour
{
    public List<WeaponDefinition> weapons = new();
    public int currentIndex = 0;

    public System.Action OnLoadoutChanged;

    public WeaponDefinition CurrentWeapon =>
        weapons.Count > 0 ? weapons[currentIndex] : null;

    public void AddWeapon(WeaponDefinition wd)
    {
        if (!weapons.Contains(wd)) weapons.Add(wd);
        currentIndex = weapons.IndexOf(wd);
        OnLoadoutChanged?.Invoke();
    }

    public void NextWeapon()
    {
        if (weapons.Count == 0) return;
        currentIndex = (currentIndex + 1) % weapons.Count;
        OnLoadoutChanged?.Invoke();
    }
}