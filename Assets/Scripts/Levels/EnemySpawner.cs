using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class EnemySpawner : MonoBehaviour
{
    public Image level_selector;
    public GameObject button;
    public GameObject enemy;
    public SpawnPoint[] SpawnPoints;

    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<string, Enemy> enemiesByName = new Dictionary<string, Enemy>();
    private List<LevelDefinition> levels = new List<LevelDefinition>();
    private LevelDefinition selectedLevel;
    private int currentWave = 0;

    // Levels with waves <= 0 are treated as endless.
    public bool IsCurrentLevelComplete
    {
        get
        {
            return selectedLevel != null && selectedLevel.waves > 0 && currentWave >= selectedLevel.waves;
        }
    }

    void Start()
    {
        GameManager.Instance.LoadRelics();
        LoadEnemies();
        LoadLevels();
        CreateLevelButtons();
    }

    private void LoadLevels()
    {
        // Unity Resource paths ignore the file extension, so this reads as Assets/Resources/levels.json.
        TextAsset levelJson = Resources.Load<TextAsset>("levels");

        if (levelJson == null)
        {
            Debug.LogError("Could not find the file levels.json in Assets/Resources.");
            return;
        }

        levels = JsonConvert.DeserializeObject<List<LevelDefinition>>(levelJson.text);

        if (levels == null)
        {
            Debug.LogError("Could not read the level definitions from levels.json.");
            levels = new List<LevelDefinition>();
            return;
        }

        Debug.Log($"Loaded {levels.Count} level definitions.");
    }

    private void CreateLevelButtons()
    {
        // Build the difficulty menu from JSON so that adding a level creates a button automatically in the UI..
        for (int i = 0; i < levels.Count; i++)
        {
            GameObject selector = Instantiate(button, level_selector.transform);
            selector.transform.localPosition = new Vector3(0, 130 - i * 60);

            MenuSelectorController controller = selector.GetComponent<MenuSelectorController>();
            controller.spawner = this;
            controller.SetLevel(levels[i].name);
        }
    }

    void Update()
    {

    }

    public void StartLevel(string levelname)
    {
        StopAllCoroutines();
        selectedLevel = levels.Find(level => level.name == levelname);

        if (selectedLevel == null)
        {
            Debug.LogError("Could not find level: " + levelname);
            return;
        }

        Debug.Log("Starting level: " + selectedLevel.name);

        currentWave = 0;
        level_selector.gameObject.SetActive(false);
        GameManager.Instance.player.GetComponent<PlayerController>().StartLevel();
        StartCoroutine(SpawnWave());
    }

    public void NextWave()
    {
        if (selectedLevel == null || IsCurrentLevelComplete || IsEndState())
        {
            return;
        }

        StartCoroutine(SpawnWave());
    }

    public void ReturnToStart()
    {
        StopAllCoroutines();
        GameManager.Instance.ClearEnemies();
        GameManager.Instance.ResetWaveStats();
        GameManager.Instance.countdown = 0;
        GameManager.Instance.state = GameManager.GameState.PREGAME;
        currentWave = 0;
        selectedLevel = null;

        if (level_selector != null)
        {
            level_selector.gameObject.SetActive(true);
        }
    }

    private void LoadEnemies()
    {
        // Load enemy templates once, then index them by name for quick spawn lookups.
        TextAsset enemyJson = Resources.Load<TextAsset>("enemies");

        if (enemyJson == null)
        {
            Debug.LogError("Could not find the file enemies.json in Assets/Resources.");
            return;
        }

        enemies = JsonConvert.DeserializeObject<List<Enemy>>(enemyJson.text);

        if (enemies == null)
        {
            Debug.LogError("Could not read the enemy definitions from enemies.json.");
            enemies = new List<Enemy>();
            return;
        }

        enemiesByName.Clear();

        foreach (Enemy enemyData in enemies)
        {
            enemiesByName[enemyData.name] = enemyData;
        }

        Debug.Log($"Loaded {enemies.Count} enemy definitions.");
    }


    IEnumerator SpawnWave()
    {
        if (selectedLevel == null)
        {
            Debug.LogError("Cannot spawn a wave before a level has been selected.");
            yield break;
        }

        if (IsCurrentLevelComplete)
        {
            GameManager.Instance.state = GameManager.GameState.VICTORY;
            yield break;
        }

        currentWave++;
        GameManager.Instance.ResetWaveStats();

        PlayerController player = GameManager.Instance.player.GetComponent<PlayerController>();
        if (player != null)
        {
            player.ApplyWaveStats(currentWave);
        }

        // Show the pre wave countdown before enemies begin spawning.
        GameManager.Instance.state = GameManager.GameState.COUNTDOWN;

        for (int i = 3; i > 0; i--)
        {
            GameManager.Instance.countdown = i;
            yield return new WaitForSeconds(1);

            if (IsEndState())
            {
                yield break;
            }
        }

        GameManager.Instance.countdown = 0;
        GameManager.Instance.state = GameManager.GameState.INWAVE;

        if (selectedLevel.spawns == null || selectedLevel.spawns.Count == 0)
        {
            GameManager.Instance.state = IsCurrentLevelComplete
                ? GameManager.GameState.VICTORY
                : GameManager.GameState.WAVEEND;
            yield break;
        }

        int activeSpawnDefinitions = selectedLevel.spawns.Count;

        // Each spawn definition runs independently. The wave ends after all groups finish and all enemies die.
        foreach (SpawnDefinition spawnDefinition in selectedLevel.spawns)
        {
            StartCoroutine(SpawnEnemyGroups(spawnDefinition, () => activeSpawnDefinitions--));
        }

        yield return new WaitUntil(() => IsEndState() || activeSpawnDefinitions == 0 && GameManager.Instance.enemy_count == 0);

        if (IsEndState())
        {
            yield break;
        }

        GameManager.Instance.state = IsCurrentLevelComplete
            ? GameManager.GameState.VICTORY
            : GameManager.GameState.WAVEEND;
    }

    IEnumerator SpawnEnemyGroups(SpawnDefinition spawnDefinition, Action onFinished)
    {
        if (!enemiesByName.TryGetValue(spawnDefinition.enemy, out Enemy enemyData))
        {
            Debug.LogError("Could not find enemy definition: " + spawnDefinition.enemy);
            onFinished();
            yield break;
        }

        // Count, delay, and stat fields can be RPN formulas that use "base" and "wave".
        int totalToSpawn = Mathf.Max(0, Mathf.FloorToInt(EvaluateSpawnValue(spawnDefinition.count, enemyData, 0)));
        float delay = Mathf.Max(0, EvaluateSpawnValue(spawnDefinition.delay, enemyData, 2));
        List<int> sequence = spawnDefinition.sequence != null && spawnDefinition.sequence.Count > 0
            ? spawnDefinition.sequence.Where(groupSize => groupSize > 0).ToList()
            : new List<int> { 1 };

        if (sequence.Count == 0)
        {
            sequence.Add(1);
        }

        int spawned = 0;
        int sequenceIndex = 0;

        while (spawned < totalToSpawn && !IsEndState())
        {
            // The sequence controls burst size; an example is that [2, 3] means spawn 2, wait, spawn 3, repeat.
            int groupSize = Mathf.Min(sequence[sequenceIndex], totalToSpawn - spawned);
            SpawnPoint spawnPoint = PickSpawnPoint(spawnDefinition.location);

            if (spawnPoint == null)
            {
                onFinished();
                yield break;
            }

            for (int i = 0; i < groupSize; i++)
            {
                SpawnEnemy(enemyData, spawnDefinition, spawnPoint);
            }

            spawned += groupSize;
            sequenceIndex = (sequenceIndex + 1) % sequence.Count;

            if (spawned < totalToSpawn)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        onFinished();
    }

    private bool IsEndState()
    {
        return GameManager.Instance.state == GameManager.GameState.GAMEOVER
            || GameManager.Instance.state == GameManager.GameState.VICTORY;
    }

    private void SpawnEnemy(Enemy enemyData, SpawnDefinition spawnDefinition, SpawnPoint spawnPoint)
    {
        // Spread enemies around the selected spawn point so that groups do not stack together/on top of each other.
        Vector2 offset = UnityEngine.Random.insideUnitCircle * 1.8f;
        Vector3 initialPosition = spawnPoint.transform.position + new Vector3(offset.x, offset.y, 0);
        GameObject newEnemy = Instantiate(enemy, initialPosition, Quaternion.identity);

        int hp = Mathf.RoundToInt(EvaluateSpawnValue(spawnDefinition.hp, enemyData, enemyData.hp));
        int speed = Mathf.RoundToInt(EvaluateSpawnValue(spawnDefinition.speed, enemyData, enemyData.speed));
        int damage = Mathf.RoundToInt(EvaluateSpawnValue(spawnDefinition.damage, enemyData, enemyData.damage));

        newEnemy.GetComponent<SpriteRenderer>().sprite = GameManager.Instance.enemySpriteManager.Get(enemyData.sprite);

        EnemyController enemyController = newEnemy.GetComponent<EnemyController>();
        enemyController.hp = new Hittable(hp, Hittable.Team.MONSTERS, newEnemy);
        enemyController.speed = speed;
        enemyController.damage = damage;

        GameManager.Instance.AddEnemy(newEnemy);
    }

    private SpawnPoint PickSpawnPoint(string location)
    {
        if (SpawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points are assigned to EnemySpawner.");
            return null;
        }

        // Locations like "random red" use the second word to request a spawn point area.
        string[] parts = string.IsNullOrWhiteSpace(location)
            ? new string[] { "random" }
            : location.ToLower().Split(' ');

        if (parts.Length >= 2 && Enum.TryParse(parts[1], true, out SpawnPoint.SpawnName kind))
        {
            SpawnPoint[] matchingPoints = SpawnPoints.Where(spawnPoint => spawnPoint.kind == kind).ToArray();

            if (matchingPoints.Length > 0)
            {
                return matchingPoints[UnityEngine.Random.Range(0, matchingPoints.Length)];
            }

            Debug.LogWarning("No spawn point found for location: " + location + ". Using any random spawn point.");
        }

        return SpawnPoints[UnityEngine.Random.Range(0, SpawnPoints.Length)];
    }

    private float EvaluateSpawnValue(string expression, Enemy enemyData, float defaultValue)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            expression = "base";
        }

        // "base" comes from enemies.json; "wave" lets levels scale as the run progresses.
        Dictionary<string, float> variables = new Dictionary<string, float>
        {
            { "base", defaultValue },
            { "wave", currentWave }
        };

        return RPNEvaluatorAdapter.Evaluate(expression, variables);
    }
}

