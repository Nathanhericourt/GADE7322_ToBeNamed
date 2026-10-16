using UnityEngine;

// Slow, tanky enemy
public class BruteEnemy : Enemy
{
    [Header("Slam")]
    [Tooltip("Every defender within this distance of the Brute takes damage per slam")]
    [SerializeField] private float slamRadius = 3f;

    protected override void Attack()
    {
        if (currentTarget == null) return;

        attackTimer += Time.deltaTime;
        if (attackTimer < attackInterval) return;
        attackTimer = 0f;

        // Fighting the tower: a normal single hit
        if (currentTarget is Tower)
        {
            currentTarget.TakeDamage(attackDamage);
            return;
        }

        // Fighting defenders: slam everything in the radius
        Collider[] hits = Physics.OverlapSphere(transform.position, slamRadius);
        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag(defenderTag)) continue;

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}