public class Relic
{
    public string name;
    public int sprite;
    public RelicTriggerDefinition trigger;
    public RelicEffectDefinition effect;

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

    public bool IsActive()
    {
        return false;
    }
}
