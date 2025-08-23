// Assets/Scripts/Systems/BootGuard.cs
// Unity 6 – make sure your "Main" (boot) scene is always present.

using UnityEngine;
using UnityEngine.SceneManagement;

public static class BootGuard
{
    // ← Change this if your boot scene is named differently.
    private const string BootSceneName = "Main";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureBootLoaded()
    {
        // If the boot scene isn’t in Build Settings, do nothing (no exceptions).
        if (!Application.CanStreamedLevelBeLoaded(BootSceneName)) return;

        var boot = SceneManager.GetSceneByName(BootSceneName);
        if (!boot.IsValid() || !boot.isLoaded)
        {
            // Load Main additively so it sits next to whatever scene you start from.
            SceneManager.LoadScene(BootSceneName, LoadSceneMode.Additive);
        }
    }
}