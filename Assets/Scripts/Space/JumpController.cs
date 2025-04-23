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
    [SerializeField] private float spoolUpTime = 1f;
    [SerializeField] private float launchSpeed = 120f;

    [Header("Arrival")]
    [SerializeField] private float arrivalOffset = 100f;   // metres from centre

    [Header("References")]
    [SerializeField] private MonoBehaviour movementScript; // ShipDriftController

    /* ───── Internals ───── */
    private Rigidbody rb;
    private bool jumping;
    private Queue<StarSystemData> route = new();
    private GalaxyMapController   map;

    private Vector3 approachDir;      // <-- cached per jump

    /* ═════ LIFECYCLE ═════ */
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!movementScript) movementScript = GetComponent<MonoBehaviour>();
    }

    void Update()
    {
        if (jumping) return;
        if (!RefreshRoute() || route.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.J))
            StartCoroutine(JumpSequence());
    }

    /* ═════ ROUTE CACHE ═════ */
    bool RefreshRoute()
    {
        if (!map) map = FindObjectOfType<GalaxyMapController>(true);
        if (!map) return false;

        var fi   = typeof(GalaxyMapController).GetField("activeRoute",
                   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var list = fi?.GetValue(map) as List<StarSystemData>;

        if (list == null || list.Count < 2) { route.Clear(); return false; }

        route.Clear(); for (int i = 1; i < list.Count; i++) route.Enqueue(list[i]);
        return true;
    }

    /* ═════ MAIN SEQUENCE ═════ */
    IEnumerator JumpSequence()
    {
        jumping = true;
        movementScript.enabled = false;

        StarSystemData nextSys = route.Peek();
        approachDir = ComputeJumpDirection(nextSys).normalized;   // cache for arrival

        /* 1) brake & rotate so bow faces approachDir */
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

        /* 2) spool-up */
        t = 0f;
        while (t < spoolUpTime)
        {
            t += Time.deltaTime;
            float spd = Mathf.Lerp(brakeTargetSpeed, launchSpeed, t / spoolUpTime);
            rb.linearVelocity = -transform.forward * spd;
            yield return null;
        }

        /* 3) load target scene */
        if (string.IsNullOrWhiteSpace(nextSys.sceneName))
        {
            Debug.LogError($"StarSystem '{nextSys.displayName}' has empty sceneName.");
            Restore(); yield break;
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(nextSys.sceneName);
    }

    /* ═════ ARRIVAL ═════ */
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // approachDir was cached in JumpSequence
        Vector3 dir = new Vector3(approachDir.x, 0f, approachDir.z).normalized;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;   // safety

        /* 1️⃣  spawn at correct polar position (unchanged) */
        Vector3 spawnPos = dir * arrivalOffset;        // same as before
        rb.position = spawnPos;

        /* 2️⃣  rotate 180° so BOW faces planet */
        Quaternion rot = Quaternion.LookRotation(-dir, Vector3.up); // forward = −dir
        transform.rotation = rot;

        /* 3️⃣  keep velocity toward planet */
        rb.linearVelocity = transform.forward * launchSpeed;  // now forward == toward planet

        /* 4️⃣  route book-keeping */
        if (route.Count > 0) route.Dequeue();
        map?.RemoveFirstHopFromActiveRoute();

        Restore();   // re-enable manual control
    }

    /* ═════ Helpers ═════ */
    Vector3 ComputeJumpDirection(StarSystemData next)
    {
        StarSystemData cur = GameManager.Instance?.CurrentSystem;
        if (cur == null || next == null) return -transform.forward;
        Vector2 d = (next.mapPosition - cur.mapPosition).normalized;
        return new Vector3(d.x, 0f, d.y);
    }

    void Restore()
    {
        movementScript.enabled = true;
        jumping = false;
    }
}
