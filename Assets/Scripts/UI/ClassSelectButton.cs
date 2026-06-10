using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectButton : MonoBehaviour
{
    public GameObject icon;
    public GameObject description;
    public new GameObject name;

    public void SetButtonDetails(PlayerClass data)
    {
        SetIcon(data.sprite);
        name.GetComponent<TextMeshProUGUI>().text = data.name.FirstCharacterToUpper();

        var variables = new Dictionary<string, int>
        {
            ["wave"] = 1
        };

        var health = RPNEvaluator.RPNEvaluator.Evaluate(data.health, variables);
        var mana = RPNEvaluator.RPNEvaluator.Evaluate(data.mana, variables);
        var mana_regeneration = RPNEvaluator.RPNEvaluator.Evaluate(data.mana_regeneration, variables);
        var spellpower = RPNEvaluator.RPNEvaluator.Evaluate(data.spellpower, variables);
        var speed = RPNEvaluator.RPNEvaluator.Evaluate(data.speed, variables);

        description.GetComponent<TextMeshProUGUI>().text =
            "Health: " + health +
            "\nMana: " + mana +
            "\nMana Regen: " + mana_regeneration +
            "\nSpellpower: " + spellpower +
            "\nSpeed: " + speed;
    }

    public void SetIcon(int sprite)
    {
        GameManager.Instance.playerSpriteManager.PlaceSprite(sprite, icon.GetComponent<Image>());
    }

    public void SelectClass(string className)
    {
        EventBus.Instance.DoClassSelected(className);
    }
}