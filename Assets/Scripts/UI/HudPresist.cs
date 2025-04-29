using UnityEngine;

// Attach to the root Canvas of HUD_Space
public class HudPersist : MonoBehaviour
{
    private void Awake() => DontDestroyOnLoad(gameObject);
}