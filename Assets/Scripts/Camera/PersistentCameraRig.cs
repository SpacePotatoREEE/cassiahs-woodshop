// PersistentCameraRig.cs
// Unity 6, Cinemachine 3.1.3
//
// Put this on the *only* physical Camera you keep around (in your Main/Boot scene).
// It will survive scene loads and blend between CinemachineCamera(s) in each scene.

using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;   // <-- CM 3.x namespace

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(CinemachineBrain))]
[DisallowMultipleComponent]
public class PersistentCameraRig : MonoBehaviour
{
    private static PersistentCameraRig _instance;

    [Header("Default Blend (applied at runtime)")]
    [SerializeField] private bool applyDefaultBlend = true;
    [SerializeField] private CinemachineBlendDefinition.Styles defaultBlendStyle =
        CinemachineBlendDefinition.Styles.EaseInOut;
    [SerializeField, Range(0f, 5f)] private float defaultBlendTime = 0.35f;

    private void Awake()
    {
        // Singleton
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Ensure this is the only *enabled* physical Camera
        var myCam = GetComponent<Camera>();
        foreach (var cam in FindObjectsOfType<Camera>(true))
            if (cam != myCam && cam.enabled)
                cam.enabled = false;

        // Keep a single AudioListener enabled
        bool keptListener = false;
        foreach (var al in FindObjectsOfType<AudioListener>(true))
        {
            if (!keptListener) { keptListener = true; al.enabled = true; }
            else al.enabled = false;
        }

        // Apply CM3 default blend via property (get–modify–set)
        var brain = GetComponent<CinemachineBrain>();
        if (applyDefaultBlend && brain != null)
        {
            var def = brain.DefaultBlend;  // struct copy
            def.Style = defaultBlendStyle;
            def.Time  = defaultBlendTime;
            brain.DefaultBlend = def;      // assign back
        }
    }
}