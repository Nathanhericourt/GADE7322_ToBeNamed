using UnityEngine;
using TowerDefense.Placement;

public class Pathgenerator : MonoBehaviour
{
    [Header("Terrain")]
    [Tooltip("Drag in the GameObject that parents all generated terrain pieces")]
    public Transform terrainRoot;

    [Header("Tower")]
    [Tooltip("If set, a Tower is instantiated at the terrain's centre at runtime. If empty, the Tower already in the scene is moved there instead.")]
    public GameObject towerPrefab;

    [Header("Target")]
    [Tooltip("Where all paths should lead - Tower's position")]
    public Transform towerPoint;

    [Header("Path Settings")]
    [Tooltip("How many pathways to generate")]
    public int pathCount = 3;
 
    [Tooltip("How many waypoints make up each path")]
    public int waypointsPerPath = 5;

    [Header("Path Protection")]
    [Tooltip("NoBuildZone radius around each waypoint - must cover the path width")]
    public float pathZoneRadius = 2.5f;

    [Tooltip("Extra padding added around each path zone")]
    public float pathZonePadding = 0.5f;

    [Tooltip("Also place a zone at the midpoint of each segment so the whole line is protected, not just the waypoints")]
    public bool protectSegments = true;
 
    const float RaycastHeightPadding = 500f;

    public Tower PlacedTower { get; private set; }
 
    [ContextMenu("Generate Pathways")]
    public void GeneratePathways()
    {
        if (terrainRoot == null)
        {
            Debug.LogError("[PathGenerator] Assign the Terrain Root first.");
            return;
        }
    
        ClearPreviouslyGenerated();
        Bounds terrainBounds = CalculateTerrainBounds();

        PlaceTower(terrainBounds);

        if (PlacedTower == null)
        {
            Debug.LogError("[PathGenerator] Tower not found or placed. Cannot generate paths.");
            return;
        }

        Vector3 towerPosition = PlacedTower.transform.position;

        float angleOffset = Random.Range(0f, 360f);
        
        for (int i = 0; i < pathCount; i++)
        {
            Vector3 edgeStart = GetEdgeStartPoint(terrainBounds, i, angleOffset);
            CreatePath(i, edgeStart, towerPosition);
        }
 
        Debug.Log($"[PathGenerator] Generated {pathCount} pathways towards the tower.");
    }
    
    //Tower
    private void PlaceTower(Bounds bounds)
    {
        Vector3 centerXZ = new Vector3(bounds.center.x, 0f, bounds.center.z);
        Vector3 towerPos = SnapToTerrain(centerXZ, bounds.center);

        Tower existing = FindAnyObjectByType<Tower>();

        if (towerPrefab != null)
        {
            if (existing != null)
                Destroy(existing.gameObject);
            
            PlacedTower = Instantiate(towerPrefab, towerPos, Quaternion.identity).GetComponent<Tower>();
        }
        else if (existing != null)
        {
            existing.transform.position = towerPos;
            PlacedTower = existing;
        }

        if (PlacedTower == null)
            Debug.LogError("[PathGenerator] No Tower prefab assigned and no Tower found in the scene.");
        
    }

    //Paths
    private void CreatePath(int index, Vector3 startXZ, Vector3 endPosition)
    {
        GameObject pathObject = new GameObject("Path" + (index + 1));
        pathObject.transform.SetParent(transform);

        EnemyPath pathScript = pathObject.AddComponent<EnemyPath>();

        Vector3 previous = SnapToTerrain(startXZ, endPosition);
        AddWaypoint(pathObject.transform, pathScript, previous, 0);

        for (int w = 1; w <= waypointsPerPath; w++)
        {
            float t = (float)w / waypointsPerPath;
            Vector3 flatPoint = Vector3.Lerp(startXZ, endPosition, t);
            Vector3 snapped = SnapToTerrain(flatPoint, endPosition);

            // Protect the line between the previous waypoint and this one
            if (protectSegments)
                AddZone(pathObject.transform, Vector3.Lerp(previous, snapped, 0.5f), "SegmentZone");

            AddZone(pathObject.transform, snapped, "PathZone");
            AddWaypoint(pathObject.transform, pathScript, snapped, w);

            previous = snapped;
        }

        AddZone(pathObject.transform, pathScript.GetWaypointPosition(0), "SpawnZone");
    }

    private void AddWaypoint(Transform pathParent, EnemyPath pathScript, Vector3 pos, int index)
    {
        GameObject waypointObject = new GameObject("waypoint" + index);
        waypointObject.transform.SetParent(pathParent);
        waypointObject.transform.position = pos + Vector3.up * 0.1f;
        pathScript.waypoints.Add(waypointObject.transform);
    }

    //No build zones along the path
    private void AddZone(Transform parent, Vector3 pos, string zoneName)
    {
        GameObject zoneObj = new GameObject(zoneName);
        zoneObj.transform.SetParent(parent);
        zoneObj.transform.position = pos;

        NoBuildZone zone = zoneObj.AddComponent<NoBuildZone>();
        zone.shape = NoBuildZone.ZoneShape.Sphere;
        zone.radius = pathZoneRadius;
        zone.padding = pathZonePadding;
    }

    // Spreads starting points evenly around
    private Vector3 GetEdgeStartPoint(Bounds bounds, int index, float angleOffset)
    {
        float angle = angleOffset + (360f / pathCount) * index;
        float radians = angle * Mathf.Deg2Rad;
 
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) - 1.5f;
 
        float x = center.x + Mathf.Cos(radians) * radius;
        float z = center.z + Mathf.Sin(radians) * radius;
 
        return new Vector3(x, 0f, z);
    }
 
    // Finds the actual terrain surface height
    private Vector3 SnapToTerrain(Vector3 point, Vector3 towards)
    {
        Vector3 p = point;

        for (int attempt = 0; attempt < 12; attempt++)
        {
            Vector3 rayStart = new Vector3(p.x, RaycastHeightPadding, p.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RaycastHeightPadding * 2f))
                return hit.point;

            p = Vector3.MoveTowards(p, towards, 0.75f);
        }

        Debug.LogWarning($"[PathGenerator] Could not snap {point} to the terrain.");
        return p;
    }
 
    // Reads the terrain size 
    private Bounds CalculateTerrainBounds()
    {
        Renderer[] pieces = terrainRoot.GetComponentsInChildren<Renderer>();
 
        if (pieces.Length == 0)
        {
            return new Bounds(terrainRoot.position, Vector3.one);
        }
 
        Bounds combined = pieces[0].bounds;
        for (int i = 1; i < pieces.Length; i++)
        {
            combined.Encapsulate(pieces[i].bounds);
        }
 
        return combined;
    }

    // Removes paths/zones created by a previous run (or an editor test)
    private void ClearPreviouslyGenerated()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }

}
