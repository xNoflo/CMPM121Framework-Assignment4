using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RewardScreenManager : MonoBehaviour
{
    public GameObject rewardUI;

    TextMeshProUGUI statsText, rewardText, actionButtonText, acceptButtonText;
    Image rewardIcon;
    Button actionButton, acceptButton;
    EnemySpawner spawner;
    GameManager.GameState configuredState;
    Spell rewardSpell;
    SpellBuilder spellBuilder;
    PlayerController player;
    SpellUIContainer spellUIContainer;

    void Start()
    {
        spawner = FindFirstObjectByType<EnemySpawner>();
        spellBuilder = new SpellBuilder();
        actionButton = rewardUI.GetComponentInChildren<Button>(true);
        actionButtonText = actionButton != null ? actionButton.GetComponentInChildren<TextMeshProUGUI>(true) : null;

        statsText = CreateText("WaveStatsText", new Vector2(0, 145), new Vector2(600, 70), 26, TextAlignmentOptions.Center);
        rewardIcon = CreateImage("RewardSpellIcon", new Vector2(-260, 45), new Vector2(64, 64), Color.white);
        rewardText = CreateText("RewardSpellText", new Vector2(70, 45), new Vector2(560, 135), 20, TextAlignmentOptions.Left);
        acceptButton = CreateButton("AcceptSpellButton", new Vector2(0, -105), new Vector2(180, 44), AcceptRewardSpell, out acceptButtonText);
        configuredState = GameManager.GameState.PREGAME;
        HideReward();
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
            MakeReward();
            SetActionButton("Next Wave", NextWave);
        }
        else
        {
            rewardSpell = null;
            HideReward();
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

    void MakeReward()
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

    void AcceptRewardSpell()
    {
        RefreshReferences();
        if (rewardSpell == null || player == null || player.spellcaster == null) return;

        if (player.spellcaster.AddSpell(rewardSpell))
        {
            rewardSpell = null;
            HideReward();
            ShowWaveStats();
            spellUIContainer?.Refresh();
            return;
        }

        spellUIContainer?.BeginRewardReplacement(rewardSpell);
        ShowReward("Choose Slot", false);
    }

    void NextWave()
    {
        rewardSpell = null;
        HideReward();
        spellUIContainer?.CancelRewardReplacement();
        spawner.NextWave();
    }

    public void ShowWaveStats()
    {
        statsText.gameObject.SetActive(true);
    }

    public void HideReward()
    {
        rewardText.gameObject.SetActive(false);
        rewardIcon.gameObject.SetActive(false);
        acceptButton.gameObject.SetActive(false);
    }

    void SetActionButton(string text, UnityEngine.Events.UnityAction action)
    {
        actionButtonText.text = text;
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
        Button button = image.gameObject.AddComponent<Button>();
        button.onClick.AddListener(action);
        label = new GameObject(name + "Text").AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(button.transform, false);
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.black;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return button;
    }

    void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
