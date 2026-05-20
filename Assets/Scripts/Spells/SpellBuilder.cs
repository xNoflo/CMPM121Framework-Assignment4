using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SpellBuilder
{
    private const string SPELL_FILE = "spells";
    private const string STARTING_SPELL_ID = "arcane_bolt";
    private const int MAX_RANDOM_MODIFIERS = 4;
    private const float ADD_MODIFIER_CHANCE = 0.65f;
    private static readonly HashSet<string> MODIFIER_IDS = new HashSet<string>
    {
        "damage_amp",
        "speed_amp",
        "doubler",
        "splitter",
        "chaos",
        "homing",
        "echo",
        "focused",
        "heavy",
        "fracturing"
    };
    private static readonly List<string> IMPLEMENTED_BASE_IDS = new List<string>
    {
        "arcane_bolt",
        "magic_missile"
    };

    private readonly Dictionary<string, SpellDefinition> definitions = new Dictionary<string, SpellDefinition>();

    public SpellBuilder()
    {
        LoadDefinitions();
    }

    public Spell Build(SpellCaster owner)
    {
        SpellDefinition definition = GetDefinition(STARTING_SPELL_ID);

        if (definition == null)
        {
            Debug.LogWarning("Starting spell '" + STARTING_SPELL_ID + "' was not found. Falling back to hardcoded bolt values.");
            return new Spell(owner);
        }

        return new Spell(owner, definition);
    }

    public Spell BuildRandomSpell(SpellCaster owner)
    {
        string baseSpellId = PickRandomBaseSpellId();
        List<string> modifierIds = PickRandomModifierIds();
        return BuildSpell(owner, baseSpellId, modifierIds);
    }

    public Spell BuildSpell(SpellCaster owner, string baseSpellId, List<string> modifierIds)
    {
        SpellDefinition baseDefinition = GetDefinition(baseSpellId);

        if (baseDefinition == null)
        {
            Debug.LogWarning("Base spell '" + baseSpellId + "' was not found. Falling back to the starting spell.");
            return Build(owner);
        }

        if (IsModifier(baseSpellId))
        {
            Debug.LogWarning("'" + baseSpellId + "' is a modifier, not a base spell. Falling back to the starting spell.");
            return Build(owner);
        }

        Spell spell = new Spell(owner, baseDefinition);

        if (modifierIds == null)
        {
            return spell;
        }

        foreach (string modifierId in modifierIds)
        {
            spell = ApplyModifier(spell, modifierId);
        }

        return spell;
    }

    public Spell ApplyModifier(Spell spell, string modifierId)
    {
        SpellDefinition modifierDefinition = GetDefinition(modifierId);

        if (modifierDefinition == null)
        {
            Debug.LogWarning("Modifier spell '" + modifierId + "' was not found. Skipping it.");
            return spell;
        }

        if (!IsModifier(modifierId))
        {
            Debug.LogWarning("'" + modifierId + "' is not a supported modifier spell. Skipping it.");
            return spell;
        }

        return new JsonModifierSpell(spell, modifierDefinition);
    }

    public SpellDefinition GetDefinition(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        definitions.TryGetValue(id, out SpellDefinition definition);
        return definition;
    }

    public List<string> GetDefinitionIds()
    {
        return new List<string>(definitions.Keys);
    }

    public bool IsModifier(string id)
    {
        return MODIFIER_IDS.Contains(id);
    }

    public List<string> GetModifierIds()
    {
        return new List<string>(MODIFIER_IDS);
    }

    public List<string> GetImplementedBaseIds()
    {
        List<string> baseIds = new List<string>();

        foreach (string id in IMPLEMENTED_BASE_IDS)
        {
            if (definitions.ContainsKey(id))
            {
                baseIds.Add(id);
            }
        }

        return baseIds;
    }

    private string PickRandomBaseSpellId()
    {
        List<string> baseIds = GetImplementedBaseIds();

        if (baseIds.Count == 0)
        {
            return STARTING_SPELL_ID;
        }

        return baseIds[Random.Range(0, baseIds.Count)];
    }

    private List<string> PickRandomModifierIds()
    {
        List<string> availableModifiers = GetAvailableModifierIds();
        List<string> chosenModifiers = new List<string>();

        for (int i = 0; i < MAX_RANDOM_MODIFIERS && availableModifiers.Count > 0; i++)
        {
            if (i > 0 && Random.value > ADD_MODIFIER_CHANCE)
            {
                break;
            }

            int index = Random.Range(0, availableModifiers.Count);
            chosenModifiers.Add(availableModifiers[index]);
            availableModifiers.RemoveAt(index);
        }

        return chosenModifiers;
    }

    private List<string> GetAvailableModifierIds()
    {
        List<string> modifierIds = new List<string>();

        foreach (string id in MODIFIER_IDS)
        {
            if (definitions.ContainsKey(id))
            {
                modifierIds.Add(id);
            }
        }

        return modifierIds;
    }

    private void LoadDefinitions()
    {
        TextAsset spellJson = Resources.Load<TextAsset>(SPELL_FILE);

        if (spellJson == null)
        {
            Debug.LogError("Could not find spells.json in Assets/Resources.");
            return;
        }

        JObject root;

        try
        {
            root = JObject.Parse(spellJson.text);
        }
        catch
        {
            Debug.LogError("Could not parse spells.json. Check that the file contains valid JSON.");
            return;
        }

        definitions.Clear();

        foreach (JProperty spellEntry in root.Properties())
        {
            if (spellEntry.Value is JObject attributes)
            {
                definitions[spellEntry.Name] = new SpellDefinition(spellEntry.Name, attributes);
            }
            else
            {
                Debug.LogWarning("Skipping spell entry '" + spellEntry.Name + "' because it is not a JSON object.");
            }
        }

        Debug.Log("Loaded " + definitions.Count + " spell definitions from spells.json.");
    }
}
