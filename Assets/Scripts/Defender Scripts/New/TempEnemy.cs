using UnityEngine;

/// <summary>
/// Minimal enemy stub so Tower/Projectile compile and work out of the box.
/// If you already have an enemy/health script, delete this file and point
/// Projectile.HitTarget() at your existing component instead.
/// </summary>
public class TempEnemy : MonoBehaviour
{
    [SerializeField] private float maxHealth = 50f;
    private float currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        // Hook in death VFX, currency reward, pooling, etc. here.
        Destroy(gameObject);
    }
}