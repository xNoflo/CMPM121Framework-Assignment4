using UnityEngine;
using TMPro;

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
        label.text = text;
    }

    public void SetClass(string id, bool selected)
    {
        isClassButton = true;
        classId = id;
        level = "";
        label.text = selected ? "Class: " + id + " ✓" : "Class: " + id;
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
}
