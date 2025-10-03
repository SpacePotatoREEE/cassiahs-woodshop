using UnityEngine;

/// <summary>
/// Persistent stats for the on-foot player (PlayerHuman layer).
/// Singleton + DontDestroyOnLoad to ensure exactly one human exists.
/// Implements IDamageable for projectiles/AI.
/// </summary>
[DisallowMultipleComponent]
public class PlayerStatsHuman : MonoBehaviour, IDamageable
{
    /* ───────────────  SINGLETON  ─────────────── */
    public static PlayerStatsHuman Instance { get; private set; }

    [Header("Player Health Settings")]
    [Min(1f)] public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Health Bar (Optional)")]
    public PlayerHealthBar playerHealthBar;

    [Header("Aim (Optional)")]
    [Tooltip("Where enemies/auto-aim should target on the player; falls back to this.transform if null.")]
    public Transform aimPoint;

    [Header("I-Frames (Optional)")]
    [Tooltip("If assigned, damage will be ignored while PlayerHitEffects.IsInvulnerable() is true.")]
    public PlayerHitEffects hitEffects;

    private bool isDestroyed = false;

    private void Awake()
    {
        Debug.Log($"[PlayerStatsHuman] Awake on {gameObject.name}, implements IDamageable: {this is IDamageable}");

        // Enforce a single human instance for the whole app.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerStatsHuman] Duplicate human detected ({name}). Destroying this instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize health
        currentHealth = Mathf.Clamp(currentHealth <= 0f ? maxHealth : currentHealth, 0f, maxHealth);

        // Optional: wire up UI
        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);
            playerHealthBar.SetHealth(currentHealth);
        }

        // Fetch components if not assigned
        if (!hitEffects) hitEffects = GetComponent<PlayerHitEffects>();
        if (!aimPoint)   aimPoint   = transform;

        // Persist
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Apply raw damage to the player and update UI. If health hits 0, Die().</summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        if (damage <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        Debug.Log($"[PlayerStatsHuman] Took {damage}. Current health: {currentHealth}/{maxHealth}");

        if (playerHealthBar != null)
            playerHealthBar.SetHealth(currentHealth);

        if (currentHealth <= 0f)
            Die();
    }

    /// <summary>Heal the player by the given amount, clamped to maxHealth.</summary>
    public void Heal(float amount)
    {
        if (isDestroyed) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        Debug.Log($"[PlayerStatsHuman] Healed {amount}. Current health: {currentHealth}/{maxHealth}");

        if (playerHealthBar != null)
            playerHealthBar.SetHealth(currentHealth);
    }

    /// <summary>Set max health at runtime (e.g., from upgrades) and keep current ratio.</summary>
    public void SetMaxHealth(float newMax, bool keepRatio = true)
    {
        newMax = Mathf.Max(1f, newMax);
        if (keepRatio && maxHealth > 0.01f)
        {
            float ratio = Mathf.Clamp01(currentHealth / maxHealth);
            maxHealth = newMax;
            currentHealth = Mathf.Clamp(newMax * ratio, 0f, newMax);
        }
        else
        {
            maxHealth = newMax;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);
            playerHealthBar.SetHealth(currentHealth);
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerStatsHuman] Player died!");
        isDestroyed = true;
        Destroy(gameObject);
        // TODO: trigger your respawn/game over flow here instead of destroy, if desired.
    }

    /* ───────────────  IDamageable IMPLEMENTATION  ─────────────── */

    /// <summary>
    /// ApplyDamage required by IDamageable. Returns true if this hit killed the player.
    /// Matches signature: bool ApplyDamage(float, Vector3, Vector3, Object).
    /// </summary>
    public bool ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Object source = null)
    {
        // Respect i-frames if you have them
        if (IsInvulnerable()) return false;

        float pre = currentHealth;
        TakeDamage(amount);
        // If health reached zero due to this hit, report kill = true
        return pre > 0f && currentHealth <= 0f;
    }

    /// <summary>
    /// Where enemies/projectiles should aim.
    /// </summary>
    public Transform GetAimPoint()
    {
        return aimPoint ? aimPoint : transform;
    }

    /* ───────────────  I-Frame helper  ─────────────── */

    /// <summary>
    /// Returns true if the player is currently invulnerable (uses PlayerHitEffects if present).
    /// </summary>
    public bool IsInvulnerable()
    {
        if (!hitEffects) return false;

        // If your PlayerHitEffects exposes a public IsInvulnerable() method:
        var m = typeof(PlayerHitEffects).GetMethod("IsInvulnerable", System.Type.EmptyTypes);
        if (m != null)
        {
            object r = m.Invoke(hitEffects, null);
            if (r is bool b) return b;
        }

        // Or a public bool/prop called IsInvulnerable:
        var f = typeof(PlayerHitEffects).GetField("IsInvulnerable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (f != null)
        {
            object r = f.GetValue(hitEffects);
            if (r is bool b) return b;
        }
        var p = typeof(PlayerHitEffects).GetProperty("IsInvulnerable", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (p != null && p.CanRead)
        {
            object r = p.GetValue(hitEffects);
            if (r is bool b) return b;
        }

        return false;
    }
}
