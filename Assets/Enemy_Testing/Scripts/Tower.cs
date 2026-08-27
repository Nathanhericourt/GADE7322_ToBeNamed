using UnityEngine;

public class Tower : MonoBehaviour
{
    [Tooltip("The tower's starting and maximum health")]
    public int maxHealth = 100;

    [Tooltip("Current health (read-only while playing, resets on Start)")]
    public int currentHealth;

    private bool isDestroyed = false;

    private void Start()
    {
        currentHealth = maxHealth;
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
        // NOTE: Hook a real Game Over screen / game loop
        // mechanic here later. This just logs it for now.
    }
}