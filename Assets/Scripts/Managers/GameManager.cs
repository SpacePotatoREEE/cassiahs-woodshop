using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Global, persistent game controller.
/// • Keeps current player (ship in space, human on planet) + health UI sync  
/// • Owns “credits” currency (event‑driven so any scene HUD can display it)  
/// • Handles save / load (position, health, credits)  
/// • Pops up a “Save Successful” message  
/// • Provides LoadPlanetScene so other scripts can switch scenes easily
/// </summary>
public class GameManager : MonoBehaviour
{
    /* ───────────────  SINGLETON  ─────────────── */
    public static GameManager Instance { get; private set; }
    
    private int pauseCounter = 0;          // allows nested pauses

    /* ───────────────  PLAYER  ──────────────── */
    [Header("Player Reference (auto‑filled)")]
    [SerializeField] private GameObject player;      // ship or human, whichever is active
    private PlayerStats playerStats;                 // cached component

    private const string LAYER_SHIP  = "PlayerShip";
    private const string LAYER_HUMAN = "PlayerHuman";
    private int layerShip  = -1;
    private int layerHuman = -1;

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
        
        // Make the notification UI survive all scenes (once is enough)
        if (saveNotificationPanel != null)
            DontDestroyOnLoad(saveNotificationPanel.transform.root.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
        
        layerShip  = LayerMask.NameToLayer(LAYER_SHIP);
        layerHuman = LayerMask.NameToLayer(LAYER_HUMAN);
    }

    private void Start()
    {
        TryCachePlayer();
        UpdateCreditsUI();              // show 0 on startup
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

        foreach (var ps in FindObjectsOfType<PlayerStats>(true))   // include inactive just in case
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
            playerPosX        = player.transform.position.x,
            playerPosY        = player.transform.position.y,
            playerPosZ        = player.transform.position.z,
            playerHealth      = playerStats.currentHealth,
            playerLevel       = 1,
            credits           = credits,
            currentStarSystem = SceneManager.GetActiveScene().name
        };

        SaveSystem.SaveGame(data);
        ShowSaveNotification("Save Successful");
    }

    public void LoadGame() => AutoLoadOnStartup();

    private void ApplyLoadedData(SaveData data)
    {
        credits = data.credits;
        UpdateCreditsUI();

        if (data.currentStarSystem == SceneManager.GetActiveScene().name)
        {
            FinishApply(data);
        }
        else
        {
            SceneManager.LoadScene(data.currentStarSystem);
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
            player.transform.position = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);

        if (playerStats != null)
        {
            playerStats.currentHealth = data.playerHealth;
            playerStats.SyncHealthBar();
        }
    }

    /* ───── helper for PlanetLandingTrigger ───── */
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

    private IEnumerator HideNotifAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);
        saveNotificationPanel.SetActive(false);
    }
    
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
