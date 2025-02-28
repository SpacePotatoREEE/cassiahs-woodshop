using UnityEngine;

public class HitEnemyBulletController : MonoBehaviour
{
    public enum ForwardAxis
    {
        PlusZ,  // Local +Z is bullet's forward
        MinusZ, // Local -Z is bullet's forward
        PlusY,  // Local +Y is bullet's forward
        MinusY  // Local -Y is bullet's forward
    }

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
    
    [Header("Layer Filtering")]
    [Tooltip("Which layers this bullet is allowed to damage. Assigned by AIWeaponController.")]
    public LayerMask allowedLayers;

    // Internal
    private float lifeTimer;
    private Transform target;

    private void OnEnable()
    {
        lifeTimer = lifetime;

        // If we want to face the enemy at spawn
        if (isHoming && target != null)
        {
            Vector3 dir = target.position - transform.position;
            if (flattenYPosition)
                dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion initialRot = ComputeDesiredRotation(dir.normalized);
                transform.rotation = initialRot;
            }
        }

        // Now lock the X axis so it doesn’t pitch
        if (lockXRotation)
        {
            Vector3 eul = transform.localEulerAngles;
            eul.x = 0f;
            transform.localEulerAngles = eul;
        }
    }

    private void Update()
    {
        // 1) HOMING ROTATION (continuous)
        if (isHoming && target != null)
        {
            Vector3 dirToTarget = target.position - transform.position;

            if (flattenYPosition)
                dirToTarget.y = 0f;

            if (dirToTarget.sqrMagnitude > 0.001f)
            {
                dirToTarget.Normalize();
                Quaternion desiredRot = ComputeDesiredRotation(dirToTarget);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    desiredRot,
                    rotateSpeed * Time.deltaTime
                );
            }

            // Lock axes if needed
            if (lockXRotation || lockYRotation || lockZRotation)
            {
                LockRotationAxes();
            }
        }

        // 2) MOVE FORWARD
        MoveForward();

        // 3) LIFETIME CHECK
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by the weapon script to set (or clear) a target transform.
    /// If isHoming = true, the bullet will steer toward that target each frame.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void OnTriggerEnter(Collider other)
    {
        // If not "EnemySpaceShip" layer, ignore
        if (other.gameObject.layer != LayerMask.NameToLayer("EnemySpaceShip"))
            return;

        // It's the player. Do damage
        EnemySpaceShip playerStats = other.GetComponent<EnemySpaceShip>();
        if (playerStats != null)
        {
            playerStats.TakeDamage(damage);
            CallAttackStateIfNeeded(playerStats);
            // Possibly spawn effect
            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
    
    private void CallAttackStateIfNeeded(EnemySpaceShip enemy)
    {
        NPCShipAI npcAI = enemy.GetComponent<NPCShipAI>();
        if (npcAI != null)
        {
            npcAI.EnterAttackState();
        }
    }

    // ---------------------------------------------------------------
    // HELPER: Compute rotation so bullet's forward axis aims at dirToTarget
    // ---------------------------------------------------------------
    private Quaternion ComputeDesiredRotation(Vector3 dirToTarget)
    {
        Vector3 bulletForwardVector = AxisVector(forwardAxis);
        return Quaternion.FromToRotation(bulletForwardVector, dirToTarget);
    }

    private Vector3 AxisVector(ForwardAxis axis)
    {
        switch (axis)
        {
            case ForwardAxis.PlusZ:  return Vector3.forward;
            case ForwardAxis.MinusZ: return Vector3.back;
            case ForwardAxis.PlusY:  return Vector3.up;
            case ForwardAxis.MinusY: return Vector3.down;
            default:                 return Vector3.forward;
        }
    }

    private void MoveForward()
    {
        Vector3 localDir = AxisVector(forwardAxis);
        transform.Translate(localDir * speed * Time.deltaTime, Space.Self);
    }

    private void LockRotationAxes()
    {
        Vector3 eul = transform.localEulerAngles;
        if (lockXRotation) eul.x = 0f;
        if (lockYRotation) eul.y = 0f;
        if (lockZRotation) eul.z = 0f;
        transform.localEulerAngles = eul;
    }
}
