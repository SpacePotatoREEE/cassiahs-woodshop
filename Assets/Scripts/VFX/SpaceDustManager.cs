// ──────────────────────────────────────────────────────────────────────────────
// SpaceDustManager.cs  –  Unity 6 • URP • VFX Graph
// Keeps a dust volume centred on the player and streams a GraphicsBuffer full
// of InfluencerData (position + velocity) to the graph every frame.
// ──────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

[DisallowMultipleComponent]
[RequireComponent(typeof(VisualEffect))]
public class SpaceDustManager : MonoBehaviour
{
    /* ─────── Settings ─────── */
    [Header("Dust volume (follows player)")]
    [Min(1)] public float followRadius = 250f;

    [Header("GraphicsBuffer")]
    [Tooltip("Maximum simultaneous influencers the buffer can hold.")]
    [Min(1)] public int maxInfluencers = 256;

    [Header("Layers")]
    [Tooltip("Layer that marks the *player* ship – the cube follows this.")]
    public string playerLayerName = "PlayerShip";

    /* ─────── Internals ─────── */
    static readonly int kBufferId = Shader.PropertyToID("InfluencerBuffer");
    static readonly int kCountId  = Shader.PropertyToID("InfluencerCount");

    public static SpaceDustManager Instance { get; private set; }

    readonly List<SpaceDustInfluencer> influencers = new();
    GraphicsBuffer buffer;
    VisualEffect   vfx;

    Transform player;
    int       playerLayer = -1;

    /* ─────── Lifecycle ─────── */
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        vfx = GetComponent<VisualEffect>();

        // ➊ Allocate the GPU buffer once
        int stride = sizeof(float) * 6; // InfluencerData = 2×float3 = 24 bytes
        buffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                                    maxInfluencers, stride);
        vfx.SetGraphicsBuffer(kBufferId, buffer);

        playerLayer = LayerMask.NameToLayer(playerLayerName);
        TryCachePlayer();
        SceneManager.sceneLoaded += (_, __) => TryCachePlayer();
    }

    void OnDestroy()
    {
        buffer?.Release();
        if (Instance == this) Instance = null;
    }

    /* ─────── Registration API ─────── */
    public void Register(SpaceDustInfluencer inf)
    {
        if (inf && !influencers.Contains(inf)) influencers.Add(inf);
    }
    public void Unregister(SpaceDustInfluencer inf) => influencers.Remove(inf);

    /* ─────── Main update ─────── */
    void LateUpdate()
    {
        if (player == null) TryCachePlayer();

        // ➋ Centre the dust cube
        if      (player != null)         transform.position = player.position;
        else if (influencers.Count > 0)  transform.position = influencers[0].Position;

        // ➌ Stream data to GPU
        int count = Mathf.Min(influencers.Count, maxInfluencers);
        var data  = new InfluencerData[count];

        for (int i = 0; i < count; i++)
        {
            data[i].position = influencers[i].Position;
            data[i].velocity = influencers[i].Velocity;
        }
        buffer.SetData(data);
        vfx.SetInt(kCountId, count);
    }

    /* ─────── Helpers ─────── */
    void TryCachePlayer()
    {
        if (player != null || playerLayer < 0) return;

        foreach (var rb in FindObjectsOfType<Rigidbody>(true))
            if (rb.gameObject.layer == playerLayer) { player = rb.transform; break; }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected() =>
        Gizmos.DrawWireCube(transform.position, Vector3.one * followRadius * 2f);
#endif
}
