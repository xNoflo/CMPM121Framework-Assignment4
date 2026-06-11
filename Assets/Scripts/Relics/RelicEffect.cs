using System;
using UnityEngine;
using System.Collections;
using System.Dynamic;
using JetBrains.Annotations;
using Unity.VisualScripting;

public abstract class RelicEffect
{
    public string description;
    public string type;
    public string amount;
    public string until;
    
    public PlayerController player;
    
    public int currentEffectCount = 0;
    public int maxEffectCount = -1;

    public void Apply()
    {
        if (maxEffectCount >= 0 && currentEffectCount >= maxEffectCount) return;
        currentEffectCount++;

        ApplyEffect();
    }
    
    public abstract void ApplyEffect();

    public void Remove()
    {
        if (currentEffectCount <= 0) return;
        currentEffectCount--;
        
        RemoveEffect();
    }

    public virtual void RemoveEffect()
    {
        throw new NotImplementedException();
    }

    public void setPlayer(PlayerController player)
    {
        this.player = player;
    }
    
    public void setMaxStack(int count)
    {
        maxEffectCount = count;
    }
}

public class GainManaEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        var spellcaster = player.spellcaster;
        spellcaster.mana += (int)Relic.evaluate(amount);
        spellcaster.mana = Mathf.Clamp(spellcaster.mana, 0, spellcaster.max_mana);
        Debug.Log("Added mana");
    }
}

public class GainSpellPowerEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        var spellcaster = player.spellcaster;
        spellcaster.spell_power += (int)Relic.evaluate(amount);
        Debug.Log("Added spell power");
    }

    public override void RemoveEffect()
    {
        var spellcaster = player.spellcaster;
        spellcaster.spell_power -= (int)Relic.evaluate(amount);
        Debug.Log("Removed spell power");
    }
}

public class GainMovementSpeedEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        player.speed += (int)Relic.evaluate(amount);
        Debug.Log("Added speed");
    }

    public override void RemoveEffect()
    {
        player.speed -= (int)Relic.evaluate(amount);
        Debug.Log("Removed speed");
    }
}

public class HealingOverTimeEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        player.healingOverTime += Relic.evaluate(amount);
        Debug.Log("Added healing over time");
    }

    public override void RemoveEffect()
    {
        player.healingOverTime -= Relic.evaluate(amount);
        Debug.Log("Removed healing over time");
    }
}

public class HealEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        player.hp.Heal((int)Relic.evaluate(amount));
        Debug.Log("Added healing");
    }
}

public class RestoreHealthPercentEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        int percent = player.EvaluateRelicAmount(amount, 60);
        float normalizedPercent = Mathf.Clamp01(percent / 100f);
        player.hp.hp = Mathf.Max(1, Mathf.RoundToInt(player.hp.max_hp * normalizedPercent));
        
        // TODO: change it using the Heal() function
    }
}

public class GainHealthPercentEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        int percent = player.EvaluateRelicAmount(amount, 10);
        float normalizedPercent = Mathf.Clamp01(percent / 100f);
        int healAmount = Mathf.RoundToInt(player.hp.max_hp * normalizedPercent);
        player.hp.hp = Mathf.Clamp(player.hp.hp + healAmount, 0, player.hp.max_hp);
    }
}

public class GainArmorEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        player.SetRelicArmorBonus(this, (int)Relic.evaluate(amount));
        Debug.Log("Added armor");
    }

    public override void RemoveEffect()
    {
        player.RemoveRelicArmorBonus(this);
        Debug.Log("Removed armor");
    }
}

public class SpawnHomingProjectileEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        Vector3 where = player.transform.position;
        GameObject closestEnemy = GameManager.Instance.GetClosestEnemy(where);
        Vector3 direction = closestEnemy != null ? closestEnemy.transform.position - where : Vector3.right;
        int damageAmount = player.EvaluateRelicAmount(amount, 40);

        GameManager.Instance.projectileManager.CreateProjectile(
            0,
            "homing",
            where,
            direction,
            18f,
            (other, impact) =>
            {
                if (other.team != player.spellcaster.team)
                {
                    other.Damage(new Damage(damageAmount, Damage.Type.ARCANE));
                }
            },
            3f);
    }
}

public class TemporarySpeedEffect : RelicEffect
{
    public override void ApplyEffect()
    {
        float duration = 3f;
        player.ApplyTemporarySpeedBoost(this, (int)Relic.evaluate(amount), duration);
    }
}


