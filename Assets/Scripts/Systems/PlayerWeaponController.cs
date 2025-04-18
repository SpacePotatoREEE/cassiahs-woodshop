using UnityEngine;

[RequireComponent(typeof(PlayerLoadout))]
public class PlayerWeaponController : MonoBehaviour
{
    [Header("Fire Controls")]
    public float fireRate = 2f;
    public Transform firePoint;

    [Header("Enemy Detection")]
    public float detectionRadius = 20f;
    public LayerMask enemyLayer;
    public bool homingBullet = true;

    private float fireTimer;
    private EnemySpaceShip currentClosestShip;
    private PlayerLoadout loadout;

    private void Awake()
    {
        loadout = GetComponent<PlayerLoadout>();
    }

    private void Update()
    {
        UpdateClosestEnemyIndicator();

        fireTimer -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Space) && fireTimer <= 0f)
        {
            Transform target = (homingBullet && currentClosestShip) ? currentClosestShip.transform : null;
            FireWeapon(target);
            fireTimer = 1f / fireRate;
        }

        // Tab cycles weapons
        if (Input.GetKeyDown(KeyCode.Tab))
            loadout.NextWeapon();
    }

    private void FireWeapon(Transform target)
    {
        var weapon = loadout.CurrentWeapon;
        if (!weapon || !weapon.bulletPrefab || !firePoint) return;

        Quaternion rot = firePoint.rotation;
        GameObject bullet = Instantiate(weapon.bulletPrefab, firePoint.position, rot);

        HitEnemyBulletController ctrl = bullet.GetComponent<HitEnemyBulletController>();
        if (ctrl)
        {
            ctrl.isHoming = (target != null);
            ctrl.SetTarget(target);
        }
    }

    #region closest‑enemy helper (unchanged logic)
    private void UpdateClosestEnemyIndicator()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        EnemySpaceShip newClosest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            var enemy = c.GetComponent<EnemySpaceShip>();
            if (!enemy) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                newClosest = enemy;
            }
        }

        if (newClosest != currentClosestShip)
        {
            currentClosestShip?.SetPlayerClosest(false);
            newClosest?.SetPlayerClosest(true);
            currentClosestShip = newClosest;
        }
    }
    #endregion
}
