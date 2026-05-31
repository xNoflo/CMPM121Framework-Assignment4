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
    public ClassSelectButton buttonPrefab;
    public Transform container;

    public GameObject classSelectUI;

    public ClassSelectButton[] classButtons;

    private string selectedLevel;

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

        int i = -1;

        foreach (PlayerClass playerClass in GameManager.Instance.playerClasses.Values)
        {
            ClassSelectButton newButtonObj = Instantiate(buttonPrefab, container);
            newButtonObj.transform.localPosition = new Vector3(345 * i, newButtonObj.transform.localPosition.y, newButtonObj.transform.localPosition.z);

            newButtonObj.SetButtonDetails(playerClass);

            Button buttonObj = newButtonObj.GetComponent<Button>();
            buttonObj.onClick.AddListener(() => newButtonObj.SelectClass(playerClass.name));

            i++;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void HandleLevelSelected(string levelName)
    {
        selectedLevel = levelName;

        ShowClassSelection();
    }

    public void ShowClassSelection()
    {
        var classes = GameManager.Instance.playerClasses.Values.ToList();

        for (int i = 0; i < classButtons.Length; i++)
        {
            if (i < classes.Count && classes[i] is not null)
            {
                classButtons[i].SetButtonDetails(classes[i]);
            }
        }

        classSelectUI.SetActive(true);
    }

    public void HandleClassSelected(string playerClass)
    {
        classSelectUI.SetActive(false);

        //EventBus.Instance.StartGame(selectedLevel, playerClass);
    }
}