using System.Collections.Generic;
using System.Linq;
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

    private const string LAYER_SHIP  = "PlayerShip";
    private const string LAYER_HUMAN = "PlayerHuman";

    private void Start()
    {
        ToggleCharacters();
    }

    private void ToggleCharacters()
    {
        int shipLayer  = LayerMask.NameToLayer(LAYER_SHIP);
        int humanLayer = LayerMask.NameToLayer(LAYER_HUMAN);

        if (shipLayer < 0 || humanLayer < 0)
        {
            Debug.LogWarning("[CharacterSceneManager] Missing layers PlayerShip/PlayerHuman in Project Settings > Tags and Layers.");
            return;
        }

        // Collect all unique roots on each layer (across all scenes, incl. DontDestroyOnLoad)
        var shipRoots  = RootsOnLayer(shipLayer).ToList();
        var humanRoots = RootsOnLayer(humanLayer).ToList();

        // Decide the ONE root we want active
        GameObject activeRoot = null;

        if (useSpaceCharacter)
        {
            // Prefer the singleton if present, else first found
            activeRoot = PlayerStats.Instance ? PlayerStats.Instance.gameObject
                                              : shipRoots.FirstOrDefault();
            // Enable the chosen ship, disable all other ships + all humans
            SetActiveOnly(activeRoot, shipRoots);
            SetActiveOnly(null, humanRoots);
        }
        else
        {
            activeRoot = PlayerStatsHuman.Instance ? PlayerStatsHuman.Instance.gameObject
                                                   : humanRoots.FirstOrDefault();
            // Enable the chosen human, disable all other humans + all ships
            SetActiveOnly(activeRoot, humanRoots);
            SetActiveOnly(null, shipRoots);
        }

        // Wire the vcam
        if (virtualCamera != null && activeRoot != null)
        {
            virtualCamera.Follow = activeRoot.transform;
            virtualCamera.LookAt = activeRoot.transform;
        }
    }

    private IEnumerable<GameObject> RootsOnLayer(int layerIndex)
    {
        foreach (var go in FindObjectsOfType<GameObject>(true))
        {
            if (go.layer == layerIndex && go.transform.root == go.transform)
                yield return go;
        }
    }

    private void SetActiveOnly(GameObject toEnable, IEnumerable<GameObject> candidates)
    {
        foreach (var go in candidates)
            go.SetActive(go == toEnable && toEnable != null);
    }
}
