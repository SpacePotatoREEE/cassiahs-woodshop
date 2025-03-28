using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStatsHuman : MonoBehaviour
{
    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI Health Bar (Optional)")]
    public PlayerHealthBar playerHealthBar;

    private bool isDestroyed = false;

    private void Awake()
    {
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

    /// <summary>
    /// Apply damage to the player. If health hits 0, call Die().
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return; // Already destroyed

        currentHealth -= damage;
        if (currentHealth < 0f) currentHealth = 0f;

        Debug.Log($"[PlayerHumanStats] Took {damage} damage. Current health: {currentHealth}/{maxHealth}");

        if (playerHealthBar != null)
        {
            playerHealthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the player by the given amount, clamped to maxHealth.
    /// </summary>
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

    /// <summary>
    /// Called when health <= 0.
    /// </summary>
    private void Die()
    {
        Debug.Log("[PlayerHumanStats] Player died!");
        isDestroyed = true;
        Destroy(gameObject);
    }
}
