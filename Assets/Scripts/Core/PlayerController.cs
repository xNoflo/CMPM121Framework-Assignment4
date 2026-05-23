using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public Hittable hp;
    public HealthBar healthui;
    public ManaBar manaui;
    public SpellCaster spellcaster;
    public SpellUI spellui;
    public SpellUIContainer spellUIContainer;
    public int speed;
    public Unit unit;
    public List<Relic> relics = new List<Relic>();
    readonly Dictionary<object, int> activeRelicSpellPowerBonuses = new Dictionary<object, int>();

    const string DEFAULT_CLASS_ID = "mage";
    public string selectedClassId = DEFAULT_CLASS_ID;
    JObject selectedClassAttributes;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    public void StartLevel(string classId = DEFAULT_CLASS_ID)
    {
        ClearRelics();
        activeRelicSpellPowerBonuses.Clear();
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

        int maxHealth = EvaluateClassInt("health", 100, wave);
        int maxMana = EvaluateClassInt("mana", 125, wave);
        int manaRegeneration = EvaluateClassInt("mana_regeneration", 8, wave);
        int spellPower = EvaluateClassInt("spellpower", 0, wave);
        speed = EvaluateClassInt("speed", 5, wave);

        if (hp != null) hp.SetMaxHP(maxHealth);
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
        relic.Initialize(this);
        EventBus.Instance.DoRelicPickup(relic);
        return true;
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
            relic?.Cleanup();
        }

        relics.Clear();
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
            selectedClassAttributes = JObject.Parse(classJson.text)[classId] as JObject;
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

        JToken spriteToken = selectedClassAttributes.SelectToken("sprite");
        if (spriteToken == null) return;

        int spriteIndex = Mathf.Max(0, spriteToken.ToObject<int>());
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null) renderer.sprite = GameManager.Instance.playerSpriteManager.Get(spriteIndex);
    }

    int EvaluateClassInt(string field, int defaultValue, int wave)
    {
        JToken token = selectedClassAttributes?.SelectToken(field);
        if (token == null) return defaultValue;
        return Mathf.RoundToInt(RPNEvaluatorAdapter.Evaluate(token.ToString(), new Dictionary<string, float> { { "wave", wave } }));
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
        if (!CanPlayerAct())
        {
            unit.movement = Vector2.zero;
            return;
        }

        unit.movement = value.Get<Vector2>() * speed;
    }

    void Die()
    {
        Debug.Log("You Lost");
        GameManager.Instance.state = GameManager.GameState.GAMEOVER;
        unit.movement = Vector2.zero;
    }

    bool CanPlayerAct()
    {
        return GameManager.Instance.state != GameManager.GameState.PREGAME
            && GameManager.Instance.state != GameManager.GameState.GAMEOVER
            && GameManager.Instance.state != GameManager.GameState.VICTORY;
    }
}
