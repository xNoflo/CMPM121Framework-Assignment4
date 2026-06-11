using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class ClassSelectScreenManager : MonoBehaviour
{
    const float ClassButtonSpacing = 20f;

    public ClassSelectButton buttonPrefab;
    public Transform container;

    public GameObject classSelectUI;

    public ClassSelectButton[] classButtons;


    void Start()
    {
        classSelectUI.SetActive(false);

        CreateClassButtons();
        EventBus.Instance.OnLevelSelected += HandleLevelSelected;
        EventBus.Instance.OnClassSelected += HandleClassSelected;
    }

    public void CreateClassButtons()
    {
        foreach (Transform child in container) { Destroy(child.gameObject); }

        List<ClassSelectButton> createdButtons = new List<ClassSelectButton>();

        foreach (PlayerClass playerClass in GameManager.Instance.playerClasses.Values)
        {
            ClassSelectButton newButtonObj = Instantiate(buttonPrefab, container);

            newButtonObj.SetButtonDetails(playerClass);

            Button buttonObj = newButtonObj.GetComponent<Button>();

            buttonObj.onClick.AddListener(() => newButtonObj.SelectClass(playerClass.name));
            createdButtons.Add(newButtonObj);
        }

        float buttonWidth = buttonPrefab.GetComponent<RectTransform>().rect.width;
        float totalWidth = (createdButtons.Count * buttonWidth) + (Mathf.Max(0, createdButtons.Count - 1) * ClassButtonSpacing);
        float startX = -totalWidth / 2f + buttonWidth / 2f;

        for (int i = 0; i < createdButtons.Count; i++)
        {
            Transform buttonTransform = createdButtons[i].transform;
            Vector3 localPosition = buttonTransform.localPosition;
            buttonTransform.localPosition = new Vector3(startX + (i * (buttonWidth + ClassButtonSpacing)), localPosition.y, localPosition.z);
        }
    }

    public void HandleLevelSelected(string levelName)
    {
        ShowClassSelection();
    }

    public void ShowClassSelection()
    {
        classSelectUI.SetActive(true);
    }

    public void HandleClassSelected(string playerClass)
    {
        classSelectUI.SetActive(false);
        GameManager.Instance.player.GetComponent<PlayerController>().SetClass(playerClass);
        EventBus.Instance.DoLevelStarted();
    }
}
