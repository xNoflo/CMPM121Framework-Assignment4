using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectScreenManager : MonoBehaviour
{
    public ClassSelectButton buttonPrefab;
    public Transform container;
    public GameObject classSelectUI;
    public ClassSelectButton[] classButtons;

    void Start()
    {
        if (classSelectUI != null)
        {
            classSelectUI.SetActive(false);
        }

        CreateClassButtons();
        EventBus.Instance.OnLevelSelected += HandleLevelSelected;
        EventBus.Instance.OnClassSelected += HandleClassSelected;
    }

    public void CreateClassButtons()
    {
        if (container == null)
        {
            Debug.LogError("ClassSelectScreenManager is missing its container reference.", this);
            return;
        }

        List<ClassSelectButton> availableButtons = GetAvailableButtons();
        if (availableButtons.Count == 0 && buttonPrefab == null)
        {
            Debug.LogError("ClassSelectScreenManager is missing both scene buttons and a button prefab reference.", this);
            return;
        }

        int index = 0;
        foreach (PlayerClass playerClass in GameManager.Instance.playerClasses.Values)
        {
            ClassSelectButton button = GetOrCreateButton(availableButtons, index);
            if (button == null)
            {
                Debug.LogError("Failed to create a class select button.", this);
                return;
            }

            button.gameObject.SetActive(true);
            button.SetButtonDetails(playerClass);

            Button uiButton = button.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.onClick.RemoveAllListeners();
                string className = playerClass.name;
                uiButton.onClick.AddListener(() => button.SelectClass(className));
            }

            index++;
        }

        for (int i = index; i < availableButtons.Count; i++)
        {
            if (availableButtons[i] != null)
            {
                availableButtons[i].gameObject.SetActive(false);
            }
        }

        classButtons = availableButtons.ToArray();
    }

    public void HandleLevelSelected(string levelName)
    {
        ShowClassSelection();
    }

    public void ShowClassSelection()
    {
        if (classSelectUI != null)
        {
            classSelectUI.SetActive(true);
        }
    }

    public void HandleClassSelected(string playerClass)
    {
        if (classSelectUI != null)
        {
            classSelectUI.SetActive(false);
        }

        GameManager.Instance.player.GetComponent<PlayerController>().SetClass(playerClass);
        EventBus.Instance.DoLevelStarted();
    }

    List<ClassSelectButton> GetAvailableButtons()
    {
        var buttons = new List<ClassSelectButton>();

        if (classButtons != null)
        {
            foreach (ClassSelectButton button in classButtons)
            {
                if (button != null && !buttons.Contains(button))
                {
                    buttons.Add(button);
                }
            }
        }

        foreach (ClassSelectButton button in container.GetComponentsInChildren<ClassSelectButton>(true))
        {
            if (button != null && !buttons.Contains(button))
            {
                buttons.Add(button);
            }
        }

        return buttons;
    }

    ClassSelectButton GetOrCreateButton(List<ClassSelectButton> buttons, int index)
    {
        if (index < buttons.Count)
        {
            return buttons[index];
        }

        if (buttonPrefab == null)
        {
            return null;
        }

        ClassSelectButton createdButton = Instantiate(buttonPrefab, container);
        buttons.Add(createdButton);
        return createdButton;
    }
}
