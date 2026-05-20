using UnityEngine;

public class SpellUIContainer : MonoBehaviour
{
    public GameObject[] spellUIs;
    public PlayerController player;
    public RewardScreenManager rewardScreenManager;
    Spell rewardSpellWaitingForReplacement;

    void Start()
    {
        for (int i = 0; i < spellUIs.Length; i++)
        {
            spellUIs[i].SetActive(false);
            SpellUI ui = spellUIs[i].GetComponent<SpellUI>();
            if (ui != null) ui.SetIndex(this, i);
        }
    }

    public void Refresh()
    {
        if (player == null || player.spellcaster == null) return;
        for (int i = 0; i < spellUIs.Length; i++)
        {
            SpellUI ui = spellUIs[i].GetComponent<SpellUI>();
            if (ui != null)
            {
                ui.SetSpell(i < player.spellcaster.spells.Count ? player.spellcaster.spells[i] : null, rewardSpellWaitingForReplacement != null);
                ui.SetHighlighted(i == player.spellcaster.activeSpellIndex);
            }
        }
    }

    public void BeginRewardReplacement(Spell rewardSpell)
    {
        rewardSpellWaitingForReplacement = rewardSpell;
        Refresh();
    }

    public void CancelRewardReplacement()
    {
        rewardSpellWaitingForReplacement = null;
        Refresh();
    }

    public void DropSpell(int index)
    {
        if (player == null || player.spellcaster == null) return;
        if (rewardSpellWaitingForReplacement != null)
        {
            player.spellcaster.ReplaceSpell(index, rewardSpellWaitingForReplacement);
            rewardSpellWaitingForReplacement = null;
            if (rewardScreenManager == null) rewardScreenManager = FindFirstObjectByType<RewardScreenManager>();
            rewardScreenManager?.HideReward();
            rewardScreenManager?.ShowWaveStats();
        }
        else player.spellcaster.DropSpell(index);
        Refresh();
    }
}
