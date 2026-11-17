using UnityEngine;
using System.Collections.Generic;

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

        // The defender this Brute is fighting always gets hit
        Defender mainTarget = currentTarget as Defender;

        // Loop over a copy of the list, because a defender dying can remove itself from the real list mid-loop
        List<Defender> targets = new List<Defender>(Defender.All);

        foreach (Defender defender in targets)
        {
            if (defender == null) continue;

            Vector3 flat = defender.transform.position - transform.position;
            flat.y = 0f;

            if (defender == mainTarget || flat.magnitude <= slamRadius)
            {
                defender.TakeDamage(attackDamage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, slamRadius);
    }
}