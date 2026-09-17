using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Placement;

/// <summary>
/// Scans an area of the 3D map by raycasting straight down at each grid point,
/// and generates a placement spot wherever the ray hits valid buildable ground
/// and isn't blocked by the enemy path or obstacles.
/// </summary>
public class PlacementSpotGenerator : MonoBehaviour
{
    [Header("Scan Area")]
    [Tooltip("World-space center of the area to scan.")]
    [SerializeField] private Vector3 areaCenter = Vector3.zero;
    [Tooltip("Width (X) and depth (Z) of the area to scan.")]
    [SerializeField] private Vector2 areaSize = new Vector2(50f, 50f);
    [Tooltip("Distance between grid points.")]
    [SerializeField] private float cellSize = 2f;
    [Tooltip("Height above the area to start each downward raycast from.")]
    [SerializeField] private float raycastHeight = 50f;
    [Tooltip("Max raycast distance downward.")]
    [SerializeField] private float raycastDistance = 200f;

    [Header("Layers")]
    [Tooltip("Layer(s) considered valid buildable ground.")]
    [SerializeField] private LayerMask buildableGroundLayer;
    [Tooltip("Layer(s) that block placement even over buildable ground (enemy path, obstacles, props).")]
    [SerializeField] private LayerMask blockingLayer;
    [Tooltip("Radius used to check for blocking colliders/other spots near a candidate point.")]
    [SerializeField] private float blockCheckRadius = 0.5f;

    [Header("Spot Prefab")]
    [Tooltip("Prefab must have a Collider on it for click/raycast detection to work.")]
    [SerializeField] private GameObject placementSpotPrefab;
    [SerializeField] private Transform spotsParent;

    [Header("No-Build Zones")]
    [Tooltip("Leave empty and enable auto find zones to grab every NoBuildZone in the scene (e.g. the enemy paths)")]
    [SerializeField] private List<NoBuildZone> noBuildZones = new List<NoBuildZone>();
    [SerializeField] private bool autoFindZones = true;

    private readonly List<PlacementSpot> generatedSpots = new List<PlacementSpot>();

    public IReadOnlyList<PlacementSpot> Spots => generatedSpots;

    private void Start()
    {
        GenerateSpots();
    }

    [ContextMenu("Generate Spots")]
    public void GenerateSpots()
    {
        ClearSpots();

        if (autoFindZones)
        {
            noBuildZones.Clear();
            noBuildZones.AddRange(FindObjectsByType<NoBuildZone>(FindObjectsInactive.Exclude));
        }

        float halfWidth = areaSize.x * 0.5f;
        float halfDepth = areaSize.y * 0.5f;

        for (float x = -halfWidth; x <= halfWidth; x += cellSize)
        {
            for (float z = -halfDepth; z <= halfDepth; z += cellSize)
            {
                Vector3 rayOrigin = areaCenter + new Vector3(x, raycastHeight, z);

                if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, buildableGroundLayer))
                    continue;

                if (Physics.CheckSphere(hit.point, blockCheckRadius, blockingLayer))
                    continue;

                if (IsInsideAnyNoBuildZone(hit.point))
                    continue;    

                SpawnSpot(hit.point);
            }
        }

        Debug.Log($"PlacementSpotGenerator: generated {generatedSpots.Count} placement spots.");
    }

    // Skips this point if it falls inside a designer-placed NoBuildZone, like one covering an enemy path
    private bool IsInsideAnyNoBuildZone(Vector3 point)
    {
        foreach (NoBuildZone zone in noBuildZones)
        {
            if (zone != null && zone.Contains(point))
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnSpot(Vector3 worldPos)
    {
        GameObject spotObj = Instantiate(placementSpotPrefab, worldPos, Quaternion.identity, spotsParent);
        PlacementSpot spot = spotObj.GetComponent<PlacementSpot>();

        if (spot == null)
            spot = spotObj.AddComponent<PlacementSpot>();

        generatedSpots.Add(spot);
    }

    private void ClearSpots()
    {
        foreach (var spot in generatedSpots)
        {
            if (spot != null)
                Destroy(spot.gameObject);
        }
        generatedSpots.Clear();
    }

    // Visualize the scan area in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}