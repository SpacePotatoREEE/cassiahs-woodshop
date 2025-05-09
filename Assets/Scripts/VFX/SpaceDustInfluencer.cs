using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;  

/// <summary>
/// Sends position & velocity to <see cref="SpaceDustManager"/> every frame.
/// • Registers as soon as the manager exists (even if spawned before it).
/// • Works with or without a non-kinematic Rigidbody.
/// </summary>
[DisallowMultipleComponent]
public class SpaceDustInfluencer : MonoBehaviour
{
    [Header("Velocity source (optional)")]
    [Tooltip("If true and no non-kinematic Rigidbody is present, velocity is " +
             "estimated from transform movement.")]
    public bool computeVelocityFromTransform = true;

    Rigidbody rb;
    Vector3   lastPos;
    bool      registered;

    /* ─────── Lifecycle ─────── */
    void Awake()
    {
        rb      = GetComponent<Rigidbody>();   // might be null
        lastPos = transform.position;
    }

    void OnEnable()  => TryRegister();
    void Start()     => TryRegister();         // catch the case manager spawns later

    void Update()
    {
        if (!registered) TryRegister();        // keep trying until success
        lastPos = transform.position;
        Debug.DrawRay(transform.position, Velocity, Color.magenta);
    }

    void OnDisable() => SpaceDustManager.Instance?.Unregister(this);

    /* ─────── Public for manager ─────── */
    public Vector3 Position => transform.position;

    public Vector3 Velocity
    {
        get
        {
            if (rb && !rb.isKinematic)
            {
                Vector3 v = rb.linearVelocity;
                if (v.sqrMagnitude > 0.0001f) return v;      // normal path
            }
            // Fallback: Δposition / Δt
            return computeVelocityFromTransform
                ? (transform.position - lastPos) / Time.deltaTime
                : Vector3.zero;
        }
        
    }

    /* ─────── Helpers ─────── */
    void TryRegister()
    {
        if (registered) return;
        if (SpaceDustManager.Instance == null) return;

        SpaceDustManager.Instance.Register(this);
        registered = true;
    }
}