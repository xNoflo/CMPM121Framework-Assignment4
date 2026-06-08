using System.Collections;
using UnityEngine;

public class JsonModifierSpell : ModifierSpell
{
    public JsonModifierSpell(Spell innerSpell, SpellDefinition modifierDefinition)
        : base(innerSpell, modifierDefinition)
    {
    }

    protected override void AddOwnModifiers(SpellModifierContext context, int spellPower, int wave)
    {
        AddMultiplier(context.damageModifiers, "damage_multiplier", spellPower, wave);
        AddMultiplier(context.manaCostModifiers, "mana_multiplier", spellPower, wave);
        AddAdder(context.manaCostModifiers, "mana_adder", spellPower, wave);
        AddMultiplier(context.cooldownModifiers, "cooldown_multiplier", spellPower, wave);
        AddMultiplier(context.projectileSpeedModifiers, "speed_multiplier", spellPower, wave);

        string projectileTrajectory = modifierDefinition.GetString("projectile_trajectory", "");

        if (!string.IsNullOrWhiteSpace(projectileTrajectory))
        {
            context.projectileTrajectoryOverride = projectileTrajectory;
        }



        if (modifierDefinition.id == "stun")
        {
            context.stunDuration = modifierDefinition.GetFloat("time", 1f, spellPower, wave);
        }

        if (modifierDefinition.id == "fracturing")
        {
            context.splitOnHit = true;
            context.splitDamageMultiplier = modifierDefinition.GetFloat("split_damage_multiplier", 0.5f, spellPower, wave);
            context.splitSpeedMultiplier = modifierDefinition.GetFloat("split_speed_multiplier", 0.8f, spellPower, wave);
            context.splitLifetime = modifierDefinition.GetFloat("split_lifetime", 0.6f, spellPower, wave);
        }
    }

    protected internal override IEnumerator CastWithContext(Vector3 where, Vector3 target, Hittable.Team team, int spellPower, int wave, SpellModifierContext context)
    {
        if (modifierDefinition.id == "doubler")
        {
            yield return innerSpell.CastWithContext(where, target, team, spellPower, wave, context);
            float delay = modifierDefinition.GetFloat("delay", 0.5f, spellPower, wave);
            yield return new WaitForSeconds(Mathf.Max(0, delay));
            yield return innerSpell.CastWithContext(where, target, team, spellPower, wave, context);
            yield break;
        }

        if (modifierDefinition.id == "splitter" || modifierDefinition.HasField("multishot"))
        {
            int shotCount = Mathf.Max(2, modifierDefinition.GetInt("multishot", 2, spellPower, wave));
            float maxAngle = modifierDefinition.GetFloat("angle", 10f, spellPower, wave);
            Vector3 direction = target - where;

            if (direction.sqrMagnitude <= 0)
            {
                direction = Vector3.right;
            }

            float startAngle = -maxAngle * (shotCount - 1) / 2f;

            for (int i = 0; i < shotCount; i++)
            {
                Vector3 shotDirection = RotateDirection(direction, startAngle + (maxAngle * i));
                yield return innerSpell.CastWithContext(where, where + shotDirection, team, spellPower, wave, context);
            }

            yield break;
        }


        if (modifierDefinition.id == "echo")
        {
            yield return innerSpell.CastWithContext(where, target, team, spellPower, wave, context);

            float delay = modifierDefinition.GetFloat("delay", 0.2f, spellPower, wave);
            yield return new WaitForSeconds(Mathf.Max(0, delay));

            Vector3 direction = target - where;
            if (direction.sqrMagnitude <= 0)
            {
                direction = Vector3.right;
            }

            Vector3 echoTarget = where - direction;
            yield return innerSpell.CastWithContext(where, echoTarget, team, spellPower, wave, context);
            yield break;
        }

        yield return innerSpell.CastWithContext(where, target, team, spellPower, wave, context);
    }

    private void AddMultiplier(System.Collections.Generic.List<ValueModifier> modifiers, string field, int spellPower, int wave)
    {
        if (modifierDefinition.HasField(field))
        {
            modifiers.Add(new ValueModifier(ValueModifierType.Multiply, modifierDefinition.GetFloat(field, 1, spellPower, wave)));
        }
    }

    private void AddAdder(System.Collections.Generic.List<ValueModifier> modifiers, string field, int spellPower, int wave)
    {
        if (modifierDefinition.HasField(field))
        {
            modifiers.Add(new ValueModifier(ValueModifierType.Add, modifierDefinition.GetFloat(field, 0, spellPower, wave)));
        }
    }

    private Vector3 RotateDirection(Vector3 direction, float degrees)
    {
        if (direction.sqrMagnitude <= 0)
        {
            direction = Vector3.right;
        }

        return Quaternion.Euler(0, 0, degrees) * direction;
    }
}
