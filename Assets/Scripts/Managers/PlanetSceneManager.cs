using UnityEngine;
using UnityEngine.SceneManagement;

public class PlanetSceneManager : MonoBehaviour
{
    [Header("Associated Space Scene")]
    [Tooltip("Which space scene should we load when we leave this planet?")]
    [SerializeField] private string spaceSceneName = "Space_A";

    [Header("UI References")]
    [Tooltip("Panel or Canvas that contains the 'Leave' button.")]
    [SerializeField] private GameObject leavePanel;

    [Header("Landing Heal")]
    [Tooltip("Percentage of max‑health to heal the player on landing (0–1). 1 = full heal.")]
    [Range(0f, 1f)]
    [SerializeField] private float percentHeal = 1f;

    private void Start()
    {
        // Heal the player as soon as the planet scene starts
        HealPlayerOnLanding();

        // Optionally show the leave‑planet UI panel
        if (leavePanel != null)
            leavePanel.SetActive(true);
    }

    /// <summary>
    /// Heals the persistent player by (percentHeal * maxHealth) and refreshes the health bar.
    /// </summary>
    private void HealPlayerOnLanding()
    {
        // The persistent player should still exist (DontDestroyOnLoad).
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;

        float healAmount = percentHeal * stats.maxHealth;
        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);
        stats.SyncHealthBar();

        Debug.Log($"[PlanetSceneManager] Healed player by {healAmount} → {stats.currentHealth}/{stats.maxHealth}");
    }

    /// <summary>
    /// Called by the 'Leave' button in the UI. Loads the associated space scene.
    /// </summary>
    public void OnLeavePlanetClicked()
    {
        // Optionally save before leaving:
        // GameManager.Instance?.SaveGame();

        SceneManager.LoadScene(spaceSceneName);
    }
}