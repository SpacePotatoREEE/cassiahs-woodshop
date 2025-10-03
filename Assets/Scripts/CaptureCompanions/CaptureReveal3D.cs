using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CaptureReveal3D : MonoBehaviour
{
    public static IEnumerator Play(GameObject prefab, Camera cam, float appearTime = 0.35f, float holdTime = 0.9f, float exitTime = 0.4f, float distanceFromCam = 2.2f, float baseScale = 1.0f)
    {
        if (!prefab || !cam) yield break;

        // Spawn a preview under the camera so it's always centered
        var root = new GameObject("[CaptureReveal3D]");
        root.transform.SetParent(cam.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, distanceFromCam);
        root.transform.localRotation = Quaternion.identity;

        var inst = Instantiate(prefab, root.transform);
        inst.transform.localPosition = Vector3.zero;
        inst.transform.localRotation = Quaternion.identity;

        // Strip gameplay bits
        foreach (var c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var rb in inst.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
        foreach (var a in inst.GetComponentsInChildren<Animator>(true)) a.updateMode = AnimatorUpdateMode.UnscaledTime;

        // Rough fit: try to estimate bounds
        Bounds b = new Bounds(inst.transform.position, Vector3.one);
        var rends = inst.GetComponentsInChildren<Renderer>(true);
        bool any = false;
        foreach (var r in rends) { if (!r.enabled) continue; b.Encapsulate(r.bounds); any = true; }
        float size = any ? Mathf.Max(0.001f, b.size.magnitude) : 1f;

        float scale = baseScale * (1.2f / size); // crude fit
        inst.transform.localScale = Vector3.one * scale;

        // APPEAR (scale/alpha pop)
        float t = 0f;
        Vector3 start = Vector3.one * (scale * 0.2f);
        Vector3 end   = Vector3.one * scale;
        inst.transform.localScale = start;

        while (t < appearTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / appearTime);
            inst.transform.localScale = Vector3.LerpUnclamped(start, end, u);
            inst.transform.Rotate(0f, 90f * Time.unscaledDeltaTime, 0f, Space.Self);
            yield return null;
        }
        inst.transform.localScale = end;

        // HOLD
        t = 0f;
        while (t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            inst.transform.Rotate(0f, 45f * Time.unscaledDeltaTime, 0f, Space.Self);
            yield return null;
        }

        // EXIT (slide off to the right)
        t = 0f;
        Vector3 p0 = root.transform.localPosition;
        Vector3 p1 = p0 + new Vector3(2.0f, 0f, 0f);
        while (t < exitTime)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / exitTime);
            root.transform.localPosition = Vector3.Lerp(p0, p1, u);
            inst.transform.Rotate(0f, 90f * Time.unscaledDeltaTime, 0f, Space.Self);
            yield return null;
        }

        Destroy(root);
    }
}
