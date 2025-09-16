using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hit feedback for the player:
/// • I-frames with blink on/off
/// • White flash → red tint using emission (with intensities)
/// • Optional albedo tint alongside emission
/// • Works with URP Lit (_BaseColor) or legacy (_Color) + _EmissionColor
/// </summary>
public class PlayerHitEffects : MonoBehaviour
{
    [Header("I-Frames & Blink")]
    [SerializeField] private float invulnerabilityDuration = 3f;
    [SerializeField] private int   blinkCount = 10;

    [Header("Flash Timeline")]
    [SerializeField] private float whiteFlashDuration = 0.15f;      // white flash
    [SerializeField] private float redTintDuration   = 0.35f;       // red fade after white
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Colors")]
    [SerializeField] private Color whiteFlashColor = Color.white;
    [SerializeField] private Color redTintColor    = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("Emission Flash (recommended ON)")]
    [Tooltip("Drive _EmissionColor for white flash/tint. Strongly recommended for a readable flash on textured meshes.")]
    [SerializeField] private bool useEmission = true;
    [Tooltip("HDR intensity multiplier for the white flash (e.g. 2–6).")]
    [SerializeField] private float whiteEmissionIntensity = 3.5f;
    [Tooltip("HDR intensity multiplier for the red tint portion.")]
    [SerializeField] private float redEmissionIntensity = 1.75f;

    [Header("Albedo Tint (optional)")]
    [Tooltip("Also lerp the albedo tint toward white/red. Keep low if you just want emission to carry the flash.")]
    [Range(0f, 1f)] [SerializeField] private float albedoWhiteStrength = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float albedoRedStrength   = 0.4f;

    [Header("Camera Shake (called externally via CameraShake)")]
    [SerializeField] private float shakeAmplitude = 0.45f; // world units
    [SerializeField] private float shakeFrequency = 22f;   // Hz
    [SerializeField] private float shakeDuration  = 0.24f; // seconds

    // ─────────────────────────────────────────────────────────────

    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private float _invulnerableUntil;
    private Coroutine _blinkCo;

    private struct OrigCols
    {
        public bool hasBase; public Color baseCol;
        public bool hasCol ; public Color col;
        public bool hasEm  ; public Color em;
    }

    private readonly Dictionary<Renderer, OrigCols> _orig = new();

    private static readonly int BaseColorID     = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID         = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private const string EMISSION_KEYWORD = "_EMISSION";

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _mpb       = new MaterialPropertyBlock();

        _orig.Clear();
        foreach (var r in _renderers)
        {
            if (!r) continue;

            // Cache original colors from the sharedMaterial (the authored values)
            var mat = r.sharedMaterial;
            var c = new OrigCols();

            if (mat)
            {
                if (mat.HasProperty(BaseColorID)) { c.hasBase = true; c.baseCol = mat.GetColor(BaseColorID); }
                if (mat.HasProperty(ColorID))     { c.hasCol  = true; c.col     = mat.GetColor(ColorID);     }
                if (mat.HasProperty(EmissionColorID)) { c.hasEm = true; c.em = mat.GetColor(EmissionColorID); }
            }
            _orig[r] = c;

            // Ensure emission keyword is enabled on the *instance* at runtime if we plan to drive emission
            if (useEmission && c.hasEm)
            {
                // Using r.material instantiates a per-renderer material (okay for hit FX; count is typically small)
                var inst = r.material;
                if (inst && !inst.IsKeywordEnabled(EMISSION_KEYWORD))
                    inst.EnableKeyword(EMISSION_KEYWORD);
            }
        }
    }

    /// <summary>External entry point when the player takes a hit.</summary>
    public void OnHit()
    {
        _invulnerableUntil = Time.time + invulnerabilityDuration;

        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = StartCoroutine(BlinkAndFlashRoutine());

        // Camera shake (HitShakeExtension must be on the live vcam)
        CameraShake.Shake(shakeAmplitude, shakeFrequency, shakeDuration);
    }

    public bool IsInvulnerable() => Time.time < _invulnerableUntil;

    // ─────────────────────────────────────────────────────────────
    // Blink + (white → red) flash during i-frames

    private IEnumerator BlinkAndFlashRoutine()
    {
        int toggles     = Mathf.Max(1, blinkCount * 2); // off/on pairs
        float slice     = invulnerabilityDuration / toggles;
        float startTime = Time.time;
        bool  visible   = true;

        for (int i = 0; i < toggles; i++)
        {
            visible = !visible;
            SetVisible(visible);

            float end = Mathf.Min(Time.time + slice, _invulnerableUntil);
            while (Time.time < end)
            {
                if (visible) ApplyFlash(Time.time - startTime);
                yield return null;
            }
        }

        SetVisible(true);
        RestoreAll();
        _blinkCo = null;
    }

    /// <summary>Drive emission + optional albedo tints for white → red flash.</summary>
    private void ApplyFlash(float elapsed)
    {
        // Compute phase and strengths
        bool inWhite = elapsed < whiteFlashDuration;
        bool inRed   = !inWhite && elapsed < (whiteFlashDuration + redTintDuration);

        float whiteT = 0f, redT = 0f;
        if (inWhite)
        {
            whiteT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, whiteFlashDuration));
            whiteT = flashCurve.Evaluate(whiteT);
        }
        else if (inRed)
        {
            float t = Mathf.InverseLerp(whiteFlashDuration, whiteFlashDuration + redTintDuration, elapsed);
            // Fade out red over time (reverse curve)
            redT = flashCurve.Evaluate(1f - t);
        }

        foreach (var r in _renderers)
        {
            if (!r) continue;
            if (!_orig.TryGetValue(r, out var o)) continue;

            _mpb.Clear();

            // ALBEDO: lerp toward white, then toward red (low strength so emission carries the punch)
            if (o.hasBase || o.hasCol)
            {
                Color baseCol = o.hasBase ? o.baseCol : o.col;
                Color albedo = baseCol;

                if (inWhite && albedoWhiteStrength > 0f)
                {
                    albedo = Color.Lerp(albedo, whiteFlashColor, whiteT * albedoWhiteStrength);
                }
                else if (inRed && albedoRedStrength > 0f)
                {
                    albedo = Color.Lerp(albedo, redTintColor, redT * albedoRedStrength);
                }

                if (o.hasBase) _mpb.SetColor(BaseColorID, albedo);
                else           _mpb.SetColor(ColorID, albedo);
            }

            // EMISSION: punch to white, then to red, else restore
            if (useEmission && o.hasEm)
            {
                Color em = o.em; // default (restore)
                if (inWhite)
                {
                    em = whiteFlashColor * Mathf.LinearToGammaSpace(whiteEmissionIntensity * whiteT);
                }
                else if (inRed)
                {
                    em = redTintColor * Mathf.LinearToGammaSpace(redEmissionIntensity * redT);
                }
                _mpb.SetColor(EmissionColorID, em);
            }

            r.SetPropertyBlock(_mpb);
        }
    }

    private void RestoreAll()
    {
        foreach (var r in _renderers)
        {
            if (!r) continue;
            if (!_orig.TryGetValue(r, out var o)) continue;

            _mpb.Clear();

            if (o.hasBase) _mpb.SetColor(BaseColorID, o.baseCol);
            else if (o.hasCol) _mpb.SetColor(ColorID, o.col);

            if (useEmission && o.hasEm) _mpb.SetColor(EmissionColorID, o.em);

            r.SetPropertyBlock(_mpb);
        }
    }

    private void SetVisible(bool on)
    {
        foreach (var r in _renderers)
            if (r) r.enabled = on;
    }
}
