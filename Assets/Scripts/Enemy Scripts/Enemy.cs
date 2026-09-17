using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [Tooltip("How much damage this enemy can take before dying")]
    public int maxHealth = 20;

    [Tooltip("How fast the enemy walks (applied to the NavMeshAgent's speed)")]
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
    private NavMeshAgent agent;

    private IDamageable currentTarget;
    private bool reachedTower = false;
    private float attackTimer = 0f;

    private void Start()
    {
        currentHealth = maxHealth;

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        // Head straight for the tower 
        Tower tower = FindAnyObjectByType<Tower>();
        if (tower != null)
        {
            agent.SetDestination(tower.transform.position);
        }
        else
        {
            Debug.LogWarning("Enemy could not find a Tower to path towards.");
        }
    }

    // So EnemySpawner doesn't need to change how it creates enemies
    public void SetPath(EnemyPath assignedPath)
    {
        // Intentionally empty - see comment above.
    }

    private void Update()
    {
        // A defender blocking the way nearby always gets fought first
        IDamageable nearbyDefender = FindNearbyDefender();
        if (nearbyDefender != null)
        {
            agent.isStopped = true;
            currentTarget = nearbyDefender;
            Attack();
            return;
        }

        // No defender nearby - keep walking
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        // Check if we've arrived at the tower
        if (!reachedTower && HasArrivedAtDestination())
        {
            reachedTower = true;
            currentTarget = FindAnyObjectByType<Tower>();
        }

        if (reachedTower)
        {
            Attack();
        }
    }

    private bool HasArrivedAtDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
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

    // Anything that damages the enemy calls this
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