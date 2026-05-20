using System.Collections.Generic;
using UnityEngine;

public enum ValueModifierType
{
    Add,
    Multiply,
    Override
}

public class ValueModifier
{
    public readonly ValueModifierType type;
    public readonly float value;

    public ValueModifier(ValueModifierType type, float value)
    {
        this.type = type;
        this.value = value;
    }

    public static float Apply(float baseValue, List<ValueModifier> modifiers)
    {
        float result = baseValue;

        if (modifiers == null)
        {
            return result;
        }

        foreach (ValueModifier modifier in modifiers)
        {
            if (modifier.type == ValueModifierType.Add)
            {
                result += modifier.value;
            }
            else if (modifier.type == ValueModifierType.Multiply)
            {
                result *= modifier.value;
            }
            else if (modifier.type == ValueModifierType.Override)
            {
                result = modifier.value;
            }
        }

        return result;
    }

    public static int ApplyToInt(int baseValue, List<ValueModifier> modifiers)
    {
        return Mathf.RoundToInt(Apply(baseValue, modifiers));
    }
}
