using UnityEngine;
using Unity.Cinemachine;

/// Cinemachine extension that applies a shake to the live vcam (CM v2 & v3).
/// All values come from the caller (e.g., PlayerHitEffects → CameraShake.Shake).
[ExecuteAlways, DisallowMultipleComponent]
public class HitShakeExtension : CinemachineExtension
{
    // Runtime state (set via Shake)
    private float _timeLeft;
    private float _amp, _freq, _duration;
    private int   _seed;

    // Falloff aggressiveness (tweak here if you ever want a different curve)
    private const float FALLOFF_SHARPNESS = 10f;

    public void Shake(float amplitude, float frequency, float duration)
    {
        _amp      = Mathf.Max(0f, amplitude);
        _freq     = Mathf.Max(0f, frequency);
        _duration = Mathf.Max(0.0001f, duration);
        _timeLeft = _duration;
        _seed     = Random.Range(0, 99999);
    }

#if CINEMACHINE_3_0_0_OR_HIGHER
    protected override void PostPipelineStageCallback(
        CinemachineCamera vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        ApplyShake(stage, ref state, deltaTime);
    }
#else
    protected override void PostPipelineStageCallback(
        CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        ApplyShake(stage, ref state, deltaTime);
    }
#endif

    private void ApplyShake(CinemachineCore.Stage stage, ref CameraState state, float dt)
    {
        if (stage != CinemachineCore.Stage.Finalize || _timeLeft <= 0f || dt <= 0f) return;

        float t = Time.time * _freq;

        float life    = Mathf.Clamp01(_timeLeft / _duration);
        float falloff = (FALLOFF_SHARPNESS > 0f)
            ? Mathf.SmoothStep(0f, 1f, Mathf.Pow(life, FALLOFF_SHARPNESS))
            : life;

        // Position + small rotational wobble
        Vector3 n  = Noise(t, _seed) * (_amp * falloff);
        Vector3 rn = Noise(t + 123.456f, _seed) * (_amp * 2f * falloff);

        state.PositionCorrection    += n;
        state.OrientationCorrection *= Quaternion.Euler(rn * 2f);

        _timeLeft -= dt;
    }

    private static Vector3 Noise(float t, int seed)
    {
        float nx = Mathf.PerlinNoise(seed + 1.37f, t) * 2f - 1f;
        float ny = Mathf.PerlinNoise(seed + 4.21f, t * 0.97f) * 2f - 1f;
        float nz = Mathf.PerlinNoise(seed + 8.88f, t * 1.13f) * 2f - 1f;
        return new Vector3(nx, ny, nz);
    }
}

/// Helper to shake the *currently live* vcam (no references required).
public static class CameraShake
{
    public static void Shake(float amplitude, float frequency, float duration)
    {
        var brain = Object.FindObjectOfType<CinemachineBrain>();
        if (brain == null) return;

#if CINEMACHINE_3_0_0_OR_HIGHER
        var live = brain.ActiveVirtualCamera as CinemachineCamera;
#else
        var live = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
#endif
        if (live == null) return;

        var ext = live.GetComponent<HitShakeExtension>();
        if (ext != null) ext.Shake(amplitude, frequency, duration);
    }
}
