using UnityEngine;

public class UITriggerZone : MonoBehaviour
{
    [Header("UI to Toggle")]
    [SerializeField] private GameObject uiPanel;

    [Header("Optional Settings")]
    [Tooltip("What layer is considered 'Player'? If empty, any object can trigger.")]
    [SerializeField] private string playerLayerName = "Player";

    private bool playerInRange = false;

    private void Start()
    {
        // Ensure the UI starts hidden (optional).
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // If we want to check a specific layer
        if (!string.IsNullOrEmpty(playerLayerName))
        {
            if (other.gameObject.layer == LayerMask.NameToLayer(playerLayerName))
            {
                ShowUI();
                playerInRange = true;
            }
        }
        else
        {
            // If no specific layer set, any collider triggers
            ShowUI();
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // If checking a specific layer
        if (!string.IsNullOrEmpty(playerLayerName))
        {
            if (other.gameObject.layer == LayerMask.NameToLayer(playerLayerName))
            {
                HideUI();
                playerInRange = false;
            }
        }
        else
        {
            // If no specific layer set, any collider
            HideUI();
            playerInRange = false;
        }
    }

    private void ShowUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }
    }

    private void HideUI()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }
}