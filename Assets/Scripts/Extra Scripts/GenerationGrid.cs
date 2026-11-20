using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;

public class GenerationGrid : MonoBehaviour
{
    public GameObject blockGameObject;

    public GameObject objectToSpawn;

    // FIX 2 (new): one field so the spawner/enemies know when the world is ready.
    // Set to true after generation AND the NavMesh bake - this alone fixes
    // the "Failed to create agent" errors, because nothing spawns before
    // the NavMesh exists.
    public static bool TerrainReady { get; private set; } = false;

    [Tooltip("Drag the GameObject with the NavMeshSurface component here (AI Navigation package)")]
    public NavMeshSurface navMeshSurface;

    [Header("Runtime Paths")]
    [Tooltip("Drag the GameObject with the Pathgenerator component here. Paths + tower are created after the cubes spawn and BEFORE the NavMesh bake, so the spawner and placement scanner only ever see a finished world.")]
    public Pathgenerator pathGenerator;

    [Header("Map Size")]
    [SerializeField] private int worldSizeX = 40;
    [SerializeField] private int worldSizeZ = 40;
    
    [Tooltip("How many objects are scattered over the map. Raise this when the map gets bigger")]
    [SerializeField] private int objectCount = 300;
    
    [Header("Terrain Shape")]
    [Tooltip("How tall the hills can get, in blocks")]
    [SerializeField] private int noiseHeight = 8;

    [Tooltip("Bigger = wide, gentle hills. Smaller = busy, bumpy terrain")]
    [SerializeField] private float detailScale = 14f;

    private float gridOffset = 1f;

    // FIX 3 (new): random per-game offset for the noise
    private float noiseOffsetX;
    private float noiseOffsetZ;

    private List<Vector3> blockPositions = new List<Vector3>();

    private void Awake()
    {
        // Reset before other scene objects begin waiting for terrain generation.
        TerrainReady = false;
    }

    void Start()
    {
        // randomize so the terrain is different every new game
        noiseOffsetX = Random.Range(0f, 10000f);
        noiseOffsetZ = Random.Range(0f, 10000f);

        // 1. Work out how tall every column is (always at least 1 block)
        int[,] heights = new int[worldSizeX, worldSizeZ];

        for (int x = 0; x < worldSizeX; x++)
        {
            for (int z = 0; z < worldSizeZ; z++)
            {
                heights[x, z] = Mathf.RoundToInt(generateNoise(x, z, detailScale) * noiseHeight) + 1;
            }
        }

        // 2. Remove cliffs so every part of the map can be walked to
        SmoothHeights(heights);

        // 3. Build solid columns that reach all the way down to the ground
        for (int x = 0; x < worldSizeX; x++)
        {
            for (int z = 0; z < worldSizeZ; z++)
            {
                int height = heights[x, z];

                Vector3 pos = new Vector3(x * gridOffset, height / 2f, z * gridOffset);
                GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity) as GameObject;

                // Stretch the cube so its bottom is at ground level and its top is at 'height'
                Vector3 size = blockGameObject.transform.localScale;
                block.transform.localScale = new Vector3(size.x, height, size.z);

                // Remember the TOP of the column (used for placing objects)
                blockPositions.Add(new Vector3(pos.x, height, pos.z));

                block.transform.SetParent(this.transform);
            }
        }

        SpawnObject();

        StartCoroutine(BakeNavMeshWhenReady());
    }

    // Makes sure no column is more than 1 block higher than the columns next to it.
    // No cliffs means every part of the map can be walked to, and enemies take direct routes
    private void SmoothHeights(int[,] heights)
    {
        bool changed = true;
        int safety = 0;

        while (changed && safety < 100)
        {
            changed = false;
            safety++;

            for (int x = 0; x < worldSizeX; x++)
            {
                for (int z = 0; z < worldSizeZ; z++)
                {
                    int allowed = heights[x, z];

                    if (x > 0) allowed = Mathf.Min(allowed, heights[x - 1, z] + 1);
                    if (x < worldSizeX - 1) allowed = Mathf.Min(allowed, heights[x + 1, z] + 1);
                    if (z > 0) allowed = Mathf.Min(allowed, heights[x, z - 1] + 1);
                    if (z < worldSizeZ - 1) allowed = Mathf.Min(allowed, heights[x, z + 1] + 1);

                    if (allowed < heights[x, z])
                    {
                        heights[x, z] = allowed;
                        changed = true;
                    }
                }
            }
        }
    }

    // NavMesh
    private IEnumerator BakeNavMeshWhenReady()
    {
        // let physics register the freshly spawned block colliders first
        yield return new WaitForFixedUpdate();

        if (pathGenerator != null)
        {
            pathGenerator.GeneratePathways();
        }
        else
        {
            Debug.LogWarning("[GenerationGrid] No Pathgenerator assigned - the game will use whatever paths are already in the scene.");
        }

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("[GenerationGrid] No NavMeshSurface assigned! Install the AI Navigation package (Window > Package Manager > Unity Registry) and add the component to the terrain root.");
        }

        TerrainReady = true;
        Debug.Log("[GenerationGrid] Terrain ready - enemies can spawn.");
    }

    private void SpawnObject()
    {
        for(int c = 0; c < objectCount; c++)
        {
            GameObject toPlaceObject = Instantiate(objectToSpawn, ObjectSpawnLocation(), Quaternion.identity);

        }
    }

    private Vector3 ObjectSpawnLocation ()
    {
        int rndIndex = Random.Range(0, blockPositions.Count);

        Vector3 newPos = blockPositions[rndIndex];
        blockPositions.RemoveAt(rndIndex);
        return newPos;
    }

    private float generateNoise (int x, int z, float detailScale)
    {
        float xNoise = (x + noiseOffsetX) / detailScale;   // FIX 3: was (x + this.transform.position.x)
        float zNoise = (z + noiseOffsetZ) / detailScale;   // FIX 3: was (z + this.transform.position.y) - also
                                                           // sampled Y for Z, which was a second bug

        float wave = Mathf.PerlinNoise(xNoise, zNoise);

        // Deleted Debug.Log(wave) because it fired 625 times...
        return wave;
    }
}