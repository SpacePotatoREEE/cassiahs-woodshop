using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class HitFlash : MonoBehaviour
{
    [Header("Renderers")]
    public Renderer[] renderers;  // auto-finds if empty

    [Header("Flash Settings")]
    public Color flashEmission = new Color(1f, 0.8f, 0.8f, 1f);
    public string emissionProperty = "_EmissionColor";
    public float flashUpTime = 0.05f;
    public float holdTime = 0.06f;
    public float fadeTime = 0.15f;

    private List<MaterialPropertyBlock> _blocks = new();
    private List<Renderer> _rends = new();
    private Coroutine _fx;

    void Awake()
    {
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in renderers)
        {
            if (!r) continue;
            _rends.Add(r);
            _blocks.Add(new MaterialPropertyBlock());
        }
    }

    public void FlashOnce()
    {
        if (_fx != null) StopCoroutine(_fx);
        _fx = StartCoroutine(DoFlash());
    }

    IEnumerator DoFlash()
    {
        // Up
        float t = 0f;
        while (t < flashUpTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / flashUpTime);
            SetEmission(Color.Lerp(Color.black, flashEmission, a));
            yield return null;
        }

        // Hold
        if (holdTime > 0f) yield return new WaitForSeconds(holdTime);

        // Fade
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeTime);
            SetEmission(Color.Lerp(Color.black, flashEmission, a));
            yield return null;
        }

        SetEmission(Color.black);
        _fx = null;
    }

    void SetEmission(Color c)
    {
        for (int i = 0; i < _rends.Count; i++)
        {
            var r = _rends[i];
            var b = _blocks[i];
            r.GetPropertyBlock(b);
            b.SetColor(emissionProperty, c);
            r.SetPropertyBlock(b);
        }
    }
}
