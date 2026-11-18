using UnityEngine;

// Changes how a defender / tower looks when it levels up.
// It tints every renderer on the object and makes the whole thing a bit bigger.
// Not a MonoBehaviour - Defender and Tower just create one with "new UpgradeVisuals(transform)".
public class UpgradeVisuals
{
    private readonly Transform root;
    private readonly Renderer[] renderers;
    private readonly Color[] originalColors;
    private readonly Vector3 baseScale;
    private readonly float bottomOffset;      // how far the bottom of the model is below its pivot
    private float currentMultiplier = 1f;
    private readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP shaders
    private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in shaders

    public UpgradeVisuals(Transform root)
    {
        this.root = root;
        baseScale = root.localScale;
        renderers = root.GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        // Remember each renderer's starting colour so we can tint from it
        for (int i = 0; i < renderers.Length; i++)
        {
            Material mat = renderers[i].sharedMaterial;
            originalColors[i] = Color.white;

            if (mat == null)
                continue;

            if (mat.HasProperty(BaseColorId))
                originalColors[i] = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId))
                originalColors[i] = mat.GetColor(ColorId);
        }

        // Remember how far the bottom is below the pivot, so growing doesn't sink it into the ground
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            bottomOffset = root.position.y - bounds.min.y;
        }
    }

    // tint = colour multiplied onto the original (white = no change)
    // ScaleMultiplier = 1 is the normal size.
    public void Apply(Color tint, float scaleMultiplier)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || renderers[i].sharedMaterial == null)
                continue;

            Color finalColor = originalColors[i] * tint;

            renderers[i].GetPropertyBlock(block);
            block.SetColor(BaseColorId, finalColor);
            block.SetColor(ColorId, finalColor);
            renderers[i].SetPropertyBlock(block);
        }

        root.localScale = baseScale * scaleMultiplier;

        // Growing happens around the middle, so push it up to keep its feet on the ground
        root.position += Vector3.up * bottomOffset * (scaleMultiplier - currentMultiplier);
        currentMultiplier = scaleMultiplier;
    }

    // Moves an object up so its BOTTOM sits on groundY instead of its middle (pivot)
    public static void SitOnGround(Transform target, float groundY)
    {
        Renderer[] rends = target.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
            return;

        Bounds bounds = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
            bounds.Encapsulate(rends[i].bounds);

        target.position += Vector3.up * (groundY - bounds.min.y);
    }
}
