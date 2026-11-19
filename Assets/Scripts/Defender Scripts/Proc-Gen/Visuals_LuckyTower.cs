using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the tower model from primitives. Proportions come from the stat shares:
/// damage = wide body, health = tall body, range = long barrel, fire rate = big head.
/// Trait parts (gems, the second barrel) exist from the start and are switched on
/// with SetActiveTraits, so upgrading never has to rebuild the model.
/// </summary>
public class LuckyTowerVisuals
{
    private static readonly PrimitiveType[] BaseShapes = { PrimitiveType.Cylinder, PrimitiveType.Cube };
    private static readonly PrimitiveType[] BodyShapes = { PrimitiveType.Cylinder, PrimitiveType.Cube, PrimitiveType.Capsule };
    private static readonly PrimitiveType[] HeadShapes = { PrimitiveType.Capsule, PrimitiveType.Cube, PrimitiveType.Cylinder };

    private const float BaseHeight = 0.3f;
    private const float BaseWidth = 1.4f;

    private readonly Dictionary<LuckyTrait, GameObject> gems = new Dictionary<LuckyTrait, GameObject>();
    private readonly List<Transform> muzzles = new List<Transform>();
    private readonly GameObject[] twinBarrels = new GameObject[2];
    private readonly Transform[] twinMuzzles = new Transform[2];
    private GameObject centerBarrel;
    private Transform centerMuzzle;

    /// <summary>Where projectiles leave the tower: one muzzle normally, two with Twin.</summary>
    public IReadOnlyList<Transform> Muzzles => muzzles;

    public static LuckyTowerVisuals Build(Transform tower, LuckyTowerData d)
    {
        LuckyTowerVisuals model = new LuckyTowerVisuals();

        Transform root = new GameObject("LuckyVisuals").transform;
        root.SetParent(tower, false);

        // A typical share is ~0.25, so doubling maps it to roughly 0.5
        float bodyWidth = Mathf.Lerp(0.6f, 1.1f, Norm(d.damageShare));
        float bodyHeight = Mathf.Lerp(0.7f, 1.5f, Norm(d.healthShare));
        float headSize = Mathf.Lerp(0.5f, 0.9f, Norm(d.fireRateShare));
        float barrelLength = Mathf.Lerp(0.7f, 1.8f, Norm(d.rangeShare));

        Color primary = d.primaryColor;
        Color accent = d.accentColor;
        Color glow = d.rarity == LuckyRarity.Common ? Color.black : accent * (0.4f * (int)d.rarity);

        // Base
        AddPart(root, BaseShapes[d.baseShape], "Base",
            new Vector3(0f, BaseHeight * 0.5f, 0f),
            new Vector3(BaseWidth, BaseHeight, BaseWidth), Darken(primary, 0.6f), Color.black);

        // Accent ring on top of the base (carries the rarity glow)
        AddPart(root, PrimitiveType.Cylinder, "Trim",
            new Vector3(0f, BaseHeight, 0f),
            new Vector3(BaseWidth * 0.85f, 0.08f, BaseWidth * 0.85f), accent, glow);

        // Body
        AddPart(root, BodyShapes[d.bodyShape], "Body",
            new Vector3(0f, BaseHeight + bodyHeight * 0.5f, 0f),
            new Vector3(bodyWidth, bodyHeight, bodyWidth), primary, Color.black);

        // Head
        float headCenterY = BaseHeight + bodyHeight + headSize * 0.5f;
        AddPart(root, HeadShapes[d.headShape], "Head",
            new Vector3(0f, headCenterY, 0f),
            new Vector3(headSize, headSize, headSize), Darken(primary, 1.15f), Color.black);

        // Barrels: one in the middle, or two side by side once the tower has Twin
        float startZ = headSize * 0.4f;
        float twinOffset = headSize * 0.25f;

        model.centerBarrel = AddBarrel(root, 0f, headCenterY, startZ, barrelLength, accent, glow, out model.centerMuzzle);
        model.twinBarrels[0] = AddBarrel(root, -twinOffset, headCenterY, startZ, barrelLength, accent, glow, out model.twinMuzzles[0]);
        model.twinBarrels[1] = AddBarrel(root, twinOffset, headCenterY, startZ, barrelLength, accent, glow, out model.twinMuzzles[1]);

        // One gem per trait on the body (Twin is shown by its second barrel instead)
        float gemRadius = bodyWidth * 0.5f + 0.08f;
        float midY = BaseHeight + bodyHeight * 0.5f;
        AddGem(model, root, LuckyTrait.Chill, new Vector3(gemRadius, midY, 0f));
        AddGem(model, root, LuckyTrait.Burn, new Vector3(-gemRadius, midY, 0f));
        AddGem(model, root, LuckyTrait.Crit, new Vector3(0f, midY, -gemRadius));
        AddGem(model, root, LuckyTrait.Sturdy, new Vector3(0f, BaseHeight + bodyHeight * 0.25f, gemRadius));

        // Existing defenders have their pivot in the middle, so centre the model around the origin
        float totalHeight = BaseHeight + bodyHeight + headSize;
        root.localPosition = new Vector3(0f, -totalHeight * 0.5f, 0f);

        return model;
    }

