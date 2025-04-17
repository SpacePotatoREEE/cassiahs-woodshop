using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardingPanelUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button takeEnergyButton;
    [SerializeField] private Button takeCreditsButton;
    [SerializeField] private Button takeCargoButton;
    [SerializeField] private Button takeAmmoButton;
    [SerializeField] private Button captureShipButton;

    [Header("NEW Leave Button")]
    [SerializeField] private Button leaveButton;

    [Header("Credits Display")]
    [SerializeField] private TextMeshProUGUI creditsLabel;

    private EnemySpaceShip targetShip;
    private System.Action onClose;   // callback supplied by BoardingTrigger

    /* -------- initialisation, called once from BoardingTrigger -------- */
    public void Init(EnemySpaceShip ship, System.Action closeCallback)
    {
        targetShip = ship;
        onClose    = closeCallback;
        UpdateCreditsLabel();

        takeCreditsButton.onClick.AddListener(OnTakeCredits);

        // placeholders
        takeEnergyButton .onClick.AddListener(() => Debug.Log("Energy not implemented"));
        takeCargoButton  .onClick.AddListener(() => Debug.Log("Cargo  not implemented"));
        takeAmmoButton   .onClick.AddListener(() => Debug.Log("Ammo   not implemented"));
        captureShipButton.onClick.AddListener(() => Debug.Log("Capture not implemented"));

        /* new leave button */
        leaveButton.onClick.AddListener(ClosePanel);
    }

    private void OnTakeCredits()
    {
        if (targetShip == null) return;

        int amount = targetShip.LootCredits();
        if (amount > 0)
        {
            GameManager.Instance.AddCredits(amount);
            UpdateCreditsLabel();
            takeCreditsButton.interactable = false;
        }
    }

    private void UpdateCreditsLabel()
    {
        if (creditsLabel && targetShip != null)
            creditsLabel.text = $"Credits Onboard: {targetShip.Credits:n0}";
    }

    private void ClosePanel()
    {
        onClose?.Invoke();           // tell BoardingTrigger to hide & resume game
    }
}
