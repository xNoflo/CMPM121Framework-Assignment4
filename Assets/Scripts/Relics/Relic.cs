using System;
using System.Collections;
using UnityEngine;

public class Relic
{
    public string name;
    public int sprite;
    public RelicTriggerDefinition trigger;
    public RelicEffectDefinition effect;

    PlayerController owner;
    IRelicTrigger runtimeTrigger;
    IRelicEffect runtimeEffect;
    bool initialized;

    public Relic(RelicDefinition definition)
    {
        name = definition.name;
        sprite = definition.sprite;
        trigger = definition.trigger;
        effect = definition.effect;
    }

    public Relic(Relic source)
    {
        name = source.name;
        sprite = source.sprite;
        trigger = source.trigger;
        effect = source.effect;
    }

    public string GetLabel()
    {
        string triggerText = trigger != null ? trigger.description : "";
        string effectText = effect != null ? effect.description : "";
        return (triggerText + " " + effectText).Trim();
    }

    public void Initialize(PlayerController player)
    {
        if (initialized || player == null)
        {
            return;
        }

        owner = player;
        runtimeEffect = RelicRuntimeFactory.CreateEffect(this, player);
        runtimeTrigger = RelicRuntimeFactory.CreateTrigger(this, player, runtimeEffect);
        runtimeTrigger?.Register();
        initialized = runtimeTrigger != null && runtimeEffect != null;
    }

    public void Cleanup()
    {
        runtimeTrigger?.Unregister();
        runtimeEffect?.Cleanup();
        runtimeTrigger = null;
        runtimeEffect = null;
        owner = null;
        initialized = false;
    }

    public bool IsActive()
    {
        return runtimeEffect != null && runtimeEffect.IsActive;
    }
}

public interface IRelicTrigger
{
    void Register();
    void Unregister();
}

public interface IRelicEffect
{
    bool IsActive { get; }
    void Activate();
    void Cleanup();
}

public static class RelicRuntimeFactory
{
    public static IRelicEffect CreateEffect(Relic relic, PlayerController owner)
    {
        if (relic == null || owner == null || relic.effect == null)
        {
            return null;
        }

        string effectType = Normalize(relic.effect.type);

        if (effectType == "gain-mana")
        {
            return new GainManaRelicEffect(relic, owner);
        }

        if (effectType == "gain-spellpower")
        {
            return new GainSpellPowerRelicEffect(relic, owner);
        }

        if (effectType == "temporary-speed")
        {
            return new TemporarySpeedRelicEffect(relic, owner);
        }

        Debug.LogWarning("Unsupported relic effect type: " + relic.effect.type);
        return null;
    }

    public static IRelicTrigger CreateTrigger(Relic relic, PlayerController owner, IRelicEffect effect)
    {
        if (relic == null || owner == null || effect == null || relic.trigger == null)
        {
            return null;
        }

        string triggerType = Normalize(relic.trigger.type);

        if (triggerType == "take-damage")
        {
            return new PlayerDamagedRelicTrigger(effect);
        }

        if (triggerType == "on-kill")
        {
            return new EnemyKilledRelicTrigger(effect);
        }

        if (triggerType == "stand-still")
        {
            return new StandStillRelicTrigger(relic, effect);
        }

        if (triggerType == "spell-cast")
        {
            return new SpellCastRelicTrigger(effect);
        }

        Debug.LogWarning("Unsupported relic trigger type: " + relic.trigger.type);
        return null;
    }

    static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}

public abstract class RelicEffectBase : IRelicEffect
{
    protected readonly Relic relic;
    protected readonly PlayerController owner;

    public abstract bool IsActive { get; }

    protected RelicEffectBase(Relic relic, PlayerController owner)
    {
        this.relic = relic;
        this.owner = owner;
    }

    public abstract void Activate();

    public virtual void Cleanup()
    {
    }
}

public class GainManaRelicEffect : RelicEffectBase
{
    public override bool IsActive { get { return false; } }

    public GainManaRelicEffect(Relic relic, PlayerController owner) : base(relic, owner)
    {
    }

    public override void Activate()
    {
        if (owner?.spellcaster == null || relic?.effect == null)
        {
            return;
        }

        int amount = owner.EvaluateRelicAmount(relic.effect.amount, 0);
        owner.spellcaster.mana = Mathf.Clamp(owner.spellcaster.mana + amount, 0, owner.spellcaster.max_mana);
    }
}

public class GainSpellPowerRelicEffect : RelicEffectBase
{
    bool isActive;

    public override bool IsActive { get { return isActive; } }

    public GainSpellPowerRelicEffect(Relic relic, PlayerController owner) : base(relic, owner)
    {
    }

    public override void Activate()
    {
        if (owner == null || relic?.effect == null)
        {
            return;
        }

        int amount = owner.EvaluateRelicAmount(relic.effect.amount, 0);
        owner.SetRelicSpellPowerBonus(this, amount);
        isActive = true;

        string until = NormalizeUntil();

        if (until == "cast-spell")
        {
            EventBus.Instance.OnSpellCast -= HandleSpellCastEnd;
            EventBus.Instance.OnSpellCast += HandleSpellCastEnd;
        }
        else if (until == "move")
        {
            EventBus.Instance.OnPlayerMoved -= HandleMoveEnd;
            EventBus.Instance.OnPlayerMoved += HandleMoveEnd;
        }
    }

