using UnityEngine;

public class FreezeHotkeyDemo : MonoBehaviour
{
    [SerializeField] private CaptureFreezeOverlay overlay;

    private void Awake()
    {
        if (!overlay)
        {
            // Finds inactive components too, just in case.
            overlay = FindObjectOfType<CaptureFreezeOverlay>(true);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (overlay)
            {
                overlay.PlayCutIn(
                    "Capturing…",
                    onFrozenReady: null,
                    onFinish:      null
                );
            }
            else
            {
                // Fallback: just freeze/resume to prove the global pause works
                GameManager.Instance?.PauseGame();
                StartCoroutine(UnfreezeSoon());
            }
        }
    }

    private System.Collections.IEnumerator UnfreezeSoon()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        GameManager.Instance?.ResumeGame();
    }
}