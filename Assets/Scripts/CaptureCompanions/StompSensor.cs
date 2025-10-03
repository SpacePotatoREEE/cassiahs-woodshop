using UnityEngine;

[DisallowMultipleComponent]
public class StompSensor : MonoBehaviour
{
    [Tooltip("Leave empty to auto-find on parent.")]
    public StompableEnemy stompable;

    [Tooltip("Only these layers can stomp (your Player layer).")]
    public LayerMask playerLayers;

    public bool debugLogs = true;

    void Awake()
    {
        if (!stompable) stompable = GetComponentInParent<StompableEnemy>();
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & playerLayers) == 0) return;

        // Best-available vertical speed
        Vector3 vel = Vector3.down;
        var rb = other.attachedRigidbody;
        if (rb) vel = rb.linearVelocity;
        else { var cc = other.GetComponent<CharacterController>(); if (cc) vel = cc.velocity; }

        if (debugLogs) Debug.Log($"[StompSensor] OnTriggerEnter by {other.name}, velY={vel.y:F2}", this);

        if (!stompable) { Debug.LogWarning("[StompSensor] No StompableEnemy found in parent.", this); return; }

        if (debugLogs) Debug.Log("[StompSensor] -> forwarding to StompableEnemy.TryStomp", this);
        stompable.TryStomp(other.gameObject, vel);
    }
}