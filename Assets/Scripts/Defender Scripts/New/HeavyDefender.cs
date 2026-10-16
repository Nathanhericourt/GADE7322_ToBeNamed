using UnityEngine;

// Slow, durable defender. High health and damage, long gap between shots.
public class HeavyDefender : Defender
{
    public override string DisplayName => "Heavy";

    protected override void ApplyTypeDefaults()
    {
        maxHealth = 180;
        cost = 90;
        range = 9f;
        turnSpeedDegPerSec = 90f;
        fireRate = 0.35f;
        damage = 40f;
    }

    protected override void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        projObj.transform.localScale *= 1.75f;

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
            proj.Launch(currentTarget, damage);
    }
}
