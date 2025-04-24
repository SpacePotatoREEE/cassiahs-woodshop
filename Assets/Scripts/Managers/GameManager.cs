using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Global, persistent game controller.
/// • Keeps current player (ship/human) + UI sync  
/// • Owns currency and save / load  
/// • Understands the galaxy map layer (StarSystemData)  
/// </summary>
public class GameManager : MonoBehaviour
{
    /* ───────────────  SINGLETON  ─────────────── */
    public static GameManager Instance { get; private set; }

    /* ───────────────  GALAXY  ─────────────── */
    [Header("Galaxy Database (drag asset here)")]
    [SerializeField] private GalaxyDatabase galaxyDatabase;
    public StarSystemData CurrentSystem => currentSystem;

    private StarSystemData currentSystem;                       // set by StarSystemIdentifier
    private readonly HashSet<StarSystemData> discoveredSystems = new();

    public void RegisterCurrentSystem(StarSystemData sys)
    {
        currentSystem = sys;
        if (sys != null) discoveredSystems.Add(sys);
    }
    
    /* ─── discovery API used by GalaxyMapController ─── */
    public bool IsSystemDiscovered(StarSystemData sys) => discoveredSystems.Contains(sys);

    public void AddDiscoveredSystem(StarSystemData sys)
    {
        if (sys != null) discoveredSystems.Add(sys);
    }

    /* ───────────────  PLAYER  ──────────────── */
    [Header("Player Reference (auto‑filled)")]
    [SerializeField] private GameObject player;
    private PlayerStats playerStats;

    private const string LAYER_SHIP  = "PlayerShip";
    private const string LAYER_HUMAN = "PlayerHuman";
    private int layerShip, layerHuman;

    /* ───────────────  CREDITS  ─────────────── */
    [Header("Credits Label (optional global)")]
    [SerializeField] private TextMeshProUGUI creditsText;

    private int credits = 0;
    public System.Action<int> OnCreditsChanged;
    public int GetCreditsForUI() => credits;

    /* ────── SAVE NOTIFICATION POP‑UP ────── */
    [Header("Save Notification UI")]
    [SerializeField] private GameObject saveNotificationPanel;
    [SerializeField] private TextMeshProUGUI saveNotificationText;
    [SerializeField] private float notificationDuration = 2f;
    private Coroutine hideNotificationCoroutine;

    /* ───────────────  LIFECYCLE  ────────────── */
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (saveNotificationPanel != null)
            DontDestroyOnLoad(saveNotificationPanel.transform.root.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        layerShip  = LayerMask.NameToLayer(LAYER_SHIP);
        layerHuman = LayerMask.NameToLayer(LAYER_HUMAN);
    }

    private void Start()
    {
        TryCachePlayer();
        UpdateCreditsUI();
        AutoLoadOnStartup();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /* ───────────  SCENE EVENTS  ─────────── */
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCachePlayer();
        Debug.Log($"[GameManager] Scene '{scene.name}' loaded. Player found: {player != null}");
    }

    private void TryCachePlayer()
    {
        if (player != null) return;

        foreach (var ps in FindObjectsOfType<PlayerStats>(true))
        {
            int l = ps.gameObject.layer;
            if (l == layerShip || l == layerHuman)
            {
                player      = ps.gameObject;
                playerStats = ps;
                break;
            }
        }
    }

    /* ───────────  CREDITS API  ─────────── */
    public void AddCredits(int amount)
    {
        credits += amount;
        UpdateCreditsUI();
    }

    public bool SpendCredits(int amount)
    {
        if (credits < amount) return false;
        credits -= amount;
        UpdateCreditsUI();
        return true;
    }

    private void UpdateCreditsUI()
    {
        if (creditsText != null)
            creditsText.text = $"₡ {credits:n0}";
        OnCreditsChanged?.Invoke(credits);
    }

