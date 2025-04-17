using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class EnemySpaceShip : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    private int currentHealth;

    [Header("Target Indicator")]
    public GameObject targetingIndicator; 
    // We'll decide to turn this on/off if (trackerCount>0 || isPlayerClosest)
    // unless the ship isDisabled.

    private int trackerCount = 0; 
    private bool isPlayerClosest = false; 

    // NEW: So we never re‐enable targeting after disabled
    [HideInInspector] public bool isDisabled = false;

    [Header("Disable At Low Health")]
    public float disableShipAtPercent = 0.3f;

    [Header("Disabled Indicator")]
    [Tooltip("Shown when the AI enters its Disabled state.")]
    public GameObject disabledIndicator; 

    [Header("References")]
    public Rigidbody rb;
    
    [Header("Loot: Credits")]
    [Tooltip("Random credits dropped when this ship is disabled.")]
    [SerializeField] private int minCredits = 50;
    [SerializeField] private int maxCredits = 300;

    public int Credits { get; private set; }  // public read‑only

    private void Awake()
    {
        currentHealth = maxHealth;
        if (rb == null) 
        rb = GetComponent<Rigidbody>();
        
        Credits = Random.Range(minCredits, maxCredits + 1);

        // Initially hide indicators
        if (targetingIndicator != null)   targetingIndicator.SetActive(false);
        if (disabledIndicator != null)    disabledIndicator.SetActive(false);
    }

    // --------------------------------------
    //  BULLET TRACKING METHODS
    // --------------------------------------
    public void OnStartTracking()
    {
        trackerCount++;
        RefreshIndicator();
    }

    public void OnStopTracking()
    {
        trackerCount = Mathf.Max(0, trackerCount - 1);
        RefreshIndicator();
    }

    // --------------------------------------
    //  CLOSEST-TO-PLAYER HIGHLIGHT
    // --------------------------------------
    public void SetPlayerClosest(bool value)
    {
        isPlayerClosest = value;
        RefreshIndicator();
    }

    private void RefreshIndicator()
    {
        // If the ship is disabled, targetingIndicator remains off
        if (isDisabled)
        {
            if (targetingIndicator != null)
                targetingIndicator.SetActive(false);
            return;
        }

        // Otherwise, if EITHER bullets are tracking us or we are "closest" => show targeting
        bool shouldShow = (trackerCount > 0) || isPlayerClosest;
        if (targetingIndicator != null)
            targetingIndicator.SetActive(shouldShow);
    }

    // --------------------------------------
    //  DAMAGE & DEATH
    // --------------------------------------
    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        float hpPercent = (float)currentHealth / maxHealth;
        if (hpPercent < disableShipAtPercent)
        {
            // TELL AI => DISABLED
            NPCShipAI ai = GetComponent<NPCShipAI>();
            if (ai != null)
            {
                ai.EnterDisabledState();
            }
        }
    }
    
    /// <summary>Called by BoardingPanel to transfer all credits to the player.</summary>
    public int LootCredits()
    {
        int amt = Credits;
        Credits = 0;
        return amt;
    }

    private void Die()
    {
        // Instead of destroying ourselves, we ask the AI to do final state
        NPCShipAI ai = GetComponent<NPCShipAI>();
        if (ai != null)
        {
            ai.EnterDestroyingState();
        }
        else
        {
            // fallback
            Destroy(gameObject);
        }
    }
}
