using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Global, persistent game controller (Unity 6, URP).
/// • Keeps current player (ship/human) + UI in sync
/// • Owns currency and save / load
/// • Understands the galaxy map layer (StarSystemData)
/// • Centralizes additive world-scene loading (Main stays resident)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private static bool saveLoadedOnce = false;

    /* ───────── Config ───────── */

    [Header("Persistent Scene")]
    [Tooltip("Exact name of the persistent scene that contains your ONLY real Camera + managers (e.g., \"Main\").")]
    [SerializeField] private string persistentMainSceneName = "Main";

    [Header("Galaxy Database (drag asset here)")]
    [SerializeField] private GalaxyDatabase galaxyDatabase;

    [Header("Credits Label (optional global)")]
    [SerializeField] private TextMeshProUGUI creditsText;

    [Header("Save Notification UI")]
    [SerializeField] private GameObject saveNotificationPanel;
    [SerializeField] private TextMeshProUGUI saveNotificationText;
    [SerializeField] private float notificationDuration = 2f;

    /* ───────── Runtime State ───────── */

    public StarSystemData CurrentSystem => currentSystem;
    private StarSystemData currentSystem;
    private readonly HashSet<StarSystemData> discoveredSystems = new();

    [Header("Player Reference (auto‑filled)")]
    [SerializeField] private GameObject player;
    private PlayerStats playerStats;

    private const string LAYER_SHIP  = "PlayerShip";
    private const string LAYER_HUMAN = "PlayerHuman";
    private int layerShip, layerHuman;

    private int credits = 0;
    public System.Action<int> OnCreditsChanged;
    public int GetCreditsForUI() => credits;

    private Coroutine hideNotificationCoroutine;

    private string currentWorldScene;
    private bool switching;    // re-entrancy guard

    public string CurrentWorldSceneName => currentWorldScene;
    public string MainSceneName => persistentMainSceneName;

    /* ───────── Scene Switch API ───────── */

    public void SwitchToWorldScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (switching) return; // prevent re-entrancy / double calls
        StartCoroutine(SwitchWorldScene_Coro(sceneName));
    }

    private System.Collections.IEnumerator SwitchWorldScene_Coro(string sceneName)
    {
        switching = true;

        // Validate main name
        if (string.IsNullOrWhiteSpace(persistentMainSceneName))
        {
            Debug.LogError("[GameManager] Persistent Main Scene Name is empty. Set it on the GameManager.", this);
            switching = false;
            yield break;
        }

        // 1) Ensure MAIN is loaded and deduped
        yield return EnsureMainLoadedAndDedup();

        // 2) If target is already the active scene, short-circuit
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            currentWorldScene = sceneName;
            switching = false;
            yield break;
        }

        // 3) Load the target content scene additively (or reuse if already loaded)
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            DumpLoadedScenes($"[GameManager] Scene '{sceneName}' is not in Build Settings; cannot switch.");
            switching = false;
            yield break;
        }

        // Try to find a loaded instance first
        Scene target = FindLoadedSceneByName(sceneName);

        if (!target.IsValid() || !target.isLoaded)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                DumpLoadedScenes($"[GameManager] LoadSceneAsync returned null for '{sceneName}'.");
                switching = false;
                yield break;
            }

            op.allowSceneActivation = true;
            while (!op.isDone) yield return null;
            yield return null; // let Unity register scene

            target = FindLoadedSceneByName(sceneName); // pick the LOADED handle
        }

        if (!target.IsValid() || !target.isLoaded)
        {
            DumpLoadedScenes($"[GameManager] After load, scene '{sceneName}' is not valid/loaded.");
            switching = false;
            yield break;
        }

        // 4) If it's already active (another system made it active), skip SetActive
        if (target == SceneManager.GetActiveScene())
        {
            currentWorldScene = sceneName;
        }
        else
        {
            // Use the LOADED handle; do not use a stale struct.
            if (!SceneManager.SetActiveScene(target))
            {
                DumpLoadedScenes($"[GameManager] SetActiveScene failed for '{sceneName}'.");
                switching = false;
                yield break;
            }
            currentWorldScene = sceneName;
        }

        // 5) Unload other content scenes (never unload Main)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (!s.isLoaded) continue;
            if (s == target) continue;
            if (s.name == persistentMainSceneName) continue;

            var unload = SceneManager.UnloadSceneAsync(s);
            while (unload != null && !unload.isDone) yield return null;
        }

        TryCachePlayer();
        switching = false;
    }

    private System.Collections.IEnumerator EnsureMainLoadedAndDedup()
    {
        var mains = GetLoadedScenesByName(persistentMainSceneName);

        if (mains.Count == 0)
        {
            if (!Application.CanStreamedLevelBeLoaded(persistentMainSceneName))
            {
                Debug.LogError($"[GameManager] Main scene '{persistentMainSceneName}' not in Build Settings.");
                yield break;
            }

            var loadMain = SceneManager.LoadSceneAsync(persistentMainSceneName, LoadSceneMode.Additive);
            while (loadMain != null && !loadMain.isDone) yield return null;
            yield return null;

            mains = GetLoadedScenesByName(persistentMainSceneName);
            if (mains.Count == 0)
            {
                DumpLoadedScenes($"[GameManager] Failed to load main scene '{persistentMainSceneName}'.");
                yield break;
            }
        }

        // Set one Main active
        if (SceneManager.GetActiveScene().name != persistentMainSceneName)
            SceneManager.SetActiveScene(mains[0]);

        // Unload accidental duplicates of Main
        if (mains.Count > 1)
        {
            for (int i = 1; i < mains.Count; i++)
            {
                var u = SceneManager.UnloadSceneAsync(mains[i]);
                while (u != null && !u.isDone) yield return null;
            }
        }
    }

    private static Scene FindLoadedSceneByName(string name)
    {
        // Return the LOADED instance with this name, if any.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name == name) return s;
        }
        return default;
    }

    private static List<Scene> GetLoadedScenesByName(string name)
    {
        var list = new List<Scene>();
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && s.name == name) list.Add(s);
        }
        return list;
    }

    private static void DumpLoadedScenes(string prefix)
    {
        var list = "";
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            list += $"{i}: '{s.name}' loaded={s.isLoaded} active={(s == SceneManager.GetActiveScene())}\n";
        }
        Debug.LogError($"{prefix}\nLoaded scenes:\n{list}");
    }

    /* ───────── Lifecycle ───────── */

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
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

        if (!saveLoadedOnce)
        {
            AutoLoadOnStartup();
            saveLoadedOnce = true;
        }
    }

    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

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

    /* ───────── Credits ───────── */

    public void AddCredits(int amount) { credits += amount; UpdateCreditsUI(); }
    public bool SpendCredits(int amount)
    {
        if (credits < amount) return false;
        credits -= amount; UpdateCreditsUI(); return true;
    }

    private void UpdateCreditsUI()
    {
        if (creditsText != null) creditsText.text = $"₡ {credits:n0}";
        OnCreditsChanged?.Invoke(credits);
    }

    /* ───────── Save / Load ───────── */

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
            playerPosX = player.transform.position.x,
            playerPosY = player.transform.position.y,
            playerPosZ = player.transform.position.z,
            playerHealth = playerStats.currentHealth,
            playerEnergy = playerStats.CurrentEnergy,
            playerLevel  = 1,
            credits = credits,
            currentStarSystem = currentSystem != null ? currentSystem.displayName
                                                      : SceneManager.GetActiveScene().name,
            discoveredSystems = discoveredSystems.Select(s => s.displayName).ToList()
        };
        SaveSystem.SaveGame(data);
        ShowSaveNotification("Save Successful");
    }

    public void LoadGame() => AutoLoadOnStartup();

    private void ApplyLoadedData(SaveData data)
    {
        credits = data.credits; UpdateCreditsUI();

        discoveredSystems.Clear();
        if (galaxyDatabase != null && data.discoveredSystems != null)
        {
            foreach (string name in data.discoveredSystems)
            {
                StarSystemData sys = galaxyDatabase.allSystems.FirstOrDefault(s => s.displayName == name);
                if (sys != null) discoveredSystems.Add(sys);
            }
        }

        string targetScene = SceneManager.GetActiveScene().name;
        if (galaxyDatabase != null)
        {
            StarSystemData sys = galaxyDatabase.allSystems.FirstOrDefault(s => s.displayName == data.currentStarSystem);
            if (sys != null) targetScene = sys.sceneName;
        }

        if (targetScene == SceneManager.GetActiveScene().name) { FinishApply(data); }
        else
        {
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> handler = null;
            handler = (s, m) => { FinishApply(data); SceneManager.sceneLoaded -= handler; };
            SceneManager.sceneLoaded += handler;

            // Use additive switcher so Main persists (and we pick the loaded handle):
            SwitchToWorldScene(targetScene);
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
            float restoredEnergy = (data.playerEnergy <= 0f) ? playerStats.maxEnergy : data.playerEnergy;
            playerStats.CurrentEnergy = Mathf.Clamp(restoredEnergy, 0f, playerStats.maxEnergy);
            playerStats.SyncHealthBar();
        }

        StarSystemIdentifier id = FindObjectOfType<StarSystemIdentifier>();
        if (id != null) RegisterCurrentSystem(id.starSystem);
    }

    /* ───────── Galaxy Helpers ───────── */

    public void RegisterCurrentSystem(StarSystemData sys)
    {
        currentSystem = sys;
        if (sys != null) discoveredSystems.Add(sys);
    }

    public bool IsSystemDiscovered(StarSystemData sys) => discoveredSystems.Contains(sys);
    public void AddDiscoveredSystem(StarSystemData sys) { if (sys != null) discoveredSystems.Add(sys); }

    /* ───────── Back‑compat ───────── */

    public void LoadPlanetScene(string planetSceneName) => SwitchToWorldScene(planetSceneName);

    /* ───────── Save notif ───────── */

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

    /* ───────── Pause ───────── */

    private int pauseCounter = 0;
    public void PauseGame()  { if (++pauseCounter == 1) Time.timeScale = 0f; }
    public void ResumeGame() { if (--pauseCounter <= 0) { pauseCounter = 0; Time.timeScale = 1f; } }
}
