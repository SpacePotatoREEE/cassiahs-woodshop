// SpaceDustController.cs
// Keeps the VFX dust volume centred on – and moving relative to – the player ship.
// Robust against scene-load order: looks for the ship every scene-load and, if still
// missing, keeps polling each frame until it succeeds.

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(VisualEffect))]
public class SpaceDustController : MonoBehaviour
{
    /* ─────────────── SEARCH SETTINGS ─────────────── */
    [Header("Player Search")]
    [Tooltip("Layer that the player ship uses (Project Settings ▶︎ Tags & Layers).")]
    [SerializeField] private string playerLayerName = "PlayerShip";

    /* ─────────────── DUST SETTINGS ─────────────── */
    [Header("Dust Volume")]
    [Tooltip("Half-size of the cubic field that follows the player.")]
    [SerializeField] private float followRadius = 250f;

    /* ─────────────── INTERNAL STATE ─────────────── */
    private VisualEffect vfx;
    private Transform    player;
    private Rigidbody    playerRb;

    private static readonly int PlayerVelId = Shader.PropertyToID("PlayerVelocity");
    private int playerLayer = -1;          // cache the layer index so we don’t hash every frame
    
    private Vector3 lastPos;   // add this field

    /* ─────────────── LIFECYCLE ─────────────── */
    private void Awake()
    {
        vfx         = GetComponent<VisualEffect>();
        playerLayer = LayerMask.NameToLayer(playerLayerName);

        if (playerLayer == -1)
            Debug.LogWarning($"[SpaceDustController] Layer '{playerLayerName}' not found. " +
                             $"Check Project Settings ▶︎ Tags & Layers.");

        TryCachePlayer();                  // first attempt
        SceneManager.sceneLoaded += OnSceneLoaded;   // retry when any scene finishes loading
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __) => TryCachePlayer();

    /* ─────────────── MAIN UPDATE ─────────────── */
    private void LateUpdate()
    {
        if (player == null) TryCachePlayer();
        if (player == null) return;

        // 1) centre the cube
        transform.position = player.position;

        // 2) choose a velocity source
        Vector3 vel = Vector3.zero;
        if (playerRb)
        {
            vel = playerRb.linearVelocity;          // preferred, Unity-6 safe
            if (vel.sqrMagnitude < 0.0001f)         // fallback when MovePosition was used
                vel = (player.position - lastPos) / Time.deltaTime;
        }
        vfx.SetVector3(PlayerVelId, vel);
        lastPos = player.position;                  // update for next frame
    }

    /* ─────────────── HELPER ─────────────── */
    private void TryCachePlayer()
    {
        if (playerLayer == -1 || player != null) return;    // layer invalid or already found

        foreach (var rb in FindObjectsOfType<Rigidbody>(true))        // include inactive
        {
            if (rb.gameObject.layer == playerLayer)
            {
                player   = rb.transform;
                playerRb = rb;
                Debug.Log($"[SpaceDustController] Player ship cached: {player.name}");
                return;
            }
        }
    }

#if UNITY_EDITOR
    /* Scene-view gizmo so you can see the volume */
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * followRadius * 2f);
    }
#endif
}
