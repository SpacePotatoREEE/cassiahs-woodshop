using UnityEngine;
using UnityEngine.SceneManagement;

public class PlanetLandingTrigger : MonoBehaviour
{
    /* ────────────  INSPECTOR  ──────────── */
    [Header("UI")]
    [SerializeField] private GameObject planetLandingUI;

    [Header("Scene to Load")]
    [SerializeField] private string planetSceneName = "PlanetScene";

    [Header("Trigger Layer")]
    [SerializeField] private string layerTriggerString = "PlayerShip";

    [Header("Options")]
    [Tooltip("If ON, the player ship is healed to full immediately after landing.")]
    [SerializeField] private bool fullHealOnLanding = true;

    /* ────────────  INTERNAL  ──────────── */
    private bool playerInRange;

    /* ───────────────────────────────────── */
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            planetLandingUI?.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            planetLandingUI?.SetActive(false);
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.L))
            BeginLandingSequence();
    }

    /* ────────────  LANDING  ──────────── */
    private void BeginLandingSequence()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("PlanetLandingTrigger: GameManager.Instance is null.");
            return;
        }

        GameManager.Instance.SaveGame();                    // 1) auto‑save
        SceneManager.sceneLoaded += OnPlanetSceneLoaded;    // 2) hook once
        GameManager.Instance.LoadPlanetScene(planetSceneName);
    }

    private void OnPlanetSceneLoaded(Scene s, LoadSceneMode m)
    {
        SceneManager.sceneLoaded -= OnPlanetSceneLoaded;    // one‑shot

        if (!fullHealOnLanding) return;

        // ───── heal ship, if it exists ─────
        var shipStats = FindObjectOfType<PlayerStats>(true);        // includeInactive = true
        if (shipStats)
        {
            shipStats.currentHealth = shipStats.maxHealth;
            shipStats.SyncHealthBar();
        }

        // ───── heal human‑on‑planet, if it exists ─────
        var humanStats = FindObjectOfType<PlayerStatsHuman>(true);
        if (humanStats)
        {
            humanStats.currentHealth = humanStats.maxHealth;
            if (humanStats.playerHealthBar)
                humanStats.playerHealthBar.SetHealth(humanStats.currentHealth);
        }

        Debug.Log("[PlanetLandingTrigger] Full heal applied on landing.");
    }
}
