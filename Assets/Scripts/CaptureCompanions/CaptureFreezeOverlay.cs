using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CaptureFreezeOverlay : MonoBehaviour
{
    [Header("Overlay Roots")]
    [SerializeField] private CanvasGroup overlayGroup;
    [SerializeField] private RectTransform ballVisual;     // optional UI ball
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("Timings (unscaled)")]
    [SerializeField] private float fadeInTime   = 0.12f;
    [SerializeField] private float fadeOutTime  = 0.12f;

    [Header("UI Ball Shake (optional)")]
    [SerializeField] private float uiShakeTime   = 0.18f;
    [SerializeField] private float uiShakeAmount = 10f;

    [Header("Behaviour")]
    [SerializeField] private bool hideOnAwake = true;

    private Vector2 _ballHomePos;
    private Coroutine _active;

    void Awake()
    {
        if (!overlayGroup) overlayGroup = GetComponentInChildren<CanvasGroup>(true);
        if (ballVisual) _ballHomePos = ballVisual.anchoredPosition;
        if (hideOnAwake) HideInstant(); else ShowInstant();
    }

    void OnEnable()  { CaptureOverlayHub.Register(this); }
    void OnDisable() { CaptureOverlayHub.Unregister(this); }
    void OnDestroy() { CaptureOverlayHub.Unregister(this); }

    /// <summary>
    /// Freeze gameplay and run custom actions while frozen.
    /// If playUiShakes = true, we animate the small UI ball 3 times as a flair.
    /// </summary>
    public void PlayCutIn(string labelText, System.Action onFrozenReady, System.Action onFinish, bool playUiShakes = false)
    {
        if (_active != null) StopCoroutine(_active);
        _active = StartCoroutine(Sequence(labelText, onFrozenReady, onFinish, playUiShakes));
    }

    private IEnumerator Sequence(string labelText, System.Action onFrozenReady, System.Action onFinish, bool playUiShakes)
    {
        GameManager.Instance?.PauseGame();

        overlayGroup.gameObject.SetActive(true);
        overlayGroup.alpha = 0f;
        if (statusLabel) statusLabel.text = labelText;

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Clamp01(t / fadeInTime);
            yield return null;
        }

        onFrozenReady?.Invoke();

        if (playUiShakes && ballVisual)
        {
            for (int i = 0; i < 3; i++)
            {
                yield return Shake(ballVisual, uiShakeTime, uiShakeAmount);
                float wait = 0.10f, w = 0f;
                while (w < wait) { w += Time.unscaledDeltaTime; yield return null; }
            }
            ballVisual.anchoredPosition = _ballHomePos;
        }

        onFinish?.Invoke();

        t = 0f;
        while (t < fadeOutTime)
        {
            t += Time.unscaledDeltaTime;
            overlayGroup.alpha = 1f - Mathf.Clamp01(t / fadeOutTime);
            yield return null;
        }
        HideInstant();

        GameManager.Instance?.ResumeGame();
        _active = null;
    }

    private IEnumerator Shake(RectTransform target, float duration, float amplitudePx)
    {
        Vector2 start = target.anchoredPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.PerlinNoise(t * 20f, 0.123f) * 2f - 1f;
            target.anchoredPosition = start + new Vector2(n * amplitudePx, 0f);
            yield return null;
        }
        target.anchoredPosition = start;
    }

    private void HideInstant()
    {
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = false;
        overlayGroup.blocksRaycasts = false;
        if (!overlayGroup.gameObject.activeSelf) overlayGroup.gameObject.SetActive(true);
    }

    private void ShowInstant()
    {
        overlayGroup.alpha = 0f;
        overlayGroup.interactable = true;
        overlayGroup.blocksRaycasts = true;
        if (!overlayGroup.gameObject.activeSelf) overlayGroup.gameObject.SetActive(true);
    }
}
