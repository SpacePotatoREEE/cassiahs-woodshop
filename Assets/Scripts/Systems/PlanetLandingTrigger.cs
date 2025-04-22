using UnityEngine;

public class PlanetLandingTrigger : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject planetLandingUI;

    [Header("Scene to Load")]
    [SerializeField] private string planetSceneName = "PlanetScene";

    [Header("String to trigger UI")]
    [SerializeField] private string layerTriggerString = "PlayerShip";

    [Header("Extras")]
    [Tooltip("If ticked, the player is fully healed when the landing succeeds.")]
    [SerializeField] private bool healPlayerOnLand = true;          // ← NEW

    private bool playerInRange = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            if (planetLandingUI) planetLandingUI.SetActive(true);
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer(layerTriggerString))
        {
            if (planetLandingUI) planetLandingUI.SetActive(false);
            playerInRange = false;
        }
    }

    private void Update()
    {
        if (!playerInRange || !Input.GetKeyDown(KeyCode.L)) return;

        // 1) heal (optional)
        if (healPlayerOnLand)
        {
            var stats = FindObjectOfType<PlayerStats>(true);     // includes inactive
            if (stats != null) stats.FullHeal();                 // FullHeal() = set to max
        }

        // 2) save
        GameManager.Instance?.SaveGame();

        // 3) load the planet scene
        GameManager.Instance?.LoadPlanetScene(planetSceneName);
    }
}