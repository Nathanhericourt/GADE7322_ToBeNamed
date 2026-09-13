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
    [SerializeField] private GameObject hitEffectPrefab;

    private Transform target;
    private float damage;
    private Vector3 lastKnownDirection = Vector3.forward;

    public void Launch(Transform newTarget, float damageAmount)
    {
        target = newTarget;
        damage = damageAmount;
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 toTarget = target.position - transform.position;
            lastKnownDirection = toTarget.normalized;

            if (toTarget.magnitude <= hitDistance)
            {
                HitTarget(target);
                return;
            }
        }

        transform.position += lastKnownDirection * speed * Time.deltaTime;
        transform.forward = lastKnownDirection;
    }

    private void HitTarget(Transform hitTarget)
    {
        // Swap this for your actual enemy/health interface.
        Enemy enemy = hitTarget.GetComponent<Enemy>();
        if (enemy != null)
            enemy.TakeDamage(damage);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}