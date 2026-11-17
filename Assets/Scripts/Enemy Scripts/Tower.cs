using UnityEngine;

public class Tower : MonoBehaviour, IDamageable
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

    private bool isDestroyed = false;
    private float attackTimer = 0f;

    public static event System.Action<Tower> OnTowerSpawned;

    private void Awake()
    {
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