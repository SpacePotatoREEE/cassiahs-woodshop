// PlanetSceneManager.cs
//
// Combines previous planet‑scene logic with the new spawn‑pipeline bootstrap.
// ‑‑ Version: 2025‑08‑06
//
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PlanetSceneManager : MonoBehaviour
{
    /* ───────────────  INSPECTOR  ─────────────── */

    [Header("► Space Scene To Load When Leaving")]
    [Tooltip("Name of the space scene that should be loaded when the player clicks ‘Leave’. "
           + "This scene must be added to File ▶ Build Settings.")]
    [SerializeField] private string spaceSceneName = "Space_A";

    [Header("► Leave‑Button Panel (optional)")]
    [Tooltip("Root GameObject that contains the Leave button. "
           + "If left unassigned the panel will not be toggled.")]
    [SerializeField] private GameObject leavePanel = null;

    [Header("► Landing Heal")]
    [Tooltip("Percentage of max‑health restored the instant the player lands (0–1).")]
    [Range(0f, 1f)] [SerializeField] private float percentHeal = 1f;

    [Header("► Player Bootstrap")]
    [Tooltip("Prefab that represents the player on foot (Top‑Down controller). "
           + "Only spawned if one is not already in the scene.")]
    [SerializeField] private GameObject playerPrefab = null;

    [Tooltip("Tag that marks the desired spawn Transform in the scene.")]
    [SerializeField] private string playerSpawnTag = "PlayerSpawn";

    [Header("► (Optional) Spawn Volumes")]
    [Tooltip("If you want to make sure certain AreaSpawnManagers are enabled at runtime, "
           + "list them here; otherwise they will self‑register when the player enters.")]
    [SerializeField] private AreaSpawnManager[] spawnVolumes = { };

    /* ───────────────  LIFECYCLE  ─────────────── */

    private void Awake()
    {
        // Scene‑name sanity check
        if (string.IsNullOrEmpty(spaceSceneName))
            Debug.LogError("[PlanetSceneManager] ‘spaceSceneName’ is empty – set it in the Inspector.", this);
        else if (!Application.CanStreamedLevelBeLoaded(spaceSceneName))
            Debug.LogError($"[PlanetSceneManager] The scene “{spaceSceneName}” is NOT in Build Settings – add it or fix the name.", this);

        // Make sure we have a player
        EnsurePlayerPresence();
    }

    private void Start()
    {
        HealPlayerOnLanding();
        ShowLeavePanel();
        AutoWireLeaveButton();
        InitialiseSpawnVolumes();
        RestoreSceneState();               // hook for your SaveSystem (optional)
    }

    /* ───────────────  PUBLIC UI CALLBACK  ─────────────── */

    /// <summary>Called by the Leave button. Switches back to the space scene.</summary>
    public void OnLeavePlanetClicked()
    {
        // Example autosave – uncomment if desired.
        // GameManager.Instance?.SaveGame();

        if (string.IsNullOrEmpty(spaceSceneName)) return;
        if (!Application.CanStreamedLevelBeLoaded(spaceSceneName)) return;

        SceneManager.LoadScene(spaceSceneName);
    }

    /* ───────────────  INTERNAL HELPERS  ─────────────── */

    private void EnsurePlayerPresence()
    {
        if (FindObjectOfType<TopDownPlayerController>() != null) return;   // already here

        if (!playerPrefab)
        {
            Debug.LogWarning("[PlanetSceneManager] No playerPrefab assigned – cannot spawn player!", this);
            return;
        }

        // Spawn position
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        GameObject marker = GameObject.FindWithTag(playerSpawnTag);
        if (marker)
        {
            pos = marker.transform.position;
            rot = marker.transform.rotation;
        }

        Instantiate(playerPrefab, pos, rot);
    }

    private void HealPlayerOnLanding()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (!stats) return;

        float healAmount = percentHeal * stats.maxHealth;
        stats.currentHealth = Mathf.Min(stats.currentHealth + healAmount, stats.maxHealth);
        stats.SyncHealthBar();

        Debug.Log($"[PlanetSceneManager] Healed player by {healAmount} → {stats.currentHealth}/{stats.maxHealth}");
    }

    private void ShowLeavePanel()
    {
        if (leavePanel) leavePanel.SetActive(true);
    }

    /// <summary>Automatically hooks the first Button found under <c>leavePanel</c>.</summary>
    private void AutoWireLeaveButton()
    {
        if (!leavePanel) return;

        Button btn = leavePanel.GetComponentInChildren<Button>(true);
        if (!btn)
        {
            Debug.LogWarning("[PlanetSceneManager] No <Button> found under ‘leavePanel’.", this);
            return;
        }

        btn.onClick.RemoveListener(OnLeavePlanetClicked); // avoid duplicates
        btn.onClick.AddListener(OnLeavePlanetClicked);
    }

    private void InitialiseSpawnVolumes()
    {
        foreach (var v in spawnVolumes)
            if (v) v.enabled = true;
    }

    /// <summary>
    /// Placeholder for whatever save‑state restoration your project already performs
    /// (time‑of‑day, music cues, quest flags, etc.).
    /// </summary>
    private void RestoreSceneState()
    {
        // Example sketch:
        // if (SaveSystem.TryGetPlanetState(SceneManager.GetActiveScene().name, out PlanetSaveData st))
        // {
        //     DayNightCycle.Instance.SetTimeOfDay(st.timeOfDay);
        //     BackgroundMusicManager.Instance.Play(st.musicCue);
        // }
    }
}
