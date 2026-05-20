using UnityEngine;
using UnityEngine.InputSystem;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

    const string DEFAULT_CLASS_ID = "mage";
    JObject selectedClassAttributes;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    public void StartLevel()
    {
        LoadPlayerClass(DEFAULT_CLASS_ID);
        spellcaster = new SpellCaster(125, 8, Hittable.Team.PLAYER);
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

    public void EquipSpell(Spell newSpell)
    {
        if (newSpell == null || spellcaster == null) return;
        spellcaster.ReplaceSpell(0, newSpell);
        spellUIContainer?.Refresh();
        if (spellUIContainer == null && spellui != null) spellui.SetSpell(spellcaster.spell);
    }

    void LoadPlayerClass(string classId)
    {
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
                Debug.LogError("Could not find player class '" + classId + "' in classes.json. Using fallback player stats.");
        }
        catch
        {
            Debug.LogError("Could not parse classes.json. Using fallback player stats.");
            selectedClassAttributes = null;
        }
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
