using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace TowerDefense.Placement.Editor
{
    //Analyzes terrain, samples a grid of points, filters them by slops/height/obsticles/ designer-placed NoBuildZones, then spawns tower platform prefabs on the points

    public class TowerPlacementGenerator : MonoBehaviour
    {
        [Header("Terrain")]
        [SerializeField] Terrain terrain;

        [Header("Sampling Grid")]
        [Tooltip("Distance in meters between sample points")]
        [SerializeField] float cellSize = 5f;
        [Tooltip("Keep points this far away for the terrain edge")]
        [SerializeField] float edgeMargin = 5f;

        [Header("slope/height constraints")]
        [SerializeField] float maxSlopeAngle = 20f;
        [SerializeField] float minHeight = -1000f;
        [SerializeField] float maxHeight = 1000f;

        [Header("Obsticle Check")]
        [Tooltip("Layers considered 'blocking' - trees, rocks, etc..")]
        [SerializeField] LayerMask obsticleMask;
        [SerializeField] float obsticleCheckRadius = 1.5f;

        [Header("No-Build Zones")]
        [Tooltip("Leave empty and enable auto find zones to grab every noBuildZone in the screen")]
        [SerializeField] List<NoBuildZone> noBuildZones = new List<NoBuildZone>();
        [SerializeField] bool autoFindZones = true;

        [Header("Platform Spacing")]
        [Tooltip("Minimum distance kept between 2 generated platforms")]
        [SerializeField] float minPlatformSpacing = 8f;
        [Header("Generaation")]
        [SerializeField] GameObject platformPrefab;
        [SerializeField] Transform platformParent;
        [SerializeField] int randomSeed = 0;

        [Header("Debug")]
        [SerializeField] bool drawGizmos = true;

        //Cached analysis Results
        readonly List<Vector3> validPoints = new List<Vector3>();
        readonly List<Vector3> rejectedPoints = new List<Vector3>();
        readonly List<Transform> spawnedPlatforms = new List<Transform>();

        const float RaycastHeightPadding = 500f;

        [ContextMenu("1. Analyze Terrain")]
        public void AnalyzeTerrain()
        {
            validPoints.Clear();
            rejectedPoints.Clear();

            if (terrain == null)
            {
                Debug.LogError
                    ("[TowerPlacementGenerator] No Terrain Assigned");
                return;
            }

            if (autoFindZones)
            {
                noBuildZones.Clear();
                noBuildZones.AddRange
                    (FindAnyObjectByType<NoBuildZone>());
            }

            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            float width = data.size.x;
            float length = data.size.z;

            for (float x = edgeMargin; x <= width - edgeMargin; x += cellSize)
            {
                for (float z = edgeMargin; z <= length - edgeMargin; z += cellSize)
                {
                    Vector3 worldXZ = origin + new Vector3(x, 0f, z);
                    EvaluatePoint(worldXZ);
                }
            }

            Debug.Log($"[TowerPlacementGenerator] Analyzed Terrain: {validPoints.Count} valid / " + $"{rejectedPoints.Count} rejected candidate points");
        }

        private void EvaluatePoint(Vector3 worldXZ)
        {
            //raycast down from high point so it also detects props rather than reading the height map

            Vector3 rayStart = new Vector3(worldXZ.x, terrain.transform.position.y + RaycastHeightPadding, worldXZ.z);
            //if raycast pointing down and is not hitting
            if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RaycastHeightPadding * 2F))
            {
                return; //nothing is under the XZ 
            }

            Vector3 point = hit.point;

            //height range check
            if (point.y < minHeight || point.y > maxHeight)
            {
                rejectedPoints.Add(point);
                return;
            }

            //slope check using the surface normal at the hit point. if the point is invalid it adds it to the rejected points list
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle > maxSlopeAngle)
            {
                rejectedPoints.Add(point);
                return;
            }

            //no-build zone check
            foreach (NoBuildZone zone in noBuildZones)
            {
                if (zone != null && zone.Contains(point))
                {
                    rejectedPoints.Add(point);
                    return;
                }
            }

            //obstacle check
            if (Physics.CheckSphere(point + Vector3.up * 0.25f, obsticleCheckRadius, obsticleMask))
            {
                rejectedPoints.Add(point);
                return;
            }

            validPoints.Add(point);
        }

        [ContextMenu("2. Generate Platforms")]
        public void GeneratePlatforms()
        {
            if (validPoints.Count == 0)
            {
                Debug.LogWarning
                    ("[TowerPlacementGenerator] No valid points cached - run analyze terrain first.");
                return;
            }

            ClearPlatforms();

            System.Random rng = new System.Random(randomSeed);
            List<Vector3> shuffled = new List<Vector3>(validPoints);
            Shuffle(shuffled, rng);

            List<Vector3> chosen = new List<Vector3>();

            foreach (Vector3 candidate in shuffled)
            {
                bool tooCLose = false;
                foreach (Vector3 existing in chosen)
                {
                    if(Vector3.Distance(candidate, existing) < minPlatformSpacing)
                    {
                        tooCLose = true; 
                        break;
                    }
                }
                if (tooCLose) continue;

                chosen.Add(candidate);
                SpawnPlatform(candidate);
            }

            Debug.Log($"[TowerPlacementGenerator] Spawned {chosen.Count} platforms " + $"(spacing >= {minPlatformSpacing} m)");

        }

        private void SpawnPlatform(Vector3 position)
        {
            if (platformPrefab == null)
            {
                Debug.LogError
                    ("[TowerPlacementGenerator] No platform prefab assigned.");
                return;
            }

            //align rotation to the terrain normal so platforms sit flush on slopes
            Vector3 normal = Vector3.up;
            if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 5f))
            {
                normal = hit.normal;
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
            Transform parent = platformParent != null ? platformParent.transform : transform;

            GameObject instance = Instantiate(platformPrefab, position, rotation, parent);
            spawnedPlatforms.Add(instance.transform);
        }

        [ContextMenu("3. Clear platforms")]
        private void ClearPlatforms()
        {
            for (int i = spawnedPlatforms.Count - 1; i >= 0; i--)
            {
                if (spawnedPlatforms[i] != null)
                {
                    if(Application.isPlaying)
                        Destroy(spawnedPlatforms[i].gameObject);
                    else
                        Destroy(spawnedPlatforms[i].gameObject);
                }
            }
            spawnedPlatforms.Clear();
        }

        static void Shuffle(List<Vector3> list, System.Random rng)
        {
            for(int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public IReadOnlyList<Vector3> ValidPoints => validPoints;

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Gizmos.color = Color.green;
            foreach (Vector3 p in validPoints)
                Gizmos.DrawSphere(p, 0.4f);

            Gizmos.color = Color.red;
            foreach (Vector3 p in rejectedPoints)
                Gizmos.DrawWireSphere(p, 0.4f);
        }
    }
}