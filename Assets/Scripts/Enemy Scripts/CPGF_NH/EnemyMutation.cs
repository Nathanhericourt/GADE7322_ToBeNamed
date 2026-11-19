using System.Collections.Generic;
using UnityEngine;

//Sits on an enemy that rolled a trait (added by the EnemyMutator when the enemy spawns).
//It handles what the trait actually does: less damage taken, a shield, healing, or an explosion on death.
public class EnemyMutation : MonoBehaviour
{
    public enum Trait { None, Armoured, Swift, Regenerating, Shielded, Volatile }

    [Header("Set by the EnemyMutator (visible here so you can check it while playing)")]
    public Trait trait = Trait.None;

    [Tooltip("Armoured: 0.6 means the enemy only takes 60% of every hit")]
    public float damageTakenMultiplier = 1f;

    [Tooltip("Shielded: damage points that must be burned through before health is touched")]
    public int shield = 0;

    [Tooltip("Regenerating: heals this fraction of max health every second")]
    public float regenPercentPerSecond = 0f;

    [Tooltip("Volatile: defenders within this distance take damage when the enemy dies")]
    public float explosionRadius = 0f;
    public int explosionDamage = 0;

    //The glowing circle on the ground under the enemy (made by the EnemyMutator)
    [HideInInspector] public Transform ring;

    private Enemy enemy;
    private Collider body;
    private int groundMask;
    private float regenBuffer = 0f;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        body = GetComponent<Collider>();

        //The floor is anything solid except enemies, UI and the placement spots
        groundMask = ~LayerMask.GetMask("Enemy", "UI", "PlacementSpot", "Ignore Raycast", "Water");
    }

    //The ring slides along the ground underneath the enemy
    private void LateUpdate()
    {
        if (ring == null) return;

        ring.position = new Vector3(transform.position.x, GetGroundY() + 0.05f, transform.position.z);
    }

    //Shoots a ray straight down to find the real floor under the enemy (works for hovering enemies too)
    private float GetGroundY()
    {
        Vector3 origin = transform.position + Vector3.up * 1f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 60f, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        //Nothing found below, so fall back to the bottom of the body shape
        return GetBottomY();
    }

    //The lowest point of the enemy's body shape (the same shape as its hidden placeholder mesh)
    private float GetBottomY()
    {
        if (body == null) return transform.position.y - 1f;

        Vector3 scale = transform.lossyScale;
        float biggestSide = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));

        if (body is BoxCollider box)
        {
            float centerY = transform.TransformPoint(box.center).y;
            return centerY - box.size.y * 0.5f * scale.y;
        }

        if (body is CapsuleCollider capsule)
        {
            float centerY = transform.TransformPoint(capsule.center).y;
            float halfHeight = Mathf.Max(capsule.height * 0.5f * scale.y, capsule.radius * Mathf.Max(scale.x, scale.z));
            return centerY - halfHeight;
        }

        if (body is SphereCollider sphere)
        {
            float centerY = transform.TransformPoint(sphere.center).y;
            return centerY - sphere.radius * biggestSide;
        }

        return body.bounds.min.y;
    }

    private void OnDestroy()
    {
        if (ring != null)
        {
            Destroy(ring.gameObject);
        }
    }

    private void Update()
    {
        if (regenPercentPerSecond <= 0f || enemy == null) return;

        //Only heal while alive (health is 0 until the enemy's Start has run) and hurt
        if (enemy.CurrentHealth <= 0 || enemy.CurrentHealth >= enemy.MaxHealth) return;

        //Healing is collected as a decimal and paid out in whole points
        regenBuffer += enemy.MaxHealth * regenPercentPerSecond * Time.deltaTime;

        if (regenBuffer >= 1f)
        {
            int whole = (int)regenBuffer;
            regenBuffer -= whole;
            enemy.Heal(whole);
        }
    }

    //Enemy.TakeDamage passes every hit through here first and uses the number that comes back
    public int ModifyIncomingDamage(int amount)
    {
        float damage = amount * damageTakenMultiplier;

        //The shield soaks up damage first
        if (shield > 0)
        {
            int absorbed = Mathf.Min(shield, Mathf.CeilToInt(damage));
            shield -= absorbed;
            damage -= absorbed;
        }

        if (damage <= 0f) return 0;

        //A hit that gets through always does at least 1
        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    //Enemy.Die calls this just before the enemy is removed
    public void OnDeath()
    {
        if (explosionDamage <= 0 || explosionRadius <= 0f) return;

        //Loop over a copy, because a defender dying can change the real list
        List<Defender> defenders = new List<Defender>(Defender.All);

        foreach (Defender defender in defenders)
        {
            if (defender == null) continue;

            Vector3 flat = defender.transform.position - transform.position;
            flat.y = 0f;

            if (flat.magnitude <= explosionRadius)
            {
                defender.TakeDamage(explosionDamage);
            }
        }

        Debug.Log(name + " exploded for " + explosionDamage + " damage.");
    }

    private void OnDrawGizmosSelected()
    {
        if (explosionRadius <= 0f) return;

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
