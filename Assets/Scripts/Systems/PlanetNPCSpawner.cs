using System.Collections;
using UnityEngine;

public class PlanetNPCSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject npcShipPrefab;          // The ship to spawn
    public Transform planetTransform;         // Centre of the orbit

    public int   minShipsPerSpawn  = 1;
    public int   maxShipsPerSpawn  = 3;
    public float minSpawnInterval  = 2f;
    public float maxSpawnInterval  = 8f;

    [Tooltip("Distance from planet centre to place newly spawned ships.")]
    public float spawnDistanceFromPlanet = 15f;

    [Header("Delay")]
    [Tooltip("Time (seconds) to wait after scene load before the first ships appear.")]
    public float initialSpawnDelay = 5f;      // ← NEW

    private void Start() => StartCoroutine(SpawnRoutine());

    private IEnumerator SpawnRoutine()
    {
        // ---------- initial delay ----------
        if (initialSpawnDelay > 0f)
            yield return new WaitForSeconds(initialSpawnDelay);

        // ---------- main loop ----------
        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            int shipsToSpawn = Random.Range(minShipsPerSpawn, maxShipsPerSpawn + 1);

            for (int i = 0; i < shipsToSpawn; i++)
            {
                // Random radial direction around the planet (XZ plane)
                Vector2 rand2D      = Random.insideUnitCircle.normalized;
                Vector3 spawnDir3D  = new Vector3(rand2D.x, 0f, rand2D.y);

                Vector3 spawnPos    = planetTransform.position + spawnDir3D * spawnDistanceFromPlanet;
                Vector3 outwardDir  = (spawnPos - planetTransform.position).normalized;
                Quaternion spawnRot = Quaternion.LookRotation(-outwardDir, Vector3.up);

                GameObject shipGO = Instantiate(npcShipPrefab, spawnPos, spawnRot);

                // Tell the AI which planet it orbits
                if (shipGO.TryGetComponent(out NPCShipAI ai))
                    ai.planetTransform = planetTransform;
            }
        }
    }
}
