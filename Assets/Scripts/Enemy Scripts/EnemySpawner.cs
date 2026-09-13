using System.Collections.Generic;
using UnityEngine;

// Spawns enemies at set time intervals and sends each one down a path to the tower
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("The enemy prefab to spawn (must have the Enemy script on it)")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Time in seconds between each enemy spawning")]
    public float spawnInterval = 2f;

    [Tooltip("Optional: manually drag EnemyPath objects here. Leave empty to automatically use every EnemyPath found in the scene at runtime (used once real procedural paths exist).")]
    public List<EnemyPath> paths = new List<EnemyPath>();

    private List<EnemyPath> activePaths = new List<EnemyPath>();
    private float spawnTimer = 0f;

    private void Start()
    {
        if (paths != null && paths.Count > 0)
        {
            activePaths = paths;
        }
        else
        {
            // Not a manual list, it automatically finds every EnemyPath
            activePaths = new List<EnemyPath>(FindObjectsByType<EnemyPath>(FindObjectsInactive.Exclude));

            if (activePaths.Count == 0)
            {
                Debug.LogWarning("EnemySpawner found no EnemyPath objects in the scene at Start.");
            }
        }
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefab == null || activePaths.Count == 0)
        {
            Debug.LogWarning("EnemySpawner is missing an enemy prefab or has no paths available.");
            return;
        }

        // Pick a random path
        EnemyPath chosenPath = activePaths[Random.Range(0, activePaths.Count)];

        if (chosenPath.WaypointCount == 0)
        {
            Debug.LogWarning("The chosen path has no waypoints set up.");
            return;
        }

        // Spawn the enemy at the first waypoint of the chosen path
        Vector3 spawnPosition = chosenPath.GetWaypointPosition(0);
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Tell the new enemy which path to walk
        Enemy enemyScript = newEnemy.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            enemyScript.SetPath(chosenPath);
        }
    }
}