public static class RPNEvaluatorAdapter
{
    private class EvaluatorBinding
    {
        public MethodInfo method;
        public object target;
        public Type variableType;
        public bool usesVariables;
    }

    private static EvaluatorBinding cachedBinding;
    private static bool searchedForBinding;
    private static bool externalEvaluatorFailed;

    public static float Evaluate(string expression, Dictionary<string, float> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return 0;
        }

        if (float.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out float constantValue))
        {
            return constantValue;
        }

        EvaluatorBinding binding = externalEvaluatorFailed ? null : GetEvaluatorBinding();

        if (binding != null)
        {
            try
            {
                object result = binding.usesVariables
                    ? binding.method.Invoke(binding.target, new object[] { expression, BuildVariableArgument(binding.variableType, variables) })
                    : binding.method.Invoke(binding.target, new object[] { expression });

                return Convert.ToSingle(result);
            }
            catch
            {
                externalEvaluatorFailed = true;
            }
        }

        if (TryEvaluateFallback(expression, variables, out float fallbackResult))
        {
            return fallbackResult;
        }

        Debug.LogError("Could not evaluate RPN expression '" + expression + "'.");
        return 0;
    }

    private static bool TryEvaluateFallback(string expression, Dictionary<string, float> variables, out float result)
    {
        result = 0;
        Stack<float> stack = new Stack<float>();
        string[] tokens = expression.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string token in tokens)
        {
            if (float.TryParse(token, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float number))
            {
                stack.Push(number);
                continue;
            }

            if (variables != null && variables.TryGetValue(token, out float variableValue))
            {
                stack.Push(variableValue);
                continue;
            }

            if (IsBinaryOperator(token))
            {
                if (stack.Count < 2)
                {
                    return false;
                }

                float right = stack.Pop();
                float left = stack.Pop();
                stack.Push(ApplyBinaryOperator(token, left, right));
                continue;
            }

            return false;
        }

        if (stack.Count != 1)
        {
            return false;
        }

        result = stack.Pop();
        return true;
    }

    private static bool IsBinaryOperator(string token)
    {
        return token == "+" || token == "-" || token == "*" || token == "/" || token == "%";
    }

    private static float ApplyBinaryOperator(string token, float left, float right)
    {
        if (token == "+") return left + right;
        if (token == "-") return left - right;
        if (token == "*") return left * right;
        if (token == "/") return Mathf.Abs(right) > Mathf.Epsilon ? left / right : 0;
        if (token == "%") return Mathf.Abs(right) > Mathf.Epsilon ? left % right : 0;
        return 0;
    }

    private static EvaluatorBinding GetEvaluatorBinding()
    {
        if (searchedForBinding)
        {
            return cachedBinding;
        }

        searchedForBinding = true;

        // Find a compatible RPNEvaluator method once, then reuse it for all future evaluations.
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (type == typeof(RPNEvaluatorAdapter) || !type.Name.Contains("RPNEvaluator"))
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.Name != "Evaluate" && method.Name != "Evaluatef")
                    {
                        continue;
                    }

                    EvaluatorBinding binding = TryCreateBinding(method);

                    if (binding != null)
                    {
                        cachedBinding = binding;
                        return cachedBinding;
                    }
                }
            }
        }

        return null;
    }

    private static EvaluatorBinding TryCreateBinding(MethodInfo method)
    {
        ParameterInfo[] parameters = method.GetParameters();

        try
        {
            object target = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                method.Invoke(target, new object[] { "1" });

                return new EvaluatorBinding
                {
                    method = method,
                    target = target,
                    usesVariables = false
                };
            }

            if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string))
            {
                Type variableType = parameters[1].ParameterType;
                object variableArgument = BuildVariableArgument(variableType, new Dictionary<string, float> { { "x", 1 } });

                if (variableArgument != null)
                {
                    method.Invoke(target, new object[] { "x", variableArgument });

                    return new EvaluatorBinding
                    {
                        method = method,
                        target = target,
                        variableType = variableType,
                        usesVariables = true
                    };
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.Where(type => type != null);
        }
    }

    private static object BuildVariableArgument(Type parameterType, Dictionary<string, float> variables)
    {
        // Support common dictionary value types used by different evaluator implementations.
        if (parameterType == typeof(Dictionary<string, float>))
        {
            return variables;
        }

        if (parameterType == typeof(Dictionary<string, double>))
        {
            return variables.ToDictionary(pair => pair.Key, pair => (double)pair.Value);
        }

        if (parameterType == typeof(Dictionary<string, int>))
        {
            return variables.ToDictionary(pair => pair.Key, pair => Mathf.RoundToInt(pair.Value));
        }

        return null;
    }
}
