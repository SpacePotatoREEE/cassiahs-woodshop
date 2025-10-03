using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
[DefaultExecutionOrder(10000)] // run very late so we beat most scripts/Animator
public class SquishGraphController : MonoBehaviour
{
    [Header("Target (set to the mesh/visual root with SkinnedMeshRenderer)")]
    public Transform target;

    [Header("Curves")]
    public AnimationCurve yScaleCurve;   // vertical (squish)
    public AnimationCurve xzScaleCurve;  // horizontal (stretch)

    [Header("Timing & Style")]
    public float duration = 0.35f;

    [Tooltip(">1 exaggerates the curve delta. Uses LerpUnclamped (no cap).")]
    public float intensity = 1.0f;

    public bool useUnscaledTime = false;

    [Tooltip("Write scale in LateUpdate so Animator/other scripts don't overwrite it.")]
    public bool applyInLateUpdate = true;

    [Header("Debug")]
    public bool debugLogs = false;

    private Coroutine _anim;
    private bool _playing;
    private Vector3 _baseScale;
    private Vector3 _frameScale;

    void Reset()
    {
        yScaleCurve = new AnimationCurve(
            new Keyframe(0.00f, 1.00f,  0f,   -8f),
            new Keyframe(0.05f, 0.72f, -4f,   8f),
            new Keyframe(0.12f, 1.12f,  4f,  -4f),
            new Keyframe(0.20f, 0.96f, -2f,   2f),
            new Keyframe(0.30f, 1.03f,  2f,  -2f),
            new Keyframe(0.40f, 1.00f,  0f,   0f)
        );
        xzScaleCurve = new AnimationCurve(
            new Keyframe(0.00f, 1.00f, 0f,   6f),
            new Keyframe(0.05f, 1.22f, 4f,  -6f),
            new Keyframe(0.12f, 0.92f, -4f,  3f),
            new Keyframe(0.20f, 1.04f,  2f, -2f),
            new Keyframe(0.30f, 0.99f, -2f,  2f),
            new Keyframe(0.40f, 1.00f,  0f,  0f)
        );
    }

    void Awake()
    {
        if (!target) target = transform;
        if (!target) Debug.LogWarning("[Squish] No target set; squish will do nothing.", this);
        if (yScaleCurve == null || yScaleCurve.length == 0) Reset();
        if (xzScaleCurve == null || xzScaleCurve.length == 0) Reset();
    }

    void LateUpdate()
    {
        if (applyInLateUpdate && _playing && target)
            target.localScale = _frameScale;
    }

    [ContextMenu("Test Squish")]
    public void TestSquish() => PlaySquish();

    public void PlaySquish()
    {
        if (!target) { Debug.LogWarning("[Squish] Missing target.", this); return; }
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        _playing = true;
        _baseScale = target.localScale;
        float t = 0f;
        if (debugLogs) Debug.Log($"[Squish] Begin @ baseScale={_baseScale}, intensity={intensity}", this);

        while (t < duration)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);

            // UNCLAMPED: amplify delta beyond 1 if intensity > 1
            float sY  = Mathf.LerpUnclamped(1f, yScaleCurve.Evaluate(u),  intensity);
            float sXZ = Mathf.LerpUnclamped(1f, xzScaleCurve.Evaluate(u), intensity);

            _frameScale = new Vector3(_baseScale.x * sXZ, _baseScale.y * sY, _baseScale.z * sXZ);

            if (!applyInLateUpdate && target)
                target.localScale = _frameScale;

            yield return null;
        }

        if (target) target.localScale = _baseScale;
        if (debugLogs) Debug.Log($"[Squish] End (restored) baseScale={_baseScale}", this);
        _playing = false;
        _anim = null;
    }
}
