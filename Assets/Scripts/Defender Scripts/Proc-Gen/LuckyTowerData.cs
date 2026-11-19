using System;
using System.Collections.Generic;
using UnityEngine;

public enum LuckyRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum LuckyTrait { Chill, Burn, Crit, Twin, Sturdy }

/// <summary>The result of one lucky roll. Pure data, no scene references.</summary>
[Serializable]
public class LuckyTowerData
{
    // Trait tuning lives here so the tester's DPS estimate matches the real tower
    public const float ChillDuration = 0.3f;
    public const float CritChance = 0.2f;
    public const float CritMultiplier = 2.5f;
    public const float TwinDamageMultiplier = 0.6f;
    public const float BurnDuration = 3f;
    public const float BurnTickInterval = 1f;
    public const float BurnDamageFraction = 0.25f; // of one shot's damage, per tick

    public int seed;
    public LuckyRarity rarity;

    // Final stats at level 1
    public float damage;
    public float range;
    public float fireRate;
    public int maxHealth;

    // Fraction (0-1) of the stat budget each stat received. Drives the visuals.
    public float damageShare;
    public float rangeShare;
    public float fireRateShare;
    public float healthShare;

    // Traits the tower is built with
    public List<LuckyTrait> traits = new List<LuckyTrait>();

    // Traits granted by upgrades, in order: level 2 gets [0], level 3 gets [1].
    // Never contains Sturdy (see LuckyDefender for why).
    public List<LuckyTrait> upgradeTraits = new List<LuckyTrait>();

    // Look
    public Color primaryColor;
    public Color accentColor;
    public int baseShape;
    public int bodyShape;
    public int headShape;

    public bool Has(LuckyTrait trait) => traits.Contains(trait);

    // True if the tower has the trait after buying the given number of upgrades
    private bool HasAfter(LuckyTrait trait, int upgradesApplied)
    {
        if (traits.Contains(trait))
            return true;

        int index = upgradeTraits.IndexOf(trait);
        return index >= 0 && index < upgradesApplied;
    }

    public float EstimatedDps => GetDps(0);

    /// <summary>Rough DPS from damage, fire rate and traits only (ignores the per-level stat bonuses).</summary>
    public float GetDps(int upgradesApplied)
    {
        bool twin = HasAfter(LuckyTrait.Twin, upgradesApplied);
        bool crit = HasAfter(LuckyTrait.Crit, upgradesApplied);
        bool burn = HasAfter(LuckyTrait.Burn, upgradesApplied);

        float shotDamage = damage * (twin ? TwinDamageMultiplier : 1f);
        float perShot = shotDamage * (crit ? 1f + CritChance * (CritMultiplier - 1f) : 1f);

        float dps = perShot * (twin ? 2f : 1f) * fireRate;

        // Burn doesn't stack, so it adds one burn's worth no matter how many barrels hit
        if (burn)
            dps += shotDamage * BurnDamageFraction / BurnTickInterval;

        return dps;
    }
}