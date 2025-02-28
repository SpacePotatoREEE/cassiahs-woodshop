using System.Collections;
using UnityEngine;

public class PlanetNPCSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject npcShipPrefab;       
    public Transform planetTransform;      

    public int minShipsPerSpawn = 1;       
    public int maxShipsPerSpawn = 3;       

    public float minSpawnInterval = 2f;    
    public float maxSpawnInterval = 8f;    

    [Tooltip("Distance from planet center to place newly spawned ships.")]
    public float spawnDistanceFromPlanet = 15f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            // Wait random time
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // Number of ships to spawn this cycle
            int shipsToSpawn = Random.Range(minShipsPerSpawn, maxShipsPerSpawn + 1);

            for (int i = 0; i < shipsToSpawn; i++)
            {
                // Random direction
                Vector2 rand2D = Random.insideUnitCircle.normalized;
                Vector3 spawnDir3D = new Vector3(rand2D.x, 0f, rand2D.y);

                // Spawn position
                Vector3 spawnPos = planetTransform.position + spawnDir3D * spawnDistanceFromPlanet;

                // If your "ship forward" is negative Z, face that direction outward
                Vector3 outwardDir = (spawnPos - planetTransform.position).normalized;
                Quaternion spawnRot = Quaternion.LookRotation(-outwardDir, Vector3.up);

                // Instantiate
                GameObject shipGO = Instantiate(npcShipPrefab, spawnPos, spawnRot);

                // Assign planet
                NPCShipAI ai = shipGO.GetComponent<NPCShipAI>();
                if (ai != null)
                {
                    ai.planetTransform = planetTransform;
                }
            }
        }
    }
}