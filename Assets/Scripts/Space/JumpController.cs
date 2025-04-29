using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class JumpController : MonoBehaviour
{
    /* ───── Parameters ───── */
    [Header("Brake Phase")]
    [SerializeField] private float brakeTargetSpeed = 5f;
    [SerializeField] private float brakeDuration    = 2f;

    [Header("Spool-Up Phase")]
    [SerializeField] private float spoolUpTime  = 1f;   // ← we’ll drain energy over this time
    [SerializeField] private float launchSpeed = 120f;

    [Header("Arrival")]
    [SerializeField] private float arrivalOffset = 100f;

    [Header("Energy")]
    [Tooltip("Energy cost per hyperspace jump.")]
    [SerializeField] private float jumpEnergyCost = 10f;

    [Header("References")]
    [SerializeField] private MonoBehaviour movementScript; // ShipDriftController

    /* ───── Internals ───── */
    private Rigidbody rb;
    private bool      jumping;
    private Queue<StarSystemData> route = new();
    private GalaxyMapController   map;
    private Vector3   approachDir;
    private PlayerStats stats;

    /* ═════════════  LIFECYCLE  ═════════════ */
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
            // Enough energy to start?
            if (stats != null && !stats.HasEnoughEnergy(jumpEnergyCost))
            {
                Debug.LogWarning("[JumpController] Not enough energy for hyperspace jump!");
                return;
            }
            StartCoroutine(JumpSequence());
        }
    }

    /* ═════════════  ROUTE CACHE  ═════════════ */
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

    /* ═════════════  MAIN SEQUENCE  ═════════════ */
    private IEnumerator JumpSequence()
    {
        jumping = true;
        movementScript.enabled = false;

        StarSystemData nextSys = route.Peek();
        approachDir = ComputeJumpDirection(nextSys).normalized;

        /* 1) BRAKE & ROTATE (unchanged) */
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

        /* 2) SPOOL-UP -- drain energy gradually */
        float energyStart  = stats.CurrentEnergy;
        float energyTarget = energyStart - jumpEnergyCost;
        t = 0f;

        while (t < spoolUpTime)
        {
            t += Time.deltaTime;
            float frac = Mathf.Clamp01(t / spoolUpTime);

            // lerp ship speed
            float spd = Mathf.Lerp(brakeTargetSpeed, launchSpeed, frac);
            rb.linearVelocity = -transform.forward * spd;

            // lerp energy
            stats.CurrentEnergy = Mathf.Lerp(energyStart, energyTarget, frac);

            yield return null;
        }
        // ensure exact final value
        stats.CurrentEnergy = energyTarget;

        /* 3) LOAD TARGET SCENE */
        if (string.IsNullOrWhiteSpace(nextSys.sceneName))
        {
            Debug.LogError($"StarSystem '{nextSys.displayName}' has empty sceneName.");
            Restore(); yield break;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(nextSys.sceneName);
    }

    /* ═════════════  ARRIVAL  ═════════════ */
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Vector3 dir = new Vector3(approachDir.x, 0f, approachDir.z).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

        // 1) position
        rb.position = dir * arrivalOffset;

        // 2) rotate 180° so bow faces planet
        transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);

        // 3) keep velocity toward planet
        rb.linearVelocity = transform.forward * launchSpeed;

        // 4) route bookkeeping
        if (route.Count > 0) route.Dequeue();
        map?.RemoveFirstHopFromActiveRoute();

        Restore();
    }

    /* ═════════════  HELPERS  ═════════════ */
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
