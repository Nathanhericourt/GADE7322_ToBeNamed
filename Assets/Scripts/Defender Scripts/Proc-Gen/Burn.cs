using UnityEngine;

/// <summary>
/// Damage over time. Added to an enemy by Burn shots, removes itself when it runs out.
/// Hitting an already burning enemy refreshes the timer instead of stacking.
/// </summary>
public class BurnEffect : MonoBehaviour
{
    private Enemy enemy;
    private int damagePerTick;
    private float tickInterval;
    private float timeLeft;
    private float tickTimer;

    public static void Apply(Enemy target, int damagePerTick, float duration, float tickInterval)
    {
        if (target == null)
            return;

        BurnEffect burn = target.GetComponent<BurnEffect>();
        if (burn == null)
        {
            burn = target.gameObject.AddComponent<BurnEffect>();
            burn.enemy = target;
            burn.tickTimer = tickInterval;
        }

        burn.tickInterval = tickInterval;
        burn.damagePerTick = Mathf.Max(burn.damagePerTick, damagePerTick);
        burn.timeLeft = duration;
    }

    private void Update()
    {
        if (enemy == null || !enemy.isActiveAndEnabled)
        {
            Destroy(this);
            return;
        }

        timeLeft -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (tickTimer <= 0f)
        {
            tickTimer += tickInterval;
            enemy.TakeDamage(damagePerTick);
        }

        if (timeLeft <= 0f)
            Destroy(this);
    }
}