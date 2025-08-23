using UnityEngine;
using UnityEngine.UI;

public class PlanetSceneManager : MonoBehaviour
{
    [Header("Space Scene To Load When Leaving")]
    [SerializeField] private string spaceSceneName = "Space_A";

    [Header("Leave-Button Panel (optional)")]
    [SerializeField] private GameObject leavePanel = null;

    [Header("Landing Heal")]
    [Range(0f, 1f)] [SerializeField] private float percentHeal = 1f;

    private void Awake()
    {
        if (string.IsNullOrEmpty(spaceSceneName))
            Debug.LogError("[PlanetSceneManager] spaceSceneName is empty.", this);
        else if (!Application.CanStreamedLevelBeLoaded(spaceSceneName))
            Debug.LogError($"[PlanetSceneManager] Scene '{spaceSceneName}' is NOT in Build Settings.", this);
    }

    private void Start()
    {
        HealPlayerOnLanding();
        if (leavePanel) leavePanel.SetActive(true);
        AutoWireLeaveButton();
    }

    public void OnLeavePlanetClicked()
    {
        if (string.IsNullOrEmpty(spaceSceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(spaceSceneName)) return;

        GameManager.Instance?.SwitchToWorldScene(spaceSceneName);
    }

    private void HealPlayerOnLanding()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (!p) return;

        var stats = p.GetComponent<PlayerStats>();
        if (!stats) return;

        float healAmount = percentHeal * stats.maxHealth;
        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);
        stats.SyncHealthBar();
    }

    private void AutoWireLeaveButton()
    {
        if (!leavePanel) return;
        var btn = leavePanel.GetComponentInChildren<Button>(true);
        if (!btn) { Debug.LogWarning("[PlanetSceneManager] No Button under leavePanel.", this); return; }
        btn.onClick.RemoveListener(OnLeavePlanetClicked);
        btn.onClick.AddListener(OnLeavePlanetClicked);
    }
}