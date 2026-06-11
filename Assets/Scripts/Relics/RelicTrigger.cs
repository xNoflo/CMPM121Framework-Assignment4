using System;
using UnityEngine;
using System.Collections;
using System.Dynamic;
using JetBrains.Annotations;

public abstract class RelicTrigger
{
    public string description;
    public string type;
    public string amount;
    
    public Action effect;
    
    public RelicTrigger() {
    }
    
    public abstract void Create(Action effect);
    
    public abstract void Destroy();
}

public class TakeDamageTrigger : RelicTrigger
{
    public Action<Vector3, Damage, Hittable> effectWithTrigger;
    public override void Create(Action effect)
    {
        this.effect = effect;
        effectWithTrigger = (where, dmg, target) =>
        {
            if (target.team == Hittable.Team.PLAYER)
            {
                effect();
            }
        };
        
        EventBus.Instance.OnDamage += effectWithTrigger;
    }
    
    public override void Destroy()
    {
        EventBus.Instance.OnDamage -= effectWithTrigger;
    }
}

public class PlayerStopedMovingTrigger : RelicTrigger
{
    public override void Create(Action effect)
    {
        EventBus.Instance.OnPlayerStoppedMoving += effect;
    }
    
    public override void Destroy() 
    {
        EventBus.Instance.OnPlayerStoppedMoving -= effect;
    }
}

public class PlayerMoveTrigger: RelicTrigger
{
    public Action<float> effectWithTrigger;
    
    public override void Create(Action effect)
    {
        this.effect = effect;
        effectWithTrigger = (distance) =>
        {
            if (amount == null || distance >= Relic.evaluate(amount))
            {
                effect();
            }
        };
        
        EventBus.Instance.OnPlayerMoved += effectWithTrigger;
    }

    public override void Destroy()
    {
        EventBus.Instance.OnPlayerMoved -= effectWithTrigger;
    }
}

public class KillTrigger : RelicTrigger
{
    public Action<GameObject> effectWithTrigger;
    public override void Create(Action effect)
    {
        effectWithTrigger = (target) =>
        {
            Debug.Log("Trigger onkill");
            effect();
        };
        
        EventBus.Instance.OnEnemyKilled += effectWithTrigger;
    }

    public override void Destroy()
    {
        EventBus.Instance.OnEnemyKilled -= effectWithTrigger;
    }
}

public class CastSpellTrigger : RelicTrigger
{
    public Action<Spell> effectWithTrigger;
    public override void Create(Action effect)
    {
        effectWithTrigger = (spell) =>
        {
            Debug.Log("Trigger cast spell");
            effect();
        };
        EventBus.Instance.OnSpellCast += effectWithTrigger;
    }

    public override void Destroy()
    {
        EventBus.Instance.OnSpellCast -= effectWithTrigger;
    }
}

public class NotTakingDamageTrigger : RelicTrigger
{
    public Action<float, Hittable> effectWithTrigger;
    public override void Create(Action effect)
    {
        effectWithTrigger = (time, target) =>
        {
            if (time >= Relic.evaluate(amount) &&
                target.team == Hittable.Team.PLAYER)
            {
                effect();
            }
        };
        EventBus.Instance.OnNotTakingDamage += effectWithTrigger;
    }

    public override void Destroy()
    {
        EventBus.Instance.OnNotTakingDamage -= effectWithTrigger;
    }
}

public class LethalDamageTrigger : RelicTrigger
{
    public Action<Hittable> effectWithTrigger;
    public override void Create(Action effect)
    {
        effectWithTrigger = (target) =>
        {
            Debug.Log("Trigger on lethal damage");
            effect();
        };
        EventBus.Instance.OnPlayerLethalDamage += effectWithTrigger;
    }

    public override void Destroy()
    {
        EventBus.Instance.OnPlayerLethalDamage -= effectWithTrigger;
    }
}
