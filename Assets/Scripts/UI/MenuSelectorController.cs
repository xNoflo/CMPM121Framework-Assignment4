using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MenuSelectorController : MonoBehaviour
{
    public TextMeshProUGUI label;
    public string level;
    public EnemySpawner spawner;
    Button button;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        button = GetComponent<Button>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLevel(string levelId, string text = null)
    {
        level = levelId;
        label.text = string.IsNullOrWhiteSpace(text) ? levelId : text;
        EnsureButtonReference();
        if (button == null) return;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(StartLevel);
    }

    public void SetAction(string text, UnityEngine.Events.UnityAction action)
    {
        level = null;
        label.text = text;
        EnsureButtonReference();
        if (button == null) return;
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(action);
    }

    public void StartLevel()
    {
        spawner.StartLevel(level);
    }

    void EnsureButtonReference()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }
}
