using UnityEngine;

/// <summary>
/// Simple homing projectile. Fired by Tower.cs, flies toward the target
/// it was launched at and deals damage on contact. If the target is
/// destroyed mid-flight, it flies straight and self-destructs after
/// missing rather than crashing.
/// </summary>
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float hitDistance = 0.3f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] protected GameObject hitEffectPrefab;

    private Transform target;
    protected float damage;
    private float freezeDuration;
    private Vector3 lastKnownDirection = Vector3.forward;

    public void Launch(Transform newTarget, float damageAmount)
    {
        Launch(newTarget, damageAmount, 0f);
    }

    public void Launch(Transform newTarget, float damageAmount, float freezeOnHitDuration)
    {
        target = newTarget;
        damage = damageAmount;
        freezeDuration = freezeOnHitDuration;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = target.position - transform.position;
            lastKnownDirection = toTarget.normalized;

            // Also counts as a hit if this frame's move would carry it through the target (stops fast bullets skipping past at low FPS)
            float stepThisFrame = speed * Time.deltaTime;

            if (toTarget.magnitude <= hitDistance)
            {
                HitTarget(target);
                return;
            }
        }

        transform.position += lastKnownDirection * speed * Time.deltaTime;
        transform.forward = lastKnownDirection;
    }

    protected void HitTarget(Transform hitTarget)
    {
        // Swap this for your actual enemy/health interface.
        Enemy enemy = hitTarget.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage((int)damage);
            if (freezeDuration > 0f)
                enemy.ApplyFreeze(freezeDuration);
        }

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}