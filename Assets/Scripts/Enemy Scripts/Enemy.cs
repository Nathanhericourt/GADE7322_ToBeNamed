using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [Tooltip("How much damage this enemy can take before dying")]
    public int maxHealth = 20;

    [Tooltip("How fast the enemy walks along the path")]
    public float moveSpeed = 2f;

    [Tooltip("Damage dealt per attack, to either the tower or a defender")]
    public int attackDamage = 5;

    [Tooltip("Seconds between each attack once something is in range")]
    public float attackInterval = 1f;

    [Header("Defender Detection")]
    [Tooltip("How close a defender needs to be for this enemy to stop and fight it instead of walking past")]
    public float defenderDetectRange = 1.5f;

    [Tooltip("Defenders must be tagged with this exact tag so enemies can find them (create this Tag in Unity's Tag Manager)")]
    public string defenderTag = "Defender";

    private int currentHealth;
    private EnemyPath path;
    private int currentWaypointIndex = 0;

    private IDamageable currentTarget;
    private bool reachedTower = false;
    private float attackTimer = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void SetPath(EnemyPath assignedPath)
    {
        path = assignedPath;
        currentWaypointIndex = 0;
    }

    private void Update()
    {
        // A defender blocking the way gets fought first
        IDamageable nearbyDefender = FindNearbyDefender();
        if (nearbyDefender != null)
        {
            currentTarget = nearbyDefender;
            Attack();
            return;
        }

        if (reachedTower)
        {
            Attack();
            return;
        }

        MoveAlongPath();
    }

    // Checks a small radius around the enemy for the Defender
    private IDamageable FindNearbyDefender()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, defenderDetectRange);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(defenderTag))
            {
                IDamageable damageable = hit.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    return damageable;
                }
            }
        }

        return null;
    }

    private void MoveAlongPath()
    {
        if (path == null || currentWaypointIndex >= path.WaypointCount)
        {
            return;
        }

        Vector3 targetPosition = path.GetWaypointPosition(currentWaypointIndex);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex >= path.WaypointCount)
            {
                StartAttackingTower();
            }
        }
    }

    private void StartAttackingTower()
    {
        // Finds the Tower object in the scene
        Tower tower = FindAnyObjectByType<Tower>();

        if (tower != null)
        {
            currentTarget = tower;
            reachedTower = true;
        }
    }

    private void Attack()
    {
        if (currentTarget == null)
        {
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            currentTarget.TakeDamage(attackDamage);
        }
    }

    // Anything that damages the enemy (tower, defenders) calls this
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}