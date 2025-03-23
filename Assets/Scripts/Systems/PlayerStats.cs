using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody))]
public class PlayerStats : MonoBehaviour
{
    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Disable Threshold")]
    [Tooltip("If health < disableShipAtPercent * maxHealth, we consider this ship 'disabled'.")]
    [Range(0f, 1f)]
    public float disableShipAtPercent = 0.3f;

    [Header("Disable Behavior")]
    [Tooltip("If true, we disable the movement script when disabled.")]
    public bool disableMovement = true;

    [Tooltip("If true, we disable the weapon script when disabled.")]
    public bool disableWeapon = true;

    [Tooltip("Slowdown duration after entering disabled.")]
    public float slowDownDuration = 2f;

    [Header("References")]
    [Tooltip("Optional movement script (e.g., ShipDriftController) to disable.")]
    public MonoBehaviour movementScript;

    [Tooltip("Optional weapon script (e.g., PlayerWeaponController) to disable.")]
    public MonoBehaviour weaponScript;

    // Internal state
    private bool isDisabled = false;
    private bool isDestroyed = false;

    private Rigidbody rb;
    
    public PlayerHealthBar playerHealthBar;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerHealthBar.SetMaxHealth(maxHealth);
        rb = GetComponent<Rigidbody>();

        // Make this player persist across scene loads
        DontDestroyOnLoad(gameObject);
    }

    public void TakeDamage(float dmg)
    {
        if (isDestroyed) return; // Already destroyed

        currentHealth -= dmg;
        Debug.Log($"Player took {dmg} damage. Current health: {currentHealth} / {maxHealth}");
        playerHealthBar.SetHealth(currentHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
            return;
        }

        float hpPercent = currentHealth / maxHealth;
        // If not yet disabled, but HP < threshold => disable
        if (!isDisabled && hpPercent < disableShipAtPercent)
        {
            EnterDisabledState();
        }
    }
    
    // Example: a method to heal
    public void Heal(float amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        Debug.Log($"Player healed {amount}. Current health: {currentHealth} / {maxHealth}");
    }

    private void Die()
    {
        Debug.Log("[PlayerSpaceShipStats] Player died!");
        isDestroyed = true;
        // Destroys the persistent player
        Destroy(gameObject);
    }

    private void EnterDisabledState()
    {
        isDisabled = true;
        Debug.Log("[PlayerSpaceShipStats] Player is now disabled!");

        // 1) Stop movement & weapon scripts
        if (disableMovement && movementScript != null)
        {
            movementScript.enabled = false;  // This prevents any further movement
        }
        if (disableWeapon && weaponScript != null)
        {
            weaponScript.enabled = false;    // This prevents firing
        }

        // 2) Optionally do a slowdown if we're in motion
        StartCoroutine(SlowDownRoutine());
    }

    private IEnumerator SlowDownRoutine()
    {
        Vector3 startVel = rb ? rb.linearVelocity : Vector3.zero;
        float timer = 0f;

        while (timer < slowDownDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / slowDownDuration);
            if (rb) rb.linearVelocity = Vector3.Lerp(startVel, Vector3.zero, t);
            yield return null;
        }

        if (rb) rb.linearVelocity = Vector3.zero;
        // At this point, the player is fully immobile and cannot shoot or move.
    }

    public bool IsDisabledOrDestroyed()
    {
        return isDisabled || isDestroyed;
    }
}
