using UnityEngine;

/// <summary>
/// Represents a single point on the map where a tower can be built.
/// Attach this to the spot's GameObject (needs a Collider for click/raycast detection).
/// </summary>
public class PlacementSpot : MonoBehaviour
{
    public bool IsOccupied { get; private set; }
    public GameObject CurrentTower { get; private set; }

    [Header("Visuals (optional)")]
    [SerializeField] private Renderer indicatorRenderer; // e.g. a flat disc/decal mesh
    [SerializeField] private Color freeColor = new Color(0f, 1f, 0f, 0.4f);
    [SerializeField] private Color occupiedColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0f, 0.6f);

    private MaterialPropertyBlock propBlock;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

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
        CurrentTower = spawnedTower;
        IsOccupied = true;
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
        RefreshVisual();
    }

    public void SetHover(bool isHovering)
    {
        SetColor(isHovering ? hoverColor : (IsOccupied ? occupiedColor : freeColor));
    }

    private void RefreshVisual()
    {
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