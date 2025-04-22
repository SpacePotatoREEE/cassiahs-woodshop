using System.Collections.Generic;
using UnityEngine;

public class PlayerLoadout : MonoBehaviour
{
    [System.Serializable] public class Stack
    {
        public WeaponDefinition weapon;
        public int qty;
        public Stack(WeaponDefinition w, int q) { weapon = w; qty = q; }
    }

    public List<Stack> inventory = new();
    public int currentIndex = 0;

    public System.Action OnLoadoutChanged;
    public System.Action<Dictionary<WeaponDefinition,int>> OnActiveCountsChanged;

    public WeaponDefinition CurrentWeapon =>
        inventory.Count > 0 ? inventory[currentIndex].weapon : null;

    public void AddWeapon(WeaponDefinition wd, int amount = 1)
    {
        var stack = inventory.Find(s => s.weapon == wd);
        if (stack != null) stack.qty += amount;
        else inventory.Add(new Stack(wd, amount));

        currentIndex = inventory.FindIndex(s => s.weapon == wd);
        OnLoadoutChanged?.Invoke();
    }
    
    public void NextWeapon()
    {
        if (inventory.Count == 0) return;

        currentIndex = (currentIndex + 1) % inventory.Count;
        OnLoadoutChanged?.Invoke();
    }

    public int GetQuantity(WeaponDefinition wd)
    {
        var s = inventory.Find(st => st.weapon == wd);
        return s != null ? s.qty : 0;
    }
    
    /// <summary>
    /// Remove <paramref name="amount"/> copies of this weapon from the player's inventory.
    /// Returns true if at least one item was removed, false if the player had none.
    /// </summary>
    public bool RemoveWeapon(WeaponDefinition wd, int amount = 1)
    {
        var stack = inventory.Find(s => s.weapon == wd);
        if (stack == null || stack.qty < amount) return false;

        stack.qty -= amount;
        if (stack.qty == 0)
            inventory.Remove(stack);

        // keep currentIndex valid
        if (currentIndex >= inventory.Count)
            currentIndex = Mathf.Max(0, inventory.Count - 1);

        OnLoadoutChanged?.Invoke();
        return true;
    }

    public void NotifyActiveCountsChanged(Dictionary<WeaponDefinition,int> map) =>
        OnActiveCountsChanged?.Invoke(map);
}