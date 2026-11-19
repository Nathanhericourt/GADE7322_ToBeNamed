using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Spawns enemies at set time intervals and sends each one down a path to the tower
public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Enemy prefabs to spawn (each must have the Enemy script, or a subclass of it, on it). Picked at random for now - the wave system will replace this.")]
    public List<GameObject> enemyPrefabs = new List<GameObject>();

    [Tooltip("Spawned in the final boss round. Use a big, high health Brute-style prefab. If left empty the last prefab in the list above is used.")]
    public GameObject bossPrefab;

    [Header("Spawn Settings")]
    [Tooltip("How far in from the very edge of the map enemies spawn")]
    public float edgeMargin = 1.5f;

    [Tooltip("Optional: manually drag EnemyPath objects here. Leave empty to automatically use every EnemyPath found in the scene at runtime (used once real procedural paths exist).")]
    public List<EnemyPath> paths = new List<EnemyPath>();

    [Header("Off Screen Spawning")]
    [Tooltip("Enemies only spawn where the player's camera can't see them")]
    public bool spawnOffScreen = true;

    [Tooltip("How far outside the screen a spawn point must be. 0.1 = 10% of the screen size")]
    public float offScreenMargin = 0.1f;

    [Tooltip("Reject a spawn point if its walking route to the start of the path is more than this many times longer than a straight line")]
    public float maxDetourFactor = 1.6f;

    [Tooltip("How far (in world units) from a path's start an enemy can appear. Enemies take turns on each path")]
    public float spawnSpread = 6f;

    private int pathCursor = -1;

    private Camera mainCamera;
    private EnemyMutator mutator;

    private List<EnemyPath> activePaths = new List<EnemyPath>();
    private bool pathsCollected = false;
    private Bounds terrainBounds;
    private NavMeshPath navPath;

    // Called by the WaveManager - spawns the WHOLE wave in one go
    public int SpawnWave(int count)
    {
        if (!EnsureSetup())
        {
            return 0;
        }

        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogWarning("EnemySpawner has no enemy prefabs assigned.");
            return 0;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = count * 10;

        // Keep going until every enemy is out
        while (spawned < count && attempts < maxAttempts)
        {
            attempts++;

            GameObject prefabToSpawn = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            if (prefabToSpawn == null) continue;

            if (SpawnEnemy(prefabToSpawn, false))
            {
                spawned++;
            }
        }

        if (spawned < count)
        {
            Debug.LogWarning($"EnemySpawner could only spawn {spawned} of {count} enemies.");
        }

        return spawned;
    }

    // Boss round: one boss plus a few regular enemies
    public bool SpawnBoss(int escortCount)
    {
        if (!EnsureSetup())
        {
            return false;
        }

        GameObject prefab = bossPrefab;
        if (prefab == null && enemyPrefabs != null && enemyPrefabs.Count > 0)
        {
            Debug.LogWarning("EnemySpawner has no Boss Prefab assigned - using the last enemy prefab as the boss.");
            prefab = enemyPrefabs[enemyPrefabs.Count - 1];
        }

        if (prefab == null)
        {
            Debug.LogWarning("EnemySpawner has no boss or enemy prefab to spawn.");
            return false;
        }

        bool bossSpawned = false;
        for (int attempt = 0; attempt < 20 && !bossSpawned; attempt++)
        {
            bossSpawned = SpawnEnemy(prefab, true);
        }

        if (bossSpawned && escortCount > 0)
        {
            SpawnWave(escortCount);
        }

        return bossSpawned;
    }

    // One-time setup once the terrain exists: find the paths and measure the map
    private bool EnsureSetup()
    {
        if (!GenerationGrid.TerrainReady)
        {
            return false;
        }

        if (!pathsCollected)
        {
            navPath = new NavMeshPath();
            CollectPaths();
            CalculateTerrainBounds();
            pathsCollected = true;
        }

        return activePaths.Count > 0;
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

    // Measures the generated terrain so spawn points can be placed around its edge
    private void CalculateTerrainBounds()
    {
        GenerationGrid grid = FindAnyObjectByType<GenerationGrid>();
        Renderer[] pieces = grid != null ? grid.GetComponentsInChildren<Renderer>() : new Renderer[0];

        if (pieces.Length == 0)
        {
            terrainBounds = new Bounds(transform.position, new Vector3(25f, 10f, 25f));
            Debug.LogWarning("EnemySpawner could not measure the terrain - using a default size.");
            return;
        }

        terrainBounds = pieces[0].bounds;
        for (int i = 1; i < pieces.Length; i++)
        {
            terrainBounds.Encapsulate(pieces[i].bounds);
        }
    }

    private bool SpawnEnemy(GameObject prefabToSpawn, bool asBoss)
    {
        if (!TryGetSpawnPoint(out Vector3 spawnPosition, out EnemyPath chosenPath))
        {
            return false;
        }

        GameObject newEnemy = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Gives the path to the enemy
        if (newEnemy.TryGetComponent(out Enemy enemy))
        {
            enemy.SetPath(chosenPath);

            // Later waves are harder
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ScaleEnemy(enemy);
            }

            if (asBoss)
            {
                enemy.isBoss = true;
            }

            // Every enemy gets its own random stats, look and (later in the game) trait
            if (mutator == null)
            {
                mutator = GetComponent<EnemyMutator>();

                if (mutator == null)
                {
                    mutator = gameObject.AddComponent<EnemyMutator>();
                }
            }

            mutator.Mutate(enemy, asBoss);
        }

        return true;
    }

    // Finds a random spot on the outskirts of the map to the start of the nearest path
    private bool TryGetSpawnPoint(out Vector3 spawnPosition, out EnemyPath chosenPath)
    {
        spawnPosition = Vector3.zero;
        chosenPath = null;

        // Pick the path first (taking turns), so every path gets enemies
        EnemyPath wantedPath = GetNextPath();
        if (wantedPath == null) return false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 edgePoint = PointNearPathStart(wantedPath);

            // Snap the spawn point onto the NavMesh so the agent activates
            if (!NavMesh.SamplePosition(edgePoint, out NavMeshHit edgeHit, 8f, NavMesh.AllAreas))
            {
                continue;
            }

            // Never spawn anywhere the player can see
            if (spawnOffScreen && IsOnScreen(edgeHit.position))
            {
                continue;
            }

            EnemyPath nearestPath = wantedPath;

            if (!NavMesh.SamplePosition(nearestPath.GetWaypointPosition(0), out NavMeshHit startHit, 5f, NavMesh.AllAreas))
            {
                continue;
            }

            // Only spawn where the enemy can really walk to the start of its path
            if (NavMesh.CalculatePath(edgeHit.position, startHit.position, NavMesh.AllAreas, navPath)
                && navPath.status == NavMeshPathStatus.PathComplete
                && IsDirectEnough(navPath, edgeHit.position, startHit.position))
            {
                spawnPosition = edgeHit.position;
                chosenPath = nearestPath;
                return true;
            }
        }

        // Fallback so a wave is never lost: spawn on the start of a random path
        EnemyPath fallbackPath = wantedPath;

        if (fallbackPath.WaypointCount == 0)
        {
            Debug.LogWarning("The chosen path has no waypoints set up.");
            return false;
        }

        if (NavMesh.SamplePosition(fallbackPath.GetWaypointPosition(0), out NavMeshHit fallbackHit, 5f, NavMesh.AllAreas))
        {
            spawnPosition = fallbackHit.position;
            chosenPath = fallbackPath;
            return true;
        }

        Debug.LogWarning("No NavMesh found for a spawn point - skipping this spawn.");
        return false;
    }

    // Gives out the paths in turn, so each one gets the same number of enemies
    private EnemyPath GetNextPath()
    {
        for (int i = 0; i < activePaths.Count; i++)
        {
            pathCursor = (pathCursor + 1) % activePaths.Count;
            EnemyPath p = activePaths[pathCursor];

            if (p != null && p.WaypointCount > 0) return p;
        }

        return null;
    }

    // A random point close to the start of this path, kept inside the map
    private Vector3 PointNearPathStart(EnemyPath path)
    {
        Vector3 start = path.GetWaypointPosition(0);
        Vector2 offset = Random.insideUnitCircle * spawnSpread;

        float x = Mathf.Clamp(start.x + offset.x, terrainBounds.min.x + edgeMargin, terrainBounds.max.x - edgeMargin);
        float z = Mathf.Clamp(start.z + offset.y, terrainBounds.min.z + edgeMargin, terrainBounds.max.z - edgeMargin);

        return new Vector3(x, terrainBounds.center.y, z);
    }

    // Picks one of the four map edges, then a random spot along it
    private Vector3 RandomPointOnOutskirts()
    {
        float minX = terrainBounds.min.x + edgeMargin;
        float maxX = terrainBounds.max.x - edgeMargin;
        float minZ = terrainBounds.min.z + edgeMargin;
        float maxZ = terrainBounds.max.z - edgeMargin;
        float y = terrainBounds.center.y;

        switch (Random.Range(0, 4))
        {
            case 0:  return new Vector3(Random.Range(minX, maxX), y, maxZ);
            case 1:  return new Vector3(Random.Range(minX, maxX), y, minZ);
            case 2:  return new Vector3(minX, y, Random.Range(minZ, maxZ));
            default: return new Vector3(maxX, y, Random.Range(minZ, maxZ));
        }
    }

    // True if this world position is currently on (or very close to) the player's screen
    private bool IsOnScreen(Vector3 worldPosition)
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return false;

        Vector3 view = mainCamera.WorldToViewportPoint(worldPosition);

        // z below 0 means the point is behind the camera
        if (view.z < 0f) return false;

        return view.x > -offScreenMargin && view.x < 1f + offScreenMargin &&
            view.y > -offScreenMargin && view.y < 1f + offScreenMargin;
    }

    private bool IsDirectEnough(NavMeshPath path, Vector3 from, Vector3 to)
    {
        float routeLength = 0f;
        Vector3[] corners = path.corners;

        for (int i = 1; i < corners.Length; i++)
        {
            routeLength += Vector3.Distance(corners[i - 1], corners[i]);
        }

        float straightLine = Vector3.Distance(from, to);

        // +5 so very short trips are never rejected over a tiny detour
        return routeLength <= straightLine * maxDetourFactor + 5f;
    }

    // The path whose first waypoint is closest to this point
    private EnemyPath GetNearestPath(Vector3 point)
    {
        EnemyPath best = null;
        float bestDistance = float.MaxValue;

        foreach (EnemyPath path in activePaths)
        {
            if (path == null || path.WaypointCount == 0) continue;

            Vector3 start = path.GetWaypointPosition(0);
            float dx = start.x - point.x;
            float dz = start.z - point.z;
            float distance = dx * dx + dz * dz;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = path;
            }
        }

        return best;
    }
}