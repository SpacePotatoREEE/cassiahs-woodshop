using UnityEngine;
using UnityEngine.UI;

public class WorldspaceHealthBar : MonoBehaviour
{
    public Slider slider;
    public Canvas canvas;
    public Camera targetCam; // optional; auto-finds main

    [Header("Behaviour")]
    public bool faceCamera = true;
    public bool hideUntilDamaged = true;

    private bool _shownOnce;

    void Awake()
    {
        if (!canvas) canvas = GetComponentInChildren<Canvas>(true);
        if (!slider) slider = GetComponentInChildren<Slider>(true);
        if (targetCam == null && Camera.main != null) targetCam = Camera.main;
        if (hideUntilDamaged) Hide();
    }

    void LateUpdate()
    {
        if (faceCamera && targetCam && canvas && canvas.gameObject.activeSelf)
        {
            var t = canvas.transform;
            var dir = (t.position - targetCam.transform.position).normalized;
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    public void SetValue(float current, float max, bool instant)
    {
        if (!slider) return;
        slider.maxValue = Mathf.Max(1f, max);
        slider.value = Mathf.Clamp(current, 0f, slider.maxValue);
        if (!_shownOnce && hideUntilDamaged && current < max) Show();
    }

    public void Show()
    {
        _shownOnce = true;
        if (canvas) canvas.enabled = true;
    }

    public void Hide()
    {
        if (canvas) canvas.enabled = false;
    }
}