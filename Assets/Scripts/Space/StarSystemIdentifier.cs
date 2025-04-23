using UnityEngine;

/// <summary>
/// Drops a reference to the StarSystemData that represents this scene.
/// GameManager reads it when saving and when a scene loads at runtime.
/// </summary>
public class StarSystemIdentifier : MonoBehaviour
{
    public StarSystemData starSystem;

    private void Awake()
    {
        if (starSystem == null)
            Debug.LogError("[StarSystemIdentifier] StarSystemData reference missing!", this);

        // Let GameManager cache us automatically
        GameManager.Instance?.RegisterCurrentSystem(starSystem);
    }
}