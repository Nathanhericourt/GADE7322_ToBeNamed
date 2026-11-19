using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attach to a defender tower prefab. Automatically finds enemies within
/// range, aims at the closest one, and fires projectiles at a fixed rate.
/// Assumes enemies have a component implementing IDamageable (see below),
/// and are on the layer set in enemyLayer / tagged "Enemy".
/// </summary>
public class Defender : MonoBehaviour, IDamageable, IUpgradable
{
    public static readonly List<Defender> All = new List<Defender>();
    public Collider BodyCollider { get; private set; }
    public virtual string DisplayName => "Defender";

    [Header("Health")]
    [Tooltip("The defender's starting and maximum health")]
    [SerializeField] protected int maxHealth = 50;
    [Tooltip("Current health (read-only while playing, resets on Start)")]
    [SerializeField] protected int currentHealth;

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 2f;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    private bool isDestroyed = false;

    [Header("Cost")]
    [Tooltip("How many resources it costs to build this defender.")]
    [SerializeField] protected int cost = 50;
    public int Cost => cost;
    
    [Header("Targeting")]
    [SerializeField] protected float range = 8f;
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("How often (seconds) the tower re-scans for the closest target.")]
    [SerializeField] private float targetUpdateInterval = 0.2f;

    [Header("Aiming")]
    [Tooltip("Part of the tower that rotates to face the target (e.g. the turret head). Leave empty to rotate the whole tower.")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] protected float turnSpeedDegPerSec = 360f;

    [Header("Firing")]
    [SerializeField] protected float fireRate = 1f; // shots per second
    [SerializeField] protected Transform firePoint;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected float damage = 10f;

    [Header("Upgrades")]
    [Tooltip("Total levels. 3 = the starting level plus 2 upgrades")]
    [SerializeField] private int maxLevel = 3;
    [Tooltip("Price of upgrade to level 2, then to level 3, and so on")]
    [SerializeField] private int[] upgradeCosts = { 60, 120 };
    [Tooltip("Each upgrade adds this fraction of the BASE value. 0.5 = +50% health per upgrade")]
    [SerializeField] private float healthBonusPerLevel = 0.5f;
    [SerializeField] private float damageBonusPerLevel = 0.3f;
    [SerializeField] private float fireRateBonusPerLevel = 0.15f;
    [Tooltip("Extra range (in units) added per upgrade")]
    [SerializeField] private float rangeBonusPerLevel = 0.75f;
    [Header("Upgrade Visuals")]
    [Tooltip("Colour tint for level 1, 2, 3... White = no change")]
    [SerializeField] private Color[] levelColors = { Color.white, new Color(0.5f, 0.8f, 1f), new Color(1f, 0.85f, 0.3f) };
    [Tooltip("Defender grows by this fraction per upgrade. 0.15 = 15% bigger each level")]
    [SerializeField] private float scaleGrowthPerLevel = 0.15f;

    private int level = 1;
    private UpgradeVisuals visuals;
    private int baseMaxHealth;
    private float baseDamage;
    private float baseFireRate;
    private float baseRange;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public bool IsMaxLevel => level >= maxLevel;

    protected Transform currentTarget;
    private float fireCooldown;
    private float targetScanCooldown;

    protected virtual void Reset()
    {
        ApplyTypeDefaults();
    }

    protected virtual void ApplyTypeDefaults()
    {
    }

    protected virtual void OnUpgraded()
    {
        
    }

    protected virtual void Awake()
    {
        // Remember the starting stats so every upgrade is worked out from the base values
        baseMaxHealth = maxHealth;
        baseDamage = damage;
        baseFireRate = fireRate;
        baseRange = range;
        visuals = new UpgradeVisuals(transform);
    }

    private void OnEnable()
    {
        All.Add(this);
        BodyCollider = GetComponentInChildren<Collider>();
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    protected virtual void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();

        BodyCollider = GetComponent<Collider>();
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

    // Called by anything that damages this defender (e.g. Enemy.cs),
    // matching the same IDamageable pattern used by Tower.cs
    public void TakeDamage(int amount)
    {
        if (isDestroyed)
        {
            return;
        }

        currentHealth -= amount;
        Debug.Log("Defender took " + amount + " damage. Health left: " + currentHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        isDestroyed = true;
        Debug.Log("Defender destroyed.");
        Destroy(gameObject);
    }

    private void Update()
    {
        if (isDestroyed)
        {
            return;
        }
        
        UpdateTarget();

        if (currentTarget == null)
            return;

        AimAtTarget();

        fireCooldown -= Time.deltaTime;
        if (fireCooldown <= 0f && IsAimedAtTarget())
        {
            Fire();
            fireCooldown = 1f / fireRate;
        }
    }

    private void UpdateTarget()
    {
        targetScanCooldown -= Time.deltaTime;

        // Keep current target if it's still alive and in range.
        if (currentTarget != null)
        {
            bool stillValid = currentTarget.gameObject.activeInHierarchy &&
                               Vector3.Distance(transform.position, currentTarget.position) <= range;

            if (stillValid && targetScanCooldown > 0f)
                return;

            if (!stillValid)
                currentTarget = null;
        }

        if (targetScanCooldown > 0f && currentTarget != null)
            return;

        targetScanCooldown = targetUpdateInterval;
        currentTarget = FindClosestEnemy();
    }

    private Transform FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayer);

        Transform closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
            }
        }

        return closest;
    }

    private void AimAtTarget()
    {
        Transform pivot = turretPivot != null ? turretPivot : transform;

        Vector3 direction = currentTarget.position - pivot.position;
        direction.y = 0f; // keep the turret level; remove this line for full 3D aiming

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        pivot.rotation = Quaternion.RotateTowards(pivot.rotation, targetRotation, turnSpeedDegPerSec * Time.deltaTime);
    }

    private bool IsAimedAtTarget()
    {
        Transform pivot = turretPivot != null ? turretPivot : transform;
        Vector3 direction = currentTarget.position - pivot.position;
        direction.y = 0f;

        float angle = Vector3.Angle(pivot.forward, direction);
        return angle < 5f; // small tolerance so it doesn't need to be pixel-perfect
    }

    protected virtual void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
            proj.Launch(currentTarget, damage);
    }

    // ---------------- Upgrades ----------------

    public int NextUpgradeCost
    {
        get
        {
            if (IsMaxLevel || upgradeCosts == null || upgradeCosts.Length == 0)
                return 0;

            // level 1 -> index 0, level 2 -> index 1. Reuse the last price if the list is too short
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

        Debug.Log(DisplayName + " upgraded to level " + level);
        OnUpgraded();
        return true;
    }

    private void ApplyLevelStats()
    {
        int steps = level - 1; // how many upgrades have been bought

        maxHealth = Mathf.RoundToInt(baseMaxHealth * (1f + healthBonusPerLevel * steps));
        damage = baseDamage * (1f + damageBonusPerLevel * steps);
        fireRate = baseFireRate * (1f + fireRateBonusPerLevel * steps);
        range = baseRange + rangeBonusPerLevel * steps;

        Color tint = Color.white;
        if (levelColors != null && levelColors.Length > 0)
            tint = levelColors[Mathf.Min(steps, levelColors.Length - 1)];

        visuals.Apply(tint, 1f + scaleGrowthPerLevel * steps);
    }

    public string GetUpgradeSummary()
    {
        if (IsMaxLevel)
        {
            // Nothing left to buy, so just show the current stats
            return "Health " + maxHealth + "\n"
                 + "Damage " + damage.ToString("0.#") + "\n"
                 + "Fire rate " + fireRate.ToString("0.0") + "\n"
                 + "Range " + range.ToString("0.#");
        }

        int steps = level; // the steps the NEXT level would have

        int nextHealth = Mathf.RoundToInt(baseMaxHealth * (1f + healthBonusPerLevel * steps));
        float nextDamage = baseDamage * (1f + damageBonusPerLevel * steps);
        float nextFireRate = baseFireRate * (1f + fireRateBonusPerLevel * steps);
        float nextRange = baseRange + rangeBonusPerLevel * steps;

        return "Health " + maxHealth + " > " + nextHealth + "\n"
             + "Damage " + damage.ToString("0.#") + " > " + nextDamage.ToString("0.#") + "\n"
             + "Fire rate " + fireRate.ToString("0.0") + " > " + nextFireRate.ToString("0.0") + "\n"
             + "Range " + range.ToString("0.#") + " > " + nextRange.ToString("0.#");
    }

    // Right-click the component header in the Inspector while playing to test an upgrade for free
    [ContextMenu("Test Upgrade (free)")]
    private void TestUpgrade()
    {
        if (IsMaxLevel) return;
        ResourceManager.Instance?.Add(NextUpgradeCost);
        TryUpgrade();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}