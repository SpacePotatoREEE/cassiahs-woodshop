using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CompanionProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 30f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private LayerMask hitMask = ~0; // everything by default
    [SerializeField] private float radius = 0.05f;

    private Rigidbody _rb;
    private Object _source;

    /// <summary>Initialize projectile at spawn.</summary>
    public void Init(float dmg, float spd, Object source)
    {
        damage = dmg;
        speed  = spd;
        _source = source;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = transform.forward * speed;

        // Manual sweep to reduce tunneling on tiny colliders.
        if (Physics.SphereCast(transform.position,
                               radius,
                               transform.forward,
                               out var hit,
                               speed * Time.fixedDeltaTime,
                               hitMask,
                               QueryTriggerInteraction.Collide))
        {
            HandleRayHit(hit);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTriggerHit(other);
    }

    // --------- Hit Handling ---------

    private void HandleRayHit(RaycastHit hit)
    {
        if (!hit.collider) { Destroy(gameObject); return; }

        var damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            DoDamage(damageable, hit.point, hit.normal);
        }

        Destroy(gameObject);
    }

    private void HandleTriggerHit(Collider other)
    {
        if (!other) { Destroy(gameObject); return; }

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            // Best-effort contact data for triggers:
            Vector3 point = other.ClosestPoint(transform.position);
            // If ClosestPoint returns our position (inside trigger), fake a normal opposite travel dir.
            Vector3 normal = (point - transform.position).sqrMagnitude < 0.0001f
                ? -transform.forward
                : (transform.position - point).normalized;

            DoDamage(damageable, point, normal);
        }

        Destroy(gameObject);
    }

    private void DoDamage(IDamageable damageable, Vector3 point, Vector3 normal)
    {
        bool killed = damageable.ApplyDamage(damage, point, normal, _source);

        // Award XP to the firing companion, if applicable.
        if (killed && _source is CompanionBrain brain)
        {
            brain.NotifyKilledEnemy();
        }
    }
}
