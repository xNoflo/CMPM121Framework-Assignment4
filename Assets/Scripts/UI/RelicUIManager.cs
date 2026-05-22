using UnityEngine;

public class RelicUIManager : MonoBehaviour
{
    public GameObject relicUIPrefab;
    public PlayerController player;

    void Start()
    {
        if (player == null)
            player = GameManager.Instance.player != null ? GameManager.Instance.player.GetComponent<PlayerController>() : FindFirstObjectByType<PlayerController>();

        EventBus.Instance.OnRelicPickup += OnRelicPickup;
    }

    void OnDestroy()
    {
        EventBus.Instance.OnRelicPickup -= OnRelicPickup;
    }

    public void OnRelicPickup(Relic relic)
    {
        if (relicUIPrefab == null || player == null) return;

        GameObject relicUIObject = Instantiate(relicUIPrefab, transform);
        relicUIObject.transform.localPosition = new Vector3(-450 + 40 * (player.relics.Count - 1), 0, 0);

        RelicUI relicUI = relicUIObject.GetComponent<RelicUI>();
        if (relicUI == null) return;

        relicUI.player = player;
        relicUI.index = player.relics.Count - 1;
        relicUI.Refresh();
    }
}