    /* ───────────  SAVE / LOAD  ─────────── */
    private void AutoLoadOnStartup()
    {
        SaveData data = SaveSystem.LoadGame();
        if (data != null) ApplyLoadedData(data);
    }

    public void SaveGame()
    {
        if (player == null || playerStats == null)
        {
            Debug.LogWarning("SaveGame aborted – player reference missing.");
            return;
        }

        SaveData data = new SaveData
        {
            // transform
            playerPosX = player.transform.position.x,
            playerPosY = player.transform.position.y,
            playerPosZ = player.transform.position.z,

            // vitals
            playerHealth = playerStats.currentHealth,
            playerLevel  = 1,

            // economy
            credits = credits,

            // galaxy
            currentStarSystem = currentSystem != null
                              ? currentSystem.displayName
                              : SceneManager.GetActiveScene().name,

            discoveredSystems = discoveredSystems
                                .Select(s => s.displayName)
                                .ToList()
        };

        SaveSystem.SaveGame(data);
        ShowSaveNotification("Save Successful");
    }

    public void LoadGame() => AutoLoadOnStartup();

    private void ApplyLoadedData(SaveData data)
    {
        credits = data.credits;
        UpdateCreditsUI();

        // rebuild discovery set
        discoveredSystems.Clear();
        if (galaxyDatabase != null && data.discoveredSystems != null)
        {
            foreach (string name in data.discoveredSystems)
            {
                StarSystemData sys = galaxyDatabase.allSystems
                                          .FirstOrDefault(s => s.displayName == name);
                if (sys != null) discoveredSystems.Add(sys);
            }
        }

        // find the right scene
        string targetScene = SceneManager.GetActiveScene().name;
        if (galaxyDatabase != null)
        {
            StarSystemData sys = galaxyDatabase.allSystems
                                   .FirstOrDefault(s => s.displayName == data.currentStarSystem);
            if (sys != null) targetScene = sys.sceneName;
        }

        if (targetScene == SceneManager.GetActiveScene().name)
        {
            FinishApply(data);
        }
        else
        {
            SceneManager.LoadScene(targetScene);
            SceneManager.sceneLoaded += (s, m) =>
            {
                FinishApply(data);
                SceneManager.sceneLoaded -= null;
            };
        }
    }

    private void FinishApply(SaveData data)
    {
        TryCachePlayer();

        if (player != null)
            player.transform.position = new Vector3(data.playerPosX,
                                                    data.playerPosY,
                                                    data.playerPosZ);

        if (playerStats != null)
        {
            playerStats.currentHealth = data.playerHealth;
            playerStats.SyncHealthBar();
        }

        StarSystemIdentifier id = FindObjectOfType<StarSystemIdentifier>();
        if (id != null) RegisterCurrentSystem(id.starSystem);
    }

    /* ─────────  PLANET LOAD HELPER  ───────── */
    /// <summary>Used by PlanetLandingTrigger to leave orbit.</summary>
    public void LoadPlanetScene(string planetSceneName)
    {
        SceneManager.LoadScene(planetSceneName);
    }

    /* ─────────  SAVE NOTIF POP‑UP  ───────── */
    private void ShowSaveNotification(string msg)
    {
        if (saveNotificationPanel == null || saveNotificationText == null) return;

        saveNotificationText.text = msg;
        saveNotificationPanel.SetActive(true);

        if (hideNotificationCoroutine != null) StopCoroutine(hideNotificationCoroutine);
        hideNotificationCoroutine = StartCoroutine(HideNotifAfterDelay());
    }

    private System.Collections.IEnumerator HideNotifAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        saveNotificationPanel.SetActive(false);
    }

    /* ─────────  PAUSE / RESUME  ───────── */
    private int pauseCounter = 0;

    public void PauseGame()
    {
        pauseCounter++;
        if (pauseCounter == 1) Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        pauseCounter--;
        if (pauseCounter <= 0)
        {
            pauseCounter = 0;
            Time.timeScale = 1f;
        }
    }
}
