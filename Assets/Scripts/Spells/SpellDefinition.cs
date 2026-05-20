using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SpellDefinition
{
    public readonly string id;
    public readonly JObject attributes;

    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Icon { get; private set; }

    public SpellDefinition(string id, JObject attributes)
    {
        this.id = id;
        this.attributes = attributes;

        Name = GetString("name", id);
        Description = GetString("description", "");
        Icon = GetInt("icon", 0, 0, 1);
    }

    public string GetString(string path, string defaultValue = "")
    {
        JToken token = attributes.SelectToken(path);
        return token != null ? token.ToString() : defaultValue;
    }

    public bool HasField(string path)
    {
        return attributes.SelectToken(path) != null;
    }

    public float GetFloat(string path, float defaultValue, float power, float wave)
    {
        JToken token = attributes.SelectToken(path);

        if (token == null)
        {
            return defaultValue;
        }

        return SpellExpression.Evaluate(token.ToString(), power, wave);
    }

    public int GetInt(string path, int defaultValue, float power, float wave)
    {
        return Mathf.RoundToInt(GetFloat(path, defaultValue, power, wave));
    }

    public Damage.Type GetDamageType(string path = "damage.type", Damage.Type defaultValue = Damage.Type.ARCANE)
    {
        string damageType = GetString(path, defaultValue.ToString()).ToUpperInvariant();

        if (Enum.TryParse(damageType, out Damage.Type parsedType))
        {
            return parsedType;
        }

        Debug.LogWarning("Unknown damage type '" + damageType + "' on spell '" + id + "'. Using " + defaultValue + ".");
        return defaultValue;
    }
}

public static class SpellExpression
{
    public static float Evaluate(string expression, float power, float wave)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return 0;
        }

        Dictionary<string, float> variables = new Dictionary<string, float>
        {
            { "power", power },
            { "wave", wave }
        };

        return RPNEvaluatorAdapter.Evaluate(expression, variables);
    }
}
