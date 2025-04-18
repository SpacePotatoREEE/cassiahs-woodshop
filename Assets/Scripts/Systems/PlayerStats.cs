using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerStats : MonoBehaviour
{
    /* ─────────────────────────  CONFIG  ───────────────────────── */
    [Header("Player Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Disable Threshold")]
    [Range(0f, 1f)]
    [Tooltip("If currentHealth / maxHealth drops below this, the ship is 'disabled'.")]
    public float disableShipAtPercent = 0.3f;

    [Header("Disable Behaviour")]
    public bool disableMovement = true;
    public bool disableWeapon   = true;
    public float slowDownDuration = 2f;

    [Header("References")]
    [Tooltip("Optional movement script (e.g., ShipDriftController).")]
    public MonoBehaviour movementScript;
    [Tooltip("Optional weapon script.")]
    public MonoBehaviour weaponScript;
    [Tooltip("Optional UI health bar.")]
    public PlayerHealthBar playerHealthBar;

    /* ──────────────────────  INTERNAL STATE  ───────────────────── */
    private bool isDisabled  = false;
    private bool isDestroyed = false;
    private Rigidbody rb;

    /* ─────────────────────────  UNITY  ─────────────────────────── */
    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();

        // Init health bar
        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);
            playerHealthBar.SetHealth(currentHealth);
        }

        // Persist across scenes
        DontDestroyOnLoad(gameObject);
    }
    
    private void Start()
    {
        if (playerHealthBar == null)
            playerHealthBar = FindObjectOfType<PlayerHealthBar>(true);  // include inactive HUD

        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);   // ← sets slider.maxValue = 400
            playerHealthBar.SetHealth(currentHealth);  // ← shows full bar
        }
    }

    /* ────────────────────────  PUBLIC API  ─────────────────────── */

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        Debug.Log($"[PlayerStats] Took {damage} dmg → {currentHealth}/{maxHealth}");
        SyncHealthBar();

        if (currentHealth == 0f)
        {
            Die();
            return;
        }

        if (!isDisabled && currentHealth / maxHealth < disableShipAtPercent)
            EnterDisabledState();
    }

    public void Heal(float amount)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"[PlayerStats] Healed {amount} → {currentHealth}/{maxHealth}");
        SyncHealthBar();

        // Optionally exit disabled state here if you want
    }

    /// <summary>Synchronise UI with the currentHealth value.</summary>
    public void SyncHealthBar()
    {
        if (playerHealthBar != null)
            playerHealthBar.SetHealth(currentHealth);
    }

    public bool IsDisabledOrDestroyed() => isDisabled || isDestroyed;

    /* ──────────────────────  INTERNAL LOGIC  ───────────────────── */

    private void Die()
    {
        Debug.Log("[PlayerStats] Player died");
        isDestroyed = true;
        Destroy(gameObject);
    }

    private void EnterDisabledState()
    {
        isDisabled = true;
        Debug.Log("[PlayerStats] Ship disabled");

        if (disableMovement && movementScript != null) movementScript.enabled = false;
        if (disableWeapon   && weaponScript   != null) weaponScript.enabled   = false;

        StartCoroutine(SlowDownRoutine());
    }

    private IEnumerator SlowDownRoutine()
    {
        Vector3 startVel = rb ? rb.linearVelocity : Vector3.zero;
        float timer = 0f;

        while (timer < slowDownDuration)
        {
            timer += Time.deltaTime;
            float t = timer / slowDownDuration;
            if (rb) rb.linearVelocity = Vector3.Lerp(startVel, Vector3.zero, t);
            yield return null;
        }

        if (rb) rb.linearVelocity = Vector3.zero;
    }
}
