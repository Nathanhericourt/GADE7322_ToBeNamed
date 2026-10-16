using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Spawns enemies at set time intervals and sends each one down a path to the tower
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Enemy prefabs to spawn (each must have the Enemy script, or a subclass of it, on it). Picked at random for now - the wave system will replace this.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Header("Spawn Settings")]
    [Tooltip("Time in seconds between each enemy spawning")]
    public float spawnInterval = 2f;

    [Tooltip("Optional: manually drag EnemyPath objects here. Leave empty to automatically use every EnemyPath found in the scene at runtime (used once real procedural paths exist).")]
    public List<EnemyPath> paths = new List<EnemyPath>();

    private List<EnemyPath> activePaths = new List<EnemyPath>();
    private float spawnTimer = 0f;
    private bool pathsCollected = false;

    private void Update()
    {
        // Don't spawn until the procedural terrain exists and the NavMesh is baked
        if (!GenerationGrid.TerrainReady)
        {
            return;
        }

        if (!pathsCollected)
        {
            CollectPaths();
            pathsCollected = true;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void CollectPaths()
    {
        if (paths != null && paths.Count > 0)
        {
            activePaths = paths;
        }
        else
        {
            activePaths = new List<EnemyPath>(FindObjectsByType<EnemyPath>(FindObjectsInactive.Exclude));

            if (activePaths.Count == 0)
            {
                Debug.LogWarning("EnemySpawner found no EnemyPath objects after terrain generation.");
            }
            else
            {
                Debug.Log($"EnemySpawner: found {activePaths.Count} paths, spawning begins.");
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0 || activePaths.Count == 0)
        {
            Debug.LogWarning("EnemySpawner is missing enemy prefabs or has no paths available.");
            return;
        }

        // Pick a random path
        EnemyPath chosenPath = activePaths[Random.Range(0, activePaths.Count)];

        if (chosenPath.WaypointCount == 0)
        {
            Debug.LogWarning("The chosen path has no waypoints set up.");
            return;
        }

        Vector3 spawnPosition = chosenPath.GetWaypointPosition(0);

        // Snap the spawn point onto the NavMesh so the agent activates
        if (NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }
        else
        {
            Debug.LogWarning($"No NavMesh near {spawnPosition} - skipping this spawn.");
            return;
        }

        GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        GameObject newEnemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Gives the path to the enemy
        if (newEnemy.TryGetComponent(out Enemy enemy))
        {
            enemy.SetPath(chosenPath);
        }

    }
}