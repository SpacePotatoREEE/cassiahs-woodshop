using UnityEngine;

public class PlayerSpaceShipStats : MonoBehaviour
{
    [Header("Player Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Disable Threshold")]
    [Tooltip("If health < disableShipAtPercent * maxHealth, we consider this ship 'disabled'.")]
    [Range(0f, 1f)]
    public float disableShipAtPercent = 0.3f;

    // Example: If you want to slow down once disabled
    public float slowDownDuration = 2f;

    // Track whether we’re disabled or destroyed
    private bool isDisabled = false;
    private bool isDestroyed = false;

    private Rigidbody rb;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(float dmg)
    {
        if (isDestroyed) return; // Already destroyed

        currentHealth -= dmg;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
            return;
        }

        float hpPercent = currentHealth / maxHealth;
        if (!isDisabled && hpPercent < disableShipAtPercent)
        {
            EnterDisabledState();
        }
    }

    private void Die()
    {
        // If you want to do a final blow-up or fade out:
        Debug.Log("[PlayerSpaceShipStats] Player died!");
        isDestroyed = true;
        // Possibly remove control, spawn effect, etc.
        // For now, just do:
        Destroy(gameObject);
    }

    private void EnterDisabledState()
    {
        isDisabled = true;
        Debug.Log("[PlayerSpaceShipStats] Player is disabled.");
        // Example slow-down logic:
        StartCoroutine(SlowDownRoutine());
        // Also stop input from your movement script, if needed
    }

    private System.Collections.IEnumerator SlowDownRoutine()
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
        // Fully stopped
        if (rb) rb.linearVelocity = Vector3.zero;
    }

    public bool IsDisabledOrDestroyed()
    {
        return isDisabled || isDestroyed;
    }
}
