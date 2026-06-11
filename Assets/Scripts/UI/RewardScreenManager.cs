using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class RewardScreenManager : MonoBehaviour
{
    const float SPELL_SECTION_Y = 82f;
    const float RELIC_SECTION_Y = -62f;

    public GameObject rewardUI;

    TextMeshProUGUI statsText, rewardText, actionButtonText, acceptButtonText;
    Image rewardIcon;
    Button actionButton, acceptButton;
    EnemySpawner spawner;
    GameManager.GameState configuredState;
    Spell rewardSpell;
    Relic rewardRelic;
    List<Relic> relicChoices = new List<Relic>();
    Button[] relicChoiceButtons;
    TextMeshProUGUI[] relicChoiceTexts;
    Image[] relicChoiceIcons;
    SpellBuilder spellBuilder;
    PlayerController player;
    SpellUIContainer spellUIContainer;

    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        spellBuilder = new SpellBuilder();
        actionButton = rewardUI.GetComponentInChildren<Button>(true);
        actionButtonText = actionButton != null ? actionButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        statsText = CreateText("WaveStatsText", new Vector2(0, 160), new Vector2(600, 60), 24, TextAlignmentOptions.Center);
        rewardIcon = CreateImage("RewardSpellIcon", new Vector2(-245, SPELL_SECTION_Y + 6), new Vector2(56, 56), Color.white);
        rewardText = CreateText("RewardSpellText", new Vector2(35, SPELL_SECTION_Y), new Vector2(500, 92), 16, TextAlignmentOptions.Left);
        rewardText.enableAutoSizing = true;
        rewardText.fontSizeMin = 12;
        rewardText.fontSizeMax = 16;
        rewardText.textWrappingMode = TextWrappingModes.Normal;
        acceptButton = CreateButton("AcceptSpellButton", new Vector2(0, 0), new Vector2(170, 38), AcceptRewardSpell, out acceptButtonText);
        CreateRelicChoiceUI();
        configuredState = GameManager.GameState.PREGAME;
        HideReward();
        HideRelicChoices();
        statsText.gameObject.SetActive(false);
    }

    void Update()
    {
        GameManager.GameState state = GameManager.Instance.state;
        bool showingPanel = state == GameManager.GameState.WAVEEND || state == GameManager.GameState.GAMEOVER || state == GameManager.GameState.VICTORY;
        rewardUI.SetActive(showingPanel);

        if (showingPanel) ConfigureForState(state);
        else
        {
            configuredState = state;
            statsText.gameObject.SetActive(false);
            HideReward();
            HideRelicChoices();
            spellUIContainer?.CancelRewardReplacement();
        }
    }

    void ConfigureForState(GameManager.GameState state)
    {
        if (configuredState == state) return;
        configuredState = state;
        statsText.text = MessageFor(state);
        statsText.gameObject.SetActive(state != GameManager.GameState.WAVEEND);
        actionButton.onClick.RemoveAllListeners();

        if (state == GameManager.GameState.WAVEEND)
        {
            RefreshReferences();
            rewardSpell = null;
            rewardRelic = null;
            HideReward();
            HideRelicChoices();

            MakeSpellReward();

            if (ShouldOfferRelicReward())
            {
                MakeRelicReward();
            }

            SetActionButton("Next Wave", NextWave);
        }
        else
        {
            rewardSpell = null;
            rewardRelic = null;
            HideReward();
            HideRelicChoices();
            SetActionButton("Return to Start", spawner.ReturnToStart);
        }
    }

    string MessageFor(GameManager.GameState state)
    {
        string defeated = "\nEnemies defeated this wave: " + GameManager.Instance.enemiesDefeatedThisWave;
        if (state == GameManager.GameState.GAMEOVER) return "You died!" + defeated;
        if (state == GameManager.GameState.VICTORY) return "Difficulty completed!" + defeated;
        return "Wave Complete!" + defeated;
    }

    bool ShouldOfferRelicReward()
    {
        if (spawner == null || player == null) return false;
        if (spawner.CurrentWave <= 0 || spawner.CurrentWave % 3 != 0) return false;
        return GetAvailableRelics().Count > 0;
    }

    void MakeSpellReward()
    {
        RefreshReferences();
        if (player == null || player.spellcaster == null) return;
        rewardSpell = spellBuilder.BuildRandomSpell(player.spellcaster);
        rewardText.text = rewardSpell.GetName()
            + "\n" + rewardSpell.GetDescription()
            + "\nMana: " + rewardSpell.GetManaCost()
            + "  Damage: " + rewardSpell.GetDamage()
            + "  Cooldown: " + rewardSpell.GetCooldown().ToString("0.0");
        GameManager.Instance.spellIconManager.PlaceSprite(rewardSpell.GetIcon(), rewardIcon);
        ShowReward("Accept Spell", true);
    }

    void MakeRelicReward()
    {
        RefreshReferences();
        rewardRelic = null;
        relicChoices = PickRelicChoices(3);

        statsText.text = "Choose one relic" + "\nEnemies defeated this wave: " + GameManager.Instance.enemiesDefeatedThisWave;
        statsText.gameObject.SetActive(true);

        for (int i = 0; i < relicChoiceButtons.Length; i++)
        {
            bool hasChoice = i < relicChoices.Count;
            relicChoiceButtons[i].gameObject.SetActive(hasChoice);
            relicChoiceIcons[i].gameObject.SetActive(hasChoice);
            relicChoiceTexts[i].gameObject.SetActive(hasChoice);

            if (!hasChoice) continue;

            Relic relic = relicChoices[i];
            relicChoiceTexts[i].text = relic.name + "\n" + relic.GetLabel();

            if (GameManager.Instance.relicIconManager != null)
                GameManager.Instance.relicIconManager.PlaceSprite(relic.sprite, relicChoiceIcons[i]);
        }
    }

    List<Relic> GetAvailableRelics()
    {
        List<Relic> allRelics = GameManager.Instance.relic_definitions;
        if (allRelics == null) return new List<Relic>();

        HashSet<string> ownedNames = new HashSet<string>();

        if (player != null && player.relics != null)
        {
            foreach (Relic ownedRelic in player.relics)
            {
                if (ownedRelic != null && !string.IsNullOrWhiteSpace(ownedRelic.name))
                    ownedNames.Add(ownedRelic.name);
            }
        }

        return allRelics
            .Where(relic => relic != null && !string.IsNullOrWhiteSpace(relic.name) && !ownedNames.Contains(relic.name))
            .ToList();
    }

    List<Relic> PickRelicChoices(int count)
    {
        List<Relic> available = GetAvailableRelics();
        List<Relic> choices = new List<Relic>();

        while (available.Count > 0 && choices.Count < count)
        {
            int index = Random.Range(0, available.Count);
            choices.Add(available[index]);
            available.RemoveAt(index);
        }

        return choices;
    }

    void AcceptRewardSpell()
    {
        RefreshReferences();
        if (rewardSpell == null || player == null || player.spellcaster == null) return;

        if (player.spellcaster.AddSpell(rewardSpell))
        {
            SoundManager.PlayReward();
            rewardSpell = null;
            HideReward();
            UpdateWaveEndDisplay();
            spellUIContainer?.Refresh();
            return;
        }

        spellUIContainer?.BeginRewardReplacement(rewardSpell);
        ShowReward("Choose Slot", false);
    }

    void AcceptRewardRelic(int index)
    {
        RefreshReferences();
        if (player == null || index < 0 || index >= relicChoices.Count) return;

        rewardRelic = relicChoices[index];

        if (player.AddRelic(rewardRelic))
        {
            SoundManager.PlayReward();
            rewardRelic = null;
            HideRelicChoices();
            relicChoices.Clear();
            UpdateWaveEndDisplay();
        }
    }

    void NextWave()
    {
        rewardSpell = null;
        rewardRelic = null;
        HideReward();
        HideRelicChoices();
        spellUIContainer?.CancelRewardReplacement();
        spawner.NextWave();
    }

    public void ShowWaveStats()
    {
        statsText.text = MessageFor(GameManager.GameState.WAVEEND);
        statsText.gameObject.SetActive(true);
    }

    void UpdateWaveEndDisplay()
    {
        bool hasSpellReward = rewardSpell != null;
        bool hasRelicReward = relicChoices != null && relicChoices.Count > 0;

        if (hasSpellReward || hasRelicReward)
        {
            string message = "Wave Complete!";

            if (hasSpellReward && hasRelicReward) message = "Choose a spell and a relic";
            else if (hasSpellReward) message = "Choose a spell";
            else if (hasRelicReward) message = "Choose one relic";

            statsText.text = message + "\nEnemies defeated this wave: " + GameManager.Instance.enemiesDefeatedThisWave;
            statsText.gameObject.SetActive(true);
            return;
        }

        ShowWaveStats();
    }

    public void HideReward()
    {
        rewardText.gameObject.SetActive(false);
        rewardIcon.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
    }

    void HideRelicChoices()
    {
        if (relicChoiceButtons == null) return;

        foreach (Button button in relicChoiceButtons)
            button.gameObject.SetActive(false);

        foreach (Image icon in relicChoiceIcons)
            icon.gameObject.SetActive(false);

        foreach (TextMeshProUGUI text in relicChoiceTexts)
            text.gameObject.SetActive(false);
    }

    void SetActionButton(string text, UnityEngine.Events.UnityAction action)
    {
        actionButtonText.text = text;
        RectTransform rect = actionButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = text == "Next Wave" ? new Vector2(0, -190) : new Vector2(0, -105);
        }
        actionButton.onClick.AddListener(action);
    }

    void ShowReward(string buttonText, bool canAccept)
    {
        rewardText.gameObject.SetActive(true);
        rewardIcon.gameObject.SetActive(true);
        acceptButton.gameObject.SetActive(true);
        acceptButton.interactable = canAccept;
        acceptButtonText.text = buttonText;
    }

    void RefreshReferences()
    {
        if (player == null)
            player = GameManager.Instance.player != null ? GameManager.Instance.player.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();
        if (spellUIContainer == null)
            spellUIContainer = FindFirstObjectByType<SpellUIContainer>();
    }

    void CreateRelicChoiceUI()
    {
        relicChoiceButtons = new Button[3];
        relicChoiceTexts = new TextMeshProUGUI[3];
        relicChoiceIcons = new Image[3];

        for (int i = 0; i < 3; i++)
        {
            int choiceIndex = i;
            float x = -230 + i * 230;
            relicChoiceIcons[i] = CreateImage("RelicChoiceIcon" + i, new Vector2(x, RELIC_SECTION_Y + 8), new Vector2(44, 44), Color.white);
            relicChoiceTexts[i] = CreateText("RelicChoiceText" + i, new Vector2(x, RELIC_SECTION_Y - 38), new Vector2(180, 82), 13, TextAlignmentOptions.Center);
            relicChoiceTexts[i].enableAutoSizing = true;
            relicChoiceTexts[i].fontSizeMin = 10;
            relicChoiceTexts[i].fontSizeMax = 13;
            relicChoiceTexts[i].textWrappingMode = TextWrappingModes.Normal;
            relicChoiceButtons[i] = CreateButton("RelicChoiceButton" + i, new Vector2(x, RELIC_SECTION_Y - 92), new Vector2(140, 34), () => AcceptRewardRelic(choiceIndex), out TextMeshProUGUI buttonText);
            buttonText.text = "Take Relic";
        }
    }

    TextMeshProUGUI CreateText(string name, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = new GameObject(name).AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(rewardUI.transform, false);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.black;
        SetRect(text.GetComponent<RectTransform>(), position, size);
        return text;
    }

    Image CreateImage(string name, Vector2 position, Vector2 size, Color color)
    {
        Image image = new GameObject(name).AddComponent<Image>();
        image.transform.SetParent(rewardUI.transform, false);
        image.color = color;
        SetRect(image.GetComponent<RectTransform>(), position, size);
        return image;
    }

    Button CreateButton(string name, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action, out TextMeshProUGUI label)
    {
        Image image = CreateImage(name, position, size, new Color(0.66f, 0.38f, 0.17f, 1));
        ApplyButtonVisualStyle(image);
        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        label = new GameObject(name + "Text").AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(button.transform, false);
        label.fontSize = 18;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11;
        label.fontSizeMax = 18;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return button;
    }

    void ApplyButtonVisualStyle(Image image)
    {
        if (image == null || actionButton == null)
        {
            return;
        }

        Image templateImage = actionButton.GetComponent<Image>();
        if (templateImage == null)
        {
            return;
        }

        image.sprite = templateImage.sprite;
        image.type = templateImage.type;
        image.material = templateImage.material;
        image.pixelsPerUnitMultiplier = templateImage.pixelsPerUnitMultiplier;
        image.preserveAspect = templateImage.preserveAspect;
        image.color = templateImage.color;
    }

    void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
