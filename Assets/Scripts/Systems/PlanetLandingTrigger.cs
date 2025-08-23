using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlanetLandingTrigger : MonoBehaviour
{
    [SerializeField] private string planetSceneName = "Planet_A";
    [SerializeField] private string requiredTag = "Player";   // your ship/player tag

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(requiredTag)) return;
        if (string.IsNullOrEmpty(planetSceneName)) return;

        GameManager.Instance?.SwitchToWorldScene(planetSceneName);
    }
}