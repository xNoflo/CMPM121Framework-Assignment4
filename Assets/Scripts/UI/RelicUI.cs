using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RelicUI : MonoBehaviour
{
    public PlayerController player;
    public int index;

    public Image icon;
    public GameObject highlight;
    public TextMeshProUGUI label;

    void Start()
    {
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
    }
}
