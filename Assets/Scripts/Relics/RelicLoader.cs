using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class RelicLoader
{
    public static List<Relic> LoadAll()
    {
        // read and deserialize relics.json
        string json = File.ReadAllText("Assets/Resources/relics.json");
        
        var result = JsonConvert.DeserializeObject<List<Relic>>(json);

        foreach (var relic in result)
        {
            Debug.Log("Relic: " + relic.name);
            Debug.Log("Trigger: " + relic.trigger.type + " " + " type:  " + relic.trigger.GetType().Name);
            Debug.Log("Effect: " + relic.effect.type + " " + " type:  " + relic.effect.GetType().Name);
            Debug.Log("-----");
        }
        
        //var relicDict = result.ToDictionary(x => x.name, x => x);
        
        return result;
    }
}
