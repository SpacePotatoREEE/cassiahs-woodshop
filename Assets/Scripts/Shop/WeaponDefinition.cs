using UnityEngine;

/// One entry sold in shops and equipped by the player.
[CreateAssetMenu(fileName = "Weapon_", menuName = "Shop/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    public string      weaponName = "New Weapon";
    public Sprite      thumbnail;
    public GameObject  bulletPrefab;
    public int         basePrice = 100;

    // Helper — pulls stats off the bullet prefab for UI.
    public HitEnemyBulletController GetBulletStats()
    {
        return bulletPrefab ? bulletPrefab.GetComponent<HitEnemyBulletController>() : null;
    }
}