using UnityEngine;
using System;

public class EventBus 
{
    private static EventBus theInstance;
    public static EventBus Instance
    {
        get
        {
            if (theInstance == null)
                theInstance = new EventBus();
            return theInstance;
        }
    }

    public event Action<Vector3, Damage, Hittable> OnDamage;
    public event Action<Damage, Hittable> OnPlayerDamaged;
    public event Action<Damage, Hittable> OnEnemyDamaged;
    public event Action<Hittable> OnPlayerLethalDamage;
    public event Action<GameObject> OnEnemyKilled;
    public event Action<float> OnPlayerMoved;
    public event Action OnPlayerStoppedMoving;
    public event Action<Spell> OnSpellCast;
    public event Action<Relic> OnRelicPickup;
    public event Action<float, Hittable> OnNotTakingDamage;
    
    public void DoDamage(Vector3 where, Damage dmg, Hittable target)
    {
        OnDamage?.Invoke(where, dmg, target);
    }

    public void DoPlayerDamaged(Damage damage, Hittable target)
    {
        OnPlayerDamaged?.Invoke(damage, target);
    }

    public void DoEnemyDamaged(Damage damage, Hittable target)
    {
        OnEnemyDamaged?.Invoke(damage, target);
    }

    public void DoPlayerLethalDamage(Hittable target)
    {
        OnPlayerLethalDamage?.Invoke(target);
    }

    public void DoEnemyKilled(GameObject enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }

    public void DoPlayerMoved(float distance)
    {
        OnPlayerMoved?.Invoke(distance);
    }

    public void DoPlayerStoppedMoving()
    {
        OnPlayerStoppedMoving?.Invoke();
    }

    public void DoSpellCast(Spell spell)
    {
        OnSpellCast?.Invoke(spell);
    }

    public void DoRelicPickup(Relic relic)
    {
        OnRelicPickup?.Invoke(relic);
    }
    
    public void DoNotTakingDamage(float time, Hittable source)
    {
        OnNotTakingDamage?.Invoke(time, source);
    }
}
