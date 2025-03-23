using UnityEngine;
using UnityEngine.SceneManagement;

public class PlanetSceneManager : MonoBehaviour
{
    [Header("Associated Space Scene")]
    [Tooltip("Which space scene should we load when we leave this planet?")]
    [SerializeField] private string spaceSceneName = "Space_A";

    [Header("UI References")]
    [Tooltip("Panel or Canvas that contains the 'Leave' button.")]
    [SerializeField] private GameObject leavePanel;

    private void Start()
    {
        // Optionally show the UI panel immediately on planet load
        if (leavePanel != null)
            leavePanel.SetActive(true);
    }

    /// <summary>
    /// Called by the 'Leave' button in the UI.
    /// Loads the associated space scene.
    /// </summary>
    public void OnLeavePlanetClicked()
    {
        // Optionally, save the game here if desired:
        // GameManager.Instance?.SaveGame();

        // Load the space scene
        SceneManager.LoadScene(spaceSceneName);
    }
}