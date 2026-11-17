using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

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

    [Header("Reward")]
    [Tooltip("Resources given to the player when a enemy is destroyed")]
    public int resourceReward = 10;

    [Header("Defender Detection")]
    [Tooltip("How close a defender needs to be for this enemy to stop and fight it instead of walking past")]
    public float defenderDetectRange = 1.5f;

    [Tooltip("Defenders must be tagged with this exact tag so enemies can find them (create this Tag in Unity's Tag Manager)")]
    public string defenderTag = "Defender";

    [Header("Health Bar")]
    [SerializeField] private GameObject healthBarPrefab;
    [SerializeField] private float healthBarHeight = 2f;

    [Header("Tower Detection")]
    [Tooltip("How close (to the tower's edge) this enemy must get before it stops and attacks the tower. Stops crowds of enemies getting stuck short of the tower centre.")]
    public float towerReach = 2f;

    [Header("Boss")]
    [Tooltip("Tick this on the boss prefab. Killing the boss ends the boss round and wins the game.")]
    public bool isBoss = false;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    protected NavMeshAgent agent;

    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;

    protected IDamageable currentTarget;
    protected bool reachedTower = false;
    protected float attackTimer = 0f;

    private bool isDead = false;
    private Tower cachedTower;
    private Collider cachedTowerCollider;

    // Slow effect
    private float slowTimer = 0f;
    private float currentSlowMultiplier = 1f;

    private void Start()
    {
        currentHealth = maxHealth;
        CreateHealthBar();

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        // Enemies walk through each other, so a slow Brute can never block a fast Runner
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        // If agent doesnt land on the NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Enemy spawned off the NavMesh and will do nothing. Check the spawner's spawn position.");
            enabled = false;
            return;
        }

        // Follow the assigned path for the tower
        if (waypoints.Count > 0)
        {
            agent.SetDestination(waypoints[0]);
        }
        else
        {
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

    public void SetPath(EnemyPath assignedPath)
    {
        waypoints.Clear();
        if (assignedPath == null) return;

        for (int i = 0; i < assignedPath.WaypointCount; i++)
        {
            waypoints.Add(assignedPath.GetWaypointPosition(i));
        }
    }

    protected virtual void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // A defender blocking the way nearby always gets fought first
        IDamageable nearbyDefender = FindNearbyDefender();
        if (nearbyDefender != null)
        {
            agent.isStopped = true;
            currentTarget = nearbyDefender;
            Attack();
            return;
        }

        // No defender nearby
        if (agent.isStopped && !reachedTower)
        {
            agent.isStopped = false;
        }

        if (!reachedTower)
        {
            FollowPath();
        }
        else
        {
            currentTarget = GetTower();
            Attack();
        }
    }

    protected void FollowPath()
    {
        // Close enough to the tower to attack it - enemies don't need to reach the exact centre
        if (IsNearTower())
        {
            ArriveAtTower();
            return;
        }

        if (agent.pathPending) return;

        // Reached the current waypoint
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex < waypoints.Count)
            {
                agent.SetDestination(waypoints[currentWaypointIndex]);
            }
            else
            {
                // Path is finished
                ArriveAtTower();
            }
        }
    }

    private Tower GetTower()
    {
        if (cachedTower == null)
        {
            cachedTower = FindAnyObjectByType<Tower>();

            if (cachedTower != null)
            {
                cachedTowerCollider = cachedTower.GetComponent<Collider>();
            }
        }

        return cachedTower;
    }

    // True once this enemy is within towerReach of the tower's edge
    private bool IsNearTower()
    {
        Tower tower = GetTower();
        if (tower == null) return false;

        Vector3 closestPoint = cachedTowerCollider != null
            ? cachedTowerCollider.ClosestPoint(transform.position)
            : tower.transform.position;

        return Vector3.Distance(transform.position, closestPoint) <= towerReach;
    }

    // Stop walking and start attacking the tower
    private void ArriveAtTower()
    {
        reachedTower = true;
        currentTarget = GetTower();
        agent.isStopped = true;
    }

    private bool HasArrivedAtDestination()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance;
    }

    // Checks a small radius around the enemy for the Defender
    protected virtual IDamageable FindNearbyDefender()
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

    protected virtual void Attack()
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

    // Slows the enemy's movement for a short time
    public void ApplySlow(float speedMultiplier, float duration)
    {
        if (agent == null) return;

        currentSlowMultiplier = slowTimer > 0f ? Mathf.Min(currentSlowMultiplier, speedMultiplier) : speedMultiplier;
        slowTimer = Mathf.Max(slowTimer, duration);
        agent.speed = moveSpeed * currentSlowMultiplier;
    }

    // Counts the slow down timer down and resets the speed when it runs out
    private void LateUpdate()
    {
        if (slowTimer <= 0f) return;

        slowTimer -= Time.deltaTime;
        if (slowTimer <= 0f && agent != null)
        {
            currentSlowMultiplier = 1f;
            agent.speed = moveSpeed;
        }
    }

    private void Die()
    {
        // Two hits landing in the same frame must only count as one kill
        if (isDead) return;
        isDead = true;

        if(ResourceManager.Instance != null)
        {
            ResourceManager.Instance.Add(resourceReward);
        }
        
        WaveManager.NotifyEnemyKilled();
        Destroy(gameObject);
    }
}