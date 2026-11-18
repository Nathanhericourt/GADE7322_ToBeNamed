using UnityEngine;

/// <summary>
/// Represents a single point on the map where a tower can be built.
/// Attach this to the spot's GameObject (needs a Collider for click/raycast detection).
/// </summary>
public class PlacementSpot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public GameObject CurrentTower { get; private set; }
    public static readonly System.Collections.Generic.List<PlacementSpot> All = new System.Collections.Generic.List<PlacementSpot>();

    // Only the spots that currently have a defender on them
    private static readonly System.Collections.Generic.List<PlacementSpot> Occupied = new System.Collections.Generic.List<PlacementSpot>();

    [Header("Visuals (optional)")]
    [SerializeField] private Renderer indicatorRenderer; // e.g. a flat disc/decal mesh
    [SerializeField] private Color freeColor = new Color(0f, 1f, 0f, 0.4f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0f, 0.6f);

    private MaterialPropertyBlock propBlock;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Awake()
    {
        propBlock = new MaterialPropertyBlock();

        if (indicatorRenderer == null)
            indicatorRenderer = GetComponent<Renderer>();

        RefreshVisual();
    }

    /// <summary>Attempts to build a tower on this spot. Returns false if already occupied.</summary>
    public bool TryPlaceTower(GameObject towerPrefab, out GameObject spawnedTower)
    {
        spawnedTower = null;

        if (IsOccupied || towerPrefab == null)
            return false;

        spawnedTower = Instantiate(towerPrefab, transform.position, Quaternion.identity, transform);

        // The defender's pivot is in its middle, so lift it until its bottom sits on the ground
        UpgradeVisuals.SitOnGround(spawnedTower.transform, transform.position.y);

        CurrentTower = spawnedTower;
        IsOccupied = true;
        Occupied.Add(this);
        RefreshVisual();
        return true;
    }

    /// <summary>Removes the current tower (e.g. for selling / upgrading-by-replace).</summary>
    public void RemoveTower()
    {
        if (CurrentTower != null)
            Destroy(CurrentTower);

        CurrentTower = null;
        IsOccupied = false;
        Occupied.Remove(this);
        RefreshVisual();
    }

    // Frees any spot whose defender was destroyed (killed by enemies) so the green cube comes back
    public static void FreeSpotsWithDeadDefenders()
    {
        for (int i = Occupied.Count - 1; i >= 0; i--)
        {
            PlacementSpot spot = Occupied[i];

            if (spot == null)
            {
                Occupied.RemoveAt(i);
                continue;
            }

            if (spot.CurrentTower == null)
                spot.RemoveTower();
        }
    }

    public void SetHover(bool isHovering)
    {
        // Occupied spots are hidden, so nothing to highlight
        if (IsOccupied)
            return;

        SetColor(isHovering ? hoverColor : (IsOccupied ? occupiedColor : freeColor));
    }

    private void RefreshVisual()
    {
        // The green cube disappears while a defender is standing on the spot
        if (indicatorRenderer != null)
            indicatorRenderer.enabled = !IsOccupied;
            
        SetColor(IsOccupied ? occupiedColor : freeColor);
    }

    private void SetColor(Color color)
    {
        if (indicatorRenderer == null) return;
        indicatorRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor(ColorId, color);
        propBlock.SetColor(BaseColorId, color);
        indicatorRenderer.SetPropertyBlock(propBlock);
    }
}