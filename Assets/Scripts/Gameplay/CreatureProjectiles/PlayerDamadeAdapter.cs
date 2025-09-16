using UnityEngine;

/// <summary>
/// Bridges the IDamageable interface to your existing player stats system.
/// Place on the same GameObject as your player stats component.
/// </summary>
public class PlayerDamageAdapter : MonoBehaviour, IDamageable
{
    [Header("Stats Integration")]
    [SerializeField] private MonoBehaviour playerStatsComponent;
    
    private System.Reflection.MethodInfo _takeDamageMethod;
    private PlayerHitEffects _hitEffects;
    private PlayerStatsHuman _playerStatsHuman;

    void Awake()
    {
        // Find hit effects component
        _hitEffects = GetComponent<PlayerHitEffects>();
        if (!_hitEffects)
        {
            _hitEffects = GetComponentInChildren<PlayerHitEffects>();
        }
        
        // First try to get PlayerStatsHuman directly
        _playerStatsHuman = GetComponent<PlayerStatsHuman>();
        if (_playerStatsHuman == null && playerStatsComponent != null)
        {
            _playerStatsHuman = playerStatsComponent as PlayerStatsHuman;
        }
        
        // If no PlayerStatsHuman, try reflection for TakeDamage with float parameter
        if (_playerStatsHuman == null && playerStatsComponent != null)
        {
            // Try float parameter first (for PlayerStatsHuman)
            _takeDamageMethod = playerStatsComponent.GetType().GetMethod("TakeDamage", new[] { typeof(float) });
            
            // If not found, try int parameter
            if (_takeDamageMethod == null)
            {
                _takeDamageMethod = playerStatsComponent.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
            }
            
            if (_takeDamageMethod == null)
            {
                Debug.LogWarning($"PlayerDamageAdapter: Could not find TakeDamage method on {playerStatsComponent.GetType().Name}");
            }
        }
    }

    public void ApplyDamage(int amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        // Check if invulnerable
        if (_hitEffects && _hitEffects.IsInvulnerable())
        {
            return; // Ignore damage during i-frames
        }
        
        // Apply damage using direct reference if available
        if (_playerStatsHuman != null)
        {
            _playerStatsHuman.TakeDamage((float)amount);
        }
        // Otherwise use reflection
        else if (_takeDamageMethod != null)
        {
            var paramType = _takeDamageMethod.GetParameters()[0].ParameterType;
            if (paramType == typeof(float))
            {
                _takeDamageMethod.Invoke(playerStatsComponent, new object[] { (float)amount });
            }
            else
            {
                _takeDamageMethod.Invoke(playerStatsComponent, new object[] { amount });
            }
        }
        
        // Note: Hit effects are triggered by ProjectileManager directly
    }
}