using UnityEngine;

/// <summary>
/// Attach to a defender tower prefab. Automatically finds enemies within
/// range, aims at the closest one, and fires projectiles at a fixed rate.
/// Assumes enemies have a component implementing IDamageable (see below),
/// and are on the layer set in enemyLayer / tagged "Enemy".
/// </summary>
public class Defender : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("The defender's starting and maximum health")]
    [SerializeField] private int maxHealth = 50;
    [Tooltip("Current health (read-only while playing, resets on Start)")]
    [SerializeField] private int currentHealth;
    private bool isDestroyed = false;

    [Header("Cost")]
    [Tooltip("How many resources it costs to build this defender.")]
    [SerializeField] private int cost = 50;
    public int Cost => cost;
    
    [Header("Targeting")]
    [SerializeField] private float range = 8f;
    [SerializeField] private LayerMask enemyLayer;
    [Tooltip("How often (seconds) the tower re-scans for the closest target.")]
    [SerializeField] private float targetUpdateInterval = 0.2f;

    [Header("Aiming")]
    [Tooltip("Part of the tower that rotates to face the target (e.g. the turret head). Leave empty to rotate the whole tower.")]
    [SerializeField] private Transform turretPivot;
    [SerializeField] private float turnSpeedDegPerSec = 360f;

    [Header("Firing")]
    [SerializeField] private float fireRate = 1f; // shots per second
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float damage = 10f;

    private Transform currentTarget;
    private float fireCooldown;
    private float targetScanCooldown;

    private void Start()
    {
        currentHealth = maxHealth;
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

    private void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj != null)
            proj.Launch(currentTarget, damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}