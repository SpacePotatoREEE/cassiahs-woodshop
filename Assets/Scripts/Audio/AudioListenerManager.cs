using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guarantees that there is **exactly one enabled AudioListener** at all times.
/// • Keeps the listener on Camera.main (adding it if missing).  
/// • Disables every other AudioListener it finds.
/// </summary>
[DefaultExecutionOrder(-10000)]           // run before almost everything
public class AudioListenerManager : MonoBehaviour
{
    public static AudioListenerManager Instance { get; private set; }

    private void Awake()
    {
        // basic singleton that survives scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // re-validate when a new scene finishes loading
        SceneManager.sceneLoaded += (_, __) => ValidateAudioListeners();
    }

    private void Start() => ValidateAudioListeners();

    /// <summary>Run the validation pass: keep one, disable the rest.</summary>
    public void ValidateAudioListeners()
    {
        // 1) ensure the main camera exists and has an enabled listener
        Camera main = Camera.main;
        AudioListener mainListener = null;

        if (main != null)
        {
            mainListener = main.GetComponent<AudioListener>();
            if (mainListener == null)
                mainListener = main.gameObject.AddComponent<AudioListener>();

            mainListener.enabled = true;
        }

        // 2) turn OFF every other listener
        foreach (AudioListener listener in FindObjectsOfType<AudioListener>(true))
        {
            if (listener == null || listener == mainListener) continue;
            listener.enabled = false;
        }
    }

    /// <summary>Call this after you swap cameras manually.</summary>
    public static void EnsureSingleListener()
    {
        if (Instance != null)
            Instance.ValidateAudioListeners();
    }
}