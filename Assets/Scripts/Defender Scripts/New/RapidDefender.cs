using UnityEngine;

// Fast, fragile defender. Lower health and damage, high attack speed.
public class RapidDefender : Defender
{
    public override string DisplayName => "Rapid";

    protected override void ApplyTypeDefaults()
    {
        maxHealth = 30;
        cost = 40;
        range = 7f;
        turnSpeedDegPerSec = 540f;
        fireRate = 3.5f;
        damage = 6f;
    }
}
