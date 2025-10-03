using UnityEngine;

/// <summary>
/// Capture wrapper for enemies that use EnemyHealth_UnitStats for HP/armor.
/// CreatureDefinition here is only for capture params + which companion prefab to spawn later.
/// </summary>
[DisallowMultipleComponent]
public class CapturableEnemy : MonoBehaviour, ICapturable
{
    [Header("Definition (companion + capture rate)")]
    public CreatureDefinition definition;

    [Header("Capture Gate")]
    [Tooltip("When health fraction is <= this, the enemy becomes capturable.")]
    [Range(0f, 1f)] public float captureThreshold = 0.35f;

    [Header("Capture Chance")]
    [Range(0f, 1f)] public float minChanceAtThreshold = 0.20f;
    [Range(0f, 1f)] public float maxChanceAtZeroHP   = 0.95f;
    [Min(0.01f)] public float curveExponent = 1.35f; // shapes ramp as HP -> 0

    [Header("Visuals")]
    public Transform captureVisualRoot; // scale point while "sucking into ball"

    private EnemyHealth_UnitStats _hp; // your stats-driven health
    private Collider[] _cols;
    private bool _inCapture;

    void Awake()
    {
        _hp = GetComponent<EnemyHealth_UnitStats>();
        if (!_hp)
        {
            Debug.LogError("[CapturableEnemy] Missing EnemyHealth_UnitStats.");
            enabled = false;
            return;
        }

        _cols = GetComponentsInChildren<Collider>(true);
        if (!captureVisualRoot) captureVisualRoot = transform;

        // Ensure currentHP aligns with Max after any start-time modifiers
        _hp.InitializeFromStats(keepCurrentRatio: false);
    }

    // --- ICapturable ---

    public bool CanCapture => !_inCapture && HealthFraction <= captureThreshold;

    public float GetCaptureChance01()
    {
        // 0 at threshold edge, -> 1 as HP -> 0
        float hf = HealthFraction;
        float belowFactor = Mathf.Clamp01((captureThreshold - hf) / Mathf.Max(1e-5f, captureThreshold));
        float shaped = Mathf.Pow(belowFactor, curveExponent);

        float baseRate = definition ? definition.baseCaptureRate : minChanceAtThreshold;
        float lerpMin = Mathf.Max(baseRate, minChanceAtThreshold);
        return Mathf.Clamp01(Mathf.Lerp(lerpMin, maxChanceAtZeroHP, shaped));
    }

    public void OnCaptureStart()
    {
        _inCapture = true;
        foreach (var c in _cols) c.enabled = false; // freeze during cut-in
    }

    public void OnCaptureComplete(bool success)
    {
        _inCapture = false;
        if (!success)
        {
            foreach (var c in _cols) c.enabled = true;
        }
        // On success, CaptureBall handles shrink->return->roster add->destroy.
    }

    public CreatureDefinition GetCreatureDefinition() => definition;
    public Transform GetCaptureRoot() => captureVisualRoot ? captureVisualRoot : transform;

    // --- health helpers ---
    private float CurrentHP => _hp.GetCurrentHP();
    private float MaxHP     => Mathf.Max(1f, _hp.MaxHP);
    private float HealthFraction => Mathf.Clamp01(CurrentHP / MaxHP);
}