    public override void Cleanup()
    {
        Deactivate();
    }

    void HandleSpellCastEnd(Spell spell)
    {
        Deactivate();
    }

    void HandleMoveEnd(float distance)
    {
        Deactivate();
    }

    void Deactivate()
    {
        EventBus.Instance.OnSpellCast -= HandleSpellCastEnd;
        EventBus.Instance.OnPlayerMoved -= HandleMoveEnd;
        owner?.RemoveRelicSpellPowerBonus(this);
        isActive = false;
    }

    string NormalizeUntil()
    {
        return string.IsNullOrWhiteSpace(relic.effect.until) ? string.Empty : relic.effect.until.Trim().ToLowerInvariant();
    }
}


public class TemporarySpeedRelicEffect : RelicEffectBase
{
    bool isActive;
    int speedGeneration;

    public override bool IsActive { get { return isActive; } }

    public TemporarySpeedRelicEffect(Relic relic, PlayerController owner) : base(relic, owner)
    {
    }

    public override void Activate()
    {
        if (owner == null || relic?.effect == null)
        {
            return;
        }

        int amount = owner.EvaluateRelicAmount(relic.effect.amount, 0);
        float duration = GetDuration();

        owner.SetRelicSpeedBonus(this, amount);
        isActive = true;

        speedGeneration++;
        int generation = speedGeneration;

        if (CoroutineManager.Instance != null)
        {
            CoroutineManager.Instance.Run(DeactivateAfterDelay(generation, duration));
        }
    }

    public override void Cleanup()
    {
        Deactivate();
    }

    IEnumerator DeactivateAfterDelay(int generation, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (generation == speedGeneration)
        {
            Deactivate();
        }
    }

    void Deactivate()
    {
        speedGeneration++;
        owner?.RemoveRelicSpeedBonus(this);
        isActive = false;
    }

    float GetDuration()
    {
        float duration = 3f;

        if (relic?.effect != null && !string.IsNullOrWhiteSpace(relic.effect.duration))
        {
            if (!float.TryParse(relic.effect.duration, out duration) || duration <= 0f)
            {
                duration = 3f;
            }
        }

        return duration;
    }
}

public class PlayerDamagedRelicTrigger : IRelicTrigger
{
    readonly IRelicEffect effect;

    public PlayerDamagedRelicTrigger(IRelicEffect effect)
    {
        this.effect = effect;
    }

    public void Register()
    {
        EventBus.Instance.OnPlayerDamaged += HandlePlayerDamaged;
    }

    public void Unregister()
    {
        EventBus.Instance.OnPlayerDamaged -= HandlePlayerDamaged;
    }

    void HandlePlayerDamaged(Damage damage, Hittable target)
    {
        effect?.Activate();
    }
}

public class EnemyKilledRelicTrigger : IRelicTrigger
{
    readonly IRelicEffect effect;

    public EnemyKilledRelicTrigger(IRelicEffect effect)
    {
        this.effect = effect;
    }

    public void Register()
    {
        EventBus.Instance.OnEnemyKilled += HandleEnemyKilled;
    }

    public void Unregister()
    {
        EventBus.Instance.OnEnemyKilled -= HandleEnemyKilled;
    }

    void HandleEnemyKilled(GameObject enemy)
    {
        effect?.Activate();
    }
}

public class StandStillRelicTrigger : IRelicTrigger
{
    readonly Relic relic;
    readonly IRelicEffect effect;
    int waitGeneration;

    public StandStillRelicTrigger(Relic relic, IRelicEffect effect)
    {
        this.relic = relic;
        this.effect = effect;
    }

    public void Register()
    {
        EventBus.Instance.OnPlayerMoved += HandlePlayerMoved;
        EventBus.Instance.OnPlayerStoppedMoving += HandlePlayerStoppedMoving;
        StartWaitTimer();
    }

    public void Unregister()
    {
        waitGeneration++;
        EventBus.Instance.OnPlayerMoved -= HandlePlayerMoved;
        EventBus.Instance.OnPlayerStoppedMoving -= HandlePlayerStoppedMoving;
    }

    void HandlePlayerMoved(float distance)
    {
        waitGeneration++;
    }

    void HandlePlayerStoppedMoving()
    {
        StartWaitTimer();
    }

    void StartWaitTimer()
    {
        waitGeneration++;
        int generation = waitGeneration;
        float delay = 3f;

        if (relic?.trigger != null && !string.IsNullOrWhiteSpace(relic.trigger.amount))
        {
            if (!float.TryParse(relic.trigger.amount, out delay) || delay <= 0f)
            {
                delay = 3f;
            }
        }

        if (CoroutineManager.Instance != null)
        {
            CoroutineManager.Instance.Run(ActivateAfterDelay(generation, delay));
        }
    }

    IEnumerator ActivateAfterDelay(int generation, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (generation == waitGeneration)
        {
            effect?.Activate();
        }
    }
}


public class SpellCastRelicTrigger : IRelicTrigger
{
    readonly IRelicEffect effect;

    public SpellCastRelicTrigger(IRelicEffect effect)
    {
        this.effect = effect;
    }

    public void Register()
    {
        EventBus.Instance.OnSpellCast += HandleSpellCast;
    }

    public void Unregister()
    {
        EventBus.Instance.OnSpellCast -= HandleSpellCast;
    }

    void HandleSpellCast(Spell spell)
    {
        effect?.Activate();
    }
}
