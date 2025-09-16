// CinemachineShake.cs  (Unity 6, Cinemachine 3.1.3)
using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public static class CinemachineShake
{
    /// One-liner:
    ///   CinemachineShake.Shake(0.35f, 0.22f, 1.2f, this, s=>Debug.Log(s));
    public static void Shake(float intensity, float duration, float frequency, MonoBehaviour runner, System.Action<string> onLog = null)
    {
        if (runner == null) { onLog?.Invoke("[CinemachineShake] runner is null"); return; }

        var vcam = GetActiveVcam();
        if (vcam == null) { onLog?.Invoke("[CinemachineShake] No CinemachineCamera found or live."); return; }

        var noise = vcam.GetComponent<CinemachineBasicMultiChannelPerlin>();
        if (noise == null) noise = vcam.gameObject.AddComponent<CinemachineBasicMultiChannelPerlin>();

        runner.StartCoroutine(ShakeRoutine(vcam, noise, intensity, duration, frequency, onLog));
    }

    private static IEnumerator ShakeRoutine(CinemachineCamera vcam, CinemachineBasicMultiChannelPerlin noise,
                                            float amp, float dur, float freq, System.Action<string> log)
    {
        if (dur <= 0f || amp <= 0f) yield break;

        float startAmp  = noise.AmplitudeGain;
        float startFreq = noise.FrequencyGain;

        log?.Invoke($"[CinemachineShake] Start → vcam='{vcam.name}', amp={amp}, dur={dur}, freq={freq}");

        float t = 0f;
        while (t < dur && vcam != null && vcam.gameObject.activeInHierarchy)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);

            // 0→1→0 envelope (smooth)
            float env = Mathf.Sin(k * Mathf.PI);

            noise.AmplitudeGain = startAmp  + amp * env;
            noise.FrequencyGain = startFreq + Mathf.Lerp(freq * 0.8f, freq * 1.3f, env);

            yield return null;
        }

        // Restore
        if (noise != null)
        {
            noise.AmplitudeGain = startAmp;
            noise.FrequencyGain = startFreq;
        }

        log?.Invoke($"[CinemachineShake] End   → vcam='{(vcam ? vcam.name : "null")}'");
    }

    /// CM 3.x active vcam finder without using CinemachineCore internals
    private static CinemachineCamera GetActiveVcam()
    {
        var all = Object.FindObjectsOfType<CinemachineCamera>(true); // include inactive
        CinemachineCamera fallback = null;

        foreach (var v in all)
        {
            if (v == null) continue;
            if (v.IsLive) return v; // best
            if (fallback == null && v.enabled && v.gameObject.activeInHierarchy)
                fallback = v;       // next best
        }
        return fallback != null ? fallback : (all.Length > 0 ? all[0] : null);
    }
}
