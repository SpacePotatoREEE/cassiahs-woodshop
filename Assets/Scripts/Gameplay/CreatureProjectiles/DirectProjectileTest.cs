using UnityEngine;

public class DirectProjectileTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (ProjectileManager.Instance != null)
            {
                Vector3 pos = transform.position + Vector3.up * 2f;
                Vector3 dir = Vector3.forward;
                
                Debug.Log($"[TEST] Calling spawn at {pos}");
                ProjectileManager.Instance.SpawnProjectile(
                    pos, dir, 5f, 10, 0.5f, Color.cyan, 10f
                );
            }
            else
            {
                Debug.LogError("[TEST] No ProjectileManager.Instance!");
            }
        }
    }
}