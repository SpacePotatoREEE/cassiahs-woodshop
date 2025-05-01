using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;          // <─ new

/// <summary>
/// Handles planet-scene logic: auto-heals player on arrival, shows the
/// “Leave Planet” UI and loads the associated space scene when the button
/// is clicked.  
/// The script now:
/// • Auto-wires the first Button it finds under <c>leavePanel</c>  
/// • Logs helpful errors if the scene name is missing or not in Build Settings  
/// • Optionally saves the game before the scene switch
/// </summary>
public class PlanetSceneManager : MonoBehaviour
{
    /* ───────────────  INSPECTOR  ─────────────── */

    [Header("Space Scene To Load When Leaving")]
    [Tooltip("Name of the space scene that should be loaded when the player clicks ‘Leave’. " +
             "This scene must be added to File ▶ Build Settings.")]
    [SerializeField] private string spaceSceneName = "Space_A";

    [Header("Leave-Button Panel (optional)")]
    [Tooltip("Root GameObject that contains the Leave button. " +
             "If left unassigned the panel will not be toggled.")]
    [SerializeField] private GameObject leavePanel = null;

    [Header("Landing Heal")]
    [Tooltip("Percentage of max-health restored the instant the player lands (0–1).")]
    [Range(0f, 1f)] [SerializeField] private float percentHeal = 1f;

    /* ───────────────  LIFECYCLE  ─────────────── */

    private void Awake()
    {
        // Basic validation so mistakes are caught immediately.
        if (string.IsNullOrEmpty(spaceSceneName))
            Debug.LogError("[PlanetSceneManager] ‘spaceSceneName’ is empty – " +
                           "set it in the Inspector.", this);
        else if (!Application.CanStreamedLevelBeLoaded(spaceSceneName))
            Debug.LogError($"[PlanetSceneManager] The scene “{spaceSceneName}” is NOT in " +
                           "Build Settings – add it or fix the name.", this);
    }

    private void Start()
    {
        HealPlayerOnLanding();
        ShowLeavePanel();          // make sure the UI is visible
        AutoWireLeaveButton();     // makes the button work even if you forget to hook it up
    }

    /* ───────────────  PUBLIC API  ─────────────── */

    /// <summary>Called by the Leave button. Switches back to the space scene.</summary>
    public void OnLeavePlanetClicked()
    {
        // Optional autosave – uncomment if you like.
        // GameManager.Instance?.SaveGame();

        if (string.IsNullOrEmpty(spaceSceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(spaceSceneName)) return;

        SceneManager.LoadScene(spaceSceneName);
    }

    /* ───────────────  INTERNAL  ─────────────── */

    private void HealPlayerOnLanding()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;

        float healAmount = percentHeal * stats.maxHealth;
        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);
        stats.SyncHealthBar();

        Debug.Log($"[PlanetSceneManager] Healed player by {healAmount} → " +
                  $"{stats.currentHealth}/{stats.maxHealth}");
    }

    private void ShowLeavePanel()
    {
        if (leavePanel) leavePanel.SetActive(true);
    }

    /// <summary>
    /// Finds a <see cref="Button"/> under <c>leavePanel</c> (first one encountered)
    /// and registers <see cref="OnLeavePlanetClicked"/>.  This means you never have
    /// to wire the button manually in future planet scenes.
    /// </summary>
    private void AutoWireLeaveButton()
    {
        if (!leavePanel) return;

        Button btn = leavePanel.GetComponentInChildren<Button>(true);
        if (!btn)
        {
            Debug.LogWarning("[PlanetSceneManager] No <Button> found under ‘leavePanel’.", this);
            return;
        }

        // Remove any duplicate listeners (safe even if none exist)
        btn.onClick.RemoveListener(OnLeavePlanetClicked);
        btn.onClick.AddListener(OnLeavePlanetClicked);
    }
}
