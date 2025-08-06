// TimedDespawn.cs
using UnityEngine;

public class TimedDespawn : MonoBehaviour
{
    public float Lifetime = 60f;
    private float t;

    private void Update()
    {
        t += Time.deltaTime;
        if (t >= Lifetime) Destroy(gameObject);
    }
}