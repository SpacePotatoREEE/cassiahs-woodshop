// NameplateUI.cs
//
// 2025‑08‑07  –  Upright‑Yaw Billboard
//
// • Rotates the label only around world‑up (Y‑axis); it never pitches or rolls.
// • Optional distance‑based scaling so far labels don’t vanish (set scaleCurve
//   in the Inspector if desired – leave empty to disable).
//
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class NameplateUI : MonoBehaviour
{
    [Header("Optional distance‑based scale (units: metres → scale)")]
    [Tooltip("Leave empty to disable.  Useful if labels look tiny at distance.")]
    public AnimationCurve scaleCurve;

    private TMP_Text tmp;
    private Transform camTr;

    /* ───────────────────────────────────────── */

    private void Awake()
    {
        tmp   = GetComponent<TMP_Text>();
        camTr = Camera.main ? Camera.main.transform : null;

        tmp.fontSize  = 3;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    /* ───────────────────────────────────────── */

    private void LateUpdate()
    {
        // Cache camera if it spawned later (e.g., additive scene)
        if (!camTr && Camera.main) camTr = Camera.main.transform;
        if (!camTr) return;

        // 1. Compute yaw‑only forward vector
        Vector3 toCam = transform.position - camTr.position;
        toCam.x = 0f;                              // flatten – remove pitch component
        if (toCam.sqrMagnitude < 0.0001f) return;  // too close or invalid

        transform.rotation = Quaternion.LookRotation(toCam);

        // 2. Optional distance scaling
        if (scaleCurve != null && scaleCurve.length > 0)
        {
            float dist = Mathf.Sqrt(toCam.sqrMagnitude);
            float s    = scaleCurve.Evaluate(dist);
            transform.localScale = Vector3.one * s;
        }
    }

    /* ───────────────────────────────────────── */

    public void SetName(string n) => tmp.text = n;
}