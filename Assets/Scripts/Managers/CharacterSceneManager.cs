using UnityEngine;
using Unity.Cinemachine;

public class CharacterSceneManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Check this if this scene uses the space (ship) character, otherwise it uses the planet (human) character.")]
    [SerializeField] private bool useSpaceCharacter = false;

    [Header("Cinemachine Settings")]
    [Tooltip("Drag the Cinemachine Virtual Camera for this scene here.")]
    [SerializeField] private CinemachineCamera virtualCamera;

    // Private references
    private GameObject spaceCharacterRoot;
    private GameObject humanCharacterRoot;

    private void Start()
    {
        // 1) Find the top-level parent for each character based on their layer
        spaceCharacterRoot = FindObjectRootOnLayer("PlayerShip");
        humanCharacterRoot = FindObjectRootOnLayer("PlayerHuman");

        ToggleCharacters();
    }

    private void ToggleCharacters()
    {
        if (useSpaceCharacter)
        {
            // ENABLE the space ship
            if (spaceCharacterRoot != null)
            {
                spaceCharacterRoot.SetActive(true);

                // Assign Cinemachine camera to follow this
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = spaceCharacterRoot.transform;
                    virtualCamera.LookAt = spaceCharacterRoot.transform;
                }
            }
            // DISABLE the human
            if (humanCharacterRoot != null)
                humanCharacterRoot.SetActive(false);
        }
        else
        {
            // ENABLE the human
            if (humanCharacterRoot != null)
            {
                humanCharacterRoot.SetActive(true);

                // Assign Cinemachine camera
                if (virtualCamera != null)
                {
                    virtualCamera.Follow = humanCharacterRoot.transform;
                    virtualCamera.LookAt = humanCharacterRoot.transform;
                }
            }
            // DISABLE the space ship
            if (spaceCharacterRoot != null)
                spaceCharacterRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Finds the first GameObject in the scene on the given layer,
    /// then returns its root (top-level parent).
    /// </summary>
    private GameObject FindObjectRootOnLayer(string layerName)
    {
        int layerIndex = LayerMask.NameToLayer(layerName);
        if (layerIndex < 0)
        {
            Debug.LogWarning($"Layer '{layerName}' not found in Project Settings > Tags and Layers.");
            return null;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true); // include inactive
        foreach (var obj in allObjects)
        {
            if (obj.layer == layerIndex)
            {
                // Return the topmost parent in the hierarchy
                return obj.transform.root.gameObject;
            }
        }

        Debug.LogWarning($"No object found on layer '{layerName}'.");
        return null;
    }
}
