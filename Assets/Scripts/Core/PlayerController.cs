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
    public List<Relic> relics = new List<Relic>();

    const string DEFAULT_CLASS_ID = "mage";
    string selectedClassId = DEFAULT_CLASS_ID;
    JObject selectedClassAttributes;

    void Start()
    {
        unit = GetComponent<Unit>();
        GameManager.Instance.player = gameObject;
    }

    public void StartLevel()
    {
        relics.Clear();
        selectedClassId = string.IsNullOrWhiteSpace(GameManager.Instance.selectedClassId)
            ? DEFAULT_CLASS_ID
            : GameManager.Instance.selectedClassId;
        LoadPlayerClass(selectedClassId);
        spellcaster = new SpellCaster(125, 8, Hittable.Team.PLAYER);
        StartCoroutine(spellcaster.ManaRegeneration());

        hp = new Hittable(100, Hittable.Team.PLAYER, gameObject);
        hp.OnDeath += Die;
        hp.team = Hittable.Team.PLAYER;
        ApplyClassSprite();
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
        if (selectedClassAttributes == null) LoadPlayerClass(selectedClassId);

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
        EventBus.Instance.DoRelicPickup(relic);
        return true;
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
            selectedClassId = string.IsNullOrWhiteSpace(classId) ? DEFAULT_CLASS_ID : classId;
            selectedClassAttributes = JObject.Parse(classJson.text)[selectedClassId] as JObject;
            if (selectedClassAttributes == null)
                Debug.LogError("Could not find player class '" + selectedClassId + "' in classes.json. Using fallback player stats.");
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

    void ApplyClassSprite()
    {
        if (selectedClassAttributes == null || GameManager.Instance.playerSpriteManager == null)
        {
            return;
        }

        JToken spriteToken = selectedClassAttributes.SelectToken("sprite");
        if (spriteToken == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        int spriteIndex = spriteToken.Value<int>();
        if (spriteIndex < 0 || spriteIndex >= GameManager.Instance.playerSpriteManager.GetCount())
        {
            return;
        }

        spriteRenderer.sprite = GameManager.Instance.playerSpriteManager.Get(spriteIndex);
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
