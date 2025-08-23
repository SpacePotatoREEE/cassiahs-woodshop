using UnityEngine;
using UnityEngine.SceneManagement;     // for FindObjectOfType in other scenes
using System;                          // for Environment.StackTrace
using System.Collections;

/// <summary>
/// Persistent stats for the space-ship player (PlayerShip layer).
/// Singleton + DontDestroyOnLoad to ensure exactly one ship exists.
/// Keeps Health and Energy in sync with the HUD.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerStats : MonoBehaviour
{
    /* ───────────────  SINGLETON  ─────────────── */
    public static PlayerStats Instance { get; private set; }

    /* ───────────────  HEALTH  ─────────────── */
    [Header("Player Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    /* ───────────────  ENERGY  ─────────────── */
    [Header("Player Energy")]
    [Tooltip("Base energy capacity for this hull / ship.")]
    public float maxEnergy = 100f;

    // We track energy through a PROPERTY so we can log every write
    [SerializeField] private float _currentEnergy;
    public  float CurrentEnergy
    {
        get => _currentEnergy;
        set
        {
            if (Mathf.Approximately(_currentEnergy, value)) return;   // no change
            Debug.Log($"[Energy SET] {_currentEnergy} → {value}\n{Environment.StackTrace}");
            _currentEnergy = value;

            // update HUD immediately
            if (playerEnergyBar != null)
                playerEnergyBar.SetEnergy(_currentEnergy);
        }
    }

    /* ───────── DISABLE/DESTROY CONFIG ───────── */
    [Header("Disable Threshold")]
    [Range(0f, 1f)]
    [Tooltip("If currentHealth / maxHealth drops below this, the ship is ‘disabled’.")]
    public float disableShipAtPercent = 0.3f;

    [Header("Disable Behaviour")]
    public bool  disableMovement  = true;
    public bool  disableWeapon    = true;
    public float slowDownDuration = 2f;

    /* ───────────────  REFERENCES  ─────────────── */
    [Header("References")]
    public MonoBehaviour   movementScript;     // optional
    public MonoBehaviour   weaponScript;       // optional
    public PlayerHealthBar playerHealthBar;    // optional
    public PlayerEnergyBar playerEnergyBar;    // optional

    /* ───────────  INTERNAL STATE  ─────────── */
    private bool      isDisabled  = false;
    private bool      isDestroyed = false;
    private Rigidbody rb;

    /* ═════════════  LIFECYCLE  ═════════════ */
    private void Awake()
    {
        // Enforce a single ship instance for the whole app.
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[PlayerStats] Duplicate ship detected ({name}). Destroying this instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Debug.Log($"PlayerStats Awake on {gameObject.name}, instanceID {GetInstanceID()}");

        rb = GetComponent<Rigidbody>();
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Initialise starting values
        currentHealth = maxHealth;
        CurrentEnergy = maxEnergy;

        CacheBars();
        InitialiseBars();

        Debug.Log($"PlayerStats Start on {gameObject.name}  id:{GetInstanceID()}  energy:{CurrentEnergy}");
    }

    private void CacheBars()
    {
        if (playerHealthBar == null)
            playerHealthBar = FindObjectOfType<PlayerHealthBar>(true);

        if (playerEnergyBar == null)
            playerEnergyBar = FindObjectOfType<PlayerEnergyBar>(true);
    }

    private void InitialiseBars()
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.SetMaxHealth(maxHealth);
            playerHealthBar.SetHealth(currentHealth);
        }

        if (playerEnergyBar != null)
        {
            playerEnergyBar.SetMaxEnergy(maxEnergy);
            playerEnergyBar.SetEnergy(CurrentEnergy);
        }
    }

    /* ═════════════  ENERGY API  ═════════════ */
    public bool ConsumeEnergy(float amount)
    {
        Debug.Log($"[ConsumeEnergy] Before: {CurrentEnergy}  need:{amount}  on {GetInstanceID()}");
        if (CurrentEnergy < amount) return false;
        CurrentEnergy -= amount;          // property will update the bar
        Debug.Log($"[ConsumeEnergy]  After: {CurrentEnergy}");
        return true;
    }

    public void AddEnergy(float amount)
    {
        CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, maxEnergy);
    }

    public bool HasEnoughEnergy(float amount) => CurrentEnergy >= amount;

    /* ═════════════  HEALTH API  ═════════════ */
    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        Debug.Log($"[PlayerStats] Took {damage} dmg → {currentHealth}/{maxHealth}");
        SyncHealthBar();

        if (currentHealth == 0f) { Die(); return; }

        if (!isDisabled && currentHealth / maxHealth < disableShipAtPercent)
            EnterDisabledState();
    }

    public void Heal(float amount)
    {
        if (isDestroyed) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        SyncHealthBar();
    }

    public void FullHeal()
    {
        currentHealth = maxHealth;
        SyncHealthBar();
    }

    /* ═════════════  UI SYNC  ═════════════ */
    public void SyncHealthBar()
    {
        if (playerHealthBar != null)
            playerHealthBar.SetHealth(currentHealth);
    }

    public void SyncEnergyBar()        // kept for compatibility
    {
        if (playerEnergyBar != null)
            playerEnergyBar.SetEnergy(CurrentEnergy);
    }

    public bool IsDisabledOrDestroyed() => isDisabled || isDestroyed;

    /* ═══════════ INTERNAL LOGIC ═══════════ */
    private void Die()
    {
        isDestroyed = true;
        Destroy(gameObject);
    }

    private void EnterDisabledState()
    {
        isDisabled = true;

        if (disableMovement && movementScript != null) movementScript.enabled = false;
        if (disableWeapon   && weaponScript   != null) weaponScript.enabled   = false;

        StartCoroutine(SlowDownRoutine());
    }

    private IEnumerator SlowDownRoutine()
    {
        Vector3 startVel = rb ? rb.linearVelocity : Vector3.zero;
        float   timer    = 0f;

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
