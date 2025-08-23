using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class JumpController : MonoBehaviour
{
    [Header("Brake Phase")]
    [SerializeField] private float brakeTargetSpeed = 5f;
    [SerializeField] private float brakeDuration    = 2f;

    [Header("Spool-Up Phase")]
    [SerializeField] private float spoolUpTime  = 1f;
    [SerializeField] private float launchSpeed  = 120f;

    [Header("Arrival")]
    [SerializeField] private float arrivalOffset = 100f;

    [Header("Energy")]
    [SerializeField] private float jumpEnergyCost = 10f;

    [Header("References")]
    [SerializeField] private MonoBehaviour movementScript; // ShipDriftController

    private Rigidbody rb;
    private bool jumping;
    private Queue<StarSystemData> route = new();
    private GalaxyMapController   map;
    private Vector3 approachDir;
    private PlayerStats stats;

    private void Awake()
    {
        rb    = GetComponent<Rigidbody>();
        stats = GetComponent<PlayerStats>();
        if (!movementScript) movementScript = GetComponent<MonoBehaviour>();
    }

    private void Update()
    {
        if (jumping) return;
        if (!RefreshRoute() || route.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            if (stats != null && !stats.HasEnoughEnergy(jumpEnergyCost))
            {
                Debug.LogWarning("[JumpController] Not enough energy for jump!");
                return;
            }
            StartCoroutine(JumpSequence());
        }
    }

    private bool RefreshRoute()
    {
        if (!map) map = FindObjectOfType<GalaxyMapController>(true);
        if (!map) return false;

        var fi   = typeof(GalaxyMapController).GetField("activeRoute",
                   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = fi?.GetValue(map) as List<StarSystemData>;

        if (list == null || list.Count < 2) { route.Clear(); return false; }

        route.Clear();
        for (int i = 1; i < list.Count; i++) route.Enqueue(list[i]);
        return true;
    }

    private IEnumerator JumpSequence()
    {
        jumping = true;
        movementScript.enabled = false;

        StarSystemData nextSys = route.Peek();
        approachDir = ComputeJumpDirection(nextSys).normalized;

        // 1) Brake & rotate
        Quaternion targetRot = Quaternion.LookRotation(approachDir, Vector3.up);
        Vector3    startVel  = rb.linearVelocity;
        float      t         = 0f;

        while (t < brakeDuration)
        {
            t += Time.deltaTime;
            float lerp = t / brakeDuration;

            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.deltaTime * 5f));
            float tgtSpd = Mathf.Lerp(startVel.magnitude, brakeTargetSpeed, lerp);
            rb.linearVelocity = rb.linearVelocity.normalized * tgtSpd;
            yield return null;
        }
        rb.linearVelocity = -transform.forward * brakeTargetSpeed;

        // 2) Spool up & drain energy
        float energyStart  = stats.CurrentEnergy;
        float energyTarget = energyStart - jumpEnergyCost;
        t = 0f;

        while (t < spoolUpTime)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / spoolUpTime);

            float spd = Mathf.Lerp(brakeTargetSpeed, launchSpeed, frac);
            rb.linearVelocity = -transform.forward * spd;

            stats.CurrentEnergy = Mathf.Lerp(energyStart, energyTarget, frac);
            yield return null;
        }
        stats.CurrentEnergy = energyTarget;

        // 3) Switch scenes additively through GameManager (keep Main)
        if (string.IsNullOrWhiteSpace(nextSys.sceneName))
        {
            Debug.LogError($"[JumpController] Next system '{nextSys?.displayName}' has no sceneName.");
            Restore(); yield break;
        }

        DontDestroyOnLoad(gameObject);             // keep ship while switching
        GameManager.Instance?.SwitchToWorldScene(nextSys.sceneName);

        // Finish arrival after one frame (scene will be active now)
        StartCoroutine(FinishArrivalNextFrame());
    }

    private IEnumerator FinishArrivalNextFrame()
    {
        yield return null;

        Vector3 dir = new Vector3(approachDir.x, 0f, approachDir.z).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

        rb.position      = dir * arrivalOffset;
        transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
        rb.linearVelocity  = transform.forward * launchSpeed;

        if (route.Count > 0) route.Dequeue();
        map?.RemoveFirstHopFromActiveRoute();

        Restore();
    }

    private Vector3 ComputeJumpDirection(StarSystemData next)
    {
        StarSystemData cur = GameManager.Instance?.CurrentSystem;
        if (cur == null || next == null) return -transform.forward;
        Vector2 d = (next.mapPosition - cur.mapPosition).normalized;
        return new Vector3(d.x, 0f, d.y);
    }

    private void Restore()
    {
        movementScript.enabled = true;
        jumping = false;
    }
}
