using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Add this component to the “Recharge” button that sits in PLANET_HUD.
/// </summary>
[RequireComponent(typeof(Button))]
public class PlanetRechargeButton : MonoBehaviour
{
    [Header("Cost Settings")]
    [Tooltip("How many credits the player pays to refuel.")]
    [SerializeField] private int creditCost = 200;

    [Header("Feedback (optional)")]
    [Tooltip("UI Text, TMP, panel, etc. to show when funds are insufficient.")]
    [SerializeField] private GameObject notEnoughCreditsPopup;

    private Button      btn;
    private PlayerStats playerStats;

    private void Awake()
    {
        btn         = GetComponent<Button>();
        playerStats = FindObjectOfType<PlayerStats>(true);

        btn.onClick.AddListener(OnRechargeClicked);
    }

    private void OnRechargeClicked()
    {
        // 1) Try to pay
        if (!GameManager.Instance.SpendCredits(creditCost))
        {
            // Not enough money
            if (notEnoughCreditsPopup != null)
                notEnoughCreditsPopup.SetActive(true);

            return;
        }

        // 2) Refill energy
        if (playerStats != null)
        {
            playerStats.CurrentEnergy = playerStats.maxEnergy;  // property updates HUD
        }

        // 3) Hide / disable the button so it can’t be spammed
        gameObject.SetActive(false);
    }
}