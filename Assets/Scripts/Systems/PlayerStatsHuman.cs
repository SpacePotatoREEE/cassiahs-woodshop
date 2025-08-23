using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent stats for the on‑foot player (PlayerHuman layer).
/// Singleton + DontDestroyOnLoad to ensure exactly one human exists.
/// </summary>
public class PlayerStatsHuman : MonoBehaviour
{
    /* ───────────────  SINGLETON  ─────────────── */
    public static PlayerStatsHuman Instance { get; private set; }

    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Health Bar (Optional)")]
    public PlayerHealthBar playerHealthBar;

    private bool isDestroyed = false;

    private void Awake()
    {
        // Enforce a single human instance for the whole app.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerHumanStats] Duplicate human detected ({name}). Destroying this instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize health
        currentHealth = maxHealth;

        // Optional: if you have a PlayerHealthBar, set its max
        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);
        }

        // Make this player persist across scene loads
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Apply damage to the player. If health hits 0, call Die().</summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return; // Already destroyed

        currentHealth -= damage;
        if (currentHealth < 0f) currentHealth = 0f;

        Debug.Log($"[PlayerHumanStats] Took {damage}. Current health: {currentHealth}/{maxHealth}");

        if (playerHealthBar != null)
        {
            playerHealthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0f) Die();
    }

    /// <summary>Heal the player by the given amount, clamped to maxHealth.</summary>
    public void Heal(float amount)
    {
        if (isDestroyed) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        Debug.Log($"[PlayerHumanStats] Healed {amount}. Current health: {currentHealth}/{maxHealth}");

        if (playerHealthBar != null)
        {
            playerHealthBar.SetHealth(currentHealth);
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHumanStats] Player died!");
        isDestroyed = true;
        Destroy(gameObject);
    }
}
