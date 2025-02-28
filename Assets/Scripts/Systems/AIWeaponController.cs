using UnityEngine;

public class AIWeaponController : MonoBehaviour
{
    [Header("AI Weapon Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    [Tooltip("Shots per second")]
    public float fireRate = 1f;

    private float fireTimer = 0f;

    [Header("Homing Toggle")]
    public bool homingBullet = true;

    // We'll store the reference to the player's transform
    private Transform playerTransform;

    private void Awake()
    {
        // Attempt to find the player by tag
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[AIWeaponController] No GameObject found with tag 'Player'!");
        }
        else
        {
            // Safety check: if the object we found is ourselves, that's a problem
            if (player == this.gameObject)
            {
                Debug.LogError("[AIWeaponController] Found 'player' is this same object! Check your tags.");
            }
            else
            {
                playerTransform = player.transform;
            }
        }
    }

    private void Update()
    {
        fireTimer -= Time.deltaTime;

        if ( homingBullet && playerTransform != null)
        {
            // We'll just call FireAt each frame or once in a while
            if (fireTimer <= 0f)
            {
                FireAt(playerTransform);
                fireTimer = 1f / fireRate;
            }
        }
    }

    public void FireAt(Transform target)
    {
        if (bulletPrefab == null || firePoint == null || target == null) return;

        // Aim bullet
        Vector3 dir = target.position - firePoint.position;
        dir.y = 0f; // if top-down
        Quaternion spawnRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // Spawn bullet
        GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, spawnRot);

        // Make the bullet homing
        HitPlayerBulletController bulletCtrl = bulletGO.GetComponent<HitPlayerBulletController>();
        if (bulletCtrl != null)
        {
            bulletCtrl.isHoming = true;
            bulletCtrl.flattenYPosition = true;
            bulletCtrl.SetTarget(target);
        }
    }
}
