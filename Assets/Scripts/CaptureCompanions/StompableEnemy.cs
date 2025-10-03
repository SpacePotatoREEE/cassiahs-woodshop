using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth_UnitStats))]
public class StompableEnemy : MonoBehaviour
{
    [Header("Stomp Settings")]
    public float stompDamage = 20f;

    [Tooltip("Require at least this downward Y velocity to count as a stomp. Small negative like -0.05 is forgiving.")]
    public float minDownwardVel = -0.05f;

    [Header("Bounce")]
    [Tooltip("If the player has TopDownCharacterController, we'll call ForceJump with this Y velocity.")]
    public float bounceUpVelocity = 14f;

    [Tooltip("If true, prefer using TopDownCharacterController.ForceJump over rigidbody/adapter.")]
    public bool preferTopDownControllerBounce = true;

    public float stompCooldown = 0.15f;
    public bool debugLogs = true;

    [Header("Effects (optional)")]
    public HitFlash hitFlash;                         // set or auto-find
    public SquishGraphController squishController;    // set or auto-find

    private EnemyHealth_UnitStats _hp;
    private float _lastStompTime = -999f;

    void Awake()
    {
        _hp = GetComponent<EnemyHealth_UnitStats>();
        if (!hitFlash)         hitFlash         = GetComponentInChildren<HitFlash>(true);
        if (!squishController) squishController = GetComponentInChildren<SquishGraphController>(true);
    }

    public void TryStomp(GameObject playerGO, Vector3 playerVelocityWorld)
    {
        if (Time.time - _lastStompTime < stompCooldown) return;

        if (debugLogs) Debug.Log($"[Stomp] TryStomp velY={playerVelocityWorld.y:F2}", this);

        if (playerVelocityWorld.y > minDownwardVel)
        {
            if (debugLogs) Debug.Log($"[Stomp] Rejected: not falling enough. velY={playerVelocityWorld.y:F2} > {minDownwardVel}", this);
            return;
        }

        _lastStompTime = Time.time;

        // 1) Damage
        Vector3 hitPoint = _hp.GetAimPoint().position;
        bool killed = _hp.ApplyDamage(stompDamage, hitPoint, Vector3.up, playerGO);
        if (debugLogs)
        {
            float cur = _hp.GetCurrentHP();
            float max = Mathf.Max(1f, _hp.MaxHP);
            Debug.Log($"[Stomp] Damaged enemy for {stompDamage}. Killed={killed} | HP now {cur}/{max}", this);
        }

        // 2) FX
        if (hitFlash) { hitFlash.FlashOnce(); if (debugLogs) Debug.Log("[Stomp] Flash (HitFlash found)", this); }
        else if (debugLogs) Debug.LogWarning("[Stomp] No HitFlash on this enemy.", this);

        if (squishController) { squishController.PlaySquish(); if (debugLogs) Debug.Log("[Stomp] Squish (SquishGraphController found)", this); }
        else if (debugLogs) Debug.LogWarning("[Stomp] No SquishGraphController on this enemy.", this);

        // 3) Bounce player — prefer your TopDownCharacterController
        bool bounced = false;

        if (preferTopDownControllerBounce)
        {
            var tdc = playerGO.GetComponentInParent<TopDownCharacterController>();
            if (tdc != null)
            {
                float v = bounceUpVelocity <= 0f ? tdc.JumpInitialVelocity : bounceUpVelocity;
                tdc.ForceJump(v);
                if (debugLogs) Debug.Log($"[Stomp] Bounced via TopDownCharacterController.ForceJump({v})", this);
                bounced = true;
            }
        }

        if (!bounced)
        {
            var rb = playerGO.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 v = rb.linearVelocity;
                v.y = Mathf.Max(v.y, bounceUpVelocity);
                rb.linearVelocity = v;
                if (debugLogs) Debug.Log($"[Stomp] Bounced RB: new velY={rb.linearVelocity.y:F2}", this);
                bounced = true;
            }
        }

        if (!bounced)
        {
            var bounce = playerGO.GetComponent<IStompBounce>();
            if (bounce != null)
            {
                bounce.AddVerticalImpulse(bounceUpVelocity);
                if (debugLogs) Debug.Log($"[Stomp] Bounced via IStompBounce: +{bounceUpVelocity}", this);
                bounced = true;
            }
        }

        if (!bounced)
            Debug.LogWarning("[Stomp] No suitable bounce receiver found on player (no TopDownCharacterController, Rigidbody, or IStompBounce).", this);
    }
}

/// Optional interface for non-RB controllers in other contexts.
public interface IStompBounce
{
    void AddVerticalImpulse(float velUp);
}
