using UnityEngine;

/// <summary>
/// Simple follower/attacker brain for companions.
/// If a UnitStats component is present on the companion prefab, we read stats from it:
///   - Damage, AttackSpeed (=> cooldown = 1/AttackSpeed), AttackRange, MoveSpeed
/// Otherwise we fall back to values in CreatureDefinition (fallback fields).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CompanionController : MonoBehaviour
{
    public CreatureDefinition def;           // for prefab identity + fallbacks
    public Transform owner;                  // player (planet human)
    public CompanionRoster roster;

    [Header("Movement")]
    public float followLerp = 6f;            // smoothing into the ring slot

    [Header("Combat")]
    public LayerMask enemyMask;
    public float leashDistance = 12f;

    [Header("Optional UnitStats hookup on the companion prefab")]
    [SerializeField] private UnitStats unitStats;        // auto-finds if null
    [SerializeField] private StatType damageStat;        // e.g., Damage
    [SerializeField] private StatType attackSpeedStat;   // e.g., Attack Speed
    [SerializeField] private StatType attackRangeStat;   // (optional) if you have this stat
    [SerializeField] private StatType moveSpeedStat;     // e.g., Move Speed

    private CharacterController _cc;
    private int _slotIndex;
    private Vector3 _desiredPos;
    private float _cooldownTimer;

    public void Initialize(CreatureDefinition d, Transform owner, CompanionRoster r, int slotIndex)
    {
        def = d;
        this.owner = owner;
        roster = r;
        _slotIndex = slotIndex;

        _cc = GetComponent<CharacterController>();
        if (!_cc) _cc = gameObject.AddComponent<CharacterController>();
        _cc.height = 1.4f;
        _cc.radius = 0.35f;

        if (!unitStats) unitStats = GetComponentInChildren<UnitStats>(true);
        _cooldownTimer = 0f;
        SetDesiredSlot(slotIndex);
    }

    public void SetDesiredSlot(int slot)
    {
        _slotIndex = slot;
        _desiredPos = roster.OwnerSlotWorld(slot);
    }

    void Update()
    {
        if (!owner) return;

        // --- movement into ring slot ---
        _desiredPos = roster.OwnerSlotWorld(_slotIndex);
        Vector3 to = _desiredPos - transform.position;

        // Smooth toward slot, but clamp by a max speed (from stats if available)
        float maxSpeed = GetMoveSpeed();
        Vector3 vel = to * followLerp;
        if (maxSpeed > 0f) vel = Vector3.ClampMagnitude(vel, maxSpeed);

        _cc.Move((vel + Physics.gravity) * Time.deltaTime);

        if (to.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to, Vector3.up), 10f * Time.deltaTime);

        // --- simple melee attack ---
        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            var enemyGO = FindNearestEnemy();
            if (enemyGO != null)
            {
                float dist = Vector3.Distance(transform.position, enemyGO.transform.position);
                if (dist <= GetAttackRange() + 0.4f)
                {
                    var idmg = enemyGO.GetComponentInParent<IDamageable>();
                    if (idmg != null)
                    {
                        // Aim point if present on the enemy (UnitStats health version)
                        var statsHP = enemyGO.GetComponentInParent<EnemyHealth_UnitStats>();
                        Vector3 hitPoint = statsHP ? statsHP.GetAimPoint().position : enemyGO.transform.position;

                        idmg.ApplyDamage(GetDamage(), hitPoint, Vector3.up, this);
                        _cooldownTimer = GetAttackCooldown();
                    }
                }
            }
        }
    }

    GameObject FindNearestEnemy()
    {
        var hits = Physics.OverlapSphere(transform.position, leashDistance, enemyMask, QueryTriggerInteraction.Ignore);
        GameObject best = null;
        float bestD = float.MaxValue;
        foreach (var h in hits)
        {
            if (h.GetComponentInParent<ICapturable>() == null) continue; // target capturable enemies
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = h.transform.gameObject; }
        }
        return best;
    }

    // --- Stat helpers: prefer UnitStats if present; else CreatureDefinition fallbacks ---

    float GetDamage()
    {
        if (unitStats && damageStat != null) return unitStats.GetStat(damageStat);
        return def ? def.fallbackDamage : 6f;
    }

    float GetAttackCooldown()
    {
        if (unitStats && attackSpeedStat != null)
        {
            float atkSpd = Mathf.Max(0.0001f, unitStats.GetStat(attackSpeedStat)); // attacks per second
            return 1f / atkSpd;
        }
        return def ? def.fallbackAttackCooldown : 0.8f;
    }

    float GetAttackRange()
    {
        if (unitStats && attackRangeStat != null)
            return unitStats.GetStat(attackRangeStat);
        return def ? def.fallbackAttackRange : 1.8f;
    }

    float GetMoveSpeed()
    {
        if (unitStats && moveSpeedStat != null)
            return unitStats.GetStat(moveSpeedStat);
        return def ? def.fallbackMoveSpeed : 6f;
    }
}
