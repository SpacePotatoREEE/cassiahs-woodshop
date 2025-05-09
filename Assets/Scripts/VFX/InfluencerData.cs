using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Layout of one entry in the GraphicsBuffer the VFX Graph reads.
/// The VFXType attribute tells VFX Graph to expose this struct in node menus.
/// </summary>
[VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
public struct InfluencerData
{
    public Vector3 position;   // world-space centre of the object
    public Vector3 velocity;   // linear velocity (m/s)
}