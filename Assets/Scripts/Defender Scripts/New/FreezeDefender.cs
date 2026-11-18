using UnityEngine;

// Utility defender. Low damage, slow fire rate, freezes the enemy it hits.
public class FreezeDefender : Defender
{
    public override string DisplayName => "Freeze";

    [Header("Freeze")]
    [Tooltip("How long the hit enemy stays frozen.")]
    [SerializeField] private float freezeDuration = 0.5f;

    protected override void ApplyTypeDefaults()
    {
        maxHealth = 70;
        cost = 55;
        range = 8f;
        turnSpeedDegPerSec = 180f;
        fireRate = 1f / 3f;
        damage = 4f;
        freezeDuration = 0.5f;
    }

    protected override void Fire()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject projObj = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Color ice = new Color(0.35f, 0.85f, 1f);
        Renderer rend = projObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = ice;
            rend.material.SetColor("_BaseColor", ice);
        }

        Projectile proj = projObj.GetComponent<Projectile>();
        if (proj != null)
            proj.Launch(currentTarget, damage, freezeDuration);
    }
}
