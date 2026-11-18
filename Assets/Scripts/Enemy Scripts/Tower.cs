using UnityEngine;

public class Tower : MonoBehaviour, IDamageable, IUpgradable
{
    [Header("Health")]
    [Tooltip("The tower's starting and maximum health")]
    public int maxHealth = 100;

    [Tooltip("Current health (read-only while playing, resets on Start)")]
    public int currentHealth;
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [Header("Attack")]
    [Tooltip("How far the tower can reach to attack enemies")]
    public float attackRange = 8f;

    [Tooltip("Damage dealt per attack")]
    public int attackDamage = 10;

    [Tooltip("Seconds between each attack")]
    public float attackInterval = 1f;

    [Tooltip("Enemies must use this tag so the tower can find them (create this Tag in Unity's Tag Manager if it doesn't already exist)")]
    public string enemyTag = "Enemy";

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarPrefab;
    [Tooltip("How high above the tower's centre the bar floats. Raise it if the bar is hidden inside the tower")]
    [SerializeField] private float healthBarHeight = 4f;

    [Header("Upgrades")]
    [Tooltip("Total levels. 3 = the starting level plus 2 upgrades")]
    [SerializeField] private int maxLevel = 3;
    [Tooltip("Price of upgrade to level 2, then to level 3, and so on")]
    [SerializeField] private int[] upgradeCosts = { 100, 200 };
    [Tooltip("Each upgrade adds this fraction of the BASE value. 0.6 = +60% health per upgrade")]
    [SerializeField] private float healthBonusPerLevel = 0.6f;
    [SerializeField] private float damageBonusPerLevel = 0.4f;
    [Tooltip("Each upgrade makes the tower attack this fraction faster (shorter interval)")]
    [SerializeField] private float attackSpeedBonusPerLevel = 0.15f;
    [Tooltip("Extra range (in units) added per upgrade")]
    [SerializeField] private float rangeBonusPerLevel = 1f;
    [Header("Upgrade Visuals")]
    [Tooltip("Colour tint for level 1, 2, 3... White = no change")]
    [SerializeField] private Color[] levelColors = { Color.white, new Color(0.5f, 0.8f, 1f), new Color(1f, 0.85f, 0.3f) };
    [Tooltip("Tower grows by this fraction per upgrade. 0.1 = 10% bigger each level")]
    [SerializeField] private float scaleGrowthPerLevel = 0.1f;

    private int level = 1;
    private UpgradeVisuals visuals;
    private int baseMaxHealth;
    private int baseAttackDamage;
    private float baseAttackInterval;
    private float baseAttackRange;

    public string DisplayName => "Tower";
    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => level >= maxLevel;

    private bool isDestroyed = false;
    private float attackTimer = 0f;

    public static event System.Action<Tower> OnTowerSpawned;

    private void Awake()
    {
        // Remember the starting stats so every upgrade is worked out from the base values
        baseMaxHealth = maxHealth;
        baseAttackDamage = attackDamage;
        baseAttackInterval = attackInterval;
        baseAttackRange = attackRange;
        visuals = new UpgradeVisuals(transform);

        OnTowerSpawned?.Invoke(this);
    }
    private void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();
    }

    private void CreateHealthBar()
    {
        if (healthBarPrefab == null)
            return;

        GameObject bar = Instantiate(healthBarPrefab);

        WorldHealthBar healthBar = bar.GetComponent<WorldHealthBar>();

        if (healthBar != null)
        {
            healthBar.SetTarget(transform, healthBarHeight);
        }
    }

    private void Update()
    {
        if (isDestroyed)
        {
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            AttackNearestEnemy();
        }
    }

    private void AttackNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(enemyTag))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(attackDamage);
                    return;
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log("Tower took " + amount + " damage. Health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    // ---------------- Upgrades ----------------

    public int NextUpgradeCost
    {
        get
        {
            if (IsMaxLevel || upgradeCosts == null || upgradeCosts.Length == 0)
                return 0;

            int index = Mathf.Min(level - 1, upgradeCosts.Length - 1);
            return upgradeCosts[index];
        }
    }

    public bool TryUpgrade()
    {
        if (isDestroyed || IsMaxLevel)
            return false;

        if (ResourceManager.Instance == null || !ResourceManager.Instance.TrySpend(NextUpgradeCost))
            return false;

        level++;

        int oldMaxHealth = maxHealth;
        ApplyLevelStats();

        // Heal by however much the max health went up
        currentHealth += maxHealth - oldMaxHealth;

        Debug.Log("Tower upgraded to level " + level);
        return true;
    }

    private void ApplyLevelStats()
    {
        int steps = level - 1;

        maxHealth = Mathf.RoundToInt(baseMaxHealth * (1f + healthBonusPerLevel * steps));
        attackDamage = Mathf.RoundToInt(baseAttackDamage * (1f + damageBonusPerLevel * steps));
        attackInterval = Mathf.Max(0.2f, baseAttackInterval * (1f - attackSpeedBonusPerLevel * steps));
        attackRange = baseAttackRange + rangeBonusPerLevel * steps;

        Color tint = Color.white;
        if (levelColors != null && levelColors.Length > 0)
            tint = levelColors[Mathf.Min(steps, levelColors.Length - 1)];

        visuals.Apply(tint, 1f + scaleGrowthPerLevel * steps);
    }

    public string GetUpgradeSummary()
    {
        float fireRate = 1f / attackInterval;

        if (IsMaxLevel)
        {
            // Nothing left to buy, so just show the current stats
            return "Health " + maxHealth + "\n"
                 + "Damage " + attackDamage + "\n"
                 + "Fire rate " + fireRate.ToString("0.0") + "\n"
                 + "Range " + attackRange.ToString("0.#");
        }

        int steps = level; // the steps the NEXT level would have

        int nextHealth = Mathf.RoundToInt(baseMaxHealth * (1f + healthBonusPerLevel * steps));
        int nextDamage = Mathf.RoundToInt(baseAttackDamage * (1f + damageBonusPerLevel * steps));
        float nextInterval = Mathf.Max(0.2f, baseAttackInterval * (1f - attackSpeedBonusPerLevel * steps));
        float nextRange = baseAttackRange + rangeBonusPerLevel * steps;

        return "Health " + maxHealth + " > " + nextHealth + "\n"
             + "Damage " + attackDamage + " > " + nextDamage + "\n"
             + "Fire rate " + fireRate.ToString("0.0") + " > " + (1f / nextInterval).ToString("0.0") + "\n"
             + "Range " + attackRange.ToString("0.#") + " > " + nextRange.ToString("0.#");
    }

    // Right-click the component header in the Inspector while playing to test an upgrade for free
    [ContextMenu("Test Upgrade (free)")]
    private void TestUpgrade()
    {
        if (IsMaxLevel) return;
        ResourceManager.Instance?.Add(NextUpgradeCost);
        TryUpgrade();
    }

    private void Die()
    {
        isDestroyed = true;
        Debug.Log("Tower destroyed! GAME OVER.");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
    }
}