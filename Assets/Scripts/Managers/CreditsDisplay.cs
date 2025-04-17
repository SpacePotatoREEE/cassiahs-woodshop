using UnityEngine;
using TMPro;

public class CreditsDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditsText;

    private void Awake()
    {
        if (creditsText == null) creditsText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // Subscribe
        if (GameManager.Instance != null)
            GameManager.Instance.OnCreditsChanged += Refresh;
        
        // Show current value immediately
        if (GameManager.Instance != null)
            Refresh(GameManager.Instance.GetCreditsForUI());   // helper below
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leak warnings
        if (GameManager.Instance != null)
            GameManager.Instance.OnCreditsChanged -= Refresh;
    }

    private void Refresh(int value)
    {
        if (creditsText != null)
            creditsText.text = $"₡ {value:n0}";
    }
}