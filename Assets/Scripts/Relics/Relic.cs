using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using JetBrains.Annotations;
using UnityEngine.WSA;

public class Relic
{
    public string name;
    public int sprite;
    public bool isActive = false;

    [JsonConverter(typeof(TriggerConverter))]
    public RelicTrigger trigger;
    [JsonConverter(typeof(EffectConverter))]
    public RelicEffect effect;

    public RelicTrigger endTrigger;

    public void Activate(PlayerController player)
    {
        effect.setPlayer(player);
        Debug.Log("Activating Relic: "  + name);
        Debug.Log(trigger.description + " " + effect.description);
        isActive = true;
        trigger.Create(effect.Apply);

        if (effect.until != null)
        {
            effect.maxEffectCount = 1;
            endTrigger = stringToTrigger(effect.until);
            endTrigger.Create(effect.Remove);
        }
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void Deactivate()
    {
        trigger.Destroy();
        if (endTrigger != null) endTrigger.Destroy();
        isActive = false;
    }

    public string GetLabel()
    {
        return name;
    }

    public string GetDescription()
    {
        return trigger.description + ", " + effect.description;
    }
    
    public static RelicTrigger stringToTrigger(string type)
    {
        RelicTrigger trigger = type switch
        {
            "take-damage" => new TakeDamageTrigger(),
            "stand-still" => new PlayerStopedMovingTrigger(),
            "on-kill" => new KillTrigger(),
            "move"  => new PlayerMoveTrigger(),
            "cast-spell" => new CastSpellTrigger(),
            "not-take-damage" => new NotTakingDamageTrigger(),
            "on-lethal-damage" => new LethalDamageTrigger(),
            _ => throw new Exception("Unknown type")
        };
        
        return trigger;
    }

    public static RelicEffect stringToEffect(string type)
    {
        RelicEffect effect = type switch
        {
            "gain-mana" => new GainManaEffect(),
            "gain-spellpower" => new GainSpellPowerEffect(),
            "gain-movement-speed" => new GainMovementSpeedEffect(),
            "heal-over-time" => new HealingOverTimeEffect(),
            "heal" => new HealEffect(),
            "restore-health-percent" => new RestoreHealthPercentEffect(),
            "gain-health-percent" => new GainHealthPercentEffect(),
            "gain-armor" => new GainArmorEffect(),
            "spawn-homing-projectile" => new SpawnHomingProjectileEffect(),
            "temporary-speed" => new TemporarySpeedEffect(),
            _ => throw new Exception("Unknown type")
        };
        return effect;
    }

    public static float evaluate(string s)
    {
        var dict = new Dictionary<string, int>
        {
            // { "wave", GameManager.Instance.currentWave }
            { "wave", GameManager.Instance.player.GetComponent<PlayerController>().spellcaster.wave} // TODO: change the wave counter to game manager
        };
        
        return RPNEvaluator.RPNEvaluator.Evaluatef(s, dict);
    }
}

class TriggerConverter : JsonConverter<RelicTrigger>
{
    public override RelicTrigger ReadJson(
        JsonReader reader,
        Type objectType,
        RelicTrigger existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        string type = obj["type"]?.ToString();
        
        RelicTrigger trigger = Relic.stringToTrigger(type);
            
        serializer.Populate(obj.CreateReader(), trigger);

        return trigger;
    }
    
    public override void WriteJson(
        JsonWriter writer,
        RelicTrigger value,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

class EffectConverter : JsonConverter<RelicEffect>
{
    public override RelicEffect ReadJson(
        JsonReader reader,
        Type objectType,
        RelicEffect existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        string type = obj["type"]?.ToString();
        
        RelicEffect effect = Relic.stringToEffect(type);
        
        serializer.Populate(obj.CreateReader(), effect);

        return effect;
    }
    
    public override void WriteJson(
        JsonWriter writer,
        RelicEffect value,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

