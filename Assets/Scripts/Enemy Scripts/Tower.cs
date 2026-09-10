using UnityEngine;

public class Tower : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [Tooltip("The tower's starting and maximum health")]
    public int maxHealth = 100;

    [Tooltip("Current health (read-only while playing, resets on Start)")]
    public int currentHealth;

    [Header("Attack")]
    [Tooltip("How far the tower can reach to attack enemies")]
    public float attackRange = 8f;

    [Tooltip("Damage dealt per attack")]
    public int attackDamage = 10;

    [Tooltip("Seconds between each attack")]
    public float attackInterval = 1f;

    [Tooltip("Enemies must use this tag so the tower can find them (create this Tag in Unity's Tag Manager if it doesn't already exist)")]
    public string enemyTag = "Enemy";

    private bool isDestroyed = false;
    private float attackTimer = 0f;

    private void Start()
    {
        currentHealth = maxHealth;
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

    private void Die()
    {
        isDestroyed = true;
        Debug.Log("Tower destroyed! GAME OVER.");
    }
}