using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public float healingOverTime = 0f;
    public HealthBar healthui;
    public ManaBar manaui;
    public SpellCaster spellcaster;
    public SpellUI spellui;
    public SpellUIContainer spellUIContainer;
    public int speed;
    public Unit unit;
    public List<Relic> relics = new List<Relic>();
    readonly Dictionary<object, int> activeRelicSpellPowerBonuses = new Dictionary<object, int>();
    readonly Dictionary<object, int> activeRelicArmorBonuses = new Dictionary<object, int>();
    readonly Dictionary<object, int> activeRelicSpeedBonuses = new Dictionary<object, int>();
    readonly Dictionary<object, int> activeRelicSpeedGenerations = new Dictionary<object, int>();
    Vector2 currentMoveInput;

    const string DEFAULT_CLASS_ID = "mage";
    public string selectedClassId = DEFAULT_CLASS_ID;
    PlayerClass selectedClassAttributes;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
        
        InvokeRepeating("HealingOverTime", 0, 1);
    }

    public void StartLevel(string classId = DEFAULT_CLASS_ID)
    {
        ClearRelics();
        activeRelicSpellPowerBonuses.Clear();
        activeRelicArmorBonuses.Clear();
        activeRelicSpeedBonuses.Clear();
        activeRelicSpeedGenerations.Clear();
        currentMoveInput = Vector2.zero;
        LoadPlayerClass(classId);
        spellcaster = new SpellCaster(125, 8, Hittable.Team.PLAYER);
        spellcaster.playerOwner = this;
        StartCoroutine(spellcaster.ManaRegeneration());
    
        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;
        ApplyWaveStats(1);

        healthui.SetHealth(hp);
        manaui.SetSpellCaster(spellcaster);

        if (spellUIContainer == null) spellUIContainer = FindFirstObjectByType<SpellUIContainer>();
        if (spellUIContainer != null)
        {
            spellUIContainer.player = this;
            spellUIContainer.Refresh();
        }
        else if (spellui != null) spellui.SetSpell(spellcaster.spell);
    }

    public void ApplyWaveStats(int wave)
    {
        if (selectedClassAttributes == null) LoadPlayerClass(DEFAULT_CLASS_ID);

        int maxHealth = EvaluateClassInt(selectedClassAttributes.health, 100, wave);
        int maxMana = EvaluateClassInt(selectedClassAttributes.mana, 125, wave);
        int manaRegeneration = EvaluateClassInt(selectedClassAttributes.mana_regeneration, 8, wave);
        int spellPower = EvaluateClassInt(selectedClassAttributes.spellpower, 0, wave);
        speed = EvaluateClassInt(selectedClassAttributes.speed, 5, wave);

        hp?.SetMaxHP(maxHealth);
        spellcaster?.SetWaveStats(maxMana, manaRegeneration, spellPower, wave);
    }


    public bool AddRelic(Relic relic)
    {
        if (relic == null) return false;

        foreach (Relic ownedRelic in relics)
        {
            if (ownedRelic != null && ownedRelic.name == relic.name)
            {
                Debug.Log("Already owns relic: " + relic.name);
                return false;
            }
        }

        relics.Add(relic);
        relic.Activate(this);
        EventBus.Instance.DoRelicPickup(relic);
        return true;
    }

    // in amount per second
    public void HealingOverTime()
    {
        if (hp == null) return;
        hp.Heal(healingOverTime);
    }
    
    public int GetRelicSpellPowerBonus()
    {
        return activeRelicSpellPowerBonuses.Values.Sum();
    }

    public void SetRelicSpellPowerBonus(object source, int amount)
    {
        if (source == null) return;

        if (amount == 0) activeRelicSpellPowerBonuses.Remove(source);
        else activeRelicSpellPowerBonuses[source] = amount;
    }

    public void RemoveRelicSpellPowerBonus(object source)
    {
        if (source == null) return;
        activeRelicSpellPowerBonuses.Remove(source);
    }

    public int GetRelicArmorBonus()
    {
        return activeRelicArmorBonuses.Values.Sum();
    }

    public void SetRelicArmorBonus(object source, int amount)
    {
        if (source == null) return;

        if (amount == 0) activeRelicArmorBonuses.Remove(source);
        else activeRelicArmorBonuses[source] = amount;
    }

    public void RemoveRelicArmorBonus(object source)
    {
        if (source == null) return;
        activeRelicArmorBonuses.Remove(source);
    }

    public int ApplyRelicArmorToDamage(int incomingDamage)
    {
        if (incomingDamage <= 0)
        {
            return 0;
        }

        int armor = GetRelicArmorBonus();
        if (armor <= 0)
        {
            return incomingDamage;
        }

        activeRelicArmorBonuses.Clear();
        return Mathf.Max(0, incomingDamage - armor);
    }


    public int GetRelicSpeedBonus()
    {
        return activeRelicSpeedBonuses.Values.Sum();
    }

    public int GetCurrentMoveSpeed()
    {
        return Mathf.Max(0, speed + GetRelicSpeedBonus());
    }

    public void ApplyTemporarySpeedBoost(object source, int amount, float duration)
    {
        if (source == null || amount == 0 || duration <= 0f)
        {
            return;
        }

        activeRelicSpeedBonuses[source] = amount;

        int generation = 1;
        if (activeRelicSpeedGenerations.TryGetValue(source, out int existingGeneration))
        {
            generation = existingGeneration + 1;
        }

        activeRelicSpeedGenerations[source] = generation;
        ApplyMovementInput();
        StartCoroutine(RemoveTemporarySpeedBoostAfterDelay(source, generation, duration));
    }
    
    IEnumerator RemoveTemporarySpeedBoostAfterDelay(object source, int generation, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (activeRelicSpeedGenerations.TryGetValue(source, out int currentGeneration) && currentGeneration == generation)
        {
            RemoveRelicSpeedBonus(source);
        }
    }

    public void RemoveRelicSpeedBonus(object source)
    {
        if (source == null) return;
        activeRelicSpeedBonuses.Remove(source);
        activeRelicSpeedGenerations.Remove(source);
        ApplyMovementInput();
    }

    void ApplyMovementInput()
    {
        if (unit == null)
        {
            return;
        }

        if (!CanPlayerAct())
        {
            unit.movement = Vector2.zero;
            return;
        }

        unit.movement = currentMoveInput * GetCurrentMoveSpeed();
    }

    public int EvaluateRelicAmount(string expression, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return defaultValue;
        }

        int wave = spellcaster != null ? spellcaster.wave : 1;
        return Mathf.RoundToInt(RPNEvaluatorAdapter.Evaluate(expression, new Dictionary<string, float> { { "wave", wave } }));
    }

    void ClearRelics()
    {
        foreach (Relic relic in relics)
        {
            relic?.Deactivate();
        }

        relics.Clear();
        activeRelicSpeedBonuses.Clear();
        activeRelicSpeedGenerations.Clear();
        ApplyMovementInput();
    }

    public void EquipSpell(Spell newSpell)
    {
        if (newSpell == null || spellcaster == null) return;
        spellcaster.ReplaceSpell(0, newSpell);
        spellUIContainer?.Refresh();
        if (spellUIContainer == null && spellui != null) spellui.SetSpell(spellcaster.spell);
    }

    void LoadPlayerClass(string classId)
    {
        if (string.IsNullOrWhiteSpace(classId)) classId = DEFAULT_CLASS_ID;
        selectedClassId = classId;

        TextAsset classJson = Resources.Load<TextAsset>("classes");
        if (classJson == null)
        {
            Debug.LogError("Could not find classes.json in Assets/Resources. Using fallback player stats.");
            selectedClassAttributes = null;
            return;
        }

        try
        {
            selectedClassAttributes = JObject.Parse(classJson.text)[classId].ToObject<PlayerClass>();
            if (selectedClassAttributes == null)
            {
                Debug.LogError("Could not find player class '" + classId + "' in classes.json. Using fallback player stats.");
                return;
            }

            ApplyClassSprite();
        }
        catch
        {
            Debug.LogError("Could not parse classes.json. Using fallback player stats.");
            selectedClassAttributes = null;
        }
    }

    void ApplyClassSprite()
    {
        if (selectedClassAttributes == null || GameManager.Instance.playerSpriteManager == null) return;

        JToken spriteToken = selectedClassAttributes.sprite;
        if (spriteToken == null) return;

        int spriteIndex = Mathf.Max(0, spriteToken.ToObject<int>());
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sprite = GameManager.Instance.playerSpriteManager.Get(spriteIndex);
    }

    int EvaluateClassInt(string? value, int defaultValue, int wave)
    {
        if (value == null) return defaultValue;
        return Mathf.RoundToInt(RPNEvaluatorAdapter.Evaluate(value, new Dictionary<string, float> { { "wave", wave } }));
    }

    void Update()
    {
        if (spellcaster == null) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.digit1Key.wasPressedThisFrame) SelectSpell(0);
        if (keyboard.digit2Key.wasPressedThisFrame) SelectSpell(1);
        if (keyboard.digit3Key.wasPressedThisFrame) SelectSpell(2);
        if (keyboard.digit4Key.wasPressedThisFrame) SelectSpell(3);

        ApplyMovementInput();
    }

    void SelectSpell(int index)
    {
        if (spellcaster.SelectSpell(index))
        {
            spellUIContainer?.Refresh();
        }
    }

    void OnAttack(InputValue value)
    {
        if (!CanPlayerAct()) return;
        Vector2 mouseScreen = Mouse.current.position.value;
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
        mouseWorld.z = 0;
        StartCoroutine(spellcaster.Cast(transform.position, mouseWorld));
    }

    void OnMove(InputValue value)
    {
        currentMoveInput = value.Get<Vector2>();

        if (!CanPlayerAct())
        {
            currentMoveInput = Vector2.zero;
            unit.movement = Vector2.zero;
            return;
        }

        ApplyMovementInput();
    }

    void Die()
    {
        Debug.Log("You Lost");
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        currentMoveInput = Vector2.zero;
        unit.movement = Vector2.zero;
    }

    bool CanPlayerAct()
    {
        return GameManager.Instance.state != GameManager.GameState.PREGAME
            && GameManager.Instance.state != GameManager.GameState.GAMEOVER
            && GameManager.Instance.state != GameManager.GameState.VICTORY;
    }
}
