using UnityEngine;

public class HitPlayerBulletController : MonoBehaviour
{
    public enum ForwardAxis
    {
        PlusZ,  // Local +Z is bullet's forward
        MinusZ, // Local -Z is bullet's forward
        PlusY,  // Local +Y is bullet's forward
        MinusY  // Local -Y is bullet's forward
    }
    
    [Header("Bullet Stats")]
    [Tooltip("How many shots per second this weapon can fire.")]
    public float fireRate = 2f;                 //  ← add this line

    [Header("Bullet Settings")]
    [Tooltip("How fast the bullet travels along its forward axis each frame (units/sec).")]
    public float speed = 30f;

    [Tooltip("Time (seconds) after which the bullet will self-destruct.")]
    public float lifetime = 2f;

    [Tooltip("Damage dealt to EnemySpaceShip on collision.")]
    public int damage = 10;

    [Header("Homing Settings")]
    [Tooltip("If true, bullet continuously steers toward the target each frame. If false, bullet goes straight.")]
    public bool isHoming = false;

    [Tooltip("How fast (degrees/sec) the bullet can rotate to track the target.")]
    public float rotateSpeed = 180f;

    [Header("Axis Locking")]
    [Tooltip("Lock X rotation. If true, bullet won't tilt around X-axis.")]
    public bool lockXRotation = false;

    [Tooltip("Lock Y rotation. If true, bullet won't tilt up/down (typical for top-down).")]
    public bool lockYRotation = false;

    [Tooltip("Lock Z rotation. If true, bullet won't roll around Z-axis.")]
    public bool lockZRotation = false;

    [Header("Position Flattening")]
    [Tooltip("If true, bullet will ignore Y differences to remain on a horizontal plane (top-down).")]
    public bool flattenYPosition = true;

    [Header("Forward Axis")]
    [Tooltip("Which local axis is considered 'forward' for movement? E.g., PlusY if bullet's mesh points up the green axis.")]
    public ForwardAxis forwardAxis = ForwardAxis.PlusY;

    [Header("Effects")]
    [Tooltip("Optional hit effect spawned on impact (particle, explosion, etc.).")]
    public GameObject hitEffectPrefab;

    // internal
    private float lifeTimer;
    private Transform target;

    private void OnEnable()
    {
        lifeTimer = lifetime;
    }

    private void Update()
    {
        // homing rotate
        if (isHoming && target)
            RotateTowardsTarget();

        // move
        Vector3 localDir = AxisVector(forwardAxis);
        transform.Translate(localDir * speed * Time.deltaTime, Space.Self);

        // lifetime
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f) Destroy(gameObject);
    }

    public void SetTarget(Transform t) => target = t;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var stats = other.GetComponent<PlayerStats>();
            if (stats) stats.TakeDamage(damage);

            if (hitEffectPrefab)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }

    /* ───────── helpers ───────── */
    private void RotateTowardsTarget()
    {
        Vector3 dir = target.position - transform.position;
        if (flattenYPosition) dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        dir.Normalize();
        Quaternion desired = Quaternion.FromToRotation(AxisVector(forwardAxis), dir);
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
                                                      desired,
                                                      rotateSpeed * Time.deltaTime);

        if (lockXRotation || lockYRotation || lockZRotation)
        {
            Vector3 e = transform.localEulerAngles;
            if (lockXRotation) e.x = 0f;
            if (lockYRotation) e.y = 0f;
            if (lockZRotation) e.z = 0f;
            transform.localEulerAngles = e;
        }
    }

    private Vector3 AxisVector(ForwardAxis axis) =>
        axis switch
        {
            ForwardAxis.PlusZ  => Vector3.forward,
            ForwardAxis.MinusZ => Vector3.back,
            ForwardAxis.PlusY  => Vector3.up,
            ForwardAxis.MinusY => Vector3.down,
            _                  => Vector3.forward
        };
}
