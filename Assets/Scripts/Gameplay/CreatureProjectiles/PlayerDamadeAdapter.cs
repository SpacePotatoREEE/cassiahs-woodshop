using UnityEngine;
using System.Reflection;

/// <summary>
/// Bridges the IDamageable interface to your existing player stats system.
/// Put this on the same GameObject as your player stats (e.g., PlayerStatsHuman).
/// Works with direct reference OR by reflection to a TakeDamage method (float or int).
/// </summary>
[DisallowMultipleComponent]
public class PlayerDamageAdapter : MonoBehaviour, IDamageable
{
    [Header("Stats Integration")]
    [Tooltip("Optional. If left empty, we'll search this GameObject for PlayerStatsHuman, otherwise reflect on this component for TakeDamage.")]
    [SerializeField] private MonoBehaviour playerStatsComponent;

    [Header("Aim")]
    [Tooltip("Where enemies should aim at the player (e.g., chest/head). Falls back to this.transform if null.")]
    [SerializeField] private Transform aimPoint;

    [Header("I-Frames")]
    [Tooltip("If true, will skip damage while invulnerable via PlayerHitEffects.IsInvulnerable() (if present).")]
    [SerializeField] private bool respectIFrames = true;

    // Optional direct types (use if present)
    private PlayerHitEffects _hitEffects;
    private PlayerStatsHuman _playerStatsHuman;

    // Reflection fallbacks
    private MethodInfo _takeDamageFloat; // TakeDamage(float)
    private MethodInfo _takeDamageInt;   // TakeDamage(int)

    private MethodInfo _isInvulnerableMethod; // bool IsInvulnerable()
    private FieldInfo  _isInvulnerableField;  // public bool IsInvulnerable
    private PropertyInfo _isInvulnerableProp; // public bool IsInvulnerable { get; }

    private void Awake()
    {
        // Try to find PlayerHitEffects for i-frames (optional)
        _hitEffects = GetComponent<PlayerHitEffects>();
        if (!_hitEffects) _hitEffects = GetComponentInChildren<PlayerHitEffects>();

        // Prefer a direct PlayerStatsHuman if available
        _playerStatsHuman = GetComponent<PlayerStatsHuman>();
        if (_playerStatsHuman == null && playerStatsComponent is PlayerStatsHuman psh)
            _playerStatsHuman = psh;

        // If we don't have a direct PlayerStatsHuman, prepare reflection on provided component (or on self)
        var statsTarget = playerStatsComponent ? playerStatsComponent : (MonoBehaviour)_playerStatsHuman;
        if (statsTarget != null)
        {
            var t = statsTarget.GetType();
            _takeDamageFloat = t.GetMethod("TakeDamage", new[] { typeof(float) });
            _takeDamageInt   = t.GetMethod("TakeDamage",   new[] { typeof(int)   });
        }

        // If no explicit PlayerHitEffects, prep generic i-frame lookups on the same GameObject
        if (_hitEffects == null)
        {
            // Try to find any IsInvulnerable() method/field/prop on playerStatsComponent or this component
            var invTarget = (object)_hitEffects ?? (object)playerStatsComponent ?? (object)this;
            var t = invTarget.GetType();

            _isInvulnerableMethod = t.GetMethod("IsInvulnerable", System.Type.EmptyTypes);
            _isInvulnerableField  = t.GetField("IsInvulnerable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _isInvulnerableProp   = t.GetProperty("IsInvulnerable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    /// <summary>
    /// IDamageable implementation used by projectiles/AI.
    /// Return true if killed by this hit; for the player we usually return false (XP on kill not needed).
    /// </summary>
    public bool ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Object source = null)
    {
        // Respect i-frames if configured and available
        if (respectIFrames && IsInvulnerable()) return false;

        // Route damage to your player stats
        if (_playerStatsHuman != null)
        {
            _playerStatsHuman.TakeDamage(amount);
        }
        else if (playerStatsComponent != null)
        {
            if (_takeDamageFloat != null)
            {
                _takeDamageFloat.Invoke(playerStatsComponent, new object[] { amount });
            }
            else if (_takeDamageInt != null)
            {
                _takeDamageInt.Invoke(playerStatsComponent, new object[] { Mathf.RoundToInt(amount) });
            }
            else
            {
                Debug.LogWarning($"PlayerDamageAdapter: No TakeDamage(float/int) found on {playerStatsComponent.GetType().Name}.");
            }
        }
        else
        {
            Debug.LogWarning("PlayerDamageAdapter: No stats component assigned/found. Damage ignored.");
        }

        // If you want exact kill reporting, wire in a health query here and return true when <= 0.
        return false;
    }

    /// <summary>Where enemies should aim; required by IDamageable.</summary>
    public Transform GetAimPoint()
    {
        return aimPoint ? aimPoint : transform;
    }

    // -------- Helpers --------

    private bool IsInvulnerable()
    {
        // Prefer direct PlayerHitEffects if present
        if (_hitEffects != null)
        {
            // Assuming your PlayerHitEffects exposes IsInvulnerable(); if not, this returns false.
            var m = typeof(PlayerHitEffects).GetMethod("IsInvulnerable", System.Type.EmptyTypes);
            if (m != null)
            {
                object r = m.Invoke(_hitEffects, null);
                if (r is bool b) return b;
            }
        }

        // Generic reflection fallbacks (method, field, or property named "IsInvulnerable")
        if (_isInvulnerableMethod != null)
        {
            object r = _isInvulnerableMethod.Invoke(playerStatsComponent ? playerStatsComponent : this, null);
            if (r is bool b) return b;
        }
        if (_isInvulnerableField != null)
        {
            object r = _isInvulnerableField.GetValue(playerStatsComponent ? playerStatsComponent : (object)this);
            if (r is bool b) return b;
        }
        if (_isInvulnerableProp != null && _isInvulnerableProp.CanRead)
        {
            object r = _isInvulnerableProp.GetValue(playerStatsComponent ? playerStatsComponent : (object)this);
            if (r is bool b) return b;
        }

        return false;
    }
}
