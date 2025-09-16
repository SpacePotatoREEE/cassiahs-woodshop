using UnityEngine;

public interface IDamageable
{
    void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal);
}