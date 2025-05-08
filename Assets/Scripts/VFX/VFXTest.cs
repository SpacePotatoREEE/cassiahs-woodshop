using UnityEngine;
using UnityEngine.VFX;

public class VFXTest : MonoBehaviour
{
    void Start()
    {
        var vfx = GetComponent<VisualEffect>();
        vfx.SetVector4Array("TestVec4Array", new Vector4[4]);
    }
}