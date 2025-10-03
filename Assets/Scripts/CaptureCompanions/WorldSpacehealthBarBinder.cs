using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class WorldspaceHealthBarBinder : MonoBehaviour
{
    [Header("UI")]
    public Canvas worldspaceCanvas;
    public Slider slider;
    public bool hideUntilDamaged = true;
    public bool faceCamera = true;
    public Camera targetCam;

    [Header("Source (auto-detected)")]
    public EnemyHealth_UnitStats hpStats;

    private bool _shownOnce;

    void Awake()
    {
        if (!hpStats) hpStats = GetComponentInParent<EnemyHealth_UnitStats>();
        if (!targetCam && Camera.main) targetCam = Camera.main;

        if (worldspaceCanvas && hideUntilDamaged)
            worldspaceCanvas.enabled = false;

        UpdateImmediate();
    }

    void LateUpdate()
    {
        if (faceCamera && targetCam && worldspaceCanvas && worldspaceCanvas.gameObject.activeSelf)
        {
            var t = worldspaceCanvas.transform;
            var dir = (t.position - targetCam.transform.position).normalized;
            t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        UpdateImmediate();
    }

    void UpdateImmediate()
    {
        if (!slider || !hpStats) return;

        float max = Mathf.Max(1f, hpStats.MaxHP);
        float cur = Mathf.Clamp(hpStats.GetCurrentHP(), 0f, max);

        slider.maxValue = max;
        slider.value = cur;

        if (!_shownOnce && hideUntilDamaged && cur < max && worldspaceCanvas)
        {
            worldspaceCanvas.enabled = true;
            _shownOnce = true;
        }
    }
}