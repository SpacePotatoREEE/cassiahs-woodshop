using UnityEngine;
using UnityEngine.SceneManagement;
using System;                     // for StringComparison

/// Attach this to your HUD prefab (the object that already has the Canvas).
/// Drag the Space and Planet HUD images (or panels) into the two fields.
[RequireComponent(typeof(Canvas))]
public class SceneHudImageSwitcher : MonoBehaviour
{
    [Header("HUD Image Roots")]
    [Tooltip("Shown when the scene name begins with \"Space\".")]
    [SerializeField] private GameObject spaceHudImage;

    [Tooltip("Shown when the scene name begins with \"Planet\".")]
    [SerializeField] private GameObject planetHudImage;

    private void Awake()
    {
        // Make the whole HUD survive scene loads.
        DontDestroyOnLoad(gameObject);

        // Refresh whenever Unity finishes loading a new scene.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Also do an immediate refresh for the scene we’re already in.
        Refresh();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Refresh();
    }

    /// <summary>
    /// Enables the image that matches the current scene name prefix and
    /// disables the other. If the prefix is neither “Space” nor “Planet”,
    /// both images are hidden.
    /// </summary>
    private void Refresh()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        bool isSpace  = sceneName.StartsWith("Space",  StringComparison.OrdinalIgnoreCase);
        bool isPlanet = sceneName.StartsWith("Planet", StringComparison.OrdinalIgnoreCase);

        if (spaceHudImage  != null) spaceHudImage.SetActive(isSpace);
        if (planetHudImage != null) planetHudImage.SetActive(isPlanet);

        // Optional safety: hide both if the prefix didn’t match either.
        if (!isSpace && !isPlanet)
        {
            if (spaceHudImage  != null) spaceHudImage.SetActive(false);
            if (planetHudImage != null) planetHudImage.SetActive(false);
        }
    }
}
