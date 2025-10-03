using UnityEngine;

public interface IDamageable
{
    /// <summary>Apply damage and return true if killed by this hit.</summary>
    bool ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Object source = null);

    /// <summary>Optional world position used by aim/targeters.</summary>
    Transform GetAimPoint();
}