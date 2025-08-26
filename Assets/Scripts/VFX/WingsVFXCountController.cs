// WingsTrailsController.cs
// Put this on your always-enabled "Wings" parent (the parent of both trail objects).

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class WingsTrailsController : MonoBehaviour
{
    public enum SyncMode
    {
        Manual,             // You call BeginGlide() / EndGlideImmediate()
        WingGameObject,     // Watch wingMesh.activeSelf
        WingRenderer        // Watch renderer.enabled (SkinnedMeshRenderer / MeshRenderer)
    }

    [Header("Detection")]
    [SerializeField] private SyncMode syncMode = SyncMode.Manual;

    [Tooltip("Wing GameObject to watch if syncMode = WingGameObject.")]
    [SerializeField] private GameObject wingMesh;

    [Tooltip("Renderer to watch if syncMode = WingRenderer (e.g., SkinnedMeshRenderer).")]
    [SerializeField] private Renderer wingRenderer;

    [Header("Trails (add both left/right)")]
    [Tooltip("VisualEffect components for your trails.")]
    [SerializeField] private List<VisualEffect> trails = new();

    [Header("VFX Graph Property")]
    [Tooltip("Exposed property name in your VFX Graph that controls count/emission.")]
    [SerializeField] private string particleCountProperty = "ParticleCount";

    [Header("Counts")]
    [SerializeField] private int flyingCount    = 50;
    [SerializeField] private int retractedCount = 0;

    [Header("Fade Out")]
    [Tooltip("Seconds to wait after ParticleCount=0 before disabling trail GameObjects.")]
    [SerializeField] private float fadeOutDelay = 5f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    private bool lastOpen;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        // Initial state detection
        lastOpen = IsWingsOpen();
        if (!lastOpen)
            DisableAllTrailsImmediate();
    }

    private void Update()
    {
        if (syncMode == SyncMode.Manual) return;

        bool open = IsWingsOpen();
        if (open != lastOpen)
        {
            lastOpen = open;
            if (open) BeginGlide();
            else      EndGlideImmediate();
        }
    }

    // ─────────────── PUBLIC API (Manual mode) ───────────────
    /// <summary>Call when glide starts (input pressed).</summary>
    public void BeginGlide()
    {
        if (logDebug) Debug.Log("[WingsTrailsController] BeginGlide()");
        CancelFade();

        foreach (var vfx in trails)
        {
            if (!vfx) continue;
            if (!vfx.gameObject.activeSelf) vfx.gameObject.SetActive(true);
            SetParticleCountSafe(vfx, flyingCount);
        }
    }

    /// <summary>
    /// Call the instant glide is released.
    /// Trails stop emitting now (ParticleCount=0), then fade, then disable.
    /// </summary>
    public void EndGlideImmediate()
    {
        if (logDebug) Debug.Log("[WingsTrailsController] EndGlideImmediate()");
        foreach (var vfx in trails)
        {
            if (!vfx) continue;
            if (!vfx.gameObject.activeSelf) vfx.gameObject.SetActive(true);
            SetParticleCountSafe(vfx, retractedCount);
        }

        StartFade();
    }

    // ─────────────── INTERNALS ───────────────
    private bool IsWingsOpen()
    {
        switch (syncMode)
        {
            case SyncMode.WingGameObject: return wingMesh && wingMesh.activeSelf;
            case SyncMode.WingRenderer:   return wingRenderer && wingRenderer.enabled;
            default:                      return false; // Manual mode: we don't auto-detect
        }
    }

    private void StartFade()
    {
        CancelFade();
        fadeRoutine = StartCoroutine(FadeAndDisable());
    }

    private void CancelFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private IEnumerator FadeAndDisable()
    {
        float t = 0f;
        while (t < fadeOutDelay)
        {
            t += Time.deltaTime;
            yield return null;
        }

        foreach (var vfx in trails)
            if (vfx) vfx.gameObject.SetActive(false);

        fadeRoutine = null;
    }

    private void DisableAllTrailsImmediate()
    {
        CancelFade();
        foreach (var vfx in trails)
            if (vfx) vfx.gameObject.SetActive(false);
    }

    private void SetParticleCountSafe(VisualEffect vfx, int count)
    {
        // Prefer int; fallback to float if graph used a float instead.
        if (vfx.HasInt(particleCountProperty))
            vfx.SetInt(particleCountProperty, count);
        else if (vfx.HasFloat(particleCountProperty))
            vfx.SetFloat(particleCountProperty, count);
        else if (logDebug)
            Debug.LogWarning($"[WingsTrailsController] Property '{particleCountProperty}' not found on {vfx.name}.");
    }
}
