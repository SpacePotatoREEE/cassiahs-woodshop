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

    [Header("Leave Button")]
    [SerializeField] private Button leaveButton;

    [Header("Credits Display")]
    [SerializeField] private TextMeshProUGUI creditsLabel;

    private EnemySpaceShip targetShip;
    private System.Action  onClose;

    /* ---------- Init (called from BoardingTrigger) ---------- */
    public void Init(EnemySpaceShip ship, System.Action closeCallback)
    {
        targetShip = ship;
        onClose    = closeCallback;

        // ----- take‑credits button -----
        if (takeCreditsButton != null)
        {
            takeCreditsButton.onClick.RemoveAllListeners();
            takeCreditsButton.onClick.AddListener(OnTakeCredits);
            takeCreditsButton.interactable = (targetShip && targetShip.Credits > 0);
        }
        else
            Debug.LogWarning("[BoardingPanel] takeCreditsButton is not assigned", this);

        // ----- leave button -----
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveAllListeners();
            leaveButton.onClick.AddListener(ClosePanel);
        }
        else
            Debug.LogWarning("[BoardingPanel] leaveButton is not assigned", this);

        // ----- placeholder buttons (safe‑check) -----
        SafeAddClick(takeEnergyButton , () => Debug.Log("Energy not implemented"));
        SafeAddClick(takeCargoButton  , () => Debug.Log("Cargo  not implemented"));
        SafeAddClick(takeAmmoButton   , () => Debug.Log("Ammo   not implemented"));
        SafeAddClick(captureShipButton, () => Debug.Log("Capture not implemented"));

        UpdateCreditsLabel();
    }

    /* ---------- Take Credits ---------- */
    private void OnTakeCredits()
    {
        if (targetShip == null) { Debug.LogWarning("No target ship"); return; }
        if (takeCreditsButton == null) return;

        int amount = targetShip.LootCredits();
        if (amount > 0 && GameManager.Instance != null)
            GameManager.Instance.AddCredits(amount);

        UpdateCreditsLabel();
    }

    /* ---------- Helpers ---------- */
    private void UpdateCreditsLabel()
    {
        if (creditsLabel)
            creditsLabel.text = targetShip
                ? $"₡ {targetShip.Credits:n0}"
                : "₡ 0";

        if (takeCreditsButton)
            takeCreditsButton.interactable =
                (targetShip && targetShip.Credits > 0);
    }

    private void ClosePanel() => onClose?.Invoke();

    private static void SafeAddClick(Button btn, System.Action cb)
    {
        if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => cb()); }
    }
}
