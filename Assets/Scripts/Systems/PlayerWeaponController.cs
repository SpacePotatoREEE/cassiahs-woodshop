using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [Header("Firing Controls")]
    public float fireRate = 2f;     
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Enemy Detection")]
    public float detectionRadius = 20f;
    public LayerMask enemyLayer;

    [Header("Homing Toggle")]
    public bool homingBullet = true;

    private float fireTimer = 0f;
    
    // NEW: track the single "closest" for indicator
    private EnemySpaceShip currentClosestShip = null;

    private void Update()
    {
        // 1) Maintain highlight for the single closest enemy
        UpdateClosestEnemyIndicator();

        // 2) Shooting logic (unchanged)
        fireTimer -= Time.deltaTime;
        if (Input.GetKey(KeyCode.Space) && fireTimer <= 0f)
        {
            // If homing is on, we find the same target
            Transform target = null;
            if (homingBullet && currentClosestShip != null)
            {
                target = currentClosestShip.transform;
            }

            FireWeapon(target);
            fireTimer = 1f / fireRate;
        }
    }

    private void UpdateClosestEnemyIndicator()
    {
        // Overlap sphere to find all enemies in detectionRadius
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);

        EnemySpaceShip newClosest = null;
        float minDist = Mathf.Infinity;

        foreach (Collider c in hits)
        {
            EnemySpaceShip enemy = c.GetComponent<EnemySpaceShip>();
            if (enemy == null) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                newClosest = enemy;
            }
        }

        // If the newClosest is different than currentClosestShip, update
        if (newClosest != currentClosestShip)
        {
            // Turn off old closest
            if (currentClosestShip != null)
            {
                currentClosestShip.SetPlayerClosest(false);
            }
            // Turn on new
            if (newClosest != null)
            {
                newClosest.SetPlayerClosest(true);
            }
            currentClosestShip = newClosest;
        }
    }

    private void FireWeapon(Transform target)
    {
        if (bulletPrefab == null || firePoint == null) return;

        Quaternion spawnRot = firePoint.rotation;
        GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, spawnRot);

        HitEnemyBulletController bulletCtrl = bulletGO.GetComponent<HitEnemyBulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.isHoming = (target != null);
            bulletCtrl.SetTarget(target);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
