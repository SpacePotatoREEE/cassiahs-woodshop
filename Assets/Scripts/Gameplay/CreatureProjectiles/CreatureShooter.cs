using UnityEngine;
using System.Collections;

public enum ShootPattern { Single, Burst, Shotgun, Circle, Spiral, Wave, AimedBurst }

/// <summary>Attach to a creature. Spawns bullets via ProjectileManager.</summary>
public class CreatureShooter : MonoBehaviour
{
    [Header("Orb Stats (exposed per-creature)")]
    public Color orbColor = Color.red;
    [Min(0)]     public int   orbDamage   = 5;
    [Min(0.01f)] public float orbSpeed    = 12f;
    [Min(0.02f)] public float orbRadius   = 0.10f;
    [Min(0.1f)]  public float orbLifetime = 6f;

    [Header("Firing")]
    public ShootPattern pattern = ShootPattern.Single;
    [Min(0.05f)] public float fireInterval = 0.5f;
    public int   patternCount = 6;
    [Range(0f, 360f)] public float spreadAngle = 20f;
    public float angularSpeed = 120f;
    public float circleOffset = 0.2f;

    [Header("Targeting")]
    public Transform fireMuzzle;            // optional; if null, origin uses height offset
    public Transform target;                // assigned by aggro or auto-found
    public bool      leadTarget = false;

    [Header("Spawn Offsets")]
    public float spawnHeightOffset  = 0.6f;
    public float spawnForwardOffset = 0.4f;

    [Header("Debug")]
    public bool debugLogs = false;

    private bool  _firing;
    private float _angleAcc;

    void Start()
    {
        if (!target)
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged) target = tagged.transform;
        }
    }

    public void BeginFiring()
    {
        if (_firing) return;
        _firing = true;
        if (debugLogs) Debug.Log($"[{name}] BeginFiring()");
        StartCoroutine(FireLoop());
    }

    public void StopFiring()
    {
        if (!_firing) return;
        _firing = false;
        if (debugLogs) Debug.Log($"[{name}] StopFiring()");
        StopAllCoroutines();
    }

    private IEnumerator FireLoop()
    {
        if (ProjectileManager.Instance == null)
        {
            Debug.LogWarning($"[{name}] No ProjectileManager in the scene.");
            yield break;
        }
        var wait = new WaitForSeconds(Mathf.Max(0.05f, fireInterval));
        while (_firing) { FireOnce(); yield return wait; }
    }

    private void FireOnce()
    {
        if (ProjectileManager.Instance == null) return;

        Vector3 origin = fireMuzzle ? fireMuzzle.position : (transform.position + Vector3.up * spawnHeightOffset);
        Vector3 aimDir = target ? (target.position - origin).normalized : transform.forward;

        // little push so the bullet doesn't start inside colliders
        origin += aimDir * spawnForwardOffset;

        switch (pattern)
        {
            case ShootPattern.Single:
                Spawn(origin, aimDir); break;

            case ShootPattern.Burst:
                StartCoroutine(BurstRoutine(origin, aimDir, Mathf.Max(3, patternCount), 0.08f));
                break;

            case ShootPattern.Shotgun:
                Shotgun(origin, aimDir, Mathf.Max(3, patternCount), spreadAngle);
                break;

            case ShootPattern.Circle:
                Circle(origin, Mathf.Max(6, patternCount));
                break;

            case ShootPattern.Spiral:
                _angleAcc += angularSpeed * Time.deltaTime;
                Spawn(origin, Quaternion.Euler(0f, _angleAcc, 0f) * Vector3.forward);
                break;

            case ShootPattern.Wave:
                float ang = Mathf.Sin(Time.time * (angularSpeed * Mathf.Deg2Rad)) * (spreadAngle * 0.5f);
                Spawn(origin, Quaternion.Euler(0f, ang, 0f) * aimDir);
                break;

            case ShootPattern.AimedBurst:
                AimedBurst(origin, Mathf.Max(5, patternCount), 360f);
                break;
        }

        if (debugLogs) Debug.Log($"[{name}] Spawn orb at {origin} dir {aimDir}");
    }

    private void Spawn(Vector3 origin, Vector3 dir)
    {
        ProjectileManager.Instance.SpawnProjectile(origin, dir, orbSpeed, orbDamage, orbRadius, orbColor, orbLifetime);
    }

    private IEnumerator BurstRoutine(Vector3 origin, Vector3 dir, int count, float stepDelay)
    {
        for (int i = 0; i < count; i++) { Spawn(origin, dir); yield return new WaitForSeconds(stepDelay); }
    }

    private void Shotgun(Vector3 origin, Vector3 dir, int pellets, float totalSpread)
    {
        if (pellets <= 1) { Spawn(origin, dir); return; }
        float half = totalSpread * 0.5f;
        for (int i = 0; i < pellets; i++)
        {
            float t = pellets == 1 ? 0f : (i / (float)(pellets - 1));
            float yaw = Mathf.Lerp(-half, half, t);
            Spawn(origin, Quaternion.Euler(0f, yaw, 0f) * dir);
        }
    }

    private void Circle(Vector3 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * 360f;
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
            Spawn(origin + dir * circleOffset, dir);
        }
    }

    private void AimedBurst(Vector3 origin, int count, float ringDeg)
    {
        if (!target) { Circle(origin, count); return; }

        Vector3 toTarget = (target.position - origin).normalized;
        Vector3 up = Vector3.up;

        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            float ang = (t * ringDeg) - (ringDeg * 0.5f);
            Vector3 dir = Quaternion.AngleAxis(ang, up) * toTarget;
            Spawn(origin, dir);
        }
    }
}
