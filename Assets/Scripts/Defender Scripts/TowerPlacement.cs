using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
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

            for (float x = edgeMargin; x<= width - edgeMargin; x += cellSize)
            {
                for(float z = edgeMargin; z<= length - edgeMargin; z += cellSize)
                {
                    Vector3 worldXZ = origin + new Vector3(x,0f, z);
                    EvaluatePoint(worldXZ);
                }
            }

            Debug.Log($"[TowerPlacementGenerator] Analyzed Terrain: {validPoints.Count} valid / " + $"{rejectedPoints.Count} rejected candidate points");
        }

        private void EvaluatePoint(Vector3 worldXZ)
        {
            //raycast down from high point so it also detects props rather than reading the height map
        }
    }
}