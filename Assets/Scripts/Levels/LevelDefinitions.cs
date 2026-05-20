using System;
using System.Collections.Generic;

[Serializable]
public class SpawnDefinition
{
    public string enemy;
    public string count;
    public List<int> sequence;
    public string delay;
    public string location;
    public string hp;
    public string speed;
    public string damage;
}

[Serializable]
public class LevelDefinition
{
    public string name;
    public int waves;
    public List<SpawnDefinition> spawns;
}
