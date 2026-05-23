using System;

[Serializable]
public class RelicTriggerDefinition
{
    public string description;
    public string type;
    public string amount;
}

[Serializable]
public class RelicEffectDefinition
{
    public string description;
    public string type;
    public string amount;
    public string duration;
    public string until;
}

[Serializable]
public class RelicDefinition
{
    public string name;
    public int sprite;
    public RelicTriggerDefinition trigger;
    public RelicEffectDefinition effect;
}
