//  SlideInFromEdge.cs
//  Unity 6 - URP
//  Attach to the root RectTransform of any UI you want to animate on-screen.

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(RectTransform))]
public class SlideInFromEdge : MonoBehaviour
{
    public enum Edge { Bottom, Top, Left, Right }

    [Header("Direction")]
    [Tooltip("Which side of the screen the panel should slide in from.")]
    [SerializeField] private Edge slideFrom = Edge.Bottom;

    [Header("Timing")]
    [SerializeField] private float duration = 0.75f;
    [SerializeField] private AnimationCurve ease = new AnimationCurve(
        new Keyframe(0,   0, 0, 2),   // fast start
        new Keyframe(0.85f, 1.1f),    // overshoot (>1)
        new Keyframe(1,   1, 0, 0));  // settle

    [Header("Behaviour")]
    [SerializeField] private bool ignoreTimescale = true;
    [Tooltip("Extra distance (pixels) beyond the panel’s size so it is fully off-screen before animating.")]
    [SerializeField] private float offscreenBuffer = 50f;

    // ─────────────────── private ───────────────────
    private RectTransform rt;
    private Vector2 shownPos;   // final anchoredPosition
    private Vector2 hiddenPos;  // start pos off-screen

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        shownPos = rt.anchoredPosition;

        hiddenPos = shownPos;                           // copy first
        Vector2 size = rt.rect.size;

        switch (slideFrom)
        {
            case Edge.Bottom:
                hiddenPos.y -= size.y + offscreenBuffer;
                break;
            case Edge.Top:
                hiddenPos.y += size.y + offscreenBuffer;
                break;
            case Edge.Left:
                hiddenPos.x -= size.x + offscreenBuffer;
                break;
            case Edge.Right:
                hiddenPos.x += size.x + offscreenBuffer;
                break;
        }

        rt.anchoredPosition = hiddenPos;
    }

    private void OnEnable()  { StartCoroutine(AnimateIn()); }
    private void OnDisable() { StopAllCoroutines(); }          // safety

    // ───────────────── animation coroutine ─────────────────
    private IEnumerator AnimateIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            float dt = ignoreTimescale ? Time.unscaledDeltaTime : Time.deltaTime;
            t += dt / duration;

            float k = ease.Evaluate(t);                        // eased progress 0-1 (may overshoot)
            rt.anchoredPosition = Vector2.LerpUnclamped(hiddenPos, shownPos, k);

            yield return null;
        }
        rt.anchoredPosition = shownPos;
    }
}
