using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpellUI : MonoBehaviour
{
    public GameObject icon, highlight, dropbutton;
    public RectTransform cooldown;
    public TextMeshProUGUI manacost, damage;
    public Spell spell;
    int index;
    SpellUIContainer container;
    float last_text_update;
    const float UPDATE_DELAY = 1;

    public void SetIndex(SpellUIContainer container, int index)
    {
        this.container = container;
        this.index = index;
        Button button = dropbutton != null ? dropbutton.GetComponent<Button>() : null;
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => container.DropSpell(this.index));
    }

    public void SetSpell(Spell spell, bool canDrop = false)
    {
        this.spell = spell;
        gameObject.SetActive(spell != null);
        if (spell == null) return;
        GameManager.Instance.spellIconManager.PlaceSprite(spell.GetIcon(), icon.GetComponent<Image>());
        SetDropButtonVisible(canDrop);
        UpdateText();
    }

    public void SetDropButtonVisible(bool visible)
    {
        if (dropbutton != null) dropbutton.SetActive(visible);
    }

    public void SetHighlighted(bool selected)
    {
        if (highlight != null) highlight.SetActive(selected);
    }

    void Update()
    {
        if (spell == null) return;
        if (Time.time > last_text_update + UPDATE_DELAY)
        {
            UpdateText();
            last_text_update = Time.time;
        }

        float cooldownTime = spell.GetCooldown();
        float perc = Time.time - spell.last_cast > cooldownTime ? 0 : 1 - (Time.time - spell.last_cast) / cooldownTime;
        cooldown.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 48 * perc);
    }

    void UpdateText()
    {
        manacost.text = spell.GetManaCost().ToString();
        damage.text = spell.GetDamage().ToString();
    }
}
