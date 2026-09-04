using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GenerationGrid : MonoBehaviour
{
    public GameObject blockGameObject;

    public GameObject objectToSpawn;

    private int worldSizeX = 25;
    private int worldSizeZ = 25;
    private int noiseHeight = 8;
    private float gridOffset = 1f;


    private List<Vector3> blockPositions = new List<Vector3>();

    void Start()
    {
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
        float xNoise = (x + this.transform.position.x) / detailScale;
        float zNoise = (z + this.transform.position.y) / detailScale;

        return Mathf.PerlinNoise(xNoise, zNoise);
    }
}
