using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns a seed into a LuckyTowerData. Plain static class (no MonoBehaviour),
/// so the same seed always gives the same tower and it can be tested in bulk.
/// </summary>
public static class LuckyTowerGenerator
{
    // Stats at zero spent points. Tune these to sit alongside your other defenders.
    public const float BaseDamage = 5f;
    public const float BaseRange = 8f;
    public const float BaseFireRate = 0.8f;
    public const int BaseHealth = 70;

    // How much one budget point is worth in each stat
    private const float DamagePerPoint = 0.10f;
    private const float RangePerPoint = 0.03f;
    private const float FireRatePerPoint = 0.05f;
    private const float HealthPerPoint = 0.06f;

    private struct Tier
    {
        public LuckyRarity rarity;
        public int weight;      // chance relative to the other tiers
        public float budget;    // points split across the stats
        public int traitSlots;  // traits it starts with (max 2, so two upgrade traits always remain)
        public Color color;
    }

    private static readonly Tier[] Tiers =
    {
        new Tier { rarity = LuckyRarity.Common,    weight = 50, budget = 6f,  traitSlots = 0, color = new Color(0.75f, 0.75f, 0.75f) },
        new Tier { rarity = LuckyRarity.Uncommon,  weight = 30, budget = 10f, traitSlots = 0, color = new Color(0.35f, 0.9f, 0.35f) },
        new Tier { rarity = LuckyRarity.Rare,      weight = 15, budget = 15f, traitSlots = 1, color = new Color(0.3f, 0.55f, 1f) },
        new Tier { rarity = LuckyRarity.Epic,      weight = 4,  budget = 22f, traitSlots = 2, color = new Color(0.7f, 0.3f, 1f) },
        new Tier { rarity = LuckyRarity.Legendary, weight = 1,  budget = 32f, traitSlots = 2, color = new Color(1f, 0.8f, 0.2f) },
    };

    private static readonly Color[] NeutralColors =
    {
        new Color(0.45f, 0.5f, 0.6f),
        new Color(0.5f, 0.55f, 0.35f),
        new Color(0.6f, 0.4f, 0.3f),
        new Color(0.45f, 0.35f, 0.55f),
        new Color(0.3f, 0.55f, 0.55f),
    };

    public static LuckyTowerData Generate(int seed)
    {
        System.Random rng = new System.Random(seed);
        LuckyTowerData data = new LuckyTowerData { seed = seed };

        // 1. Rarity decides the stat budget and number of starting traits
        Tier tier = RollTier(rng);
        data.rarity = tier.rarity;

        // 2. Split the budget across the four stats. The 0.15 floor stops any stat hitting zero.
        float[] shares = new float[4];
        float sum = 0f;
        for (int i = 0; i < shares.Length; i++)
        {
            shares[i] = 0.15f + (float)rng.NextDouble();
            sum += shares[i];
        }
        for (int i = 0; i < shares.Length; i++)
            shares[i] /= sum;

        data.damageShare = shares[0];
        data.rangeShare = shares[1];
        data.fireRateShare = shares[2];
        data.healthShare = shares[3];

        data.damage = BaseDamage * (1f + tier.budget * shares[0] * DamagePerPoint);
        data.range = BaseRange * (1f + tier.budget * shares[1] * RangePerPoint);
        data.fireRate = BaseFireRate * (1f + tier.budget * shares[2] * FireRatePerPoint);
        data.maxHealth = Mathf.RoundToInt(BaseHealth * (1f + tier.budget * shares[3] * HealthPerPoint));

        // 3. Shuffle the traits once. The first few are the starting traits,
        //    the rest (minus Sturdy) become the upgrade traits in that same order.
        List<LuckyTrait> pool = new List<LuckyTrait>
        {
            LuckyTrait.Chill, LuckyTrait.Burn, LuckyTrait.Crit, LuckyTrait.Twin, LuckyTrait.Sturdy
        };
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        for (int i = 0; i < tier.traitSlots; i++)
            data.traits.Add(pool[i]);

        for (int i = tier.traitSlots; i < pool.Count; i++)
        {
            if (pool[i] != LuckyTrait.Sturdy)
                data.upgradeTraits.Add(pool[i]);
        }

        if (data.Has(LuckyTrait.Sturdy))
            data.maxHealth = Mathf.RoundToInt(data.maxHealth * 1.5f);

        // 4. Look: first starting trait sets the main colour, rarity sets the accent/glow
        Color neutral = NeutralColors[rng.Next(NeutralColors.Length)];
        data.primaryColor = data.traits.Count > 0 ? TraitColor(data.traits[0]) : neutral;
        data.accentColor = tier.color;

        data.baseShape = rng.Next(2);
        data.bodyShape = rng.Next(3);
        data.headShape = rng.Next(3);

        return data;
    }

    private static Tier RollTier(System.Random rng)
    {
        int total = 0;
        foreach (Tier t in Tiers)
            total += t.weight;

        int roll = rng.Next(total);
        foreach (Tier t in Tiers)
        {
            if (roll < t.weight)
                return t;
            roll -= t.weight;
        }

        return Tiers[0];
    }

    public static Color TraitColor(LuckyTrait trait)
    {
        switch (trait)
        {
            case LuckyTrait.Chill: return new Color(0.35f, 0.85f, 1f);
            case LuckyTrait.Burn: return new Color(1f, 0.45f, 0.1f);
            case LuckyTrait.Crit: return new Color(0.9f, 0.2f, 0.2f);
            case LuckyTrait.Twin: return new Color(1f, 0.85f, 0.2f);
            default: return new Color(0.45f, 0.6f, 0.4f); // Sturdy
        }
    }
}