using UnityEngine;

[CreateAssetMenu(fileName = "Weapon_", menuName = "Shop/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    public string      weaponName = "New Weapon";
    public Sprite      thumbnail;
    public GameObject  bulletPrefab;
    public int         basePrice = 100;

    [TextArea]                                           // NEW
    public string      description;                      // NEW  ←────────────

    public HitEnemyBulletController GetBulletStats()
        => bulletPrefab ? bulletPrefab.GetComponent<HitEnemyBulletController>() : null;
}