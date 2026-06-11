using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellCaster
{
    public const int MAX_SPELLS = 4;
    public int mana, max_mana, mana_reg, spell_power, wave;
    public int activeSpellIndex;
    public Hittable.Team team;
    public PlayerController playerOwner;
    public Spell spell;
    public List<Spell> spells = new List<Spell>();

    public SpellCaster(int mana, int mana_reg, Hittable.Team team)
    {
        this.mana = this.max_mana = mana;
        this.mana_reg = mana_reg;
        this.team = team;
        wave = 1;
        spell = new SpellBuilder().Build(this);
        spells.Add(spell);
        activeSpellIndex = 0;
    }

    public IEnumerator ManaRegeneration()
    {
        while (true)
        {
            mana = Mathf.Min(mana + mana_reg, max_mana);
            yield return new WaitForSeconds(1);
        }
    }

    public void SetWaveStats(int maxMana, int manaRegeneration, int spellPower, int waveNumber)
    {
        float manaPercent = max_mana > 0 ? mana * 1.0f / max_mana : 1f;
        max_mana = Mathf.Max(0, maxMana);
        mana = Mathf.Clamp(Mathf.RoundToInt(manaPercent * max_mana), 0, max_mana);
        mana_reg = Mathf.Max(0, manaRegeneration);
        spell_power = Mathf.Max(0, spellPower);
        wave = Mathf.Max(1, waveNumber);
    }

    public IEnumerator Cast(Vector3 where, Vector3 target)
    {
        spell = GetActiveSpell();
        if (spell != null)
        {
            int manaCost = spell.GetManaCost();
            int currentSpellPower = GetCurrentSpellPower();

            if (mana < manaCost || !spell.IsReady())
            {
                yield break;
            }

            mana -= manaCost;
            SoundManager.PlayShot();
            EventBus.Instance.DoSpellCast(spell);
            yield return spell.Cast(where, target, team, currentSpellPower, wave);
        }
    }

    public int GetCurrentSpellPower()
    {
        int relicBonus = playerOwner != null ? playerOwner.GetRelicSpellPowerBonus() : 0;
        return Mathf.Max(0, spell_power + relicBonus);
    }

    public bool AddSpell(Spell newSpell)
    {
        if (newSpell == null || spells.Count >= MAX_SPELLS) return false;
        spells.Add(newSpell);
        spell = GetActiveSpell();
        return true;
    }

    public bool ReplaceSpell(int index, Spell newSpell)
    {
        if (newSpell == null || index < 0 || index >= MAX_SPELLS) return false;
        if (index < spells.Count) spells[index] = newSpell;
        else if (spells.Count < MAX_SPELLS) spells.Add(newSpell);
        else return false;
        activeSpellIndex = Mathf.Clamp(activeSpellIndex, 0, spells.Count - 1);
        spell = GetActiveSpell();
        return true;
    }

    public bool DropSpell(int index)
    {
        if (index < 0 || index >= spells.Count) return false;
        spells.RemoveAt(index);
        activeSpellIndex = spells.Count > 0 ? Mathf.Clamp(activeSpellIndex, 0, spells.Count - 1) : 0;
        spell = GetActiveSpell();
        return true;
    }

    public bool SelectSpell(int index)
    {
        if (index < 0 || index >= spells.Count) return false;
        activeSpellIndex = index;
        spell = GetActiveSpell();
        return true;
    }

    public Spell GetActiveSpell()
    {
        return spells.Count > 0 ? spells[Mathf.Clamp(activeSpellIndex, 0, spells.Count - 1)] : null;
    }
}
