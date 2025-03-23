using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Ensure only one instance of GameManager exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Example method to load a planet scene by name.
    /// </summary>
    public void LoadPlanetScene(string sceneName)
    {
        // You can expand this to handle transitions, fade-outs, etc.
        SceneManager.LoadScene(sceneName);
    }
}