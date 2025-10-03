using UnityEngine;
using System.Collections;

public class CaptureBall : MonoBehaviour
{
    [Header("Flight")]
    public float speed = 20f;
    public float hitDistance = 0.8f;
    public float arcHeight = 1.25f;   // small arc

    [Header("Visuals")]
    public Transform ballVisual;

    // runtime
    private Transform _owner;
    private ICapturable _target;
    private CompanionRoster _roster;
    private bool _active;

    // for restoring on fail
    private Transform _tgtRoot;
    private Vector3   _tgtOrigPos, _tgtOrigScale;

    void Awake()
    {
        if (!ballVisual) ballVisual = transform;
    }

    public void Launch(Transform owner, ICapturable target, CompanionRoster roster)
    {
        _owner  = owner;
        _target = target;
        _roster = roster;
        _active = true;

        if (_target == null)
        {
            Debug.LogWarning("[CaptureBall] Launched with null target; destroying.");
            Destroy(gameObject);
            return;
        }

        _tgtRoot = _target.GetCaptureRoot();
        if (_tgtRoot)
        {
            _tgtOrigPos   = _tgtRoot.position;
            _tgtOrigScale = _tgtRoot.localScale;
        }
    }

    void Update()
    {
        if (!_active || _target == null) return;

        var tgtMB = (MonoBehaviour)_target;
        if (!tgtMB || !tgtMB.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = _target.GetCaptureRoot().position;
        Vector3 to = targetPos - transform.position;
        float dist = to.magnitude;
        if (dist <= hitDistance)
        {
            _active = false;
            StartCoroutine(CaptureSequence());
            return;
        }

        // move with a light arc
        Vector3 dir = to / (dist + 1e-5f);
        Vector3 step = dir * speed * Time.deltaTime;
        transform.position += step;

        // arc offset based on distance progress
        float total = (targetPos - (_owner ? _owner.position : transform.position)).magnitude + 1e-5f;
        float traveled = (transform.position - (_owner ? _owner.position : transform.position)).magnitude;
        float t = Mathf.Clamp01(traveled / total);
        float height = Mathf.Sin(t * Mathf.PI) * arcHeight;
        transform.position = new Vector3(transform.position.x, transform.position.y + height * Time.deltaTime, transform.position.z);

        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    IEnumerator CaptureSequence()
    {
        var overlay = CaptureOverlayHub.Current;

        // Freeze the world using overlay (no UI shakes here; we shake the real ball)
        if (overlay != null)
        {
            bool finished = false;
            overlay.PlayCutIn("Capturing…",
                onFrozenReady: () => StartCoroutine(DoCaptureFlow()),
                onFinish:      () => { finished = true; },
                playUiShakes:  false
            );
            // wait until overlay resumes (finished set inside)
            while (!finished) yield return null;
        }
        else
        {
            // Headless fallback
            GameManager.Instance?.PauseGame();
            yield return DoCaptureFlow();
            GameManager.Instance?.ResumeGame();
        }

        Destroy(gameObject);
    }

    private IEnumerator DoCaptureFlow()
    {
        // 1) Start capture (disable enemy colliders/AI)
        _target.OnCaptureStart();

        // 2) Snap ball on target a bit above root
        transform.position = _target.GetCaptureRoot().position + Vector3.up * 0.2f;

        // 3) Suck the target into the ball (scale to zero, move to ball)
        yield return SuckInTargetUnscaled(_tgtRoot);

        // 4) BALL SHAKES (3 times, unscaled)
        yield return ShakeBallUnscaled(3, 0.18f, 12f);

        // 5) Decide result
        bool success = Random.value < Mathf.Clamp01(_target.GetCaptureChance01());

        if (success)
        {
            Debug.Log("[Capture] Success!");
            // 6) Reveal creature center-screen
            var def = _target.GetCreatureDefinition();
            var cam = Camera.main;
            if (def && def.companionPrefab && cam)
                yield return CaptureReveal3D.Play(def.companionPrefab, cam, 0.35f, 0.9f, 0.4f, 2.2f, 1f);

            // 7) Add to roster
            if (_roster && def) _roster.AddCaptured(def);
            // remove enemy
            var tgtMB = (MonoBehaviour)_target;
            if (tgtMB) Destroy(tgtMB.gameObject);
        }
        else
        {
            Debug.Log("[Capture] Broke free!");
            // 6) Restore enemy back out of the ball
            yield return RestoreTargetUnscaled(_tgtRoot);
            // 7) Ball returns to owner (normal time will resume after overlay)
            if (_owner) StartCoroutine(ReturnToOwner());
            _target.OnCaptureComplete(false);
        }
    }

    private IEnumerator ReturnToOwner()
    {
        // short return after fail
        float t = 0f, dur = 0.35f;
        Vector3 start = transform.position;
        Vector3 end = _owner.position + Vector3.up * 1.4f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / dur);
            transform.position = Vector3.Lerp(start, end, u);
            yield return null;
        }
    }

    private IEnumerator ShakeBallUnscaled(int shakes, float eachDuration, float amount)
    {
        for (int i = 0; i < shakes; i++)
        {
            float t = 0f;
            Quaternion start = ballVisual ? ballVisual.localRotation : Quaternion.identity;
            while (t < eachDuration)
            {
                t += Time.unscaledDeltaTime;
                float n = (Mathf.PerlinNoise(t * 28f, 0.23f) * 2f - 1f);
                if (ballVisual) ballVisual.localRotation = start * Quaternion.Euler(0f, 0f, n * amount);
                yield return null;
            }
            if (ballVisual) ballVisual.localRotation = start;
            float pause = 0.1f, w = 0f;
            while (w < pause) { w += Time.unscaledDeltaTime; yield return null; }
        }
    }

    private IEnumerator SuckInTargetUnscaled(Transform targetRoot)
    {
        if (!targetRoot) yield break;

        Vector3 startScale = targetRoot.localScale;
        Vector3 endScale   = Vector3.zero;
        Vector3 startPos   = targetRoot.position;
        Vector3 endPos     = transform.position;

        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / dur);
            targetRoot.localScale = Vector3.Lerp(startScale, endScale, u);
            targetRoot.position   = Vector3.Lerp(startPos, endPos, u);
            yield return null;
        }
    }

    private IEnumerator RestoreTargetUnscaled(Transform targetRoot)
    {
        if (!targetRoot) yield break;

        Vector3 startScale = targetRoot.localScale;
        Vector3 endScale   = _tgtOrigScale;
        Vector3 startPos   = targetRoot.position;
        Vector3 endPos     = _tgtOrigPos;

        float t = 0f, dur = 0.3f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / dur);
            targetRoot.localScale = Vector3.Lerp(startScale, endScale, u);
            targetRoot.position   = Vector3.Lerp(startPos, endPos, u);
            yield return null;
        }
    }
}
