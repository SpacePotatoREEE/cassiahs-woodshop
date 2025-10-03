using UnityEngine;

/// <summary>
/// Health adapter that reads max HP / armor (or any stats) from UnitStats and
/// exposes IDamageable for your companions/projectiles.
/// </summary>
[DisallowMultipleComponent]
public class EnemyHealth_UnitStats : MonoBehaviour, IDamageable
{
    [Header("Stats Source")]
    [SerializeField] private UnitStats unitStats;

    [Tooltip("Which UnitStats entry represents Max Health.")]
    [SerializeField] private StatType healthStat;

    [Tooltip("Optional: Which UnitStats entry represents Armor/Defense to mitigate damage.")]
    [SerializeField] private StatType armorStat;

    [Header("Aim / Death")]
    [SerializeField] private Transform aimPoint;           // e.g. head/center; falls back to transform
    [SerializeField] private bool destroyOnDeath = true;   // set false if you pool enemies

    [Header("Debug/Runtime")]
    [SerializeField, Min(1f)] private float fallbackMaxHP = 50f; // used if UnitStats missing
    [SerializeField] private float currentHP;

    public System.Action<EnemyHealth_UnitStats> OnDeath;
    public float MaxHP { get; private set; }

    private bool _initialized;

    private void Awake()
    {
        if (!aimPoint) aimPoint = transform;
        InitializeFromStats();
    }

    private void Start()
    {
        // If modifiers are applied at Start in your game, refresh here too.
        InitializeFromStats();
    }

    /// <summary>
    /// Call this if you change UnitStats modifiers at runtime and want MaxHP to update.
    /// Current HP will scale proportionally unless you disable that behavior.
    /// </summary>
    public void InitializeFromStats(bool keepCurrentRatio = true)
    {
        float oldMax = MaxHP;

        float maxFromStats =
            unitStats ? Mathf.Max(1f, unitStats.GetStat(healthStat)) : fallbackMaxHP;

        MaxHP = maxFromStats;

        if (!_initialized)
        {
            currentHP = MaxHP;
            _initialized = true;
        }
        else if (keepCurrentRatio && oldMax > 0.01f)
        {
            float ratio = Mathf.Clamp01(currentHP / oldMax);
            currentHP = Mathf.Max(1f, MaxHP * ratio);
        }
        else
        {
            currentHP = Mathf.Min(currentHP, MaxHP);
        }
    }

    public bool ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Object source = null)
    {
        if (currentHP <= 0f) return false;

        float mitigated = amount;

        // Optional simple armor formula if you have an Armor stat:
        // Feel free to swap this for your own damage pipeline.
        if (unitStats != null && armorStat != null)
        {
            float armor = unitStats.GetStat(armorStat);
            // Example: diminishing returns curve (tweak to taste)
            float reduction = armor <= 0f ? 0f : (armor / (armor + 100f));
            mitigated = amount * (1f - Mathf.Clamp01(reduction));
        }

        currentHP -= Mathf.Max(0f, mitigated);

        if (currentHP <= 0f)
        {
            OnDeath?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject);
            return true;
        }

        return false;
    }

    public Transform GetAimPoint() => aimPoint ? aimPoint : transform;

    // --- Convenience API if you need to heal/force-kill from other systems ---
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        currentHP = Mathf.Min(MaxHP, currentHP + amount);
    }

    public void Kill(Object source = null)
    {
        if (currentHP <= 0f) return;
        currentHP = 0f;
        OnDeath?.Invoke(this);
        if (destroyOnDeath) Destroy(gameObject);
    }

    // Expose for UI/debug
    public float GetCurrentHP() => currentHP;
}
