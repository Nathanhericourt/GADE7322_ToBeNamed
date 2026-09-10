using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("How much damage this enemy can take before dying")]
    public int maxHealth = 20;

    [Tooltip("How fast the enemy walks along the path")]
    public float moveSpeed = 2f;

    [Tooltip("Damage dealt to the tower per attack")]
    public int attackDamage = 5;

    [Tooltip("Seconds between each attack once in range of the tower")]
    public float attackInterval = 1f;

    private int currentHealth;
    private EnemyPath path;
    private int currentWaypointIndex = 0;

    private Tower targetTower;
    private bool isAttacking = false;
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
        if (isAttacking)
        {
            AttackTower();
        }
        else
        {
            MoveAlongPath();
        }
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
        targetTower = FindAnyObjectByType<Tower>();
        isAttacking = true;
    }

    private void AttackTower()
    {
        if (targetTower == null)
        {
            return;
        }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            targetTower.TakeDamage(attackDamage);
        }
    }

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