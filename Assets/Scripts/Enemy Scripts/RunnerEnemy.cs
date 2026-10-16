using UnityEngine;

// Fast, fragile enemy. It never stops to fight 
public class RunnerEnemy : Enemy
{
    private float strikeTimer;

    protected override void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (!reachedTower)
        {
            // Hit and run
            StrikeNearbyDefender();
            FollowPath();
        }
        else
        {
            // Path finished
            Attack();
        }
    }

    private void StrikeNearbyDefender()
    {
        // Charge up to attackInterval while running, so the first strike is ready on contact
        strikeTimer = Mathf.Min(strikeTimer + Time.deltaTime, attackInterval);

        if (strikeTimer < attackInterval) return;

        IDamageable defender = FindNearbyDefender();
        if (defender != null)
        {
            defender.TakeDamage(attackDamage);
            strikeTimer = 0f;
        }
    }
}