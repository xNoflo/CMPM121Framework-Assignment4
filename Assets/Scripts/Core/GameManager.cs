using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager
{
    public enum GameState
    {
        PREGAME,
        INWAVE,
        WAVEEND,
        COUNTDOWN,
        GAMEOVER,
        VICTORY
    }
    public GameState state;

    public int countdown;
    public int enemiesDefeatedThisWave;

    private static GameManager theInstance;
    public static GameManager Instance { get
        {
            if (theInstance == null)
                theInstance = new GameManager();
            return theInstance;
        }
    }

    public GameObject player;

    public ProjectileManager projectileManager;
    public SpellIconManager spellIconManager;
    public EnemySpriteManager enemySpriteManager;
    public PlayerSpriteManager playerSpriteManager;
    public RelicIconManager relicIconManager;

    private List<GameObject> enemies;

    public Dictionary<string, PlayerClass> playerClasses = new();

    public int enemy_count { get { return enemies.Count; } }
    private List<Relic> relics;
    public List<Relic> relic_definitions
    {
        get
        {
            if (relics == null)
            {
                relics = RelicLoader.LoadAll();
            }

            return relics;
        }
    }

    public void LoadRelics()
    {
        relics = RelicLoader.LoadAll();
    }

    public void AddEnemy(GameObject enemy)
    {
        enemies.Add(enemy);
    }
    public void RemoveEnemy(GameObject enemy)
    {
        enemies.Remove(enemy);
    }

    public void ClearEnemies()
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy != null)
            {
                UnityEngine.Object.Destroy(enemy);
            }
        }

        enemies.Clear();
    }

    public void ResetWaveStats()
    {
        enemiesDefeatedThisWave = 0;
    }

    public void RegisterEnemyDefeated()
    {
        enemiesDefeatedThisWave++;
    }
    public void LoadPlayerClasses()
    {
        TextAsset classJson = Resources.Load<TextAsset>("classes");
        var defaultMage = new PlayerClass()
        {
            name = "Mage",
            sprite = 0,
            health = "95 wave 5 * +",
            mana = "90 wave 10 * +",
            mana_regeneration = "10 wave +",
            spellpower = "wave 10 *",
            speed = "5"
        };

        if (classJson == null)
        {
            Debug.LogError("Could not find classes.json in Assets/Resources.");
            playerClasses["mage"] = defaultMage;
            return;
        }

        try
        {
            Dictionary<string, PlayerClass> classRoot = JObject.Parse(classJson.text).ToObject<Dictionary<string, PlayerClass>>();

            var result = new Dictionary<string, PlayerClass>(classRoot);
            foreach (var key in classRoot.Keys.ToList())
            {
                result[key].name = key;
            }
            playerClasses = result;
        }
        catch
        {
            Debug.LogError("Could not parse classes.json. Using mage as the only selectable class.");
        }

        if (playerClasses.Count == 0)
        {
            playerClasses["mage"] = defaultMage;
        }

        //if (!playerClasses.Contains(selectedClassId))
        //{
        //    selectedClassId = playerClasses[0];
        //}

        Debug.Log("Loaded " + playerClasses.Count + " player classes.");
    }

    public GameObject GetClosestEnemy(Vector3 point)
    {
        if (enemies == null || enemies.Count == 0) return null;
        if (enemies.Count == 1) return enemies[0];
        return enemies.Aggregate((a,b) => (a.transform.position - point).sqrMagnitude < (b.transform.position - point).sqrMagnitude ? a : b);
    }

    private GameManager()
    {
        enemies = new List<GameObject>();
        
        LoadPlayerClasses();
    }
}
