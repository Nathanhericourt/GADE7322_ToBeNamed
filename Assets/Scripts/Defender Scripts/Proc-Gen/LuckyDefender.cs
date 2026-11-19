using System.Collections.Generic;
using UnityEngine;

// Gamble defender. Flat price, but stats, traits and look are rolled when it is built.
// Each upgrade grants one more trait from the same seed.
public class LuckyDefender : Defender
{
    public override string DisplayName => data != null ? $"Lucky ({data.rarity})" : "Lucky";

    [Header("Lucky Roll")]
    [Tooltip("0 = random seed every time one is built. Enter a number to force a specific tower for testing.")]
    [SerializeField] private int seedOverride = 0;

    private LuckyTowerData data;
    private LuckyTowerVisuals model;
    private readonly List<LuckyTrait> activeTraits = new List<LuckyTrait>();

    /// <summary>The rolled data, e.g. for showing rarity in the upgrade panel.</summary>
    public LuckyTowerData Data => data;

    /// <summary>Traits the tower has right now (starting traits plus any gained from upgrades).</summary>
    public IReadOnlyList<LuckyTrait> ActiveTraits => activeTraits;

    protected override void ApplyTypeDefaults()
    {
        // The price is fixed: paying a flat cost for a random result is the gamble.
        // The build UI and placement manager read Cost from the prefab, so it can't be rolled.
        cost = 80;
        maxHealth = 70;
        range = 8f;
        turnSpeedDegPerSec = 180f;
        fireRate = 0.8f;
        damage = 5f;
    }

    protected override void Awake()
    {
        if (projectilePrefab == null)
            Debug.LogError("LuckyDefender has no Projectile Prefab assigned.", this);

        // Roll first: Defender.Awake records the current stats as its "base" for upgrade maths,
        // and builds its UpgradeVisuals from the renderers that exist at that moment.
        Roll();
        base.Awake();
    }

    private void Roll()
    {
        int seed = seedOverride != 0 ? seedOverride : Random.Range(1, int.MaxValue);
        data = LuckyTowerGenerator.Generate(seed);

        damage = data.damage;
        range = data.range;
        fireRate = data.fireRate;
        maxHealth = data.maxHealth;

        activeTraits.Clear();
        activeTraits.AddRange(data.traits);

        model = LuckyTowerVisuals.Build(transform, data);
        model.SetActiveTraits(activeTraits);
        firePoint = model.Muzzles[0];
    }

    // Called by Defender.TryUpgrade after the new level's stats are applied
    protected override void OnUpgraded()
    {
        // Level 2 grants upgradeTraits[0], level 3 grants upgradeTraits[1], and so on
        int index = Level - 2;
        if (data == null || index < 0 || index >= data.upgradeTraits.Count)
            return;

        LuckyTrait gained = data.upgradeTraits[index];
        if (activeTraits.Contains(gained))
            return;

        activeTraits.Add(gained);
        model.SetActiveTraits(activeTraits);
        firePoint = model.Muzzles[0];

        Debug.Log($"{DisplayName} gained the {gained} trait.");
    }

    protected override void Fire()
    {
        if (data == null || model == null || projectilePrefab == null)
            return;

        bool twin = activeTraits.Contains(LuckyTrait.Twin);
        bool canCrit = activeTraits.Contains(LuckyTrait.Crit);
        bool burn = activeTraits.Contains(LuckyTrait.Burn);
        float freeze = activeTraits.Contains(LuckyTrait.Chill) ? LuckyTowerData.ChillDuration : 0f;

        foreach (Transform muzzle in model.Muzzles)
        {
            // Uses the inherited damage field, so level-ups from the upgrade panel still apply
            float shotDamage = damage * (twin ? LuckyTowerData.TwinDamageMultiplier : 1f);

            if (canCrit && Random.value < LuckyTowerData.CritChance)
                shotDamage *= LuckyTowerData.CritMultiplier;

            // Projectile truncates damage to an int, so round here to keep fractional stats meaningful
            shotDamage = Mathf.Max(1f, Mathf.Round(shotDamage));

            GameObject projObj = Instantiate(projectilePrefab, muzzle.position, muzzle.rotation);

            Renderer rend = projObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = data.accentColor;
                rend.material.SetColor("_BaseColor", data.accentColor);
            }

            Projectile proj = projObj.GetComponent<Projectile>();
            if (proj == null)
                continue;

            proj.Launch(currentTarget, shotDamage, freeze);

            if (burn)
            {
                int tickDamage = Mathf.Max(1, Mathf.RoundToInt(shotDamage * LuckyTowerData.BurnDamageFraction));
                proj.OnHitEnemy = enemy => BurnEffect.Apply(enemy, tickDamage,
                    LuckyTowerData.BurnDuration, LuckyTowerData.BurnTickInterval);
            }
        }
    }
}