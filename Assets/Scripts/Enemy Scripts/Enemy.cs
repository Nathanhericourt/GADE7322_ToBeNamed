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

    [Header("Defender Fighting")]
    [Tooltip("If a defender gets this close (gap between the enemy's body and the defender), the enemy leaves the path to go and attack it")]
    public float defenderDetectRange = 6f;

    [Tooltip("When a defender shoots this enemy, the enemy will go after any defender within this range")]
    public float retaliateRange = 12f;

    [Tooltip("How many seconds after being shot the enemy keeps looking for a defender to retaliate against")]
    public float retaliateDuration = 4f;

    [Tooltip("How close (gap between bodies) the enemy must be before it stops walking and attacks the defender")]
    public float attackReach = 1.2f;

    [Tooltip("If the defender ends up further away than this, the enemy gives up and goes back to the path")]
    public float chaseGiveUpRange = 14f;

    [Tooltip("Max seconds spent walking towards a defender without reaching it, then the enemy gives up")]
    public float maxChaseTime = 12f;

    [Tooltip("No longer used - defenders are found automatically now. Kept so old prefab settings don't break.")]
    public string defenderTag = "Defender";

    [Header("Pathfinding")]
    [Tooltip("A waypoint counts as reached when the enemy is this close to it (added to the enemy's body size). Stops big enemies getting stuck just short of a waypoint.")]
    public float waypointReachDistance = 1.5f;

    [Tooltip("If the enemy hasn't really moved for this many seconds while walking, it tries to free itself")]
    public float stuckCheckTime = 1.5f;

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

    // Only exists on enemies that rolled a trait (added by the EnemyMutator)
    private EnemyMutation mutation;
    private Tower cachedTower;
    private Collider cachedTowerCollider;

    // Size of the enemy's body, used to measure how far away a defender really is
    private float bodyRadius = 0.5f;

    // Slow effect
    private float slowTimer = 0f;
    private float currentSlowMultiplier = 1f;

    // Freeze effect
    private float freezeTimer = 0f;

    // Fighting defenders
    private Defender defenderTarget;
    private bool fightingDefender = false;
    private float defenderScanTimer = 0f;
    private float chaseTimer = 0f;
    private float chaseRepathTimer = 0f;
    private float hitAggroTimer = 0f;

    // Stuck detection
    private float stuckTimer = 0f;
    private int stuckStrikes = 0;
    private Vector3 lastStuckCheckPosition;

    private void Start()
    {
        currentHealth = maxHealth;
        mutation = GetComponent<EnemyMutation>();
        CreateHealthBar();

        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        // Enemies push around each other instead of walking through
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;

        // Different priorities let one enemy nudge past another, so they don't jam
        agent.avoidancePriority = Random.Range(30, 70);

        // Don't crawl to a stop at every waypoint - this script decides when a waypoint is reached
        agent.autoBraking = false;

        FitAgentToNavMesh();

        // If agent doesnt land on the NavMesh
        if (!agent.isOnNavMesh)
        {
            Debug.LogWarning("Enemy spawned off the NavMesh and will do nothing. Check the spawner's spawn position.");
            
            // Remove it and count it as gone, so the wave can still finish (the boss is left alone so it can't give a free win)
            if (!isBoss)
            {
                isDead = true;
                WaveManager.NotifyEnemyKilled(false);
                Destroy(gameObject);
            }

            enabled = false;
            return;
        }

        // Follow the assigned path for the tower
        if (waypoints.Count == 0 && GetTower() == null)
        {
            Debug.LogWarning("Enemy could not find a Tower to path towards.");
        }

        ResumePath();
    }

    // This keeps the agent's footprint no bigger than the baked one.
    // The enemy still LOOKS the same size.
    private void FitAgentToNavMesh()
    {
        float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        NavMeshBuildSettings baked = NavMesh.GetSettingsByID(agent.agentTypeID);
        agent.radius = Mathf.Min(agent.radius, baked.agentRadius / scale);

        Collider body = GetComponent<Collider>();
        if (body != null)
        {
            bodyRadius = Mathf.Min(body.bounds.extents.x, body.bounds.extents.z);
        }
        else
        {
            bodyRadius = 0.5f * scale;
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

    // Called by the WaveManager right after the enemy spawns, so later waves are harder
    public void ApplyWaveScaling(float healthMultiplier, float damageMultiplier, float speedMultiplier, float rewardMultiplier)
    {
        maxHealth = Mathf.RoundToInt(maxHealth * healthMultiplier);
        attackDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
        moveSpeed = moveSpeed * speedMultiplier;
        resourceReward = Mathf.RoundToInt(resourceReward * rewardMultiplier);
    }

    protected virtual void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (TickFreeze())
            return;

        if (hitAggroTimer > 0f)
        {
            hitAggroTimer -= Time.deltaTime;
        }

        // A defender that is close, or that is shooting us, gets dealt with first
        if (HandleDefenders())
        {
            return;
        }

        // No defender to deal with: walk the path, then attack the tower
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

    // ---------------------------------------------------------------------
    // Fighting defenders
    // ---------------------------------------------------------------------

    // Returns true while the enemy is busy with a defender (walking to it or attacking it)
    protected bool HandleDefenders()
    {
        // The defender we were fighting has been destroyed
        if (fightingDefender && defenderTarget == null)
        {
            EndDefenderFight();
        }

        // Not fighting anyone: look for a defender a few times a second
        if (!fightingDefender)
        {
            defenderScanTimer -= Time.deltaTime;
            if (defenderScanTimer > 0f) return false;
            defenderScanTimer = 0.25f;

            float range = defenderDetectRange;

            // Recently shot: go after defenders from further away
            if (hitAggroTimer > 0f)
            {
                range = Mathf.Max(defenderDetectRange, retaliateRange);
            }

            // Already at the tower: only fight defenders right next to us, never walk away from the tower
            if (reachedTower)
            {
                range = attackReach;
            }

            Defender found = FindClosestDefender(range);
            if (found == null) return false;

            defenderTarget = found;
            fightingDefender = true;
            chaseTimer = 0f;
            chaseRepathTimer = 0f;
        }

        float gap = GapTo(defenderTarget, out Vector3 closestPoint);

        // Too far away now, give up and go back to the path
        if (gap > chaseGiveUpRange)
        {
            EndDefenderFight();
            defenderScanTimer = 5f;
            return false;
        }

        // Close enough: stop and attack it
        if (gap <= attackReach)
        {
            agent.isStopped = true;
            FaceTowards(closestPoint);

            currentTarget = defenderTarget;
            Attack();
            return true;
        }

        // The tower is reached: never walk away from it
        if (reachedTower)
        {
            EndDefenderFight();
            return false;
        }

        // Walk towards the defender, but not forever
        chaseTimer += Time.deltaTime;
        if (chaseTimer > maxChaseTime)
        {
            EndDefenderFight();
            defenderScanTimer = 5f;
            return false;
        }

        agent.isStopped = false;

        chaseRepathTimer -= Time.deltaTime;
        if (chaseRepathTimer <= 0f)
        {
            chaseRepathTimer = 0.3f;
            SetDestinationSafe(closestPoint);
        }

        return true;
    }

    // The defender is dead or the enemy gave up: go back to what we were doing
    private void EndDefenderFight()
    {
        fightingDefender = false;
        defenderTarget = null;
        hitAggroTimer = 0f;
        currentTarget = null;

        if (reachedTower)
        {
            agent.isStopped = true;
        }
        else
        {
            // Back onto the path, towards the tower
            ResumePath();
        }
    }

    // The closest defender whose edge is within range of this enemy's body
    protected Defender FindClosestDefender(float range)
    {
        Defender closest = null;
        float closestGap = range;

        foreach (Defender defender in Defender.All)
        {
            if (defender == null) continue;

            float gap = GapTo(defender, out Vector3 unused);
            if (gap <= closestGap)
            {
                closestGap = gap;
                closest = defender;
            }
        }

        return closest;
    }

    // Distance between the edge of this enemy and the edge of the defender (flat, ignores height)
    private float GapTo(Defender defender, out Vector3 closestPoint)
    {
        Collider defenderCollider = defender.BodyCollider;

        if (defenderCollider != null)
        {
            closestPoint = defenderCollider.bounds.ClosestPoint(transform.position);
        }
        else
        {
            closestPoint = defender.transform.position;
        }

        Vector3 flat = closestPoint - transform.position;
        flat.y = 0f;

        return Mathf.Max(0f, flat.magnitude - bodyRadius);
    }

    private void FaceTowards(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion look = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, 360f * Time.deltaTime);
    }

    // Used by the Runner's hit and run: a defender close enough to strike while running past
    protected virtual IDamageable FindNearbyDefender()
    {
        return FindClosestDefender(defenderDetectRange);
    }

    // ---------------------------------------------------------------------
    // Following the path
    // ---------------------------------------------------------------------

    protected void FollowPath()
    {
        // Close enough to the tower to attack it - enemies don't need to reach the exact centre
        if (IsNearTower())
        {
            ArriveAtTower();
            return;
        }

        if (agent.pathPending) return;

        if (currentWaypointIndex >= waypoints.Count)
        {
            // Path is finished
            ArriveAtTower();
            return;
        }

        // Reached the current waypoint? Flat distance, so height differences and big
        // bodies can't stop an enemy from ever "arriving"
        Vector3 toWaypoint = waypoints[currentWaypointIndex] - transform.position;
        toWaypoint.y = 0f;

        bool closeEnough = toWaypoint.magnitude <= waypointReachDistance + bodyRadius;
        bool agentArrived = agent.hasPath && agent.remainingDistance <= agent.stoppingDistance;

        if (closeEnough || agentArrived)
        {
            currentWaypointIndex++;

            if (currentWaypointIndex < waypoints.Count)
            {
                SetDestinationSafe(waypoints[currentWaypointIndex]);
            }
            else
            {
                ArriveAtTower();
            }

            return;
        }

        CheckIfStuck();
    }

    // Sends the agent to the current waypoint (or the tower if the path is finished)
    private void ResumePath()
    {
        agent.isStopped = false;
        stuckTimer = 0f;
        lastStuckCheckPosition = transform.position;

        if (currentWaypointIndex < waypoints.Count)
        {
            SetDestinationSafe(waypoints[currentWaypointIndex]);
        }
        else
        {
            Tower tower = GetTower();
            if (tower != null)
            {
                SetDestinationSafe(tower.transform.position);
            }
        }
    }

    // Moves the destination onto the NavMesh first, so the agent always gets a valid path
    private void SetDestinationSafe(Vector3 point)
    {
        if (NavMesh.SamplePosition(point, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(point);
        }
    }

    // If the enemy hasn't really moved for a while it tries to free itself
    private void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckTime) return;
        stuckTimer = 0f;

        Vector3 moved = transform.position - lastStuckCheckPosition;
        moved.y = 0f;
        lastStuckCheckPosition = transform.position;

        // Even a slowed enemy should move at least this far in the check time
        float minimumMove = Mathf.Max(0.1f, agent.speed * stuckCheckTime * 0.25f);

        if (moved.magnitude >= minimumMove)
        {
            stuckStrikes = 0;
            return;
        }

        stuckStrikes++;

        if (stuckStrikes == 1)
        {
            // First try: work out the route again
            ResumePath();
        }
        else if (currentWaypointIndex < waypoints.Count - 1)
        {
            // Still stuck: give up on this waypoint and head for the next one
            currentWaypointIndex++;
            ResumePath();
        }
        else
        {
            // Stuck on the last stretch: shuffle a bit closer to the tower
            Tower tower = GetTower();
            if (tower != null)
            {
                Vector3 nudged = Vector3.MoveTowards(transform.position, tower.transform.position, 2f);
                if (NavMesh.SamplePosition(nudged, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }
            }

            ResumePath();
        }
    }

    // ---------------------------------------------------------------------
    // Tower
    // ---------------------------------------------------------------------

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

    // ---------------------------------------------------------------------
    // Attacking and taking damage
    // ---------------------------------------------------------------------

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
        if (isDead) return;

        // A trait (armour, shield) can reduce the hit before it lands
        if (mutation != null)
        {
            amount = mutation.ModifyIncomingDamage(amount);
        }

        currentHealth -= amount;

        // Being hit makes the enemy look for a defender to hit back at
        hitAggroTimer = retaliateDuration;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Restores health (used by the Regenerating trait)
    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    // Stops the enemy from moving or attacking for a short time
    public void ApplyFreeze(float duration)
    {
        if (isDead || duration <= 0f) return;

        freezeTimer = Mathf.Max(freezeTimer, duration);
        HoldFrozen();
    }

    // Returns true while the enemy is still frozen
    protected bool TickFreeze()
    {
        if (freezeTimer <= 0f)
            return false;

        freezeTimer -= Time.deltaTime;
        HoldFrozen();

        if (freezeTimer > 0f)
            return true;

        freezeTimer = 0f;
        EndFreeze();
        return false;
    }

    private void HoldFrozen()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    private void EndFreeze()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (reachedTower)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;

        if (fightingDefender && defenderTarget != null)
            return;

        ResumePath();
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
        
        // Volatile enemies explode when they die
        if (mutation != null)
        {
            mutation.OnDeath();
        }

        WaveManager.NotifyEnemyKilled(isBoss);
        Destroy(gameObject);
    }
}