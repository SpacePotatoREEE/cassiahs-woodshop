using UnityEngine;

public class PlanetLandingTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject planetLandingUI;

    [Header("Scene to Load")]
    [SerializeField] private string planetSceneName = "PlanetScene";
    
    [Header("String to trigger UI")]
    [SerializeField] private string layerTriggerString = "PlayerShip";

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            if (planetLandingUI != null)
                planetLandingUI.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            if (planetLandingUI != null)
                planetLandingUI.SetActive(false);
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.L))
        {
            // Make sure we have a valid GameManager
            if (GameManager.Instance != null)
            {
                // 1) Save the game before landing
                GameManager.Instance.SaveGame();

                // 2) Now load the planet scene
                GameManager.Instance.LoadPlanetScene(planetSceneName);
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null. Make sure GameManager is in the scene.");
            }
        }
    }
}