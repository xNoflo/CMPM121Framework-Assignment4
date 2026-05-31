using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectScreenManager : MonoBehaviour
{
    public GameObject classSelectUI;

    public ClassSelectButton[] classButtons;

    private string selectedLevel;

    void Start()
    {
        classSelectUI.SetActive(false);

        //EventBus.Instance.OnLevelSelected += HandleLevelSelected;
        //EventBus.Instance.OnClassSelected += HandleClassSelected;
    }

    //public void HandleLevelSelected(string levelName)
    //{
    //    selectedLevel = levelName;

    //    ShowClassSelection();
    //}

    //public void ShowClassSelection()
    //{
    //    var classes = GameManager.Instance.playerClasses.Values.ToList();

    //    for (int i = 0; i < classButtons.Length; i++)
    //    {
    //        if (i < classes.Count && classes[i] is not null)
    //        {
    //            classButtons[i].SetButtonDetails(classes[i]);
    //        }
    //    }

    //    classSelectUI.SetActive(true);
    //}

    //public void HandleClassSelected(string playerClass)
    //{
    //    classSelectUI.SetActive(false);

    //    EventBus.Instance.StartGame(selectedLevel, playerClass);
    //}
}