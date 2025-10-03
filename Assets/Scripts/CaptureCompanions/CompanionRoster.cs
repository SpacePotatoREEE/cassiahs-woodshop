using UnityEngine;
using System.Collections.Generic;

public class CompanionRoster : MonoBehaviour
{
    public const int MaxActive = 5;

    [Header("Owner & Placement")]
    public Transform owner; // your player (human on planet)
    public float ringRadius = 2.5f;
    public float ringHeightOffset = 0.8f;

    [Header("Runtime")]
    public List<CompanionController> active = new List<CompanionController>();
    public List<CreatureDefinition> storage = new List<CreatureDefinition>();

    void Awake()
    {
        if (!owner) owner = this.transform;
    }

    public void AddCaptured(CreatureDefinition def)
    {
        if (active.Count < MaxActive && def && def.companionPrefab)
        {
            int slot = active.Count;
            Vector3 pos = OwnerSlotWorld(slot);
            var go = Instantiate(def.companionPrefab, pos, Quaternion.identity);
            var comp = go.GetComponent<CompanionController>();
            if (!comp) comp = go.AddComponent<CompanionController>();
            comp.Initialize(def, owner, this, slot);
            active.Add(comp);
            RecomputeRing();
        }
        else
        {
            storage.Add(def);
        }
    }

    public void RecomputeRing()
    {
        for (int i = 0; i < active.Count; i++)
        {
            active[i].SetDesiredSlot(i);
        }
    }

    public Vector3 OwnerSlotWorld(int slot)
    {
        float angle = (Mathf.PI * 2f) * (slot / (float)MaxActive);
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
        return owner.position + offset + Vector3.up * ringHeightOffset;
    }
}