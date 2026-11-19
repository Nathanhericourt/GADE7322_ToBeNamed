using UnityEngine;

// The procedural feature: every enemy that spawns gets its own random "DNA".
// Genes: a budget of power is split randomly between health, speed and damage.
// The split decides the enemy's size and colour, so you can SEE what it is good at.
// Green + big = tanky, blue + small = fast, red = hits hard
// Traits: from a certain wave on, enemies can roll a special trait (Armoured, Swift, etc).
// The chance goes up every wave. The boss always gets one, plus a generated name.
// The EnemySpawner calls Mutate() on every enemy right after it spawns.
// Put this script on the same object as the EnemySpawner (the spawner adds it by itself if you forget).
public class EnemyMutator : MonoBehaviour
{
    //The boss's generated name, so the HUD can show it
    public static string BossName { get; private set; } = "";

    [Header("Genes (random stats)")]
    [Tooltip("0 = every enemy is the same, 1 = enemies are very different from each other. 0.5 is a good start")]
    [Range(0f, 1f)] public float geneStrength = 0.5f;

    [Tooltip("Each of health, speed and damage rolls a random weight between these two numbers, then the three are balanced so the total power stays fair")]
    public float minWeight = 0.5f;
    public float maxWeight = 1.5f;

    [Tooltip("Genes can never make an enemy slower or faster than this (as a multiplier of normal)")]
    public float minSpeedMultiplier = 0.75f;
    public float maxSpeedMultiplier = 1.4f;

    [Header("Look")]
    [Tooltip("How much the enemy's colour changes. 0 = none, 1 = fully recoloured")]
    [Range(0f, 1f)] public float tintStrength = 0.8f;

    [Tooltip("Multiplies the tint colour so dark models still show it. 1 = normal, 2 = twice as bright")]
    public float tintBrightness = 1.8f;

    [Tooltip("Parts of the model that already glow (like eyes) glow in the enemy's colour. 0 = off")]
    public float glowStrength = 2.5f;

    [Tooltip("Tanky enemies get this much bigger and fragile ones smaller. 0 = size never changes")]
    public float sizeVariation = 0.4f;

    public Color tankyColor = new Color(0.3f, 1f, 0.3f);
    public Color fastColor = new Color(0.3f, 0.8f, 1f);
    public Color hardHittingColor = new Color(1f, 0.3f, 0.3f);

    [Header("Traits")]
    [Tooltip("Enemies can only roll a trait from this wave onward")]
    public int firstTraitWave = 3;

    [Tooltip("Trait chance grows by this much every wave from the first trait wave. 0.08 = +8% per wave")]
    [Range(0f, 1f)] public float traitChancePerWave = 0.08f;

    [Tooltip("The trait chance never goes above this")]
    [Range(0f, 1f)] public float maxTraitChance = 0.6f;

    [Tooltip("How common each trait is, in this order: Armoured, Swift, Regenerating, Shielded, Volatile. Higher = more common")]
    public int[] traitWeights = { 3, 3, 2, 2, 1 };

    [Tooltip("Enemies with a trait give this many times more resources when killed")]
    public float traitRewardMultiplier = 1.4f;

    [Header("Trait Strength")]
    [Tooltip("Armoured: only takes this fraction of every hit")]
    public float armouredDamageTaken = 0.6f;

    [Tooltip("Swift: walking speed is multiplied by this")]
    public float swiftSpeedMultiplier = 1.3f;

    [Tooltip("Regenerating: heals this fraction of max health per second")]
    public float regenPercentPerSecond = 0.04f;

    [Tooltip("Shielded: the shield is this fraction of the enemy's max health")]
    public float shieldPercentOfHealth = 0.4f;

    [Tooltip("Volatile: defenders this close to the dying enemy take damage")]
    public float volatileRadius = 4f;

    [Tooltip("Volatile: explosion damage = the enemy's attack damage x this")]
    public float volatileDamageMultiplier = 3f;

    [Header("Trait Colours")]
    public Color armouredColor = new Color(0.65f, 0.3f, 1f);
    public Color swiftColor = new Color(1f, 0.95f, 0.3f);
    public Color regeneratingColor = new Color(1f, 0.5f, 0.8f);
    public Color shieldedColor = new Color(0.3f, 0.4f, 1f);
    public Color volatileColor = new Color(1f, 0.55f, 0.1f);

    [Header("Trait Ring (glowing circle on the ground under enemies with a trait)")]
    [Tooltip("Make a Material (Unlit works best) and drag it in here. Leave empty to skip the rings")]
    public Material ringMaterial;
    public float ringSize = 1.8f;
    public float bossRingSize = 4.5f;

    [Header("Boss Names")]
    public string[] bossPrefixes = { "The Rusted", "The Iron", "The Howling", "The Hollow", "The Crimson", "The Unbroken" };
    public string[] bossNouns = { "Colossus", "Warden", "Titan", "Behemoth", "Reaper", "Juggernaut" };

