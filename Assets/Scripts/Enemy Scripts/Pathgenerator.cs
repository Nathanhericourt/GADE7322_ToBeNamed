using System.Collections.Generic;
using UnityEngine;
using TowerDefense.Placement;

// Builds the Tower and a different network of enemy paths every game.
// - The number of paths is random (2 to 5 by default)
// - Every path winds sideways using Perlin noise, but always starts on the map edge and ends at the Tower
// - Some paths curve toward their neighbour and merge with it, giving a shared route and a chokepoint near the Tower
// - Everything is rolled from a seed, so a layout can be replayed
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

    [Tooltip("Longer paths get extra waypoints so no gap is left between the no-build zones. 10 works with the default zone radius of 2.5")]
    public float maxWaypointSpacing = 10f;

    [Header("Procedural Path Network")]
    [Tooltip("Pick a random number of paths every game")]
    public bool randomPathCount = true;

    [Tooltip("Fewest paths a game can have")]
    public int minPaths = 2;

    [Tooltip("Most paths a game can have")]
    public int maxPaths = 5;

    [Tooltip("How far (in world units) a path can bend sideways. 0 = straight lines")]
    public float windAmount = 10f;

    [Tooltip("How many bends fit along a path. Higher = more wiggly")]
    public float windFrequency = 2.5f;

    [Tooltip("Chance that a path curves over and merges into its neighbour. 0 = never, 1 = always (when they start close enough)")]
    [Range(0f, 1f)] public float mergeChance = 0.4f;

    [Tooltip("Tick this to replay the same layout every time (handy for testing and the demo video)")]
    public bool useFixedSeed = false;
    public int seed = 12345;

    [Header("Path Look")]
    [Tooltip("Optional: a Material (Unlit, Render Face = Both) that draws the road enemies walk on. Leave empty for no road")]
    public Material pathLineMaterial;

    [Tooltip("How wide the road is")]
    public float pathLineWidth = 2f;

    [Tooltip("How far above the ground the road floats, so it doesn't flicker")]
    public float pathLineHeight = 0.12f;

    [Tooltip("Distance between the road's points. Smaller = follows the terrain steps more closely")]
    public float pathLineStep = 1.5f;

    [Header("Path Protection")]
    [Tooltip("NoBuildZone radius around each waypoint - must cover the path width")]
    public float pathZoneRadius = 2.5f;

    [Tooltip("Extra padding added around each path zone")]
    public float pathZonePadding = 0.5f;

    [Tooltip("Also place a zone at the midpoint of each segment so the whole line is protected, not just the waypoints")]
    public bool protectSegments = true;
 
    const float RaycastHeightPadding = 500f;

    public Tower PlacedTower { get; private set; }

    // The seed used for the current layout, and how many paths it made (for the HUD or the video)
    public static int LastSeed { get; private set; }
    public int GeneratedPathCount { get; private set; }

    // What one path will look like, worked out before any objects are made
    private class PathPlan
    {
        public float angle;
        public Vector3 start;
        public float windAmount;
        public float noiseOffset;
        public int mergeInto = -1;   // index of the path this one merges into (-1 = none)
        public float mergeStart;     // how far along (0 to 1) the path starts curving toward the other one
        public float mergeEnd;       // how far along it has fully joined the other one
    }

    private readonly List<PathPlan> plans = new List<PathPlan>();
    private Vector3 towerPosition;
    private Bounds mapBounds;
 
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
        mapBounds = terrainBounds;

        PlaceTower(terrainBounds);

        if (PlacedTower == null)
        {
            Debug.LogError("[PathGenerator] Tower not found or placed. Cannot generate paths.");
            return;
        }

        towerPosition = PlacedTower.transform.position;

        // Every random decision below comes from this one seed
        LastSeed = useFixedSeed ? seed : Random.Range(0, 1000000);
        System.Random rng = new System.Random(LastSeed);

        int count = pathCount;

        if (randomPathCount)
        {
            int lowest = Mathf.Max(1, minPaths);
            int highest = Mathf.Max(lowest, maxPaths);
            count = rng.Next(lowest, highest + 1);
        }

        count = Mathf.Max(1, count);

        BuildPlans(count, terrainBounds, rng);

        for (int i = 0; i < plans.Count; i++)
        {
            CreatePath(i);
        }

        GeneratedPathCount = plans.Count;

        int merges = 0;
        foreach (PathPlan plan in plans)
        {
            if (plan.mergeInto >= 0) merges++;
        }

        Debug.Log($"[PathGenerator] Seed {LastSeed}: generated {plans.Count} pathways ({merges} merging) towards the tower.");
    }

    // Works out where every path starts, how much it winds and whether it merges
    private void BuildPlans(int count, Bounds bounds, System.Random rng)
    {
        plans.Clear();

        float spacing = 360f / count;
        float baseAngle = RandomRange(rng, 0f, 360f);

        for (int i = 0; i < count; i++)
        {
            PathPlan plan = new PathPlan();

            // Evenly spread around the map, then nudged so the layout is different every time
            float jitter = RandomRange(rng, -0.3f, 0.3f) * spacing;
            plan.angle = baseAngle + spacing * i + jitter;
            plan.start = GetEdgeStartPoint(bounds, plan.angle);
            plan.windAmount = windAmount * RandomRange(rng, 0.5f, 1.2f);
            plan.noiseOffset = RandomRange(rng, 0f, 100f);

            // Maybe merge into the previous path, but only if they start close enough to look natural
            if (i > 0 && rng.NextDouble() < mergeChance)
            {
                PathPlan neighbour = plans[i - 1];
                float gap = Mathf.Abs(Mathf.DeltaAngle(plan.angle, neighbour.angle));

                if (gap <= 130f)
                {
                    plan.mergeInto = i - 1;
                    plan.mergeStart = RandomRange(rng, 0.3f, 0.4f);
                    plan.mergeEnd = plan.mergeStart + RandomRange(rng, 0.3f, 0.4f);
                }
            }

            plans.Add(plan);
        }
    }

    private static float RandomRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    // A point a fraction t (0 = map edge, 1 = Tower) along the path, bent sideways by noise
    private Vector3 BasePoint(PathPlan plan, float t)
    {
        Vector3 flat = Vector3.Lerp(plan.start, towerPosition, t);

        Vector3 direction = towerPosition - plan.start;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return flat;
        }

        direction.Normalize();
        Vector3 sideways = new Vector3(-direction.z, 0f, direction.x);

        // Perlin noise gives 0 to 1, so this turns it into -1 to 1 (bend left or right)
        float noise = Mathf.PerlinNoise(plan.noiseOffset + t * windFrequency, plan.noiseOffset * 0.37f) * 2f - 1f;

        // Fades the bend to nothing at both ends, so the path always starts on the edge and ends at the Tower
        float fade = Mathf.Sin(t * Mathf.PI);

        return flat + sideways * (noise * fade * plan.windAmount);
    }

    // The final point on a path, including blending into the path it merges with
    private Vector3 PlanPoint(int index, float t)
    {
        PathPlan plan = plans[index];
        Vector3 point = BasePoint(plan, t);

        if (plan.mergeInto >= 0 && t > plan.mergeStart)
        {
            // Slides from this path's own line onto the other path's line, then follows it
            float blend = Mathf.SmoothStep(0f, 1f, (t - plan.mergeStart) / (plan.mergeEnd - plan.mergeStart));
            point = Vector3.Lerp(point, PlanPoint(plan.mergeInto, t), blend);
        }

        return point;
    }

    // Keeps a point inside the map
    private Vector3 ClampToMap(Vector3 point)
    {
        float margin = 1.5f;
        point.x = Mathf.Clamp(point.x, mapBounds.min.x + margin, mapBounds.max.x - margin);
        point.z = Mathf.Clamp(point.z, mapBounds.min.z + margin, mapBounds.max.z - margin);
        return point;
    }

    //Paths
    private void CreatePath(int index)
    {
        PathPlan plan = plans[index];

        GameObject pathObject = new GameObject("Path" + (index + 1));
        pathObject.transform.SetParent(transform);

        EnemyPath pathScript = pathObject.AddComponent<EnemyPath>();

        // Longer or windier paths get more waypoints, so the no-build zones always cover the whole path
        float pathLength = new Vector2(plan.start.x - towerPosition.x, plan.start.z - towerPosition.z).magnitude;
        float spacing = windAmount > 0f ? Mathf.Min(maxWaypointSpacing, 6f) : maxWaypointSpacing;
        int waypointCount = Mathf.Max(waypointsPerPath, Mathf.CeilToInt(pathLength * 1.2f / spacing));

        Vector3 previous = SnapToTerrain(ClampToMap(plan.start), towerPosition);
        AddWaypoint(pathObject.transform, pathScript, previous, 0);

        for (int w = 1; w <= waypointCount; w++)
        {
            float t = (float)w / waypointCount;
            Vector3 flatPoint = ClampToMap(PlanPoint(index, t));
            Vector3 snapped = SnapToTerrain(flatPoint, towerPosition);

            // Once this path has merged, the path it joined already protects the shared route
            bool sharedRoute = plan.mergeInto >= 0 && t > plan.mergeEnd;

            if (!sharedRoute)
            {
                // Protect the line between the previous waypoint and this one
                if (protectSegments)
                    AddZone(pathObject.transform, Vector3.Lerp(previous, snapped, 0.5f), "SegmentZone");

                AddZone(pathObject.transform, snapped, "PathZone");
            }

            AddWaypoint(pathObject.transform, pathScript, snapped, w);

            previous = snapped;
        }

        AddZone(pathObject.transform, pathScript.GetWaypointPosition(0), "SpawnZone");

        BuildPathLine(pathObject.transform, pathScript);
    }

    // Draws the road along the path, following the terrain up and down its steps
    private void BuildPathLine(Transform pathParent, EnemyPath pathScript)
    {
        if (pathLineMaterial == null || pathScript.WaypointCount < 2) return;

        List<Vector3> points = new List<Vector3>();
        Vector3 end = pathScript.GetWaypointPosition(pathScript.WaypointCount - 1);
        bool finished = false;

        for (int i = 1; i < pathScript.WaypointCount && !finished; i++)
        {
            Vector3 from = pathScript.GetWaypointPosition(i - 1);
            Vector3 to = pathScript.GetWaypointPosition(i);

            Vector3 flatDelta = to - from;
            flatDelta.y = 0f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(flatDelta.magnitude / Mathf.Max(0.5f, pathLineStep)));

            // The first segment also includes its starting point
            int firstStep = (i == 1) ? 0 : 1;

            for (int s = firstStep; s <= steps; s++)
            {
                Vector3 flat = Vector3.Lerp(from, to, (float)s / steps);

                Vector3 toEnd = end - flat;
                toEnd.y = 0f;

                // Stop just before the Tower so the road doesn't climb up its side
                if (toEnd.magnitude < 2.5f)
                {
                    finished = true;
                    break;
                }

                Vector3 onGround = SnapToTerrain(flat, end);
                points.Add(onGround + Vector3.up * pathLineHeight);
            }
        }

        if (points.Count < 2) return;

        GameObject lineObject = new GameObject("PathLine");
        lineObject.transform.SetParent(pathParent);

        // The line lies flat by facing straight up
        lineObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.TransformZ;
        line.sharedMaterial = pathLineMaterial;
        line.startWidth = pathLineWidth;
        line.endWidth = pathLineWidth;
        line.numCornerVertices = 4;
        line.numCapVertices = 2;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
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
        {
            Debug.LogError("[PathGenerator] No Tower prefab assigned and no Tower found in the scene.");
            return;
        }

        // The tower's pivot is in its middle, so lift it until its bottom sits on the ground
        UpgradeVisuals.SitOnGround(PlacedTower.transform, towerPos.y);
        
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

    // A point on the map's edge at the given angle around the centre
    private Vector3 GetEdgeStartPoint(Bounds bounds, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
 
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
