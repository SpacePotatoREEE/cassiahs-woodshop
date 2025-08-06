// AreaVolume.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AreaVolume : MonoBehaviour
{
    public AreaProfile Profile;

    private void Reset() => GetComponent<Collider>().isTrigger = true;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Profile == null) return;
        Gizmos.color = Profile.Environment == AreaProfile.EnvironmentType.Space ? Color.cyan : Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (GetComponent<Collider>() is BoxCollider box)
            Gizmos.DrawWireCube(box.center, box.size);

        UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
            $"Level {Profile.AreaLevel}\n{Profile.Environment}");
    }
#endif
}