using UnityEngine;

public class PlanetLandingTrigger : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Drag the UI panel GameObject here (the one that says 'Press L to Land').")]
    [SerializeField] private GameObject planetLandingUI;

    [Header("Scene to Load")]
    [Tooltip("Name of the scene to load when the player lands.")]
    [SerializeField] private string planetSceneName = "PlanetScene";

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is on the Player layer
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Show the landing UI
            if (planetLandingUI != null)
            {
                planetLandingUI.SetActive(true);
            }
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // Hide the landing UI
            if (planetLandingUI != null)
            {
                planetLandingUI.SetActive(false);
            }
            playerInRange = false;
        }
    }

    private void Update()
    {
        // Check if the player is in range and presses L
        if (playerInRange && Input.GetKeyDown(KeyCode.L))
        {
            if (GameManager.Instance != null)
            {
                // Call the GameManager to load the new planet scene
                GameManager.Instance.LoadPlanetScene(planetSceneName);
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null. Make sure GameManager is in the scene.");
            }
        }
    }
}