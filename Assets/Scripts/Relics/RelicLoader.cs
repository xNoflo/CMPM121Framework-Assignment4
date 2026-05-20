using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

public static class RelicLoader
{
    public static List<Relic> LoadAll()
    {
        TextAsset relicJson = Resources.Load<TextAsset>("relics");

        if (relicJson == null)
        {
            Debug.LogError("Could not find the file relics.json in Assets/Resources.");
            return new List<Relic>();
        }

        List<RelicDefinition> definitions = JsonConvert.DeserializeObject<List<RelicDefinition>>(relicJson.text);

        if (definitions == null)
        {
            Debug.LogError("Could not read the relic definitions from relics.json.");
            return new List<Relic>();
        }

        List<Relic> relics = new List<Relic>();

        foreach (RelicDefinition definition in definitions)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.name))
            {
                Debug.LogWarning("Skipping an invalid relic definition.");
                continue;
            }

            relics.Add(new Relic(definition));
        }

        Debug.Log("Loaded " + relics.Count + " relic definitions.");
        return relics;
    }
}
