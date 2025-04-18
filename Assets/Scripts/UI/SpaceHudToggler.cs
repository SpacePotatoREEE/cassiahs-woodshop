using UnityEngine;
using UnityEngine.SceneManagement;

/// Drop this on Prefab_HUD_Space
[RequireComponent(typeof(Canvas))]
public class SpaceHudToggler : MonoBehaviour
{
    [SerializeField] private string[] spaceScenes = { "Space_A" };  // add more

    private Canvas hudCanvas;

    private void Awake()
    {
        hudCanvas = GetComponent<Canvas>();
        DontDestroyOnLoad(gameObject);                // survive every scene
        SceneManager.sceneLoaded   += (_,__) => Refresh();
        SceneManager.sceneUnloaded += _      => Refresh();
        Refresh();                                    // run once for the scene we’re already in
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded   -= (_,__) => Refresh();
        SceneManager.sceneUnloaded -= _      => Refresh();
    }

    private void Refresh()
    {
        bool show = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var s = SceneManager.GetSceneAt(i);
            if (s.isLoaded &&
                System.Array.Exists(spaceScenes, n => n == s.name))
            {
                show = true;
                break;
            }
        }
        hudCanvas.enabled = show;         // Canvas on/off but script keeps running
    }
}