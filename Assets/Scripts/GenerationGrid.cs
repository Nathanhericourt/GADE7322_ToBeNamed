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

    private int worldSizeX = 25;
    private int worldSizeZ = 25;
    private int noiseHeight = 8;
    private float gridOffset = 1f;

    // FIX 3 (new): random per-game offset for the noise
    private float noiseOffsetX;
    private float noiseOffsetZ;

    private List<Vector3> blockPositions = new List<Vector3>();

    void Start()
    {
        // FIX 3: randomize so the terrain is different every new game
        noiseOffsetX = Random.Range(0f, 10000f);
        noiseOffsetZ = Random.Range(0f, 10000f);

        // FIX 2: reset the flag at the start of each run
        TerrainReady = false;

        for(int x = 0; x < worldSizeX; x++)
        {
            for(int z = 0; z < worldSizeZ; z++)
            {
                Vector3 pos = new Vector3(x * gridOffset, generateNoise(x,z,8f) * noiseHeight, z * gridOffset);
                GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity) as GameObject;

                blockPositions.Add(block.transform.position);

                block.transform.SetParent(this.transform);
            }
        }
        SpawnObject();

        StartCoroutine(BakeNavMeshWhenReady());
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
        for(int c = 0; c <24; c++)
        {
            GameObject toPlaceObject = Instantiate(objectToSpawn, ObjectSpawnLocation(), Quaternion.identity);

        }
    }

    private Vector3 ObjectSpawnLocation ()
    {
        int rndIndex = Random.Range(0, blockPositions.Count);

        Vector3 newPos = new Vector3(blockPositions[rndIndex].x, blockPositions[rndIndex].y + 0.5f, blockPositions[rndIndex].z);
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