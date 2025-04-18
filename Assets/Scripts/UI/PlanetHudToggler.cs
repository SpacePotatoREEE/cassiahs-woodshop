using UnityEngine;
using UnityEngine.SceneManagement;

/// Enables the planet HUD whenever an “on‑planet” scene is loaded.
public class PlanetHudToggler : MonoBehaviour
{
    [Tooltip("Scene names that should show HUD_Planet")]
    [SerializeField] private string[] planetScenes = { "Planet_A", "Planet_B" };

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);      // single persistent instance
        SceneManager.sceneLoaded   += (_,__) => Refresh();
        SceneManager.sceneUnloaded += _      => Refresh();
        Refresh();                          // evaluate for startup scene
    }

    private void Refresh()
    {
        bool show = false;
        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded && System.Array.Exists(planetScenes, n => n == s.name))
            {
                show = true; break;
            }
        }
        gameObject.SetActive(show);         // toggles HUD_Planet root
    }
}