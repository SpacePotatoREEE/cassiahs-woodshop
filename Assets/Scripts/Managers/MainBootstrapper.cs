using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the persistent Main scene additively, but ONLY if it's not already loaded.
/// Safe to keep in a Boot scene or a content scene; no-ops if already in Main or if Main is loaded.
/// </summary>
[DefaultExecutionOrder(-10050)]
public class MainBootstrapper : MonoBehaviour
{
    [SerializeField] private string mainSceneName = "Main";
    [SerializeField] private bool setMainActive = true;

    private static bool s_AlreadyBootstrapped = false;

    private void Awake()
    {
        // If we're already in Main, do nothing.
        if (SceneManager.GetActiveScene().name == mainSceneName)
            return;

        // If Main is already loaded, do nothing.
        var main = SceneManager.GetSceneByName(mainSceneName);
        if (main.IsValid() && main.isLoaded)
        {
            if (setMainActive) SceneManager.SetActiveScene(main);
            return;
        }

        // Prevent duplicate loads if another bootstrapper exists.
        if (s_AlreadyBootstrapped) return;
        s_AlreadyBootstrapped = true;

        StartCoroutine(LoadMain());
    }

    private IEnumerator LoadMain()
    {
        if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
        {
            Debug.LogError($"[MainBootstrapper] '{mainSceneName}' is not in Build Settings.");
            yield break;
        }

        var op = SceneManager.LoadSceneAsync(mainSceneName, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[MainBootstrapper] LoadSceneAsync returned null for '{mainSceneName}'.");
            yield break;
        }

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        yield return null;

        var main = SceneManager.GetSceneByName(mainSceneName);
        if (setMainActive && main.IsValid() && main.isLoaded)
            SceneManager.SetActiveScene(main);
    }
}