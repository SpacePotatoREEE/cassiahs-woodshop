using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class EnsureMainSceneLoaded : MonoBehaviour
{
    [Header("Persistent scene that contains the ONLY Camera + managers")]
    [SerializeField] private string mainSceneName = "Main";

    [Tooltip("Set Main as active after load (recommended).")]
    [SerializeField] private bool setMainActive = true;

    // Session guard so multiple instances won't double-load.
    private static bool s_MainAttemptedThisSession = false;

    private void Awake()
    {
        // If the active scene is Main (e.g., you pressed Play from Main), no-op.
        if (SceneManager.GetActiveScene().name == mainSceneName)
            return;

        // If Main is already loaded, optionally set active and bail.
        var main = SceneManager.GetSceneByName(mainSceneName);
        if (main.IsValid() && main.isLoaded)
        {
            if (setMainActive) SceneManager.SetActiveScene(main);
            return;
        }

        // Another Ensure may already be loading Main this session.
        if (s_MainAttemptedThisSession) return;
        s_MainAttemptedThisSession = true;

        StartCoroutine(LoadMainOnce());
    }

    private IEnumerator LoadMainOnce()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError($"[EnsureMainSceneLoaded] '{mainSceneName}' is not in Build Settings.");
            yield break;
        }

        var load = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError($"[EnsureMainSceneLoaded] LoadSceneAsync returned null for '{mainSceneName}'.");
            yield break;
        }

        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null; // let Unity register the scene

        var main = SceneManager.GetSceneByName(mainSceneName);
        if (setMainActive && main.IsValid() && main.isLoaded)
            SceneManager.SetActiveScene(main);
    }
}
