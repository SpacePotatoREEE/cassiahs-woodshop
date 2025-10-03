using UnityEngine;
using System.Linq;

[DisallowMultipleComponent]
public class CompanionBrain : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float tetherRadius = 2.5f; // how far it can wander around its assigned slot
    [SerializeField] private float slotFollowStrength = 8f; // how firmly it returns to slot anchor

    [Header("Targeting")]
    [SerializeField] private float detectionRadius = 15f;
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float retargetDelay = 0.2f;

    [Header("References")]
    [SerializeField] private CompanionWeapon weapon;

    // Leveling (very simple starter)
    public int Level { get; private set; } = 1;
    public float XP { get; private set; } = 0f;
    public float XPToNext { get; private set; } = 10f;

    private Transform _slotAnchor; // assigned by CompanionManager
    private Vector3 _vel;
    private Transform _currentTarget;
    private float _retargetTimer;
    private Transform _owner; // the player

    public void Init(Transform owner, Transform slotAnchor)
    {
        _owner = owner;
        _slotAnchor = slotAnchor;
    }

    private void Reset()
    {
        if (!weapon) weapon = GetComponentInChildren<CompanionWeapon>();
    }

    private void Update()
    {
        HandleTargeting();
        HandleFiring();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (_slotAnchor == null) return;

        // Desired position = within tether of the slot anchor (we steer back towards the anchor if we drift)
        Vector3 pos = transform.position;
        Vector3 toAnchor = _slotAnchor.position - pos;

        // Enforce tether radius softly
        Vector3 desired = Vector3.zero;
        if (toAnchor.sqrMagnitude > tetherRadius * tetherRadius)
        {
            desired = toAnchor.normalized * maxSpeed;
        }
        else
        {
            // orbit/idle micro-motion around the anchor (optional jitter can be added)
            desired = toAnchor * slotFollowStrength; // proportional control
            desired = Vector3.ClampMagnitude(desired, maxSpeed);
        }

        _vel = Vector3.MoveTowards(_vel, desired, acceleration * Time.fixedDeltaTime);
        Vector3 next = pos + _vel * Time.fixedDeltaTime;

        // Simple ground lock for top-down if needed (comment out for space scenes)
        // next.y = _slotAnchor.position.y;

        transform.position = next;

        // Face target or movement direction
        Vector3 faceDir = _currentTarget ? (_currentTarget.position - transform.position) : _vel;
        faceDir.y = 0f;
        if (faceDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(faceDir.normalized, Vector3.up), 0.2f);
    }

    private void HandleTargeting()
    {
        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer > 0f) return;
        _retargetTimer = retargetDelay;

        // Keep target if still valid & in range
        if (_currentTarget && IsValidTarget(_currentTarget)) return;

        _currentTarget = FindBestTarget();
    }

    private bool IsValidTarget(Transform t)
    {
        if (!t) return false;
        if (((1 << t.gameObject.layer) & enemyMask) == 0 && t.CompareTag("Enemy") == false)
        {
            // If using layers only, ignore tag; if using tag only, ignore layers. We allow either.
        }

        float distSqr = (t.position - transform.position).sqrMagnitude;
        float maxDetect = Mathf.Max(detectionRadius, weapon ? weapon.Range : detectionRadius);
        return distSqr <= maxDetect * maxDetect && t.gameObject.activeInHierarchy;
    }

    private Transform FindBestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, enemyMask, QueryTriggerInteraction.Collide);

        // If enemyMask not set, fall back to tag search
        if (hits == null || hits.Length == 0)
        {
            var tagged = GameObject.FindGameObjectsWithTag("Enemy");
            if (tagged.Length == 0) return null;

            return tagged
                .Select(go => go.transform)
                .OrderBy(t => (t.position - transform.position).sqrMagnitude)
                .FirstOrDefault();
        }

        return hits
            .Select(h => h.transform)
            .OrderBy(t => (t.position - transform.position).sqrMagnitude)
            .FirstOrDefault();
    }

    private void HandleFiring()
    {
        if (!weapon) return;
        if (_currentTarget) weapon.TickFire(_currentTarget, source: this);
    }

    // ===== Level / XP hooks =====
    public void AddXP(float amount)
    {
        XP += amount;
        while (XP >= XPToNext)
        {
            XP -= XPToNext;
            Level++;
            XPToNext *= 1.4f; // ramp

            // Simple scaling: buff weapon
            if (weapon)
            {
                weapon.SetDamageMultiplier(1.10f);
                weapon.SetFireRateMultiplier(1.05f);
                weapon.SetRangeMultiplier(1.05f);
            }
            // You could also increase move speed, tether, etc.
            maxSpeed += 0.25f;
        }
    }

    // Call this from projectiles when killing blows happen (optional)
    public void NotifyKilledEnemy()
    {
        AddXP(5f);
    }
}
