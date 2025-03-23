using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;    // Needed for UI references
using System.Collections;
using TMPro; // Needed for IEnumerator / Coroutines

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private GameObject player; 
    private PlayerStats playerStats;

    [Header("Save Notification UI")]
    [SerializeField] private GameObject saveNotificationPanel;
    [SerializeField] private TextMeshProUGUI  saveNotificationText;
    [SerializeField] private float notificationDuration = 2f;
    
    private Coroutine hideNotificationCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Cache the PlayerStats component if it exists
        if (player != null)
        {
            playerStats = player.GetComponent<PlayerStats>();
        }

        // Attempt to auto-load data at startup
        AutoLoadOnStartup();
    }

    private void AutoLoadOnStartup()
    {
        SaveData loadedData = SaveSystem.LoadGame();
        if (loadedData == null)
        {
            Debug.Log("No saved data found. Starting fresh.");
            return;
        }
        ApplyLoadedData(loadedData);
    }

    /// <summary>
    /// Call this from PlanetLandingTrigger to save the game.
    /// Also call from any other UI button or method if needed.
    /// </summary>
    public void SaveGame()
    {
        if (player == null)
        {
            Debug.LogWarning("No player assigned, cannot save position.");
            return;
        }
        if (playerStats == null)
        {
            Debug.LogWarning("No PlayerStats component found, cannot save health.");
            return;
        }

        // Create a SaveData object
        SaveData data = new SaveData();

        // 1) Player position
        Vector3 pPos = player.transform.position;
        data.playerPosX = pPos.x;
        data.playerPosY = pPos.y;
        data.playerPosZ = pPos.z;

        // 2) Player health (from PlayerStats)
        data.playerHealth = playerStats.currentHealth;
        data.playerLevel  = 1; // If you have a real level, set it here

        // 3) Current scene
        data.currentStarSystem = SceneManager.GetActiveScene().name;

        // Save it to disk
        SaveSystem.SaveGame(data);

        // If save was successful, show a notification in the UI
        // (You can refine "success" detection if needed.)
        ShowSaveNotification("Save Successful");
    }

    public void LoadGame()
    {
        SaveData loadedData = SaveSystem.LoadGame();
        if (loadedData == null)
        {
            Debug.LogWarning("No game data to load.");
            return;
        }
        ApplyLoadedData(loadedData);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Attempt to find the player in the newly loaded scene
        GameObject foundPlayer = GameObject.FindGameObjectWithTag("Player");
        if (foundPlayer != null)
        {
            player = foundPlayer;
            playerStats = player.GetComponent<PlayerStats>();
            Debug.Log($"GameManager: Found player '{player.name}' in scene '{scene.name}'.");
        }
        else
        {
            player = null;
            playerStats = null;
            Debug.Log($"GameManager: No player found in scene '{scene.name}'.");
        }
    }

    private void ApplyLoadedData(SaveData data)
    {
        // If the saved scene is different, load that scene
        if (data.currentStarSystem != SceneManager.GetActiveScene().name)
        {
            SceneManager.LoadScene(data.currentStarSystem);
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                // Once the scene is loaded, find the player if needed
                if (player == null)
                {
                    player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        playerStats = player.GetComponent<PlayerStats>();
                    }
                }
                PositionPlayer(player, data);
                RestorePlayerStats(playerStats, data);

                SceneManager.sceneLoaded -= null;
            };
        }
        else
        {
            // Same scene, just apply
            PositionPlayer(player, data);
            RestorePlayerStats(playerStats, data);
        }
    }

    /// <summary>
    /// Reposition the player using data from the SaveData.
    /// </summary>
    private void PositionPlayer(GameObject p, SaveData data)
    {
        if (p == null) return;
        p.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
    }

    /// <summary>
    /// Restore player's health (and future stats) from SaveData.
    /// </summary>
    private void RestorePlayerStats(PlayerStats stats, SaveData data)
    {
        if (stats == null) return;
        stats.currentHealth = data.playerHealth;
    }

    /// <summary>
    /// Loads a planet scene by name (called in PlanetLandingTrigger).
    /// </summary>
    public void LoadPlanetScene(string planetSceneName)
    {
        SceneManager.LoadScene(planetSceneName);
    }

    // -----------------------------------------------------------------------
    // Save Notification UI Methods
    // -----------------------------------------------------------------------
    
    /// <summary>
    /// Displays the save notification with the specified message.
    /// </summary>
    private void ShowSaveNotification(string message)
    {
        if (saveNotificationPanel == null || saveNotificationText == null)
        {
            Debug.LogWarning("Save notification UI references are missing!");
            return;
        }

        // Activate the panel
        saveNotificationPanel.SetActive(true);
        // Set the text
        saveNotificationText.text = message;

        // If a previous hide coroutine is running, stop it
        if (hideNotificationCoroutine != null)
        {
            StopCoroutine(hideNotificationCoroutine);
        }

        // Start a new coroutine to hide the panel after X seconds
        hideNotificationCoroutine = StartCoroutine(HideSaveNotificationAfterDelay());
    }

    private IEnumerator HideSaveNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        saveNotificationPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // Always unsubscribe to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
