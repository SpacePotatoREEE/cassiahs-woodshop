//  ──────────────────────────────────────────────────────────────
//  GPUParticleVolume.cs – any object that should push particles
//  ──────────────────────────────────────────────────────────────
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to a ship, projectile, explosion prefab, footstep object…  
/// The system samples every active volume each frame and applies a
/// radial force to nearby GPU particles.
/// </summary>
[ExecuteAlways]
public class GPUParticleVolume : MonoBehaviour
{
    [Tooltip("Sphere radius in metres that affects particles.")]
    [Min(0.01f)]
    public float radius = 3f;

    [Tooltip("Strength of the push (m/s²) away from the centre.")]
    public float forceStrength = 20f;

    [Tooltip("Optional curve to fade force by (distance / radius). " +
             "1 = full, 0 = none.")]
    public AnimationCurve attenuation =
        AnimationCurve.Linear(0, 1, 1, 0);

    /// <summary>The particle system queries this list each frame.</summary>
    public static readonly List<GPUParticleVolume> ActiveVolumes = new();

    void OnEnable()  => ActiveVolumes.Add(this);
    void OnDisable() => ActiveVolumes.Remove(this);

    /// <summary>Forward-direction is irrelevant; magnitude is all we need.</summary>
    internal Vector3 ForceVector => transform.forward * forceStrength;

    /// <summary>Exposed as a property so other assemblies see it even
    /// if they can’t access the backing field directly.</summary>
    public float Radius => radius;
}