using UnityEngine;
using TMPro;
using System.Globalization;

public class MenuSelectorController : MonoBehaviour
{
    public TextMeshProUGUI label;
    public string level;
    public string classId;
    public EnemySpawner spawner;
    public bool isClassButton;

    public void SetLevel(string text)
    {
        isClassButton = false;
        level = text;
        classId = "";
        ConfigureAsLevelButton();
        label.text = text;
    }

    public void SetClass(string id, bool selected)
    {
        isClassButton = true;
        classId = id;
        level = "";
        ConfigureAsClassButton();
        string className = FormatClassName(id);
        label.text = selected ? "Class: " + className + " *" : "Class: " + className;
    }

    public void StartLevel()
    {
        if (isClassButton)
        {
            spawner.SelectPlayerClass(classId);
            return;
        }

        spawner.StartLevel(level);
    }

    void ConfigureAsLevelButton()
    {
        if (label == null) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11;
        label.fontSizeMax = 20;
        label.alignment = TextAlignmentOptions.Center;
    }

    void ConfigureAsClassButton()
    {
        if (label == null) return;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10;
        label.fontSizeMax = 16;
        label.alignment = TextAlignmentOptions.Center;
    }

    string FormatClassName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.ToLowerInvariant().Replace("_", " "));
    }
}
