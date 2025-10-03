using UnityEngine;

public class CompanionWeapon : MonoBehaviour
{
    [Header("Weapon")]
    [SerializeField] private CompanionProjectile projectilePrefab;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float fireRate = 4f; // shots/sec
    [SerializeField] private float projectileSpeed = 30f;
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float range = 12f;

    private float _fireCooldown;

    public float Range => range;

    public void TickFire(Transform target, Object source)
    {
        if (!target || !projectilePrefab || !muzzle) return;

        _fireCooldown -= Time.deltaTime;

        Vector3 toTarget = (GetTargetAim(target) - muzzle.position);
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr <= range * range)
        {
            // Face target
            Vector3 dir = toTarget.normalized;
            if (dir.sqrMagnitude > 0.0001f)
                muzzle.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (_fireCooldown <= 0f)
            {
                _fireCooldown = 1f / Mathf.Max(0.001f, fireRate);
                var proj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);
                proj.Init(projectileDamage, projectileSpeed, source);
            }
        }
    }

    private Vector3 GetTargetAim(Transform t)
    {
        var damageable = t.GetComponentInParent<IDamageable>();
        return damageable != null && damageable.GetAimPoint() ? damageable.GetAimPoint().position : t.position;
    }

    // Level scaling hooks
    public void SetDamageMultiplier(float m) => projectileDamage = Mathf.Max(1f, projectileDamage * m);
    public void SetFireRateMultiplier(float m) => fireRate = Mathf.Max(0.1f, fireRate * m);
    public void SetRangeMultiplier(float m) => range = Mathf.Max(1f, range * m);
}