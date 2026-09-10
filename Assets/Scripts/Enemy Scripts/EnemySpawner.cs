using System.Collections.Generic;
using UnityEngine;

// Spawns enemies at set time intervals and sends each one down a path toward the tower
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("The enemy prefab to spawn (must have the Enemy script on it)")]
    public GameObject enemyPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Time in seconds between each enemy spawning")]
    public float spawnInterval = 2f;

    [Tooltip("Drag in your EnemyPath objects here - one spawner can use all 3 paths")]
    public List<EnemyPath> paths = new List<EnemyPath>();

    private float spawnTimer = 0f;

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
        if (enemyPrefab == null || paths.Count == 0)
        {
            Debug.LogWarning("EnemySpawner is missing an enemy prefab or has no paths assigned.");
            return;
        }

        // Pick a random path so enemies come from different directions
        EnemyPath chosenPath = paths[Random.Range(0, paths.Count)];

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