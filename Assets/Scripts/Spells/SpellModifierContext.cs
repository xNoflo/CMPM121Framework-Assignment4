using System.Collections.Generic;

public class SpellModifierContext
{
    public readonly List<ValueModifier> damageModifiers = new List<ValueModifier>();
    public readonly List<ValueModifier> manaCostModifiers = new List<ValueModifier>();
    public readonly List<ValueModifier> cooldownModifiers = new List<ValueModifier>();
    public readonly List<ValueModifier> projectileSpeedModifiers = new List<ValueModifier>();

    public string projectileTrajectoryOverride;
    public bool splitOnHit;
    public float splitDamageMultiplier = 0.5f;
    public float splitSpeedMultiplier = 0.8f;
    public float splitLifetime = 0.6f;
    public float stunDuration = 0f;

    public int ApplyDamage(int baseDamage)
    {
        return ValueModifier.ApplyToInt(baseDamage, damageModifiers);
    }

    public int ApplyManaCost(int baseManaCost)
    {
        return ValueModifier.ApplyToInt(baseManaCost, manaCostModifiers);
    }

    public float ApplyCooldown(float baseCooldown)
    {
        return ValueModifier.Apply(baseCooldown, cooldownModifiers);
    }

    public float ApplyProjectileSpeed(float baseSpeed)
    {
        return ValueModifier.Apply(baseSpeed, projectileSpeedModifiers);
    }
}
