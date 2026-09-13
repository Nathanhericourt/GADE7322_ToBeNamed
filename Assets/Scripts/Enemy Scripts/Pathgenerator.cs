using UnityEngine;
//ForTesting 
public class Pathgenerator : MonoBehaviour
{
    [Header("Terrain")]
    [Tooltip("Drag in the GameObject that parents all generated terrain pieces")]
    public Transform terrainRoot;

    [Header("Target")]
    [Tooltip("Where all paths should lead - Tower's position")]
    public Transform towerPoint;

    [Header("Path Settings")]
    [Tooltip("How many pathways to generate")]
    public int pathCount = 3;
 
    [Tooltip("How many waypoints make up each path")]
    public int waypointsPerPath = 5;
 
    const float RaycastHeightPadding = 500f;
 
    [ContextMenu("Generate Pathways")]
    public void GeneratePathways()
    {
        if (terrainRoot == null || towerPoint == null)
        {
            Debug.LogError("[PathGenerator] Assign both Terrain Root and Tower Point first.");
            return;
        }
 
        Bounds terrainBounds = CalculateTerrainBounds();
 
        for (int i = 0; i < pathCount; i++)
        {
            Vector3 edgeStart = GetEdgeStartPoint(terrainBounds, i);
            CreatePath(i, edgeStart, towerPoint.position);
        }
 
        Debug.Log($"[PathGenerator] Generated {pathCount} pathways towards the tower.");
    }
 
    // Spreads starting points evenly around
    private Vector3 GetEdgeStartPoint(Bounds bounds, int index)
    {
        float angle = (360f / pathCount) * index;
        float radians = angle * Mathf.Deg2Rad;
 
        Vector3 center = bounds.center;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
 
        float x = center.x + Mathf.Cos(radians) * radius;
        float z = center.z + Mathf.Sin(radians) * radius;
 
        return new Vector3(x, 0f, z);
    }
 
    private void CreatePath(int index, Vector3 startXZ, Vector3 endPosition)
    {
        GameObject pathObject = new GameObject("Path" + (index + 1));
        pathObject.transform.SetParent(transform);
 
        EnemyPath pathScript = pathObject.AddComponent<EnemyPath>();
 
        for (int w = 0; w <= waypointsPerPath; w++)
        {
            float t = (float)w / waypointsPerPath;
            Vector3 flatPoint = Vector3.Lerp(startXZ, endPosition, t);
 
            Vector3 snappedPoint = SnapToTerrain(flatPoint);
 
            GameObject waypointObject = new GameObject("waypoint" + (w + 1));
            waypointObject.transform.SetParent(pathObject.transform);
            waypointObject.transform.position = snappedPoint;
 
            pathScript.waypoints.Add(waypointObject.transform);
        }
    }
 
    // Finds the actual terrain surface height
    private Vector3 SnapToTerrain(Vector3 point)
    {
        Vector3 rayStart = new Vector3(point.x, RaycastHeightPadding, point.z);
 
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, RaycastHeightPadding * 2f))
        {
            return hit.point;
        }
 
        return point;
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

}