    /// <summary>Shows the parts for the given traits and hides the rest.</summary>
    public void SetActiveTraits(List<LuckyTrait> active)
    {
        foreach (KeyValuePair<LuckyTrait, GameObject> gem in gems)
            gem.Value.SetActive(active.Contains(gem.Key));

        bool twin = active.Contains(LuckyTrait.Twin);
        centerBarrel.SetActive(!twin);
        twinBarrels[0].SetActive(twin);
        twinBarrels[1].SetActive(twin);

        muzzles.Clear();
        if (twin)
        {
            muzzles.Add(twinMuzzles[0]);
            muzzles.Add(twinMuzzles[1]);
        }
        else
        {
            muzzles.Add(centerMuzzle);
        }
    }

    private static GameObject AddBarrel(Transform root, float x, float y, float startZ, float length,
        Color color, Color glow, out Transform muzzle)
    {
        GameObject barrel = AddPart(root, PrimitiveType.Cube, "Barrel",
            new Vector3(x, y, startZ + length * 0.5f),
            new Vector3(0.14f, 0.14f, length), color, glow);

        // The barrel is a unit cube scaled to its length, so z = 0.5 is always the tip
        GameObject m = new GameObject("Muzzle");
        m.transform.SetParent(barrel.transform, false);
        m.transform.localPosition = new Vector3(0f, 0f, 0.5f);
        muzzle = m.transform;

        return barrel;
    }

    private static void AddGem(LuckyTowerVisuals model, Transform root, LuckyTrait trait, Vector3 position)
    {
        Color c = LuckyTowerGenerator.TraitColor(trait);
        GameObject gem = AddPart(root, PrimitiveType.Cube, trait + "Gem",
            position, new Vector3(0.28f, 0.28f, 0.28f), c, c * 0.8f);

        gem.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
        model.gems[trait] = gem;
    }

    private static float Norm(float share)
    {
        return Mathf.Clamp01(share * 2f);
    }

    private static Color Darken(Color c, float factor)
    {
        return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor), Mathf.Clamp01(c.b * factor), 1f);
    }

    private static GameObject AddPart(Transform parent, PrimitiveType shape, string name,
        Vector3 localCenter, Vector3 size, Color color, Color emission)
    {
        GameObject go = GameObject.CreatePrimitive(shape);
        go.name = name;
        go.layer = parent.gameObject.layer;

        // Visual only: the tower keeps whatever collider its prefab has
        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.Destroy(col);

        Transform t = go.transform;
        t.SetParent(parent, false);
        t.localPosition = localCenter;

        // Unity's cylinder and capsule are 2 units tall at scale 1, cubes are 1
        bool twoUnitsTall = shape == PrimitiveType.Cylinder || shape == PrimitiveType.Capsule;
        t.localScale = twoUnitsTall ? new Vector3(size.x, size.y * 0.5f, size.z) : size;

        Material mat = go.GetComponent<Renderer>().material;
        mat.color = color;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (emission.maxColorComponent > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emission);
        }

        return go;
    }
}