    [Header("Testing")]
    [Tooltip("Writes a line to the Console for every mutated enemy")]
    public bool logMutations = true;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        BossName = "";
    }

    //Called by the EnemySpawner on every new enemy
    public void Mutate(Enemy enemy, bool isBoss)
    {
        if (enemy == null) return;

        int wave = WaveManager.Instance != null ? WaveManager.Instance.WaveNumber : 1;

        //Start with an even split. Normal enemies get random genes, the boss keeps its own stats
        float healthWeight = 1f;
        float speedWeight = 1f;
        float damageWeight = 1f;

        if (!isBoss)
        {
            RollGenes(enemy, out healthWeight, out speedWeight, out damageWeight);
        }

        //Colour from the genes: more health = greener, more speed = bluer, more damage = redder
        float weightTotal = healthWeight + speedWeight + damageWeight;
        Color color = (tankyColor * healthWeight + fastColor * speedWeight + hardHittingColor * damageWeight) / weightTotal;

        //Bigger for tanky, smaller for fragile. The boss keeps its size
        if (!isBoss && sizeVariation > 0f)
        {
            float size = Mathf.Clamp(1f + (healthWeight - 1f) * sizeVariation, 0.8f, 1.4f);
            ResizeModel(enemy.transform, size);
        }

        //Trait: a chance for normal enemies, guaranteed for the boss
        EnemyMutation.Trait trait = EnemyMutation.Trait.None;

        if (isBoss || Random.value < GetTraitChance(wave))
        {
            trait = RollTrait();
        }

        if (trait != EnemyMutation.Trait.None)
        {
            ApplyTrait(enemy, trait, isBoss);
            color = Color.Lerp(color, GetTraitColor(trait), 0.6f);
        }

        //The boss only shows its trait colour, genes don't tint it
        if (isBoss && trait != EnemyMutation.Trait.None)
        {
            color = GetTraitColor(trait);
        }

        ApplyTint(enemy.gameObject, color);

        //Enemies with a trait get a glowing ring on the ground, so you can spot them from far away
        if (trait != EnemyMutation.Trait.None)
        {
            CreateRing(enemy.gameObject, GetTraitColor(trait), isBoss);
        }

        string baseName = enemy.gameObject.name.Replace("(Clone)", "").Trim();

        if (isBoss)
        {
            BossName = RandomBossName() + " (" + trait + ")";
            enemy.gameObject.name = baseName + " [" + BossName + "]";
        }
        else if (trait != EnemyMutation.Trait.None)
        {
            enemy.gameObject.name = baseName + " [" + trait + "]";
        }

        if (logMutations)
        {
            Debug.Log("Mutated " + enemy.gameObject.name + " (wave " + wave + "): health " + enemy.maxHealth
                + ", speed " + enemy.moveSpeed.ToString("0.0") + ", damage " + enemy.attackDamage
                + ", reward " + enemy.resourceReward);
        }
    }

    //Splits a fixed amount of power randomly between health, speed and damage
    private void RollGenes(Enemy enemy, out float healthWeight, out float speedWeight, out float damageWeight)
    {
        float h = Random.Range(minWeight, maxWeight);
        float s = Random.Range(minWeight, maxWeight);
        float d = Random.Range(minWeight, maxWeight);
        float total = h + s + d;

        //The three weights always add up to 3, so an enemy can't be strong at everything
        healthWeight = h / total * 3f;
        speedWeight = s / total * 3f;
        damageWeight = d / total * 3f;

        float healthMultiplier = 1f + (healthWeight - 1f) * geneStrength;
        float speedMultiplier = 1f + (speedWeight - 1f) * geneStrength;
        float damageMultiplier = 1f + (damageWeight - 1f) * geneStrength;

        speedMultiplier = Mathf.Clamp(speedMultiplier, minSpeedMultiplier, maxSpeedMultiplier);

        enemy.maxHealth = Mathf.Max(1, Mathf.RoundToInt(enemy.maxHealth * healthMultiplier));
        enemy.moveSpeed = enemy.moveSpeed * speedMultiplier;
        enemy.attackDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.attackDamage * damageMultiplier));
    }

    private float GetTraitChance(int wave)
    {
        if (wave < firstTraitWave) return 0f;

        float chance = (wave - firstTraitWave + 1) * traitChancePerWave;
        return Mathf.Min(chance, maxTraitChance);
    }

    //Picks a trait, common ones more often than rare ones
    private EnemyMutation.Trait RollTrait()
    {
        int total = 0;
        for (int i = 0; i < traitWeights.Length; i++)
            total += Mathf.Max(0, traitWeights[i]);

        if (total <= 0) return EnemyMutation.Trait.None;

        int roll = Random.Range(0, total);

        for (int i = 0; i < traitWeights.Length; i++)
        {
            roll -= Mathf.Max(0, traitWeights[i]);

            //Trait numbers start at 1 because 0 is "None"
            if (roll < 0) return (EnemyMutation.Trait)(i + 1);
        }

        return EnemyMutation.Trait.None;
    }

    private void ApplyTrait(Enemy enemy, EnemyMutation.Trait trait, bool isBoss)
    {
        EnemyMutation mutation = enemy.gameObject.AddComponent<EnemyMutation>();
        mutation.trait = trait;

        switch (trait)
        {
            case EnemyMutation.Trait.Armoured:
                mutation.damageTakenMultiplier = armouredDamageTaken;
                break;

            case EnemyMutation.Trait.Swift:
                enemy.moveSpeed *= swiftSpeedMultiplier;
                break;

            case EnemyMutation.Trait.Regenerating:
                mutation.regenPercentPerSecond = regenPercentPerSecond;
                break;

            case EnemyMutation.Trait.Shielded:
                mutation.shield = Mathf.Max(1, Mathf.RoundToInt(enemy.maxHealth * shieldPercentOfHealth));
                break;

            case EnemyMutation.Trait.Volatile:
                mutation.explosionRadius = volatileRadius;
                mutation.explosionDamage = Mathf.Max(1, Mathf.RoundToInt(enemy.attackDamage * volatileDamageMultiplier));
                break;
        }

        //Dangerous enemies are worth more (the boss already has a big reward)
        if (!isBoss)
        {
            enemy.resourceReward = Mathf.RoundToInt(enemy.resourceReward * traitRewardMultiplier);
        }
    }

    private Color GetTraitColor(EnemyMutation.Trait trait)
    {
        switch (trait)
        {
            case EnemyMutation.Trait.Armoured: return armouredColor;
            case EnemyMutation.Trait.Swift: return swiftColor;
            case EnemyMutation.Trait.Regenerating: return regeneratingColor;
            case EnemyMutation.Trait.Shielded: return shieldedColor;
            case EnemyMutation.Trait.Volatile: return volatileColor;
        }

        return Color.white;
    }

    //Only the model (the children) is resized, so the enemy's collider and NavMesh size stay the same
    private void ResizeModel(Transform enemyRoot, float size)
    {
        foreach (Transform child in enemyRoot)
        {
            child.localScale *= size;
        }
    }

    //Tints every renderer, starting from its own colour so the textures still show
    private void ApplyTint(GameObject enemyObject, Color color)
    {
        Color tint = Color.Lerp(Color.white, color, tintStrength) * tintBrightness;
        tint.a = 1f;
        Renderer[] renderers = enemyObject.GetComponentsInChildren<Renderer>();
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        foreach (Renderer rend in renderers)
        {
            if (rend == null || rend.sharedMaterial == null) continue;
            if (rend is ParticleSystemRenderer || rend is TrailRenderer) continue;

            Color original = Color.white;
            Material mat = rend.sharedMaterial;

            if (mat.HasProperty(BaseColorId))
                original = mat.GetColor(BaseColorId);
            else if (mat.HasProperty(ColorId))
                original = mat.GetColor(ColorId);

            Color finalColor = original * tint;

            rend.GetPropertyBlock(block);
            block.SetColor(BaseColorId, finalColor);
            block.SetColor(ColorId, finalColor);

            //Only parts that already glow (emission switched on in their material) change their glow colour
            if (glowStrength > 0f && mat.IsKeywordEnabled("_EMISSION"))
            {
                block.SetColor(EmissionColorId, color * glowStrength);
            }

            rend.SetPropertyBlock(block);
        }
    }

    //A flat glowing disc that follows the enemy along the ground (see EnemyMutation.LateUpdate)
    private void CreateRing(GameObject enemyObject, Color color, bool isBoss)
    {
        if (ringMaterial == null) return;

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "TraitRing";

        //The disc is only for looking at, so it must not block clicks or physics
        Destroy(ring.GetComponent<Collider>());

        float size = isBoss ? bossRingSize : ringSize;
        ring.transform.localScale = new Vector3(size, 0.02f, size);

        Renderer rend = ring.GetComponent<Renderer>();
        rend.sharedMaterial = ringMaterial;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        rend.SetPropertyBlock(block);

        EnemyMutation mutation = enemyObject.GetComponent<EnemyMutation>();
        if (mutation != null)
        {
            mutation.ring = ring.transform;
        }
        else
        {
            Destroy(ring);
        }
    }

    private string RandomBossName()
    {
        if (bossPrefixes.Length == 0 || bossNouns.Length == 0) return "The Boss";

        return bossPrefixes[Random.Range(0, bossPrefixes.Length)] + " " + bossNouns[Random.Range(0, bossNouns.Length)];
    }
}
