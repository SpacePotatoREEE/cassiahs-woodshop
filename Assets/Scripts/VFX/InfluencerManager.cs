//  InfluencerManager.cs   (NEW)
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.Experimental.VFX;

[RequireComponent(typeof(VisualEffect))]
public class InfluencerManager : MonoBehaviour
{
    [Tooltip("How many ships / bullets we support at once (max 64).")]
    public int maxInfluencers = 32;

    // Packed as xyzw = pos.xyz, speed
    private Vector4[]  dataArray;
    private VisualEffect vfx;
    private int posId, countId;

    private readonly List<Rigidbody> sources = new();

    private void Awake()
    {
        dataArray = new Vector4[maxInfluencers];
        vfx       = GetComponent<VisualEffect>();
        posId     = Shader.PropertyToID("InfluencerData");
        countId   = Shader.PropertyToID("InfluencerCount");
    }

    /*  Call these from your enemy / bullet spawner scripts  */
    public void Register(Rigidbody rb)   => sources.Add(rb);
    public void Unregister(Rigidbody rb) => sources.Remove(rb);

    private void LateUpdate()
    {
        // Cull null / dead entries
        for (int i = sources.Count - 1; i >= 0; i--)
            if (sources[i] == null) sources.RemoveAt(i);

        int n = Mathf.Min(sources.Count, maxInfluencers);

        for (int i = 0; i < n; i++)
        {
            var rb = sources[i];
            Vector3 p = rb.position;
            Vector3 v = rb.linearVelocity;   // Unity-6 safe
            dataArray[i] = new Vector4(p.x, p.y, p.z, v.magnitude);
        }
        vfx.SetInt(countId, n);
        vfx.SetVectorArray(posId, dataArray);   // only first n are used
    }
}