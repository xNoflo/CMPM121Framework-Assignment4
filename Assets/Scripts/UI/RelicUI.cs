using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class RelicUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public PlayerController player;
    public int index;

    public Image icon;
    public GameObject highlight;
    public TextMeshProUGUI label;

    Image tooltipBackground;
    bool isHovered;

    void Start()
    {
        ConfigureTooltip();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (player == null || index < 0 || index >= player.relics.Count) return;

        Relic relic = player.relics[index];
        if (relic == null) return;

        if (icon != null && GameManager.Instance.relicIconManager != null)
            GameManager.Instance.relicIconManager.PlaceSprite(relic.sprite, icon);

        if (label != null)
            label.text = relic.GetLabel();

        if (highlight != null)
            highlight.SetActive(relic.IsActive());

        SetTooltipVisible(isHovered);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        SetTooltipVisible(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        SetTooltipVisible(false);
    }

    void ConfigureTooltip()
    {
        if (label == null)
        {
            return;
        }

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0, -28);
        labelRect.sizeDelta = new Vector2(170, 84);

        label.fontSize = 12;
        label.enableAutoSizing = true;
        label.fontSizeMin = 9;
        label.fontSizeMax = 12;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.margin = new Vector4(8, 6, 8, 6);
        label.raycastTarget = false;

        if (tooltipBackground == null)
        {
            GameObject backgroundObject = new GameObject("TooltipBackground");
            backgroundObject.transform.SetParent(transform, false);
            backgroundObject.transform.SetSiblingIndex(label.transform.GetSiblingIndex());

            tooltipBackground = backgroundObject.AddComponent<Image>();
            tooltipBackground.color = new Color(0.95f, 0.91f, 0.78f, 0.96f);
            tooltipBackground.raycastTarget = false;

            RectTransform backgroundRect = tooltipBackground.rectTransform;
            backgroundRect.anchorMin = backgroundRect.anchorMax = backgroundRect.pivot = new Vector2(0.5f, 1f);
            backgroundRect.anchoredPosition = labelRect.anchoredPosition;
            backgroundRect.sizeDelta = labelRect.sizeDelta;
        }

        SetTooltipVisible(false);
    }

    void SetTooltipVisible(bool visible)
    {
        if (label != null)
        {
            label.gameObject.SetActive(visible);
        }

        if (tooltipBackground != null)
        {
            tooltipBackground.gameObject.SetActive(visible);
        }
    }
}
